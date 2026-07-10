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
| `DungeonEncounterModel` | E1-C starter encounters, deterministic elite/boss generation, denominator/export source | Do not solve encounter variety with hand-authored room ladders, scene-only multipliers, or a calculation-only expedition path. |
| `EnemySpawner` | Spawned dungeon enemies | Failure and completion must remain visible. |
| `LootDropper` / `SimpleInventory` | Rewards, inventory, duplicate conversion, salvage link | Production scenes must not silently use fallback rewards. |
| `ItemDefinitionRegistry` | Authored definitions and migration IDs | Unknown saved IDs remain visible/quarantined. |
| `ItemEconomyModel` / `ItemSalvageService` | Salvage, authored Rare affix pool, and material sinks | Drop or affix changes state a denominator, preserve a sink, and keep an export path. |

## Normal-player UI ownership

| Component | Responsibility |
| --- | --- |
| `PlayableLoopHud` | Compact current frontline/dungeon state, meaningful actions, depth selection, first-session guidance, recovery copy, E2-B contract/latest-item/defense-upgrade comparison, and normal status. |
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

## E1-C implementation boundary

The first E1-C implementation adds a reusable encounter core. `DungeonEncounterModel` defines `crypt_skirmish`, `elite_guard`, and `tomb_warden`; `ExpeditionDirector` stores selected/active encounter ids in `DungeonSaveData`; `GetEffectiveDepthBalance(...)` stacks encounter HP/damage/reward-depth effects with the selected depth and contract; and `PlayableLoopHud` shows next/active encounter consequences in normal Dungeon text.

Do not solve E1-C with a hidden scene multiplier, one-off room ladder, new reward path, manual wave list, or boss art/layout decision made outside the Unity Editor. Use `Tools/Automation/Export-DungeonEncounters.ps1` when ids, multipliers, reward-depth offsets, or generation rules change.

## E2-A first-session guide boundary

The first E2-A pass keeps onboarding inside `PlayableLoopHud` normal `Next:` guidance. It routes fresh saves through frontline start, contract comparison, dungeon run/failure, reward handling, equip/salvage, and the first recovery save without adding a separate tutorial overlay or scene object. The default HUD status uses compact text and keeps detailed balance/diagnostic output behind code toggles.

The 2026-06-29 E2-A settings persistence pass adds `UiSettingsSaveData` to schema v6. `PlayableLoopHud.CreateUiSettingsSaveData()` snapshots compact text, detailed balance text, diagnostic status text, first-session guide, and first recovery-save emphasis; `PlayableLoopHud.ApplyUiSettingsSaveData(...)` restores them through `DefenseSaveManager` load. `ToggleCompactStatusText()`, `ToggleDetailedBalanceText()`, `ToggleDiagnosticStatusText()`, and `ToggleFirstSessionGuide()` are public actions for future UI wiring.

The 2026-06-30 E2-A recovery pass keeps `loadButton` enabled whenever `DefenseSaveManager` is present, even before a save exists. This is intentional: `PlayableLoopHud.LoadGame()` owns the no-save player guidance and must stay reachable during a fresh-save onboarding check.

The 2026-07-01 E2-A recovery hardening centralizes that no-save copy in `DefenseSaveManager.NoSaveRecoveryGuidance`. `PlayableLoopHud.LoadGame()` reuses the same string, so normal load clicks and save-manager load reports cannot drift into raw save-file paths.

The scoped E2-A path is accepted from the 2026-07-01 user-confirmed recovery guidance check. A full player-facing settings menu/control set remains separate product scope and must not be added just because the current HUD settings persist.

## E2-B goal comparison boundary

The first E2-B pass keeps goal comparison inside the existing contract decision path. `DungeonContractModel.FormatGoalComparisonText(...)` compares the selected contract against the other offered contract using listed threat and reward-depth offset. `PlayableLoopHud.BuildSelectedContractGoalText()` shows the result in the compact contract block, contract select/refresh messages, and first-session `Next:` hint. This contract comparison copy is accepted from the 2026-07-03 user-confirmed check.

The 2026-07-04 E2-B latest-item pass keeps reward comparison inside `PlayableLoopHud` and `EquipmentSlots`. `PlayableLoopHud.BuildLatestItemComparisonText()` reads the latest resolved reward item and `EquipmentSlots.GetEquippedItem(...)` for the same slot, then formats the normal Item line and `Next:` hint around empty-slot equip, power upgrade, sidegrade, or salvage-spare decisions.

The 2026-07-05 E2-B priority hardening keeps that latest-item decision ahead of new-contract guidance through `PlayableLoopHud.TryBuildLatestItemDecisionHint(...)`. This matters when `showFirstSessionGuide` is disabled by saved HUD settings: an unequipped or unresolved latest item should still tell the player what to do with the reward before suggesting the next dungeon contract.

The 2026-07-06 E2-B defense-upgrade pass keeps upgrade comparison inside `PlayableLoopHud` and `DefenseUpgradeModel`. `PlayableLoopHud.BuildDefenseUpgradeComparisonText()` runs only after unresolved/unequipped latest reward handling; it then recommends an affordable Wall, Tower, or Defenders upgrade from existing costs and effect deltas before falling back to the next contract. The 2026-07-07 shortfall pass keeps stressed wall/pressure goals on Wall when Wall is not affordable by showing the missing Gold/Scrap through `FormatMissingRewards(...)`. The 2026-07-08 return-guidance pass makes upgrade clicks reuse shortfall wording when resources are missing and `BuildPostDefenseUpgradeActionText(...)` after a successful purchase so the player returns to Hold/Push or the next contract. User confirmation on 2026-07-08 accepts the E2-B defense-upgrade comparison path. `DefenseUpgradeModel` exposes read-only per-upgrade gains for HUD text; it does not change upgrade formulas or costs.

Do not solve E2-B with a new settings menu, debug label, separate contract economy, item-drop denominator change, scene-only tutorial object, new save field, or a second defense-upgrade economy.

## E3-A settings and QA boundary

E3-A adds two normal HUD settings quick toggles in `Gameplay`: `Text: Compact/Detailed` calls `PlayableLoopHud.ToggleCompactStatusText()`, and `Guide: On/Off` calls `PlayableLoopHud.ToggleFirstSessionGuide()`. These reuse the existing `UiSettingsSaveData`, `PlayableLoopHud.CreateUiSettingsSaveData()`, and `PlayableLoopHud.ApplyUiSettingsSaveData(...)` save/load path instead of adding a second settings save file.

The approved visible scope is only text density (`compact`/`detailed`) and first-session guide (`on`/`off`). `ToggleDetailedBalanceText()` and `ToggleDiagnosticStatusText()` remain QA/code toggles unless the user approves a debug settings surface. Use the `06_UnitySceneAndPrefabSetupGuide.md` E3-A HUD settings quick toggles path and R3 checklist for focused Play Mode confirmation.

## Defense save/load boundary

`DefenseRuntimeState` remains the saved authority for frontline level, state, mode, wall health, pressure, progress, and elapsed time. Loading a save applies that state through `DefenseDirector.ApplySaveData(...)`, emits `SaveDataApplied`, and lets `GroundDefenseNavMeshBattlefield` rebuild presentation actors from the restored state. Visual actor positions are not saved; they must never become a second defense simulation or keep damaging the wall when `DefenseRuntimeState.IsRunning` is false.

## Verification

- Run `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` after changing a live script or wiring contract.
- Add focused checks for new data/save contracts.
- For E2-A UI settings, verify schema-v6 save/load structurally through the harness and use the `06_UnitySceneAndPrefabSetupGuide.md` settings persistence path for Play Mode confirmation.
- For E2-A fresh-save recovery regressions, use the `06_UnitySceneAndPrefabSetupGuide.md` path and include the pre-save `Load` click; the button should show guided no-save copy rather than being disabled.
- For E3-A readiness, use the `06_UnitySceneAndPrefabSetupGuide.md` HUD settings quick toggles path and R3 first-session QA checklist before claiming the build-handoff path is accepted.
- Run `Tools/Automation/Export-DungeonContracts.ps1` when contract ids/effects change; the harness checks it in `-CheckOnly` mode.
- Run `Tools/Automation/Export-DungeonEncounters.ps1` when encounter ids/effects change; the harness checks it in `-CheckOnly` mode.
- Run `Tools/Automation/Export-RareAffixes.ps1` when Rare affix ids, weights, stat formulas, tags, or slot rules change; the harness checks it in `-CheckOnly` mode.
- Use Play Mode for player input, reward flow, combat feedback, or presentation changes.
