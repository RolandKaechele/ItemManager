#if ITEMMANAGER_IM
using InventoryManager.Runtime;
using UnityEngine;

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>InventoryManagerBridge</b> connects ItemManager to InventoryManager.
    /// <para>
    /// When <c>ITEMMANAGER_IM</c> is defined:
    /// <list type="bullet">
    ///   <item>Grants <see cref="ItemWorldDefinition.inventoryItemId"/> to the Inventory
    ///         with <see cref="ItemWorldDefinition.quantity"/> when a pickup is collected.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("ItemManager/InventoryManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class InventoryManagerBridge : UnityEngine.MonoBehaviour
    {
        private ItemManager      _itemManager;
        private InventoryManager _inventoryManager;

        private void Awake()
        {
            _itemManager      = GetComponent<ItemManager>()      ?? FindFirstObjectByType<ItemManager>();
            _inventoryManager = GetComponent<InventoryManager>() ?? FindFirstObjectByType<InventoryManager>();

            if (_itemManager      == null) Debug.LogWarning("[ItemManager/InventoryManagerBridge] ItemManager not found.");
            if (_inventoryManager == null) Debug.LogWarning("[ItemManager/InventoryManagerBridge] InventoryManager not found.");
        }

        private void OnEnable()
        {
            if (_itemManager != null)
                _itemManager.OnItemCollected += HandleCollected;
        }

        private void OnDisable()
        {
            if (_itemManager != null)
                _itemManager.OnItemCollected -= HandleCollected;
        }

        private void HandleCollected(string defId, string instanceId, UnityEngine.GameObject go)
        {
            if (_inventoryManager == null) return;

            var def = _itemManager.GetDefinition(defId);
            if (def == null || string.IsNullOrEmpty(def.inventoryItemId)) return;

            _inventoryManager.AddItem(def.inventoryItemId, def.quantity);
        }
    }
}
#endif
