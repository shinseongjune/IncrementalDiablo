# Script Folder Structure

Use `Assets/02.Scripts` as the root for gameplay code.

## Character

Character code owns actors, stats, health, movement, combat execution, equipment slots, and controllers.

- `Character/Core/`: body, movement, combat execution, health, and equipment components.
- `Character/Stats/`: stat ids, stat containers, and stat modifier experiments.
- `Character/Controllers/`: player input, auto combat, and enemy AI controllers.

## Dungeon

Dungeon code owns room-based expedition flow.

- `DungeonRunState`
- `ExpeditionDirector`
- `CombatRoom`
- `EnemySpawner`
- `RoomExit`
- room clear conditions

Rooms should be prefab/runtime objects, not separate Unity scenes.

Current implementation note: `DungeonRunState`, `ExpeditionDirector`, and `CombatRoom` exist first. `ExpeditionDirector` can start a prototype expedition, complete a room, fail the run, expose Ready/Running/Cleared/Failed state, export/import `DungeonSaveData` through `DefenseSaveManager`, and grant a pending clear reward through `LootDropper`. `CombatRoom` can auto-start when an expedition is running, resolve through tracked `Health` references, or fall back to prototype health/DPS simulation when enemy prefabs are not wired yet. Enemy controllers, dungeon HUD buttons, and finished Unity room prefab wiring are still future work.

## GroundDefense

Ground defense code owns the continuous frontline, defense upgrades, local save support for the defense loop, and the HUD that inspects that loop.

- `GroundDefense/Runtime/`: frontline state, Hold/Push simulation, upgrades, and defense save manager.
- `GroundDefense/UI/`: HUD components for frontline status and defense actions.

## Items

Item code owns definitions, instances, drops, equipment effects, and simple inventory.

- `ItemSlot`
- `ItemRarity`
- `ItemDefinition`
- `ItemEconomyModel`
- `ItemSalvageService`
- `ItemInstance`
- `LootDropper`
- `SimpleInventory`

Current implementation note: `ItemSlot`, `ItemRarity`, `ItemDefinition`, `ItemEconomyModel`, `ItemSalvageService`, `ItemInstance`, `SimpleInventory`, and `LootDropper` exist first. `EquipmentSlots` can equip definition assets and feed stat modifiers into `CharacterStats`; salvage can turn item definitions into Scrap/Essence and late Rare duplicates into small amounts of AlterStone for prototype economy tests. If an item was loaded without a connected definition asset, salvage can fall back to the saved slot/rarity/level snapshot so no-trade prototype drops do not become dead inventory. `SimpleInventory` can hold rolled item instances and export/import `InventorySaveData`; `DefenseSaveManager` includes that inventory slice when a `SimpleInventory` exists in the scene. `LootDropper` can push a clear reward into `SimpleInventory`, using assigned `ItemDefinition` assets when present or an explicitly prototype-only runtime item fallback when the table is empty. Actual affix mutation, crafting UI, item-definition lookup after load, real drop tables, and player-facing inventory UI are still future work.

## Shared

Shared code is for small, generic helpers used across systems. Keep this folder small.

## Rule

Prefer component composition over inheritance.

Examples:

- player hero = `CharacterActor + PlayerController`
- auto hero = `CharacterActor + AutoCombatController`
- enemy = `CharacterActor + EnemyAIController`

`CharacterActor` is the body and component hub. Controllers decide. Motor and combat execute.
