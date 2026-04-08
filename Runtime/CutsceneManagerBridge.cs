#if ITEMMANAGER_CSM
using CutsceneManager.Runtime;
using UnityEngine;

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>CutsceneManagerBridge</b> connects ItemManager to CutsceneManager.
    /// <para>
    /// When <c>ITEMMANAGER_CSM</c> is defined:
    /// <list type="bullet">
    ///   <item>Pauses item interaction when a cutscene starts.</item>
    ///   <item>Resumes item interaction when the cutscene ends or is skipped.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("ItemManager/CutsceneManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class CutsceneManagerBridge : UnityEngine.MonoBehaviour
    {
        private ItemManager     _itemManager;
        private CutsceneManager _cutsceneManager;

        private void Awake()
        {
            _itemManager     = GetComponent<ItemManager>()     ?? FindFirstObjectByType<ItemManager>();
            _cutsceneManager = GetComponent<CutsceneManager>() ?? FindFirstObjectByType<CutsceneManager>();

            if (_itemManager     == null) Debug.LogWarning("[ItemManager/CutsceneManagerBridge] ItemManager not found.");
            if (_cutsceneManager == null) Debug.LogWarning("[ItemManager/CutsceneManagerBridge] CutsceneManager not found.");
        }

        private void OnEnable()
        {
            if (_cutsceneManager == null) return;
            _cutsceneManager.OnSequenceStarted    += HandleCutsceneStarted;
            _cutsceneManager.OnSequenceCompleted  += HandleCutsceneFinished;
            _cutsceneManager.OnSequenceSkipped    += HandleCutsceneFinished;
        }

        private void OnDisable()
        {
            if (_cutsceneManager == null) return;
            _cutsceneManager.OnSequenceStarted    -= HandleCutsceneStarted;
            _cutsceneManager.OnSequenceCompleted  -= HandleCutsceneFinished;
            _cutsceneManager.OnSequenceSkipped    -= HandleCutsceneFinished;
        }

        private void HandleCutsceneStarted(string id)  => _itemManager?.PauseInteraction();
        private void HandleCutsceneFinished(string id) => _itemManager?.ResumeInteraction();
    }
}
#endif
