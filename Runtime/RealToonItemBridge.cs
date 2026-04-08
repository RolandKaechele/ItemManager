#if ITEMMANAGER_REALTOON
using UnityEngine;

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>RealToonItemBridge</b> applies a RealToon anime/toon material to each spawned
    /// item pickup whenever <c>ITEMMANAGER_REALTOON</c> is defined.
    /// Requires <b>RealToon Pro</b> (URP / HDRP / Built-In).
    /// </summary>
    [AddComponentMenu("ItemManager/RealToon Bridge")]
    [DisallowMultipleComponent]
    public class RealToonItemBridge : MonoBehaviour
    {
        [Tooltip("RealToon material to apply to spawned item pickup renderers. Leave null to skip.")]
        [SerializeField] private Material realToonMaterial;

        private ItemManager _itemManager;

        private void Awake()
        {
            _itemManager = GetComponent<ItemManager>() ?? FindFirstObjectByType<ItemManager>();
            if (_itemManager == null)
                Debug.LogWarning("[ItemManager/RealToonItemBridge] ItemManager not found.");
        }

        private void OnEnable()
        {
            if (_itemManager != null)
                _itemManager.OnSpawnedCallback += HandleSpawned;
        }

        private void OnDisable()
        {
            if (_itemManager != null)
                _itemManager.OnSpawnedCallback -= HandleSpawned;
        }

        private void HandleSpawned(string defId, string instanceId, GameObject go)
        {
            if (go == null || realToonMaterial == null) return;

            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = realToonMaterial;
                renderer.sharedMaterials = mats;
            }
        }
    }
}
#endif
