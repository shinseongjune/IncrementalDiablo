# Base Script Usage Guide

## Ground-defense ownership

| Component | Responsibility | Do not use it for |
| --- | --- | --- |
| `DefenseRuntimeState` | Authoritative pressure, wall, state, resources, progression, save/load, offline state | Visual-only actor state or a second economy. |
| `DefenseDirector` | Starts/ticks the runtime and receives battlefield wall damage | Unit control or authored waves. |
| `GroundDefenseBalanceModel` | Formula bands, balance knobs, deterministic export | Hand-authored ladders. |
| `GroundDefenseNavMeshBattlefield` | Builds live ground/NavMesh actors, faction readability, autonomous force, reinforcement, wall bridge, and save-load visual rebuilds | Player orders, review-only level switching, visual actors attacking while defense is not running, or a separate simulation. |
| `GroundDefenseNavMeshUnit` | Autonomous targeting, movement, combat, death, reinforcement, and attacker-to-target feedback | Persisting unit positions or rewards. |
| `GroundDefenseBillboardUtility` | Runtime role sprites and readable faction treatment | A second presentation stack. |

The deleted ground-presentation stack is not an alternative implementation. It must not be restored without an explicit new production decision.

## Dungeon and progression ownership

| Component | Responsibility | Contract to preserve |
| --- | --- | --- |
| `ExpeditionDirector` | Dungeon state, room outcome, contract selection/active state, reward handoff, save recovery | Active contracts persist across running/reward-pending saves and must use the existing reward path. |
| `DungeonDepthBalanceModel` | Formula-driven depth threat/reward bands and export | No manual depth ladder as the default scaling solution. |
| `DungeonContractModel` | E1-A starter contracts, deterministic two-offer generation, denominator/export source | Do not hide contract effects in scene-only values or a second reward system. |
| `EnemySpawner` | Spawned dungeon enemies | Failure and completion must remain visible. |
| `LootDropper` / `SimpleInventory` | Rewards, inventory, duplicate conversion, salvage link | Production scenes must not silently use fallback rewards. |
| `ItemDefinitionRegistry` | Authored definitions and migration IDs | Unknown saved IDs remain visible/quarantined. |
| `ItemEconomyModel` / `ItemSalvageService` | Salvage, authored Rare affix pool, and material sinks | Drop or affix changes state a denominator, preserve a sink, and keep an export path. |

## Normal-player UI ownership

| Component | Responsibility |
| --- | --- |
| `PlayableLoopHud` | Current frontline/dungeon state, meaningful actions, depth selection, and normal status. |
| `PlayableScreenLayoutController` | Defense/dungeon focus and overlay visibility safety. |
| `PanelCameraRenderTarget` | Camera-to-`RawImage` viewport bridge. |
| `DungeonViewportInputRouter` | Converts dungeon viewport clicks to player input. |
| `InventoryOverlayPresenter`, `RewardOverlayPresenter`, `CraftingOverlayPresenter` | Player-facing item/reward/crafting content and actions. |

Keep QA diagnostics, render binding state, review labels, and temporary test controls out of normal HUD text once their validation purpose is closed.

## E1-A implementation boundary

The first E1-A implementation adds a pre-run dungeon contract core. Contract definitions live in `DungeonContractModel`, selected/active ids live in `DungeonSaveData`, and `ExpeditionDirector.GetEffectiveDepthBalance(...)` applies threat/reward-depth effects to the active run. The normal HUD exposes and selects the two generated offers through `SelectFirstDungeonContract()`, `SelectSecondDungeonContract()`, and `RefreshDungeonContractOffer()`; `Gameplay` wires those actions to the compact contract A/B/refresh buttons.

Do not solve E1-A with hidden scene multipliers, a hard-coded one-off room, a replacement depth system, rarity changes, or a reward path that bypasses `LootDropper`, duplicate conversion, and salvage.

## E1-B implementation boundary

The first E1-B implementation replaces prototype Rare reroll output with `ItemEconomyModel.AuthoredRareAffixes`. `CraftingOverlayPresenter.RerollSelectedAffix()` spends the existing `ItemDefinition.AffixRerollCost`, then calls `ItemInstance.TryApplyAuthoredAffixReroll(...)`. The reroll writes one saved `ItemAffixRoll`, avoids the current authored affix id when another slot-valid candidate exists, and formats result text through `ItemEconomyModel.FormatAffixRoll(...)`.

Do not solve E1-B with scene-only stat values, debug item seeding, silent rarity changes, a second crafting currency, or an affix result that cannot be exported through `Tools/Automation/Export-RareAffixes.ps1`.

## Defense save/load boundary

`DefenseRuntimeState` remains the saved authority for frontline level, state, mode, wall health, pressure, progress, and elapsed time. Loading a save applies that state through `DefenseDirector.ApplySaveData(...)`, emits `SaveDataApplied`, and lets `GroundDefenseNavMeshBattlefield` rebuild presentation actors from the restored state. Visual actor positions are not saved; they must never become a second defense simulation or keep damaging the wall when `DefenseRuntimeState.IsRunning` is false.

## Verification

- Run `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` after changing a live script or wiring contract.
- Add focused checks for new data/save contracts.
- Run `Tools/Automation/Export-DungeonContracts.ps1` when contract ids/effects change; the harness checks it in `-CheckOnly` mode.
- Run `Tools/Automation/Export-RareAffixes.ps1` when Rare affix ids, weights, stat formulas, tags, or slot rules change; the harness checks it in `-CheckOnly` mode.
- Use Play Mode for player input, reward flow, combat feedback, or presentation changes.
