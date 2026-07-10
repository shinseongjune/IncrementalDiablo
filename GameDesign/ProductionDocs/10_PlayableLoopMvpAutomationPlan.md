# Playable Loop Production Plan

## Purpose and authority

This is the daily production queue. `13_ReleaseReadinessAndProductionGates.md` defines the product gates; this file selects the next bounded implementation task. When they disagree, the newer explicit gate decision in `13` wins.

The product is an offline PC incremental action RPG:

```text
automatic frontline -> dungeon risk/reward choice -> direct combat -> equipment/crafting -> stronger next goal
```

The ground layer is **RTS-readable automatic defense**. It is not an RTS control game: no individual unit orders, production queues, free tower placement, worker economy, manual waves, or a second progression model.

## Progress Tracker

| Area | Status | Accepted evidence / next state |
| --- | --- | --- |
| Phase C playable-screen bridge | Done | Canvas/overlay and viewport bridge exist; normal play no longer depends on debug HUDs. |
| Phase D progression foundation | Done | D0-A depth progression, D0-B depth bands, D0-C registry/migration, D0-D duplicate conversion, and D1-A ground scaling are complete. |
| E0-A automatic defense battlefield | Done / E0-A3 accepted | Actual NavMesh battlefield, readable actor ownership, formula-driven density, and wall-damage authority are accepted. |
| E0-B defense composition | Done / User accepted camera composition | The accepted far-side enemy to lower protected wall composition is regression-only. |
| Phase E - Early Access Readiness Slice | In progress | Build the two-hour repeatable dungeon decision loop and retire alpha-blocking item prototypes. |

**Current phase | Phase E - Early Access Readiness Slice**

**Last meaningful movement:** E3-A HUD settings quick toggles were implemented on 2026-07-10 by exposing the existing persisted text-density and first-session guide settings as normal HUD buttons. The 2026-07-09 first-session QA checklist remains the R3 validation route. E2-B goal comparison clarity remains user-confirmed from 2026-07-08 and is regression-only. E2-A remains accepted from the 2026-07-01 user-confirmed recovery guidance check.

**Next unlock:** `E3-A | P1 | HUD settings quick toggles and first-session QA checklist | In progress / HUD quick toggles implemented`. Stop adding more E2-B HUD comparison micro-slices unless a regression is reported. The next evidence is a focused `Gameplay` Play Mode check: click `Text: Compact/Detailed` and `Guide: On/Off`, save, change them again, load, and confirm the saved HUD setting returns without crowding the bottom HUD row. After that evidence, decide whether E3-A is accepted as the R3 build-handoff path or whether to start a manual visual-authoring pass for boss/room presentation.

## Current product queue

| ID | Priority | Task | Status | Completion evidence |
| --- | --- | --- | --- | --- |
| E1-A | P0 | Formula-driven dungeon contract choice | Done / User accepted Play Mode validation | Before an expedition, offer two generated choices from a starter set of three. Each states threat and reward-depth effects, applies to that run, survives save/load, and resolves in HUD/result text. | User accepted the focused `Gameplay` Play Mode path on 2026-06-25: contract A/B/refresh -> start -> clear/fail -> reward -> save/load, including the defense restore check. Reopen only for contract/save/reward regressions. |
| E1-B | P0 | Authored Rare affix pool | Done / User accepted Play Mode validation | Replace prototype reroll output with data-backed tags, weights, slot rules, and clear stat text. | User accepted the focused `Gameplay` Play Mode path on 2026-06-26: reward -> equip -> reroll -> save/load. Six authored Rare affixes in `ItemEconomyModel.AuthoredRareAffixes`, slot-specific weighted reroll, current-affix avoidance when alternatives exist, readable crafting text, and `GameDesign/Balance/RareAffixPool.csv` are complete. |
| E1-C | P1 | Reusable dungeon encounter variety | Done / User accepted Play Mode validation | Add one elite rule and one boss/encounter rule without hand-authored room ladders. | User accepted the focused `Gameplay` Play Mode path on 2026-06-27: next encounter text -> start run -> elite/boss active text -> clear/fail -> reward -> save/load. `DungeonEncounterModel` defines `crypt_skirmish`, `elite_guard`, and `tomb_warden`; schema v5 stores selected/active encounter ids; `ExpeditionDirector.GetEffectiveDepthBalance(...)` applies encounter HP/damage/reward-depth modifiers; HUD and room/spawn messages name the active encounter; `DungeonEncounterBalance.csv` exports the denominator. |
| E2-A | P1 | Onboarding, settings, recovery | Done / User accepted recovery guidance | Teach the first-session loop after E1-A makes a real decision. | User confirmed the E2-A recovery guidance check on 2026-07-01. Normal HUD uses compact status plus `Next:` guidance for start frontline -> compare contracts -> run/fail/reward -> equip/salvage -> first recovery save. Save schema v6 persists HUD text density, balance-detail visibility, diagnostic text visibility, first-session guide, and first-recovery-save emphasis. The `Load` button remains clickable before the first save, and `DefenseSaveManager`/`PlayableLoopHud` share the same no-save recovery copy under harness coverage. Reopen only for first-session recovery, no-save load, or settings-restore regressions. |
| E2-B | P1 | Goal comparison clarity | Done / User accepted Play Mode validation | Help a fresh player name the better next choice after recovery works. | Contract comparison is accepted from the 2026-07-03 user-confirmed check. Latest reward item comparison is accepted from the 2026-07-05 user-confirmed check, including guide-off priority for unresolved or unequipped latest rewards. User confirmed the defense-upgrade recommendation, Wall shortfall, post-purchase return guidance, and save/load path on 2026-07-08. Reopen only for a comparison-copy, reward-decision, defense-upgrade, or save/load regression. |
| E3-A | P1 | HUD settings quick toggles and first-session QA checklist | In progress / HUD quick toggles implemented | Move beyond E2-B HUD comparison work toward R3 readiness. | 2026-07-09: the reproducible R3 first-session QA checklist was drafted in `06_UnitySceneAndPrefabSetupGuide.md`. 2026-07-10: `Gameplay` now exposes the existing persisted HUD text-density and first-session guide settings through two small normal HUD buttons, without adding debug/diagnostic settings, a new save field, or a broad settings menu. Remaining evidence is focused Play Mode validation of button click -> label/message update -> save/load restore -> bottom-row readability. |

## Closed foundation

- `D0-A | P0 | Save-backed dungeon depth progression | Done`
- `D0-B | P0 | Formula-driven depth threat and reward bands | Done`
- `D0-C | P0 | Item registry and save migration | Done`
- `D0-D | P0 | Duplicate-item sink and conversion | Done`
- `D1-A | P1 | Formula-driven ground scaling | Done`
- `E0-A | P0 | RTS-readable automatic defense battlefield | Done / E0-A3 accepted`
- `E0-B | P0 | Defense camera and reference composition pass | Done / User accepted camera composition`

Closed work is not a default task source. Reopen it only for a reported regression or a deliberate contract change.

## Phase Promotion Rule

Promote a task only when its normal player path has a player-visible delta, connects to a reward/sink/failure/next-goal loop, has a reusable scale rule, states persistence behavior, and has focused evidence. A green harness alone is structural verification, not player-value completion.

## Visible Game Production Rule

After a baseline is accepted, the next task must add a normal-path decision, encounter, reward, sink, persistence behavior, or failure/recovery improvement. Do not use camera adjustment, diagnostics, static inspection, or HUD-only status text as a substitute for a product gate.

## Prototype Debt Sweep Rule

Use `12_PrototypeDebtRegister.md` and `Tools/Automation/Get-PrototypeDebtInventory.ps1` to remove a prototype once its replacement is accepted. Do not preserve disabled components, unused prefabs, or review-only UI solely because they once helped validation.

## No-Stagnation Rules

1. Closed gates are regression-only.
2. Do not spend consecutive runs on documentation, smoke tests, debug HUDs, or helper scripts unless a real regression/build blocker requires it.
3. For item/drop changes, consult the local D2 reference first and adapt pacing/sinks without importing trading or ladder assumptions.
4. Prefer generated rules, formula bands, data tables, reusable encounter templates, salvage, and conversion over manual ladders and one-off content.
5. Keep user-visible text factual: remove review labels and diagnostics from normal play once their decision has closed.

## Loop coverage and blockers

| Loop link | Current state | Next gap |
| --- | --- | --- |
| Automatic defense -> base resources | Working and accepted | Future defense rewards need meaningful strategy, not more visual polish. |
| Dungeon entry -> direct combat | Working and accepted with E1-A contract UI and E1-C reusable encounter rules | Future changes should add first-session teaching or deeper encounter presentation, not repeat encounter-core acceptance. |
| Dungeon reward -> inventory/equipment/salvage | Working and accepted with authored affix output, encounter reward-depth offsets, E2-A recovery guidance, accepted E2-B contract comparison, user-confirmed latest-item comparison, accepted defense-upgrade comparison, an E3-A first-session QA checklist, and E3-A HUD settings quick toggles | E2-B is closed unless reward comparison, defense-upgrade guidance, or save/load regresses. The next gap is validating the R3-facing HUD settings toggle path, not adding another comparison line. |
| Crafting -> material sink | Authored affix reroll accepted | Future crafting work should add affix locking/upgrades only after onboarding explains the current loop. |
| Save/load -> progression recovery | Done for the current scoped loop: depth/items/defense/contracts/affixes/encounters restore, first-save HUD copy explains the recovery point, schema v6 persists current HUD text-density/guide settings, no-save load guidance is shared by `DefenseSaveManager` and the HUD, and the two normal HUD quick toggles now change the saved settings | Reopen only for a recovery regression or a failed HUD settings quick-toggle validation. |

Known blockers: no source-code blocker. E2-B is accepted and should not receive another comparison micro-slice without a regression. E3-A now has the fresh-save QA checklist and HUD settings quick toggles; it still needs focused Play Mode confirmation for click behavior, save/load restore, and bottom-row readability before acceptance. The optional automation prompt update still needs user approval before the TOML freshness warnings can disappear.

## Verification and documentation

- Run `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` after code, scene, or production-document work.
- Treat harness success as structural verification. Use a short Play Mode path when behavior, input, layout, camera, or combat readability changes.
- Sync this plan, `13_ReleaseReadinessAndProductionGates.md`, the relevant system spec, `06_UnitySceneAndPrefabSetupGuide.md`, `09_BaseScriptUsageGuide.md`, and `ScriptFolderStructure.md` in the same run when their contract changes.
