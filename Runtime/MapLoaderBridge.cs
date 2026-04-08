#if ITEMMANAGER_MLF
using MapLoaderFramework.Runtime;
using UnityEngine;

namespace ItemManager.Runtime
{
    /// <summary>
    /// <b>MapLoaderBridge</b> connects ItemManager to MapLoaderFramework.
    /// <para>
    /// When <c>ITEMMANAGER_MLF</c> is defined:
    /// <list type="bullet">
    ///   <item>Clears all live pickups when a new chapter/map is loaded.</item>
    ///   <item>Spawns item definitions whose <c>mapIds</c> match the newly loaded map.</item>
    ///   <item>Also spawns items listed in <c>MapData.autoItemIds</c>.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("ItemManager/MapLoader Bridge")]
    [DisallowMultipleComponent]
    public class MapLoaderBridge : MonoBehaviour
    {
        [Tooltip("Clear all live pickups when a new map is loaded.")]
        [SerializeField] private bool clearOnMapLoad = true;

        [Tooltip("Spawn items whose mapIds match the loaded map id.")]
        [SerializeField] private bool autoSpawnByMapId = true;

        [Tooltip("Also spawn items explicitly listed in MapData.autoItemIds.")]
        [SerializeField] private bool autoSpawnFromMapData = true;

        private ItemManager         _itemManager;
        private MapLoaderFramework  _mapLoader;

        private void Awake()
        {
            _itemManager = GetComponent<ItemManager>() ?? FindFirstObjectByType<ItemManager>();
            _mapLoader   = GetComponent<MapLoaderFramework>() ?? FindFirstObjectByType<MapLoaderFramework>();

            if (_itemManager == null) Debug.LogWarning("[ItemManager/MapLoaderBridge] ItemManager not found.");
            if (_mapLoader   == null) Debug.LogWarning("[ItemManager/MapLoaderBridge] MapLoaderFramework not found.");
        }

        private void OnEnable()
        {
            if (_mapLoader != null) _mapLoader.OnMapLoaded += HandleMapLoaded;
        }

        private void OnDisable()
        {
            if (_mapLoader != null) _mapLoader.OnMapLoaded -= HandleMapLoaded;
        }

        private void HandleMapLoaded(MapData mapData)
        {
            if (_itemManager == null || mapData == null) return;

            if (clearOnMapLoad)
                _itemManager.ClearAllPickups();

            if (autoSpawnByMapId && !string.IsNullOrEmpty(mapData.id))
                _itemManager.SpawnItemsForMap(mapData.id);

            if (autoSpawnFromMapData && mapData.autoItemIds != null)
                foreach (var id in mapData.autoItemIds)
                    _itemManager.SpawnItem(id);
        }
    }
}
#else
namespace ItemManager.Runtime
{
    /// <summary>No-op stub — enable define <c>ITEMMANAGER_MLF</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("ItemManager/MapLoader Bridge")]
    public class MapLoaderBridge : UnityEngine.MonoBehaviour { }
}
#endif
