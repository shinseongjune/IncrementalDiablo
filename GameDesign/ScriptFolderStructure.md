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
- `DungeonRoomPresenter`
- `DungeonLoopSmokeTest`
- `EnemySpawner`
- `RoomExit`
- room clear conditions

Rooms should be prefab/runtime objects, not separate Unity scenes.

Current implementation note: `DungeonRunState`, `ExpeditionDirector`, `CombatRoom`, `DungeonRoomPresenter`, `DungeonLoopSmokeTest`, and `DungeonDebugHud` exist first. `ExpeditionDirector` can start a prototype expedition, complete a room, fail the run, expose Ready/Running/Cleared/Failed state, export/import `DungeonSaveData` through `DefenseSaveManager`, and grant a pending clear reward through `LootDropper`. `CombatRoom` can auto-start when an expedition is running, auto-discover the current player plus `CharacterTeam.Enemy` actors, bind tracked enemies to the room lifecycle, resolve through tracked `Health` references, or fall back to prototype health/DPS simulation when scene actors are not wired yet. `DungeonRoomPresenter` supplies a prototype-only room-shell fallback and optional debug tint so the encounter can be read before authored room prefabs exist; final room presentation should replace that with authored gates, spawn cues, reward reveals, lighting/VFX, and UI feedback. `DungeonDebugHud` is an OnGUI Play Mode smoke-test surface for dungeon state, force clear/fail, pending reward grant, inventory count, save/load validation, and a one-button loop smoke test. `EnemyAIController` now exists for simple chase/attack behavior; prefab/spawner-driven room setup is still future work.

## GroundDefense

Ground defense code owns the continuous frontline, defense upgrades, local save support for the defense loop, and the HUD that inspects that loop.

- `GroundDefense/Runtime/`: frontline state, Hold/Push simulation, upgrades, and defense save manager.
- `GroundDefense/UI/`: HUD components for frontline status and defense actions.

## UI

Shared UI code owns player-facing screens that cross ground defense, dungeon, item, and save systems.

- `PlayableLoopHud`

Current implementation note: `PlayableLoopHud` is the first Canvas/TMP/Button bridge away from OnGUI debug panels. It can show frontline status, resources, dungeon state, latest item, current/max hero HP, a message line, and an action hint, then call button-safe methods for ground defense, dungeon, item, save, and load actions. It is wired into `Gameplay` as the normal combined-loop HUD; the debug OnGUI HUDs remain smoke-test fallbacks only.

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

Current implementation note: `ItemSlot`, `ItemRarity`, `ItemDefinition`, `ItemEconomyModel`, `ItemSalvageService`, `ItemInstance`, `SimpleInventory`, `LootDropper`, and `InventoryDebugHud` exist first. `EquipmentSlots` can equip definition assets or live `ItemInstance` objects and feed stat modifiers into `CharacterStats`; salvage can turn item definitions into Scrap/Essence and late Rare duplicates into small amounts of AlterStone for prototype economy tests. If an item was loaded without a connected definition asset, salvage can fall back to the saved slot/rarity/level snapshot so no-trade prototype drops do not become dead inventory. `SimpleInventory` can hold rolled item instances, keep a small known-definition registry, equip one item per slot into `EquipmentSlots`, and export/import `InventorySaveData`; `DefenseSaveManager` includes that inventory slice and writes equipped item ids through `HeroSaveData` when the scene has a `SimpleInventory`. `LootDropper` registers authored reward definitions with `SimpleInventory`, so saved ids for authored rewards can reconnect after load. Runtime prototype items without a resolved definition now restore a small prototype power modifier from saved slot/rarity/rolledPower, while a fuller item-definition registry remains required for production itemization. `LootDropper` can push a clear reward into `SimpleInventory`, using assigned `ItemDefinition` assets when present or an explicitly prototype-only runtime item fallback when the table is empty. `InventoryDebugHud` is an OnGUI Play Mode smoke-test surface for inventory count, latest item, latest-item equip, latest-item salvage, and wallet feedback. Actual affix mutation, crafting UI, full item-definition registry, real drop tables, and player-facing inventory UI are still future work.

## Shared

Shared code is for small, generic helpers used across systems. Keep this folder small.

## Rule

Prefer component composition over inheritance.

Examples:

- player hero = `CharacterActor + PlayerController`
- auto hero = `CharacterActor + AutoCombatController`
- enemy = `CharacterActor + EnemyAIController`

`CharacterActor` is the body and component hub. Controllers decide. Motor and combat execute.
