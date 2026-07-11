# Playable Loop Production Plan

## Purpose and authority

This is the daily production queue. `13_ReleaseReadinessAndProductionGates.md` defines the product gates, `14_CompleteGameProductionBacklog.md` defines the complete-game production order, and this file selects the next bounded implementation task. When they disagree, the newer explicit gate decision in `13` wins, then the ordered P0 work in `14` wins over a local HUD or verification follow-up.

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
| Phase E - Presentable Combat Vertical Slice | In progress | Connect actual models and animation to direct combat, add readable enemy behavior and a first authored dungeon map, then prove one complete session. |

**Current phase | Phase E - Presentable Combat Vertical Slice**

**Last meaningful movement:** The 2026-07-10 user confirmation accepts E3-A HUD quick toggles and first-session QA as a completed regression-only surface. On 2026-07-11 the queue was corrected: it no longer treats that validation as the active production task. `E3-B` is now the first P0 production task, followed by direct-combat telegraphs, an authored dungeon map, and a complete playable session in `14_CompleteGameProductionBacklog.md`. E2-A and E2-B remain accepted and regression-only.

**Next unlock:** `E3-B | P0 | Combat model and animation binding | Next / Product work`. Inspect the available Hero and first-enemy art assets, rig/clip imports, and live prefab owners; then bind actual model/Animator states to movement, attack, hit, and death without replacing combat authority. If suitable approved source assets are absent, record that precise asset dependency once and continue only deterministic animator/prefab/state work. Do not add HUD text, another settings control, or E2-B comparison work while E3-B remains open.

## Current product queue

| ID | Priority | Task | Status | Completion evidence |
| --- | --- | --- | --- | --- |
| E1-A | P0 | Formula-driven dungeon contract choice | Done / User accepted Play Mode validation | Before an expedition, offer two generated choices from a starter set of three. Each states threat and reward-depth effects, applies to that run, survives save/load, and resolves in HUD/result text. | User accepted the focused `Gameplay` Play Mode path on 2026-06-25: contract A/B/refresh -> start -> clear/fail -> reward -> save/load, including the defense restore check. Reopen only for contract/save/reward regressions. |
| E1-B | P0 | Authored Rare affix pool | Done / User accepted Play Mode validation | Replace prototype reroll output with data-backed tags, weights, slot rules, and clear stat text. | User accepted the focused `Gameplay` Play Mode path on 2026-06-26: reward -> equip -> reroll -> save/load. Six authored Rare affixes in `ItemEconomyModel.AuthoredRareAffixes`, slot-specific weighted reroll, current-affix avoidance when alternatives exist, readable crafting text, and `GameDesign/Balance/RareAffixPool.csv` are complete. |
| E1-C | P1 | Reusable dungeon encounter variety | Done / User accepted Play Mode validation | Add one elite rule and one boss/encounter rule without hand-authored room ladders. | User accepted the focused `Gameplay` Play Mode path on 2026-06-27: next encounter text -> start run -> elite/boss active text -> clear/fail -> reward -> save/load. `DungeonEncounterModel` defines `crypt_skirmish`, `elite_guard`, and `tomb_warden`; schema v5 stores selected/active encounter ids; `ExpeditionDirector.GetEffectiveDepthBalance(...)` applies encounter HP/damage/reward-depth modifiers; HUD and room/spawn messages name the active encounter; `DungeonEncounterBalance.csv` exports the denominator. |
| E2-A | P1 | Onboarding, settings, recovery | Done / User accepted recovery guidance | Teach the first-session loop after E1-A makes a real decision. | User confirmed the E2-A recovery guidance check on 2026-07-01. Normal HUD uses compact status plus `Next:` guidance for start frontline -> compare contracts -> run/fail/reward -> equip/salvage -> first recovery save. Save schema v6 persists HUD text density, balance-detail visibility, diagnostic text visibility, first-session guide, and first-recovery-save emphasis. The `Load` button remains clickable before the first save, and `DefenseSaveManager`/`PlayableLoopHud` share the same no-save recovery copy under harness coverage. Reopen only for first-session recovery, no-save load, or settings-restore regressions. |
| E2-B | P1 | Goal comparison clarity | Done / User accepted Play Mode validation | Help a fresh player name the better next choice after recovery works. | Contract comparison is accepted from the 2026-07-03 user-confirmed check. Latest reward item comparison is accepted from the 2026-07-05 user-confirmed check, including guide-off priority for unresolved or unequipped latest rewards. User confirmed the defense-upgrade recommendation, Wall shortfall, post-purchase return guidance, and save/load path on 2026-07-08. Reopen only for a comparison-copy, reward-decision, defense-upgrade, or save/load regression. |
| E3-A | P2 | HUD settings quick toggles and first-session QA checklist | Done / User accepted Play Mode validation | Keep the accepted text-density/guide toggles and fresh-save checklist as regression evidence only. | User confirmed the focused HUD settings path on 2026-07-10 before `1163416 Add E3-A HUD settings quick toggles` was published. This does not authorize a broad settings menu or more HUD guidance work. |
| E3-B | P0 | Combat model and animation binding | Next / Product work | Bind actual Hero and first-enemy models, rigs, clips, and Animator states to the live direct-combat movement/attack/hit/death path. | `Gameplay` Play Mode shows the live combat actors, not detached decoration, in the five required states. Asset/source approval, prefab ownership, save behavior, and manual Editor setup are recorded in `14`. |
| E3-C | P0 | Direct-combat behavior and attack telegraphs | Pending / After E3-B | Add one reusable elite/boss action with a readable wind-up, avoidable threat, actual hit timing, failure, and reward resolution. | The action is state/data driven and changes what the player does; a tint, multiplier, or HUD-name-only variation is not acceptance. |
| E3-D | P0 | Authored dungeon map vertical slice | Pending / After E3-C | Build one intended entrance -> route -> arena -> reward/exit dungeon map with collision, NavMesh, spawn, input, camera, and failure return. | The visual composition is manually approved in Unity; prefabs, spawns, save behavior, and setup steps are verifiable. |
| E3-E | P0 | First complete playable session | Pending / After E3-D | Connect frontline -> contract -> direct combat/boss -> reward -> equipment/crafting -> defense investment -> save/load into a self-explanatory 20-30 minute session. | A focused fresh-save Play Mode path proves the session without developer HUD instructions and leaves a clear next goal. |

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

After a baseline is accepted, the next task must add a normal-path decision, encounter, reward, sink, persistence behavior, direct-combat behavior, model/animation feedback, or playable map step. Do not use camera adjustment, diagnostics, static inspection, or HUD-only status text as a substitute for a product gate.

## Prototype Debt Sweep Rule

Use `12_PrototypeDebtRegister.md` and `Tools/Automation/Get-PrototypeDebtInventory.ps1` to remove a prototype once its replacement is accepted. Do not preserve disabled components, unused prefabs, or review-only UI solely because they once helped validation.

## No-Stagnation Rules

1. Closed gates are regression-only.
2. Do not spend consecutive runs on documentation, smoke tests, debug HUDs, or helper scripts unless a real regression/build blocker requires it.
3. For item/drop changes, consult the local D2 reference first and adapt pacing/sinks without importing trading or ladder assumptions.
4. Prefer generated rules, formula bands, data tables, reusable encounter templates, salvage, and conversion over manual ladders and one-off content.
5. Keep user-visible text factual: remove review labels and diagnostics from normal play once their decision has closed.
6. Treat E3-A as regression-only. While E3-B through E3-E are open, a daily run must advance the current P0 model/animation, combat, map, or complete-session task or report a real asset/Editor blocker.

## Loop coverage and blockers

| Loop link | Current state | Next gap |
| --- | --- | --- |
| Automatic defense -> base resources | Working and accepted | Future defense rewards need meaningful strategy, not more visual polish. |
| Dungeon entry -> direct combat | Working with E1-A contract UI and E1-C reusable encounter rules | E3-B through E3-D must replace the one-room presentation with model/animation feedback, a real avoidable action, and one authored playable map before new text work. |
| Dungeon reward -> inventory/equipment/salvage | Working with authored affix output, encounter reward-depth offsets, accepted E2-A recovery, accepted E2-B comparison, and accepted E3-A QA | After E3-E proves the complete session, F4 must add actual build choices and sinks rather than another comparison line. |
| Crafting -> material sink | Authored affix reroll accepted | F4 must add locking/upgrades, conversion, filtering, or other scalable sinks only after consulting the D2 reference and defining denominators. |
| Save/load -> progression recovery | Done for the current scoped loop: depth/items/defense/contracts/affixes/encounters restore, first-save HUD copy explains the recovery point, schema v6 persists current HUD text-density/guide settings, no-save load guidance is shared by `DefenseSaveManager` and the HUD, and the two normal HUD quick toggles now change the saved settings | Reopen only for a recovery regression or a failed HUD settings quick-toggle validation. |

Known blockers: no source-code blocker. E3-A is accepted and must not receive another HUD micro-slice without a regression. E3-B may expose a real external dependency if the project contains no approved Hero/enemy model, rig, or animation source; that dependency must name the exact required assets and cannot redirect work to UI text. The optional automation prompt update still needs user approval before the TOML freshness warnings can disappear.

## Verification and documentation

- Run `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` after code, scene, or production-document work.
- Treat harness success as structural verification. Use a short Play Mode path when behavior, input, layout, camera, or combat readability changes.
- Sync this plan, `13_ReleaseReadinessAndProductionGates.md`, the relevant system spec, `06_UnitySceneAndPrefabSetupGuide.md`, `09_BaseScriptUsageGuide.md`, and `ScriptFolderStructure.md` in the same run when their contract changes.
