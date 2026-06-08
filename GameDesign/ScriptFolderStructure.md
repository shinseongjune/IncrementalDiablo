# Script Folder Structure

Use `Assets/02.Scripts` as the root for gameplay code.

## Character

Character code owns actors, stats, health, movement, combat execution, equipment slots, and controllers.

- `Character/Core/`: body, movement, combat execution, health, and equipment components.
- `Character/Stats/`: stat ids, stat containers, and stat modifier experiments.
- `Character/Controllers/`: player input, auto combat, and enemy AI controllers.

Current implementation note: `PlayerController` owns direct-control click movement, click attacks, Shift stationary attacks, and external camera-ray clicks for UI-rendered dungeon panels. It ignores duplicate world-click handling while the pointer is over UI, and it skips self/friendly actor colliders as movement surfaces so the hero should not self-target or move onto its own click collider. Shift-clicking a valid target keeps a stationary command alive while waiting for range, then hits when the target is in range; Shift-clicking ground still plays a single in-place attack.

## Dungeon

Dungeon code owns room-based expedition flow.

- `DungeonRunState`
- `DungeonDepthBalanceModel`
- `ExpeditionDirector`
- `CombatRoom`
- `DungeonRoomPresenter`
- `DungeonLoopSmokeTest`
- `EnemySpawner`
- `RoomExit`
- room clear conditions

Rooms should be prefab/runtime objects, not separate Unity scenes.

Current implementation note: `DungeonRunState`, `ExpeditionDirector`, `CombatRoom`, `DungeonRoomPresenter`, `DungeonLoopSmokeTest`, `EnemySpawner`, and `DungeonDebugHud` exist first. `ExpeditionDirector` can start an expedition, complete a room, fail the run, expose Ready/Running/Cleared/Failed state, export/import `DungeonSaveData` through `DefenseSaveManager`, and grant a pending clear reward through `LootDropper`. `CombatRoom` binds tracked enemies to the room lifecycle and resolves through tracked `Health` references. Its prototype health/DPS simulation remains available for isolated dev/test use, but normal `Gameplay` disables it. `EnemySpawner` validates the melee prefab's Health, Enemy team, AI, enabled NavMeshAgent, and click collider, resolves every intended spawn position onto nearby NavMesh before instantiation, registers spawned `Health` components with `CombatRoom`, and keeps spawned enemies inactive until the room enters combat. The user accepted the current `PF_DungeonEnemy_Melee` spawn, chase/attack, routed player attack, HP/death, clear, authored reward, and retry path on 2026-06-06. `DungeonRoomPresenter` remains prototype room-presentation debt, and `DungeonDebugHud` remains an edge-only smoke-test surface.

Phase D progression note: `ExpeditionDirector` now owns active, selected, and highest-unlocked dungeon depths. It starts the selected depth, unlocks exactly one next depth after clearing the current highest, keeps failure non-advancing, and exports/imports the ladder through `DungeonSaveData`.

D0-B implementation note: `DungeonDepthBalanceModel` owns ten-depth milestone bands for enemy health, enemy damage, reward power, and material yield. `EnemySpawner` applies the active profile through runtime `CharacterStats` multipliers, `LootDropper` writes depth-scaled item level/power, `ItemEconomyModel.GetSalvageRewards(ItemInstance)` keeps overlay previews and actual payout aligned from saved item level, and `PlayableLoopHud` exposes the selected/active profile.

## GroundDefense

Ground defense code owns the continuous frontline, defense upgrades, local save support for the defense loop, and the HUD that inspects that loop.

- `GroundDefense/Runtime/`: frontline state, Hold/Push simulation, upgrades, and defense save manager.
- `GroundDefense/UI/`: HUD components for frontline status and defense actions.

Current implementation note: `DefenseDirector` and `DefenseRuntimeState` own the formula-driven frontline simulation. `DefenseRuntimeState` also exposes last-tick incoming pressure, pressure cleared by defense, wall damage, and push progress as per-second feedback values for presentation and QA. `GroundDefenseActorRuntime` converts those authoritative rates into a small reusable set of individual actor slots with health, travel, defense-hit, defeat, and wall-contact events; those transient actor slots rebuild after load and do not duplicate progression or reward save state. `DefenseHud` remains the focused numeric/debug HUD, while `GroundDefenseLanePresenter` is the first Phase C visual bridge for the ground lane. `GroundDefenseCombatPresenter` maps `GroundDefenseActorRuntime` slots onto scene-authored pressure actors, wall-contact flash feedback, and tower/defender attack pulses, then exposes active actor/pulse counts and hit/defeat/contact totals through `LastCombatMessage`. The user accepted this P0-C behavior on 2026-06-05. The fixed three-slot/blockout composition is frozen and registered for replacement; do not expand it through more marker, color, speed, spacing, silhouette, or camera tuning. Future ground production should move directly to pooled enemy prefabs, archetype stats/data, real targeting/death handling, and reusable combat feedback.

Automation tool note: `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` owns the static phase-routing freshness contract for `10_PlayableLoopMvpAutomationPlan.md`. As of 2026-06-07 it requires Phase D, completed D0-A, and D0-B as the next task; update those required tokens whenever the canonical phase/task selector advances.

D0-A automation note: the same harness now checks the `Gameplay` depth-button references and source tokens for selected/highest depth, one-step clear unlock, save schema v2 migration, and save diagnostics.

D0-B automation note: `Tools/Automation/Export-DungeonDepthBalance.ps1` reads the runtime model constants, checks depth 1 plus monotonic growth, and exports `GameDesign/Balance/DungeonDepthBalance.csv`. The main harness runs its `-CheckOnly` path and checks the runtime scaling tokens.

## UI

Shared UI code owns player-facing screens that cross ground defense, dungeon, item, and save systems.

- `PlayableLoopHud`
- `PlayableScreenFocus`
- `PlayableScreenLayoutController`
- `InventoryOverlayPresenter`
- `RewardOverlayPresenter`
- `CraftingOverlayPresenter`
- `PanelCameraRenderTarget`
- `DungeonViewportInputRouter`

Current implementation note: `PlayableLoopHud` is the first Canvas/TMP/Button bridge away from OnGUI debug panels. It can show frontline status, defense-alert summary/action priority, resources, ground-combat presenter status, dungeon state, combat path, loot source, latest item, current/max hero HP, screen focus, dungeon viewport render/input diagnostics, a message line, and an action hint, then call button-safe methods for ground defense, dungeon, item, save, load, and optional inventory/crafting/reward overlay actions. It can also open the wired reward overlay automatically after a cleared room, so the normal dungeon-clear path leads into reward review/equip/salvage without relying on a manual debug-style follow-up button. `PlayableScreenFocus` and `PlayableScreenLayoutController` add the first reusable screen-state bridge for DefenseFocus, DungeonFocus, inventory overlay, crafting overlay, and reward overlay. The controller moves authored UI RectTransforms between the MVP defense-fullscreen and dungeon-dominant 70/30 layouts, reports whether optional overlay objects are wired, refuses to enter an invisible overlay state when a panel is missing, and can apply a target gameplay focus before opening an overlay so close actions return to the intended post-run focus. `PlayableLoopHud` can auto-find it, request DungeonFocus on dungeon start, restore DungeonFocus when a saved/loaded expedition is already Running, return to DefenseFocus when the room resolves, open reward overlay over DefenseFocus on clear, prioritize severe defense alerts or high pressure while the player is in DungeonFocus, and disable overlay buttons until the matching GameObjects exist. `PanelCameraRenderTarget` and `DungeonViewportInputRouter` are the P0-B bridge for a RenderTexture-style dungeon panel: the first binds a camera into a `RawImage` and exposes binding state, while the second converts clicks in that image into rays from the same camera before forwarding them to `PlayerController`; when both are on the same object, the router can inherit the camera from the render target. `InventoryOverlayPresenter` is the first player-facing inventory overlay content bridge: it fills authored TMP labels with item rows, selected-item details, wallet/materials, salvage preview, Rare reroll-cost preview when available, and action messages, then exposes Previous/Next/Latest/Equip/Salvage/Close methods for normal UI buttons. `RewardOverlayPresenter` is the matching reward-reveal content bridge for a wired reward overlay: it shows pending/claimed dungeon reward state, loot source, latest reward item details, wallet/material preview, claim reward, open inventory, equip reward, salvage reward, and close-overlay actions. `CraftingOverlayPresenter` is the first crafting overlay content bridge: it lists item instances, prefers the newest rerollable Rare item when opened, shows reroll-ready count, material guidance, and last-reroll result feedback, previews salvage and Rare reroll costs, salvages selected items, spends reroll materials, and writes one prototype affix roll onto selected Rare items; the current prototype reroll avoids repeating that item's saved affix when another slot-valid candidate exists. The three overlay presenters now resynchronize event subscriptions when auto-found references appear or change, so inventory, reward, wallet, and equipped-stat changes continue to refresh the visible overlay text during the normal path. The current `Gameplay` scene includes first-pass inventory/reward/crafting overlay layouts and wiring plus the dungeon RawImage/camera/render-target/input-router bridge; final camera crop, overlay density, item icons, scroll behavior, reward reveal art, crafting panel art, ornate treatment, and dungeon camera framing remain Unity Editor work. The debug OnGUI HUDs remain smoke-test fallbacks only.

Phase D HUD note: `PlayableLoopHud` now shows active depth plus `SelectedDepth/HighestUnlockedDepth`, and `Gameplay` wires normal `Depth -` / `Depth +` buttons. The controls disable at the unlocked bounds and while an expedition is running.

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

- `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1`: safe daily verification harness for Codex automation. It runs the solution build, `git diff --check`, required `Gameplay.unity` scene-contract checks, P0-B dungeon panel bridge checks, P0-C ground actor runtime wiring checks, missing-script scan, optional overlay wiring warning, automation-plan freshness checks, and local automation TOML health checks without invoking Unity batchmode.
- `Tools/Automation/Get-PrototypeDebtInventory.ps1`: scans source files for prototype/debug/fallback/temporary markers so daily automation can keep `GameDesign/ProductionDocs/12_PrototypeDebtRegister.md` current instead of letting MVP bridges harden silently.
- `Tools/Automation/Export-DungeonDepthBalance.ps1`: exports and validates the shared dungeon depth threat/reward/material curves without invoking Unity batchmode.

## Rule

Prefer component composition over inheritance.

Examples:

- player hero = `CharacterActor + PlayerController`
- auto hero = `CharacterActor + AutoCombatController`
- enemy = `CharacterActor + EnemyAIController`

`CharacterActor` is the body and component hub. Controllers decide. Motor and combat execute.
