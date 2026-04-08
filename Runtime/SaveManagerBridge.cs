#if ITEMMANAGER_SM
using SaveManager.Runtime;
using UnityEngine;

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>SaveManagerBridge</b> connects ItemManager to SaveManager.
    /// <para>
    /// When <c>ITEMMANAGER_SM</c> is defined:
    /// <list type="bullet">
    ///   <item>Writes a save flag when a one-time pickup is collected.</item>
    ///   <item>Checks save flags at spawn time so already-collected items remain absent after loading.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("ItemManager/SaveManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class SaveManagerBridge : UnityEngine.MonoBehaviour
    {
        private ItemManager  _itemManager;
        private SaveManager  _saveManager;

        private void Awake()
        {
            _itemManager = GetComponent<ItemManager>()  ?? FindFirstObjectByType<ItemManager>();
            _saveManager = GetComponent<SaveManager>() ?? FindFirstObjectByType<SaveManager>();

            if (_itemManager == null) Debug.LogWarning("[ItemManager/SaveManagerBridge] ItemManager not found.");
            if (_saveManager == null) Debug.LogWarning("[ItemManager/SaveManagerBridge] SaveManager not found.");
        }

        private void OnEnable()
        {
            if (_itemManager == null) return;
            _itemManager.IsCollectedPersistenceCheck = CheckFlag;
            _itemManager.PersistCollected            = SetFlag;
        }

        private void OnDisable()
        {
            if (_itemManager == null) return;
            _itemManager.IsCollectedPersistenceCheck = null;
            _itemManager.PersistCollected            = null;
        }

        private bool CheckFlag(string flag)
            => _saveManager != null && _saveManager.IsSet(flag);

        private void SetFlag(string flag)
            => _saveManager?.SetFlag(flag);
    }
}
#endif
