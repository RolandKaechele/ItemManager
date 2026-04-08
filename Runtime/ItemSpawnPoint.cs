using UnityEngine;

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>ItemSpawnPoint</b> marks a world-space transform as a named spawn location for item pickups.
    /// Attach to any GameObject to register it with <see cref="ItemManager"/> at runtime.
    /// </summary>
    [AddComponentMenu("ItemManager/Item Spawn Point")]
    [DisallowMultipleComponent]
    public class ItemSpawnPoint : MonoBehaviour
    {
        [Tooltip("Unique id matching ItemWorldDefinition.spawnPointId.")]
        [SerializeField] private string pointId;

        private void Awake()
        {
            if (string.IsNullOrEmpty(pointId))
            {
                Debug.LogWarning($"[ItemSpawnPoint] '{gameObject.name}' has no pointId set — skipping registration.");
                return;
            }

            var manager = FindFirstObjectByType<ItemManager>();
            if (manager == null)
            {
                Debug.LogWarning("[ItemSpawnPoint] ItemManager not found in scene — cannot register spawn point.");
                return;
            }

            manager.RegisterSpawnPoint(new ItemSpawnPointData
            {
                id       = pointId,
                position = transform.position,
                rotation = transform.rotation
            });
        }

        private void OnDestroy()
        {
            var manager = FindFirstObjectByType<ItemManager>();
            manager?.UnregisterSpawnPoint(pointId);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
                string.IsNullOrEmpty(pointId) ? "(no id)" : pointId);
        }
#endif
    }
}
