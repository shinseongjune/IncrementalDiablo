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

**Last meaningful movement:** E2-A now has settings persistence for the existing HUD text-density and first-session guide choices. Save schema v6 stores `UiSettingsSaveData`; `DefenseSaveManager` snapshots/restores it through `PlayableLoopHud`; normal save copy now says the recovery point covers HUD settings as well as frontline, dungeon, inventory, and equipment.

**Next unlock:** `E2-A | P1 | Onboarding, settings, recovery | In progress / Settings persistence implemented`. Remaining unlock evidence is focused fresh-save/recovery Play Mode QA, plus a product decision before adding a full player-facing settings menu beyond the current HUD text-density/guide toggles.

## Current product queue

| ID | Priority | Task | Status | Completion evidence |
| --- | --- | --- | --- | --- |
| E1-A | P0 | Formula-driven dungeon contract choice | Done / User accepted Play Mode validation | Before an expedition, offer two generated choices from a starter set of three. Each states threat and reward-depth effects, applies to that run, survives save/load, and resolves in HUD/result text. | User accepted the focused `Gameplay` Play Mode path on 2026-06-25: contract A/B/refresh -> start -> clear/fail -> reward -> save/load, including the defense restore check. Reopen only for contract/save/reward regressions. |
| E1-B | P0 | Authored Rare affix pool | Done / User accepted Play Mode validation | Replace prototype reroll output with data-backed tags, weights, slot rules, and clear stat text. | User accepted the focused `Gameplay` Play Mode path on 2026-06-26: reward -> equip -> reroll -> save/load. Six authored Rare affixes in `ItemEconomyModel.AuthoredRareAffixes`, slot-specific weighted reroll, current-affix avoidance when alternatives exist, readable crafting text, and `GameDesign/Balance/RareAffixPool.csv` are complete. |
| E1-C | P1 | Reusable dungeon encounter variety | Done / User accepted Play Mode validation | Add one elite rule and one boss/encounter rule without hand-authored room ladders. | User accepted the focused `Gameplay` Play Mode path on 2026-06-27: next encounter text -> start run -> elite/boss active text -> clear/fail -> reward -> save/load. `DungeonEncounterModel` defines `crypt_skirmish`, `elite_guard`, and `tomb_warden`; schema v5 stores selected/active encounter ids; `ExpeditionDirector.GetEffectiveDepthBalance(...)` applies encounter HP/damage/reward-depth modifiers; HUD and room/spawn messages name the active encounter; `DungeonEncounterBalance.csv` exports the denominator. |
| E2-A | P1 | Onboarding, settings, recovery | In progress / Settings persistence implemented | Teach the first-session loop after E1-A makes a real decision. | Normal HUD now uses compact status plus `Next:` guidance for start frontline -> compare contracts -> run/fail/reward -> equip/salvage -> first recovery save. Save schema v6 persists HUD text density, balance-detail visibility, diagnostic text visibility, first-session guide, and first-recovery-save emphasis. Focused fresh-save/recovery Play Mode QA remains. |

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
| Dungeon reward -> inventory/equipment/salvage | Working and accepted with authored affix output and encounter reward-depth offsets | Future reward work should teach comparison and recovery before adding new item breadth. |
| Crafting -> material sink | Authored affix reroll accepted | Future crafting work should add affix locking/upgrades only after onboarding explains the current loop. |
| Save/load -> progression recovery | Working for depth/items/defense/contracts/affixes/encounters; first-save HUD copy explains the recovery point; schema v6 persists the current HUD text-density/guide settings | E2-A still needs focused recovery QA from a fresh save. |

Known blockers: no source-code blocker. E2-A still needs focused Play Mode QA from a true fresh-save path. A broader player-facing settings menu needs a product decision on which settings are in scope; the current implementation only persists the existing HUD text-density/guide settings. The optional automation prompt update still needs user approval before the TOML freshness warnings can disappear.

## Verification and documentation

- Run `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` after code, scene, or production-document work.
- Treat harness success as structural verification. Use a short Play Mode path when behavior, input, layout, camera, or combat readability changes.
- Sync this plan, `13_ReleaseReadinessAndProductionGates.md`, the relevant system spec, `06_UnitySceneAndPrefabSetupGuide.md`, `09_BaseScriptUsageGuide.md`, and `ScriptFolderStructure.md` in the same run when their contract changes.
