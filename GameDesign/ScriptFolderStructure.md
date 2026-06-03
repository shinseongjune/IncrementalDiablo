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

Current implementation note: `DungeonRunState`, `ExpeditionDirector`, `CombatRoom`, `DungeonRoomPresenter`, `DungeonLoopSmokeTest`, `EnemySpawner`, and `DungeonDebugHud` exist first. `ExpeditionDirector` can start a prototype expedition, complete a room, fail the run, expose Ready/Running/Cleared/Failed state, export/import `DungeonSaveData` through `DefenseSaveManager`, and grant a pending clear reward through `LootDropper`. `CombatRoom` can auto-start when an expedition is running, auto-discover the current player plus `CharacterTeam.Enemy` actors, accept explicitly registered spawned enemies, bind tracked enemies to the room lifecycle, resolve through tracked `Health` references, or fall back to prototype health/DPS simulation when scene actors are not wired yet. If `EnemySpawner` reports a setup blocker, `CombatRoom` keeps that blocker visible and stops prototype simulation so the prefab-spawn path cannot silently fake-clear. `EnemySpawner` can instantiate a configured melee enemy prefab at room-start spawn points, register its `Health` components with `CombatRoom`, keep spawned enemies inactive until the room enters combat, and report missing prefab/Health setup problems through `LastSpawnMessage`. `DungeonRoomPresenter` supplies a prototype-only room-shell fallback and optional debug tint so the encounter can be read before authored room prefabs exist; final room presentation should replace that with authored gates, spawn cues, reward reveals, lighting/VFX, and UI feedback. `DungeonDebugHud` is an OnGUI Play Mode smoke-test surface for dungeon state, force clear/fail, pending reward grant, inventory count, save/load validation, and a one-button loop smoke test. `EnemyAIController` exists for simple chase/attack behavior; `Gameplay` currently wires `PF_DungeonEnemy_Melee` and one spawn point into `EnemySpawner`, so the remaining manual Phase C step is Play Mode validation of activation timing, NavMesh movement, click-to-attack feel, and room clear/reward continuity.

## GroundDefense

Ground defense code owns the continuous frontline, defense upgrades, local save support for the defense loop, and the HUD that inspects that loop.

- `GroundDefense/Runtime/`: frontline state, Hold/Push simulation, upgrades, and defense save manager.
- `GroundDefense/UI/`: HUD components for frontline status and defense actions.

Current implementation note: `DefenseDirector` and `DefenseRuntimeState` own the formula-driven frontline simulation. `DefenseRuntimeState` also exposes last-tick incoming pressure, pressure cleared by defense, wall damage, and push progress as per-second feedback values for presentation and QA. `DefenseHud` remains the focused numeric/debug HUD, while `GroundDefenseLanePresenter` is the first Phase C visual bridge for the ground lane. It does not author room scale, camera, enemy art, or final layout; it reads `DefenseDirector.Runtime` and drives scene-authored anchors, pressure/progress markers, auto-resolved marker renderers, optional enemy-flow markers, wall/pressure fills, state objects, renderer colors, and optional TMP labels so the visible lane stays synchronized with the real Hold/Push, pressure, wall-health, and breach state. `GroundDefenseCombatPresenter` is the next bridge toward readable ground-defense combat: it drives scene-authored pressure actors, wall-contact flash feedback, and tower/defender attack pulses from the same runtime state, uses the recent combat feedback rates for actor color and pulse intensity, then exposes `ActivePressureActorCount`, `ActiveAttackPulseCount`, `WallContactEventCount`, and `LastCombatMessage` for HUD/Inspector validation. These presenters are not final authored combat or final art; they leave silhouette, spacing, camera framing, marker count, and feel tuning to Unity scene authoring.

## UI

Shared UI code owns player-facing screens that cross ground defense, dungeon, item, and save systems.

- `PlayableLoopHud`
- `PlayableScreenFocus`
- `PlayableScreenLayoutController`
- `InventoryOverlayPresenter`
- `RewardOverlayPresenter`
- `CraftingOverlayPresenter`

Current implementation note: `PlayableLoopHud` is the first Canvas/TMP/Button bridge away from OnGUI debug panels. It can show frontline status, defense-alert summary/action priority, resources, ground-combat presenter status, dungeon state, combat path, loot source, latest item, current/max hero HP, screen focus, a message line, and an action hint, then call button-safe methods for ground defense, dungeon, item, save, load, and optional inventory/crafting/reward overlay actions. It can also open the wired reward overlay automatically after a cleared room, so the normal dungeon-clear path leads into reward review/equip/salvage without relying on a manual debug-style follow-up button. `PlayableScreenFocus` and `PlayableScreenLayoutController` add the first reusable screen-state bridge for DefenseFocus, DungeonFocus, inventory overlay, crafting overlay, and reward overlay. The controller moves authored UI RectTransforms between the MVP defense-fullscreen and dungeon-dominant 70/30 layouts, reports whether optional overlay objects are wired, refuses to enter an invisible overlay state when a panel is missing, and can apply a target gameplay focus before opening an overlay so close actions return to the intended post-run focus. `PlayableLoopHud` can auto-find it, request DungeonFocus on dungeon start, return to DefenseFocus when the room resolves, open reward overlay over DefenseFocus on clear, prioritize severe defense alerts or high pressure while the player is in DungeonFocus, and disable overlay buttons until the matching GameObjects exist. `InventoryOverlayPresenter` is the first player-facing inventory overlay content bridge: it fills authored TMP labels with item rows, selected-item details, wallet/materials, salvage preview, Rare reroll-cost preview when available, and action messages, then exposes Previous/Next/Latest/Equip/Salvage/Close methods for normal UI buttons. `RewardOverlayPresenter` is the matching reward-reveal content bridge for a wired reward overlay: it shows pending/claimed dungeon reward state, loot source, latest reward item details, wallet/material preview, claim reward, open inventory, equip reward, salvage reward, and close-overlay actions. `CraftingOverlayPresenter` is the first crafting overlay content bridge: it lists item instances, prefers the newest rerollable Rare item when opened, shows reroll-ready count, material guidance, and last-reroll result feedback, previews salvage and Rare reroll costs, salvages selected items, spends reroll materials, and writes one prototype affix roll onto selected Rare items; the current prototype reroll avoids repeating that item's saved affix when another slot-valid candidate exists. The current `Gameplay` scene includes first-pass inventory/reward/crafting overlay layouts and wiring; final camera crop, overlay density, item icons, scroll behavior, reward reveal art, crafting panel art, and ornate treatment remain Unity Editor work. The debug OnGUI HUDs remain smoke-test fallbacks only.

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

Current implementation note: `ItemSlot`, `ItemRarity`, `ItemDefinition`, `ItemEconomyModel`, `ItemSalvageService`, `ItemInstance`, `SimpleInventory`, `LootDropper`, and `InventoryDebugHud` exist first. `EquipmentSlots` can equip definition assets or live `ItemInstance` objects and feed stat modifiers into `CharacterStats`; salvage can turn item definitions into Scrap/Essence and Rare duplicates into small amounts of AlterStone for prototype economy tests. If an item was loaded without a connected definition asset, salvage can fall back to the saved slot/rarity/level snapshot so no-trade prototype drops do not become dead inventory. `SimpleInventory` can hold rolled item instances, keep a small known-definition registry, equip one item per slot into `EquipmentSlots`, export/import `InventorySaveData`, and notify UI after a live item mutation. `DefenseSaveManager` includes that inventory slice and writes equipped item ids through `HeroSaveData` when the scene has a `SimpleInventory`. `LootDropper` registers authored reward definitions with `SimpleInventory`, so saved ids for authored rewards can reconnect after load. Runtime prototype items without a resolved definition now restore a small prototype power modifier from saved slot/rarity/rolledPower, while a fuller item-definition registry remains required for production itemization. `ItemInstance` can now apply a first prototype Rare affix reroll by replacing its saved `ItemAffixRoll` array with one stat modifier and avoiding the current affix when another slot-valid prototype candidate exists. `LootDropper` can push a clear reward into `SimpleInventory`, using a weighted authored reward table first, then the legacy uniform definition list, then an explicitly prototype-only runtime item fallback when no authored table is available. It records `LastRewardSource` so HUD and QA can tell authored weighted-table rewards apart from fallback rewards. The first six authored tier-1 item assets live under `Assets/05.ScriptableObjects/Items` and are wired into `Gameplay` at prototype 78/20/2 Normal/Magic/Rare per-clear weights, with first-Rare access and a 6 non-Rare pity threshold enabled so the crafting overlay validation path does not depend on dozens of clears. `InventoryDebugHud` is an OnGUI Play Mode smoke-test surface for inventory count, latest item, latest-item equip, latest-item salvage, and wallet feedback; `InventoryOverlayPresenter` is the first normal-player overlay path for listing, selecting, equipping, and salvaging those same item instances once an authored `Panel_InventoryOverlay` is wired. Full affix pools, affix locking, item-level upgrades, full item-definition registry, real drop-table export/import, and final inventory/crafting presentation remain future work.

## Shared

Shared code is for small, generic helpers used across systems. Keep this folder small.

## Automation

Project automation helpers that are not Unity runtime code live outside `Assets`.

- `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1`: safe daily verification harness for Codex automation. It runs the solution build, `git diff --check`, required `Gameplay.unity` scene-contract checks, missing-script scan, optional overlay wiring warning, automation-plan freshness checks, and local automation TOML health checks without invoking Unity batchmode.

## Rule

Prefer component composition over inheritance.

Examples:

- player hero = `CharacterActor + PlayerController`
- auto hero = `CharacterActor + AutoCombatController`
- enemy = `CharacterActor + EnemyAIController`

`CharacterActor` is the body and component hub. Controllers decide. Motor and combat execute.
