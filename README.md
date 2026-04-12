# ItemManager

World-space item pickup manager for Unity. Handles per-map spawning of collectible items, one-time pickup tracking, JSON-driven mod-friendly definitions, and auto-prefab generation from JSON.

## Features

- Named `ItemWorldDefinition` entries configured in the Inspector or loaded from `StreamingAssets/items/`
- Per-map item spawning — definitions filter by `mapIds`; empty list means always spawned
- `ItemPickup` component supports **AutoCollect** (trigger), **Interact** (button press), and **Manual** (API-only) behaviours
- One-time pickup tracking with optional SaveManager persistence
- `PauseInteraction()` / `ResumeInteraction()` for cutscenes and loading screens
- `OnSpawnedCallback` and `OnCollectedCallback` delegate hooks for bridges
- Editor window: auto-generate pickup prefabs from `items.json` (`Generate Prefabs > Item Prefabs from JSON`)
- Custom Inspector with live instance counts and runtime spawn/collect controls
- JSON modding support via `StreamingAssets/items/` (entries merged by `id`)

## Optional Integrations

| Feature | Define Symbol |
| ------- | ------------- |
| MapLoaderFramework (spawn/clear on map load) | `ITEMMANAGER_MLF` |
| InventoryManager (grant item on collect) | `ITEMMANAGER_IM` |
| EventManager events | `ITEMMANAGER_EM` |
| SaveManager (persist one-time flags) | `ITEMMANAGER_SM` |
| StateManager pause (Cutscene/Loading/Dialogue) | `ITEMMANAGER_STM` |
| CutsceneManager pause | `ITEMMANAGER_CSM` |
| DOTween Pro (float + scale effects) | `ITEMMANAGER_DOTWEEN` |
| RealToon Pro anime/toon material | `ITEMMANAGER_REALTOON` |

## EventManager Events

When `ITEMMANAGER_EM` is active the following events are fired:

| Event Key | Payload |
| --------- | ------- |
| `item.spawned` | `instanceId` (string) |
| `item.collected` | `definitionId` (string) |

## JSON Modding

Place one or more `.json` files in `StreamingAssets/items/` to override or extend item definitions at runtime without recompiling.
All `*.json` files in the folder are loaded and merged by `id` at startup.
Each file contains exactly one item entry and is named by item ID.

**Example:** `StreamingAssets/items/key_red_rock_lab.json`

```json
{
  "items": [
    {
      "id": "key_red_rock_lab",
      "label": "Red Rock Lab Key",
      "chapter": 8,
      "mapIds": ["red_rock_lab_entrance"],
      "prefabResource": "Items/ItemPickup_key_red_rock_lab",
      "px": 12.5, "py": 0.5, "pz": -3.0,
      "inventoryItemId": "key_red_rock_lab",
      "quantity": 1,
      "oneTimePickup": true,
      "pickupBehaviour": 1,
      "collectRadius": 1.5
    }
  ]
}
```

## JSON Fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | string | Unique identifier |
| `label` | string | Human-readable name |
| `chapter` | int | Chapter number this item belongs to (`0` = global/all chapters) |
| `mapIds` | string[] | Map ids to spawn on; empty = all maps |
| `prefabResource` | string | `Resources.Load` path to pickup prefab |
| `px`, `py`, `pz` | float | World-space position |
| `rx`, `ry`, `rz` | float | World-space rotation (Euler) |
| `spawnPointId` | string | Named `ItemSpawnPoint` id (overrides position) |
| `inventoryItemId` | string | Item id granted to InventoryManager on collect |
| `quantity` | int | Quantity granted (default 1) |
| `oneTimePickup` | bool | Persist collected flag across sessions |
| `saveFlag` | string | Custom save flag key (defaults to `item_collected_{id}`) |
| `pickupBehaviour` | int | `0`=Interact, `1`=AutoCollect, `2`=Manual |
| `collectRadius` | float | Trigger sphere radius for AutoCollect |
| `pickupTag` | string | Tag applied to spawned pickup GameObject |

## Auto-Prefab Generation

1. Place your `items.json` in `StreamingAssets/`.
2. Open **Generate Prefabs › Item Prefabs from JSON**.
3. Set output path (default: `Assets/Resources/Items`).
4. Click **Generate Prefabs**.

Each entry creates `ItemPickup_{id}.prefab` with an `ItemPickup` component, a sphere collider trigger, and a placeholder visual.

## Quick Start

1. Add an `ItemManager` component to a persistent GameObject.
2. Add `ItemSpawnPoint` components to world-space spots and assign `pointId`.
3. Define `ItemWorldDefinition` entries in the Inspector (or load from JSON).
4. If using MapLoaderFramework, also add a `MapLoaderBridge` component and enable `ITEMMANAGER_MLF`.
5. Call `ItemManager.Instance.SpawnItemsForMap("my_map_id")` or let `MapLoaderBridge` do it automatically.
6. On pickup, `ItemPickup` fires `OnCollected` — `ItemManager` handles the rest.


## Editor Tools

Open via **JSON Editors → Item Manager** in the Unity menu bar, or via the **Open JSON Editor** button in the ItemManager Inspector.

| Action | Result |
| ------ | ------ |
| **Load** | Reads all `*.json` from `StreamingAssets/items/`; creates the folder if missing |
| **Edit** | Add / remove / reorder entries using the Inspector list |
| **Save** | Writes each entry as `<id>.json` to `StreamingAssets/items/`; entries without an `id` are skipped. Calls `AssetDatabase.Refresh()` |

With **ODIN_INSPECTOR** active, the list uses Odin's enhanced drawer (drag-to-sort, collapsible entries).
