using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItemManager.Runtime
{
    // -------------------------------------------------------------------------
    // ItemPickupBehaviour
    // -------------------------------------------------------------------------

    /// <summary>Controls when a world item is automatically collected.</summary>
    public enum ItemPickupBehaviour
    {
        /// <summary>Player must press interact; collected via <see cref="ItemPickup.Interact"/>.</summary>
        Interact,
        /// <summary>Collected automatically when the player enters the trigger collider.</summary>
        AutoCollect,
        /// <summary>Only collected via API — no automatic trigger logic.</summary>
        Manual
    }

    // -------------------------------------------------------------------------
    // ItemWorldDefinition
    // -------------------------------------------------------------------------

    /// <summary>
    /// Defines a single world-space item pickup: what to spawn, where, and under what conditions.
    /// Serializable so it can be authored in the Inspector and loaded from JSON.
    /// </summary>
    [Serializable]
    public class ItemWorldDefinition
    {
        /// <summary>Unique identifier (e.g. "key_red_rock_lab").</summary>
        public string id;

        /// <summary>Human-readable label shown in Editor UI and optional pickup prompt.</summary>
        public string label;

        /// <summary>
        /// Map id(s) this item should appear on. If empty the item is always spawned regardless of map.
        /// JSON: array of strings or a single string.
        /// </summary>
        public List<string> mapIds = new List<string>();

        /// <summary>
        /// Resources-relative path to the prefab (e.g. "Items/KeyRedRockLab").
        /// Used when no prefab is supplied directly.
        /// </summary>
        public string prefabResource;

        /// <summary>World-space position override. Applied after the spawn point lookup.</summary>
        public Vector3 worldPosition;

        /// <summary>World-space rotation (Euler angles).</summary>
        public Vector3 worldRotation;

        /// <summary>Optional spawn-point id registered via <see cref="ItemSpawnPoint"/>. Overrides <see cref="worldPosition"/>.</summary>
        public string spawnPointId;

        /// <summary>
        /// Inventory item id to grant when collected (used by <c>InventoryManagerBridge</c>
        /// when <c>ITEMMANAGER_IM</c> is defined).
        /// </summary>
        public string inventoryItemId;

        /// <summary>Pickup quantity granted to inventory (default 1).</summary>
        public int quantity = 1;

        /// <summary>
        /// If true, this item is only spawned once and the "collected" state is persisted
        /// across sessions (requires SaveManager bridge or custom persistence).
        /// </summary>
        public bool oneTimePickup = true;

        /// <summary>
        /// Save flag key used to persist the collected state.
        /// Defaults to <c>"item_collected_{id}"</c> when empty.
        /// </summary>
        public string saveFlag;

        /// <summary>How the player collects this item.</summary>
        public ItemPickupBehaviour pickupBehaviour = ItemPickupBehaviour.AutoCollect;

        /// <summary>Localization key for the pickup prompt text.</summary>
        public string promptLocalizationKey;

        /// <summary>
        /// Radius of the auto-collect trigger sphere (only used when
        /// <see cref="pickupBehaviour"/> is <see cref="ItemPickupBehaviour.AutoCollect"/>).
        /// </summary>
        public float collectRadius = 1f;

        /// <summary>Optional tag applied to the spawned pickup GameObject.</summary>
        public string pickupTag;

        /// <summary>Stores the original deserialized JSON for unknown / future fields.</summary>
        [NonSerialized] public string rawJson;
    }

    // -------------------------------------------------------------------------
    // ItemInstanceRecord
    // -------------------------------------------------------------------------

    /// <summary>Tracks a live world-item instance.</summary>
    [Serializable]
    public class ItemInstanceRecord
    {
        /// <summary>Definition id.</summary>
        public string definitionId;

        /// <summary>Unique instance id assigned at spawn time.</summary>
        public string instanceId;

        /// <summary>The spawned GameObject.</summary>
        [NonSerialized] public GameObject gameObject;

        /// <summary>Map id on which this instance was spawned.</summary>
        public string mapId;
    }

    // -------------------------------------------------------------------------
    // ItemJsonWrapper / ItemWorldJson  (JSON deserialization helpers)
    // -------------------------------------------------------------------------

    /// <summary>Root wrapper for the <c>items.json</c> modding file.</summary>
    [Serializable]
    public class ItemJsonWrapper
    {
        public List<ItemWorldJson> items = new List<ItemWorldJson>();
    }

    /// <summary>Flat JSON representation that mirrors <see cref="ItemWorldDefinition"/>.</summary>
    [Serializable]
    public class ItemWorldJson
    {
        public string id;
        public string label;
        public string[] mapIds;
        public string prefabResource;
        public float px, py, pz;
        public float rx, ry, rz;
        public string spawnPointId;
        public string inventoryItemId;
        public int quantity = 1;
        public bool oneTimePickup = true;
        public string saveFlag;
        public int pickupBehaviour = 1;   // 0=Interact, 1=AutoCollect, 2=Manual
        public string promptLocalizationKey;
        public float collectRadius = 1f;
        public string pickupTag;

        public ItemWorldDefinition ToDefinition()
        {
            var def = new ItemWorldDefinition
            {
                id                   = id,
                label                = label ?? id,
                prefabResource       = prefabResource,
                worldPosition        = new Vector3(px, py, pz),
                worldRotation        = new Vector3(rx, ry, rz),
                spawnPointId         = spawnPointId,
                inventoryItemId      = inventoryItemId,
                quantity             = quantity,
                oneTimePickup        = oneTimePickup,
                saveFlag             = saveFlag,
                pickupBehaviour      = (ItemPickupBehaviour)pickupBehaviour,
                promptLocalizationKey = promptLocalizationKey,
                collectRadius        = collectRadius,
                pickupTag            = pickupTag
            };

            if (mapIds != null)
                def.mapIds.AddRange(mapIds);

            return def;
        }
    }

    // -------------------------------------------------------------------------
    // SpawnPointData
    // -------------------------------------------------------------------------

    /// <summary>Data structure for a registered item spawn-point.</summary>
    [Serializable]
    public class ItemSpawnPointData
    {
        /// <summary>Globally unique spawn-point id.</summary>
        public string id;
        /// <summary>World-space position of this spawn point.</summary>
        public Vector3 position;
        /// <summary>World-space rotation of this spawn point.</summary>
        public Quaternion rotation;
    }
}
