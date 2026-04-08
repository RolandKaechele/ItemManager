using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>ItemManager</b> handles world-space item pickups: definitions, per-map spawning/despawning,
    /// one-time-pickup tracking, and optional inventory, save, and event integration.
    ///
    /// <para><b>Responsibilities:</b>
    /// <list type="number">
    ///   <item>Store <see cref="ItemWorldDefinition"/> entries authored in the Inspector and/or loaded from JSON.</item>
    ///   <item>Spawn item pickup GameObjects for the current map (filtered by <c>mapIds</c>).</item>
    ///   <item>Track live pickup instances by unique instance id.</item>
    ///   <item>Handle collection events from <see cref="ItemPickup"/> components.</item>
    ///   <item>Skip one-time items that are already collected (via in-memory set or SaveManager bridge).</item>
    ///   <item>Auto-generate prefabs from JSON via the Editor tool.</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Modding / JSON:</b> Enable <c>loadFromJson</c> and place an
    /// <c>items.json</c> in <c>StreamingAssets/</c>.
    /// JSON entries are <b>merged by id</b>: JSON overrides Inspector entries.</para>
    ///
    /// <para><b>Optional integration defines:</b>
    /// <list type="bullet">
    ///   <item><c>ITEMMANAGER_MLF</c>  — MapLoaderFramework: spawn/clear pickups on map load.</item>
    ///   <item><c>ITEMMANAGER_IM</c>   — InventoryManager: grant items to inventory on collect.</item>
    ///   <item><c>ITEMMANAGER_EM</c>   — EventManager: fire <c>item.spawned</c>, <c>item.collected</c> events.</item>
    ///   <item><c>ITEMMANAGER_SM</c>   — SaveManager: persist one-time-pickup collected flags.</item>
    ///   <item><c>ITEMMANAGER_STM</c>  — StateManager: pause spawning during non-Gameplay states.</item>
    ///   <item><c>ITEMMANAGER_CSM</c>  — CutsceneManager: disable interaction during cutscenes.</item>
    ///   <item><c>ITEMMANAGER_DOTWEEN</c> — DOTween Pro: float/scale tween on spawn and collect.</item>
    ///   <item><c>ITEMMANAGER_REALTOON</c> — RealToon: apply anime/toon material on spawned pickups.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("ItemManager/Item Manager")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class ItemManager : SerializedMonoBehaviour
#else
    public class ItemManager : MonoBehaviour
#endif
    {
        // ─── Singleton ───────────────────────────────────────────────────────────

        /// <summary>Convenience singleton. Null when not present in the scene.</summary>
        public static ItemManager Instance { get; private set; }

        // ─── Inspector ───────────────────────────────────────────────────────────

        [Header("Definitions")]
        [Tooltip("Item definitions. JSON entries are merged on top by id.")]
        [SerializeField] private List<ItemWorldDefinition> definitions = new List<ItemWorldDefinition>();

        [Header("Spawn Settings")]
        [Tooltip("Parent transform for spawned pickups. Leave null to use this GameObject.")]
        [SerializeField] private Transform pickupParent;

        [Tooltip("Layer mask for the spawned pickup colliders.")]
        [SerializeField] private LayerMask pickupLayer = 0;

        [Header("Modding / JSON")]
        [Tooltip("Merge item definitions from StreamingAssets/<jsonPath> at startup.")]
        [SerializeField] private bool loadFromJson = false;

        [Tooltip("Path relative to StreamingAssets/ (e.g. 'items.json').")]
        [SerializeField] private string jsonPath = "items.json";

        [Header("Debug")]
        [Tooltip("Log all spawn, collect, and clear events to the Unity Console.")]
        [SerializeField] private bool verboseLogging = false;

        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>Fired when a pickup is spawned. Parameters: definitionId, instanceId, gameObject.</summary>
        public event Action<string, string, GameObject> OnItemSpawned;

        /// <summary>Fired when a pickup is collected. Parameters: definitionId, instanceId, gameObject.</summary>
        public event Action<string, string, GameObject> OnItemCollected;

        // ─── Delegate hooks ──────────────────────────────────────────────────────

        /// <summary>
        /// Optional callback invoked for each spawned pickup (bridges use this for DOTween / RealToon setup).
        /// Signature: (definitionId, instanceId, gameObject).
        /// </summary>
        public Action<string, string, GameObject> OnSpawnedCallback;

        /// <summary>
        /// Optional callback invoked just before a pickup is destroyed/despawned after collection.
        /// Signature: (definitionId, instanceId, gameObject).
        /// </summary>
        public Action<string, string, GameObject> OnCollectedCallback;

        /// <summary>
        /// Optional delegate to check whether a one-time pickup has been collected in a previous session.
        /// If null, ItemManager uses its internal in-memory set only.
        /// Signature: (saveFlag) → bool.
        /// </summary>
        public Func<string, bool> IsCollectedPersistenceCheck;

        /// <summary>
        /// Optional delegate to persist a collected flag.
        /// Signature: (saveFlag).
        /// </summary>
        public Action<string> PersistCollected;

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly Dictionary<string, ItemWorldDefinition> _defIndex =
            new Dictionary<string, ItemWorldDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ItemInstanceRecord> _live =
            new Dictionary<string, ItemInstanceRecord>(StringComparer.OrdinalIgnoreCase);

        // In-memory set of collected one-time pickup save flags (session-only when no SaveManager).
        private readonly HashSet<string> _collectedFlags =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ItemSpawnPointData> _spawnPoints =
            new Dictionary<string, ItemSpawnPointData>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, GameObject> _prefabCache =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        private bool _interactionPaused;
        private int  _instanceCounter;

        /// <summary>True while item interaction (collection) is paused.</summary>
        public bool IsInteractionPaused => _interactionPaused;

        /// <summary>Number of currently live pickup instances.</summary>
        public int LiveCount => _live.Count;

        // ─── Unity lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (pickupParent == null) pickupParent = transform;

            BuildIndex();
            if (loadFromJson) LoadJsonDefinitions();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─── Spawn point registration ─────────────────────────────────────────────

        /// <summary>Register a spawn point. Called automatically by <see cref="ItemSpawnPoint"/> on Awake.</summary>
        public void RegisterSpawnPoint(ItemSpawnPointData point)
        {
            if (point == null || string.IsNullOrEmpty(point.id)) return;
            _spawnPoints[point.id] = point;
        }

        /// <summary>Unregister a spawn point by id.</summary>
        public void UnregisterSpawnPoint(string id) => _spawnPoints.Remove(id);

        // ─── Spawn API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Spawn all item definitions whose <c>mapIds</c> include <paramref name="mapId"/>,
        /// or whose <c>mapIds</c> list is empty (global items).
        /// One-time pickups that are already collected are skipped.
        /// </summary>
        public void SpawnItemsForMap(string mapId)
        {
            foreach (var def in _defIndex.Values)
            {
                bool matchesMap = def.mapIds == null || def.mapIds.Count == 0
                    || def.mapIds.Contains(mapId);

                if (!matchesMap) continue;
                SpawnItem(def.id);
            }
        }

        /// <summary>
        /// Spawn the pickup for a single definition by id.
        /// Has no effect if the item is already live or has been collected (one-time).
        /// </summary>
        public string SpawnItem(string definitionId)
        {
            if (!_defIndex.TryGetValue(definitionId, out var def))
            {
                Debug.LogWarning($"[ItemManager] No definition found for id '{definitionId}'.");
                return null;
            }

            // Skip if already collected (one-time)
            if (def.oneTimePickup && IsCollected(def))
            {
                if (verboseLogging)
                    Debug.Log($"[ItemManager] Skipping '{definitionId}' — already collected.");
                return null;
            }

            // Skip if already live
            foreach (var rec in _live.Values)
                if (rec.definitionId == definitionId) return rec.instanceId;

            // Instantiate prefab
            var go = InstantiatePrefab(def);
            if (go == null) return null;

            // Position
            if (!string.IsNullOrEmpty(def.spawnPointId)
                && _spawnPoints.TryGetValue(def.spawnPointId, out var sp))
            {
                go.transform.SetPositionAndRotation(sp.position, sp.rotation);
            }
            else
            {
                go.transform.SetPositionAndRotation(
                    def.worldPosition,
                    Quaternion.Euler(def.worldRotation));
            }

            go.transform.SetParent(pickupParent, true);

            // Tag
            if (!string.IsNullOrEmpty(def.pickupTag))
                go.tag = def.pickupTag;

            // Instance id
            string instanceId = GenerateInstanceId(definitionId);

            // Wire up ItemPickup component
            var pickup = go.GetComponent<ItemPickup>() ?? go.AddComponent<ItemPickup>();
            pickup.Definition  = def;
            pickup.InstanceId  = instanceId;
            pickup.ResetPickup();
            pickup.OnCollected += HandlePickupCollected;

            go.SetActive(true);

            var record = new ItemInstanceRecord
            {
                definitionId = definitionId,
                instanceId   = instanceId,
                gameObject   = go
            };
            _live[instanceId] = record;

            if (verboseLogging)
                Debug.Log($"[ItemManager] Spawned '{definitionId}' (instance: {instanceId}).");

            OnItemSpawned?.Invoke(definitionId, instanceId, go);
            OnSpawnedCallback?.Invoke(definitionId, instanceId, go);

            return instanceId;
        }

        // ─── Collect API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Programmatically collect a live pickup by instance id.
        /// Triggers the full collection flow (events, inventory, save, removal).
        /// </summary>
        public void CollectItem(string instanceId)
        {
            if (!_live.TryGetValue(instanceId, out var record)) return;
            var pickup = record.gameObject?.GetComponent<ItemPickup>();
            pickup?.Collect();
        }

        /// <summary>
        /// Programmatically collect all live pickups for a given definition id.
        /// </summary>
        public void CollectAllItems(string definitionId)
        {
            var keys = new List<string>(_live.Keys);
            foreach (var k in keys)
                if (_live.TryGetValue(k, out var r) && r.definitionId == definitionId)
                    CollectItem(k);
        }

        // ─── Clear API ────────────────────────────────────────────────────────────

        /// <summary>Destroy all currently live pickup instances (used on map change).</summary>
        public void ClearAllPickups()
        {
            foreach (var rec in _live.Values)
            {
                if (rec.gameObject == null) continue;
                var pickup = rec.gameObject.GetComponent<ItemPickup>();
                if (pickup != null) pickup.OnCollected -= HandlePickupCollected;
                Destroy(rec.gameObject);
            }
            _live.Clear();

            if (verboseLogging) Debug.Log("[ItemManager] Cleared all live pickups.");
        }

        // ─── Pause / Resume interaction ───────────────────────────────────────────

        /// <summary>
        /// Pause item interaction. Collected events from ItemPickup will be ignored while paused.
        /// </summary>
        public void PauseInteraction() => _interactionPaused = true;

        /// <summary>Resume item interaction.</summary>
        public void ResumeInteraction() => _interactionPaused = false;

        // ─── Query API ────────────────────────────────────────────────────────────

        /// <summary>Returns the definition for the given id, or null.</summary>
        public ItemWorldDefinition GetDefinition(string id) =>
            _defIndex.TryGetValue(id, out var d) ? d : null;

        /// <summary>Returns all registered definition ids.</summary>
        public IEnumerable<string> GetAllDefinitionIds() => _defIndex.Keys;

        /// <summary>Returns the live instance record for the given instance id, or null.</summary>
        public ItemInstanceRecord GetLiveRecord(string instanceId) =>
            _live.TryGetValue(instanceId, out var r) ? r : null;

        // ─── Internal ────────────────────────────────────────────────────────────

        private void BuildIndex()
        {
            _defIndex.Clear();
            foreach (var d in definitions)
                if (d != null && !string.IsNullOrEmpty(d.id))
                    _defIndex[d.id] = d;
        }

        private void LoadJsonDefinitions()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, jsonPath);
            if (!File.Exists(fullPath))
            {
                if (verboseLogging)
                    Debug.Log($"[ItemManager] JSON not found at '{fullPath}' — skipping.");
                return;
            }

            string json = File.ReadAllText(fullPath);
            ItemJsonWrapper wrapper;
            try { wrapper = JsonUtility.FromJson<ItemJsonWrapper>(json); }
            catch (Exception ex)
            {
                Debug.LogError($"[ItemManager] Failed to parse '{fullPath}': {ex.Message}");
                return;
            }

            if (wrapper?.items == null) return;

            foreach (var entry in wrapper.items)
            {
                var def = entry.ToDefinition();
                _defIndex[def.id] = def;   // JSON overrides Inspector entry with same id
                if (verboseLogging)
                    Debug.Log($"[ItemManager] JSON merged definition '{def.id}'.");
            }
        }

        private GameObject InstantiatePrefab(ItemWorldDefinition def)
        {
            if (string.IsNullOrEmpty(def.prefabResource))
            {
                Debug.LogWarning($"[ItemManager] Definition '{def.id}' has no prefabResource — using primitive.");
                return CreateFallbackPickup(def);
            }

            if (!_prefabCache.TryGetValue(def.prefabResource, out var prefab))
            {
                prefab = Resources.Load<GameObject>(def.prefabResource);
                if (prefab != null)
                    _prefabCache[def.prefabResource] = prefab;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[ItemManager] Prefab not found at Resources/'{def.prefabResource}' — using primitive.");
                return CreateFallbackPickup(def);
            }

            return Instantiate(prefab);
        }

        private static GameObject CreateFallbackPickup(ItemWorldDefinition def)
        {
            var go         = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name        = $"ItemPickup_{def.id}";
            go.transform.localScale = Vector3.one * 0.4f;

            // Use sphere collider as trigger for auto-collect
            var col         = go.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;

            return go;
        }

        private void HandlePickupCollected(string definitionId, string instanceId, GameObject go)
        {
            if (_interactionPaused)
            {
                if (verboseLogging)
                    Debug.Log($"[ItemManager] Interaction paused — ignoring collect for '{definitionId}'.");
                return;
            }

            if (!_live.ContainsKey(instanceId)) return;

            if (verboseLogging)
                Debug.Log($"[ItemManager] Collected '{definitionId}' (instance: {instanceId}).");

            // Persist one-time flag
            if (_defIndex.TryGetValue(definitionId, out var def) && def.oneTimePickup)
            {
                string flag = string.IsNullOrEmpty(def.saveFlag)
                    ? $"item_collected_{def.id}"
                    : def.saveFlag;

                _collectedFlags.Add(flag);
                PersistCollected?.Invoke(flag);
            }

            OnItemCollected?.Invoke(definitionId, instanceId, go);
            OnCollectedCallback?.Invoke(definitionId, instanceId, go);

            _live.Remove(instanceId);

            // Unsubscribe before destroying
            var pickup = go?.GetComponent<ItemPickup>();
            if (pickup != null) pickup.OnCollected -= HandlePickupCollected;

            if (go != null) Destroy(go);
        }

        private bool IsCollected(ItemWorldDefinition def)
        {
            string flag = string.IsNullOrEmpty(def.saveFlag)
                ? $"item_collected_{def.id}"
                : def.saveFlag;

            if (_collectedFlags.Contains(flag)) return true;
            if (IsCollectedPersistenceCheck != null) return IsCollectedPersistenceCheck(flag);
            return false;
        }

        private string GenerateInstanceId(string definitionId)
            => $"{definitionId}_{++_instanceCounter}";
    }
}
