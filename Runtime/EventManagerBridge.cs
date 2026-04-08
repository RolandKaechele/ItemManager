#if ITEMMANAGER_EM
using EventManager.Runtime;
using UnityEngine;

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>EventManagerBridge</b> connects ItemManager to EventManager.
    /// <para>
    /// When <c>ITEMMANAGER_EM</c> is defined, fires:
    /// <list type="bullet">
    ///   <item><c>item.spawned</c>   — payload: instanceId</item>
    ///   <item><c>item.collected</c> — payload: definitionId</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("ItemManager/EventManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class EventManagerBridge : UnityEngine.MonoBehaviour
    {
        private ItemManager  _itemManager;
        private EventManager _eventManager;

        private void Awake()
        {
            _itemManager  = GetComponent<ItemManager>()  ?? FindFirstObjectByType<ItemManager>();
            _eventManager = GetComponent<EventManager>() ?? FindFirstObjectByType<EventManager>();

            if (_itemManager  == null) Debug.LogWarning("[ItemManager/EventManagerBridge] ItemManager not found.");
            if (_eventManager == null) Debug.LogWarning("[ItemManager/EventManagerBridge] EventManager not found.");
        }

        private void OnEnable()
        {
            if (_itemManager == null) return;
            _itemManager.OnItemSpawned    += HandleSpawned;
            _itemManager.OnItemCollected  += HandleCollected;
        }

        private void OnDisable()
        {
            if (_itemManager == null) return;
            _itemManager.OnItemSpawned    -= HandleSpawned;
            _itemManager.OnItemCollected  -= HandleCollected;
        }

        private void HandleSpawned(string defId, string instanceId, UnityEngine.GameObject go)
            => _eventManager?.Fire("item.spawned", instanceId);

        private void HandleCollected(string defId, string instanceId, UnityEngine.GameObject go)
            => _eventManager?.Fire("item.collected", defId);
    }
}
#endif
