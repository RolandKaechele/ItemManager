#if ITEMMANAGER_STM
using StateManager.Runtime;
using UnityEngine;

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>StateManagerBridge</b> connects ItemManager to StateManager.
    /// <para>
    /// When <c>ITEMMANAGER_STM</c> is defined:
    /// <list type="bullet">
    ///   <item>Pauses item interaction when a Cutscene, Loading, or Dialogue state becomes active.</item>
    ///   <item>Resumes item interaction when those states pop.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("ItemManager/StateManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class StateManagerBridge : UnityEngine.MonoBehaviour
    {
        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("State ids that pause item interaction.")]
        private string[] pauseStateIds = { "Cutscene", "Loading", "Dialogue" };

        private ItemManager  _itemManager;
        private StateManager _stateManager;

        private void Awake()
        {
            _itemManager  = GetComponent<ItemManager>()  ?? FindFirstObjectByType<ItemManager>();
            _stateManager = GetComponent<StateManager>() ?? FindFirstObjectByType<StateManager>();

            if (_itemManager  == null) Debug.LogWarning("[ItemManager/StateManagerBridge] ItemManager not found.");
            if (_stateManager == null) Debug.LogWarning("[ItemManager/StateManagerBridge] StateManager not found.");
        }

        private void OnEnable()
        {
            if (_stateManager == null) return;
            _stateManager.OnStatePushed += HandleStatePushed;
            _stateManager.OnStatePopped += HandleStatePopped;
        }

        private void OnDisable()
        {
            if (_stateManager == null) return;
            _stateManager.OnStatePushed -= HandleStatePushed;
            _stateManager.OnStatePopped -= HandleStatePopped;
        }

        private bool IsPauseState(string id)
        {
            foreach (var s in pauseStateIds)
                if (System.StringComparer.OrdinalIgnoreCase.Equals(s, id)) return true;
            return false;
        }

        private void HandleStatePushed(string id) { if (IsPauseState(id)) _itemManager?.PauseInteraction(); }
        private void HandleStatePopped(string id) { if (IsPauseState(id)) _itemManager?.ResumeInteraction(); }
    }
}
#endif
