#if ITEMMANAGER_DOTWEEN
using DG.Tweening;
using UnityEngine;

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>DotweenItemBridge</b> adds DOTween-driven float and scale effects to ItemManager's
    /// spawn and collect operations.
    /// Enable define <c>ITEMMANAGER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Hooks <see cref="ItemManager.OnSpawnedCallback"/> and
    /// <see cref="ItemManager.OnCollectedCallback"/> to animate pickup GameObjects.
    /// </para>
    /// </summary>
    [AddComponentMenu("ItemManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenItemBridge : MonoBehaviour
    {
        [Header("Spawn Effect")]
        [Tooltip("Vertical float amplitude for the idle float tween.")]
        [SerializeField] private float floatAmplitude = 0.2f;

        [Tooltip("Duration of one float up/down cycle.")]
        [SerializeField] private float floatDuration = 1.2f;

        [Tooltip("Scale punch magnitude on spawn.")]
        [SerializeField] private float spawnPunchScale = 0.4f;

        [Tooltip("Duration of the spawn punch tween.")]
        [SerializeField] private float spawnPunchDuration = 0.35f;

        [Tooltip("Ease applied to the spawn punch.")]
        [SerializeField] private Ease spawnEase = Ease.OutElastic;

        [Header("Collect Effect")]
        [Tooltip("Duration of the scale-shrink tween on collect.")]
        [SerializeField] private float collectShrinkDuration = 0.2f;

        [Tooltip("Ease applied to the collect shrink.")]
        [SerializeField] private Ease collectEase = Ease.InBack;

        private ItemManager _itemManager;

        private void Awake()
        {
            _itemManager = GetComponent<ItemManager>() ?? FindFirstObjectByType<ItemManager>();
            if (_itemManager == null)
                Debug.LogWarning("[ItemManager/DotweenItemBridge] ItemManager not found.");
        }

        private void OnEnable()
        {
            if (_itemManager == null) return;
            _itemManager.OnSpawnedCallback   += HandleSpawned;
            _itemManager.OnCollectedCallback += HandleCollected;
        }

        private void OnDisable()
        {
            if (_itemManager == null) return;
            _itemManager.OnSpawnedCallback   -= HandleSpawned;
            _itemManager.OnCollectedCallback -= HandleCollected;
        }

        private void HandleSpawned(string defId, string instanceId, GameObject go)
        {
            if (go == null) return;

            // Punch scale on appear
            go.transform.localScale = Vector3.zero;
            go.transform
                .DOScale(Vector3.one, spawnPunchDuration)
                .SetEase(spawnEase)
                .OnComplete(() => StartFloat(go));
        }

        private void StartFloat(GameObject go)
        {
            if (go == null) return;
            Vector3 origin = go.transform.localPosition;
            go.transform
                .DOLocalMoveY(origin.y + floatAmplitude, floatDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void HandleCollected(string defId, string instanceId, GameObject go)
        {
            if (go == null) return;
            DOTween.Kill(go.transform);
            go.transform
                .DOScale(Vector3.zero, collectShrinkDuration)
                .SetEase(collectEase);
        }
    }
}
#endif
