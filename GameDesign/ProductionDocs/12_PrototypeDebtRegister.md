# Prototype Debt Register

Created: 2026-06-04

Purpose: keep prototype, debug, fallback, and temporary-MVP code visible enough that automation can decide whether to keep it at the edge, promote it into production code, replace it, or delete it before alpha hardening.

This document is not a cleanup wish list. It is the automation-owned register for code that is allowed during MVP because it unblocks validation, but should not quietly become permanent core architecture.

## Automation Contract

Every autonomous production run must:

1. Run `Tools/Automation/Get-PrototypeDebtInventory.ps1 -SummaryOnly` near the start of the run.
2. Read this register before adding new prototype, debug, fallback, or temporary-MVP behavior.
3. Add a new register row when the scan finds a meaningful new marker that is not already covered here.
4. Update `Last automation action` when a debt item is promoted, replaced, deleted, intentionally kept, or newly blocked by a Unity Play Mode decision.
5. During Phase D, fold debt retirement into the production feature that replaces it. Do not select standalone cleanup ahead of persistent progression unless the debt blocks the normal path or verification.
6. Treat unresolved `Alpha blocker` rows as blockers before entering an alpha/early-access hardening phase.

Automation may keep an item when it is isolated at the edge, named clearly as prototype/debug/fallback, and not required by the normal player path. Automation should prefer promotion/replacement when a prototype branch lives inside a central system such as combat, item generation, save/load, or player input.

## Classification

| Decision | Meaning | Automation behavior |
| --- | --- | --- |
| Keep at edge | Useful for smoke tests or emergency diagnostics, not part of the normal path. | Keep isolated, do not expand unless a regression requires it. |
| Promote | The behavior is fun or necessary and should become production architecture. | Add a focused task that replaces prototype naming/data with real contracts. |
| Replace | The behavior is useful only as a bridge and should be superseded by authored content, data, or a real system. | Keep until the replacement path passes, then disable or remove from normal scenes. |
| Delete | The behavior is no longer needed after a validated replacement. | Remove in a narrow cleanup run with verification. |
| Needs decision | Cleanup depends on product direction, camera/feel judgment, or large economy rules. | Document options; do not guess. |

## Current Register

| ID | Surface | Kind | Current owner/files | Why it is allowed now | Promotion or retirement trigger | Target decision | Alpha blocker | Last automation action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| TD-01 | OnGUI debug HUDs and smoke test buttons | Debug | `Assets/02.Scripts/Dungeon/UI/DungeonDebugHud.cs`, `Assets/02.Scripts/Items/UI/InventoryDebugHud.cs`, `Assets/02.Scripts/Dungeon/DungeonLoopSmokeTest.cs` | They provide fast regression checks outside the normal Canvas/TMP path. | Phase C cumulative acceptance confirms the normal path does not require them. | Keep at dev/test edge; hide from production scenes and do not expand without a regression need. | No while the normal path remains independent. | 2026-06-06: Phase C closed; retained only as edge regression tooling. |
| TD-02 | Prototype combat simulation fallback | Fallback | `Assets/02.Scripts/Dungeon/CombatRoom.cs` | It kept the early dungeon loop testable before spawned enemies were reliable. | P0-D accepted the real prefab combat/reward/retry path on 2026-06-06. | Keep at dev/test edge. Normal `Gameplay` disables it and must fail visibly if the real enemy path breaks. | No while production scenes keep it disabled; yes if re-enabled silently. | 2026-06-06: P0-D accepted; `simulateWhenNoEnemies` disabled in normal `Gameplay`, with a harness contract preventing silent reactivation. |
| TD-03 | Runtime prototype loot reward fallback | Fallback | `Assets/02.Scripts/Items/LootDropper.cs`, `Assets/02.Scripts/Items/ItemDefinition.cs` | It prevents dead-end tests when authored item tables are empty, and the HUD exposes `LastRewardSource`. | D0-C production registry exists and production scenes disable the fallback. | Keep only as explicit empty-table dev safety; production scenes fail visibly. | No while production scenes keep it disabled; yes if silently re-enabled. | 2026-06-09: `Gameplay` disables the fallback and the harness enforces the setting. |
| TD-04 | Prototype Rare affix reroll | Prototype | `Assets/02.Scripts/Items/ItemInstance.cs`, `Assets/02.Scripts/UI/CraftingOverlayPresenter.cs` | It proves the first real material sink and UI feedback path without waiting for a full affix system. | Phase D depth/reward bands establish item-level requirements, then authored affix pool/tag/weight work replaces prototype generation. | Promote the player-facing sink, replace prototype affix generation with authored data. | Yes, if still the only affix system at alpha. | 2026-06-06: Phase C flow accepted; replacement remains Phase D itemization work. |
| TD-05 | Prototype dungeon room visuals and tint | Prototype/Fallback | `Assets/02.Scripts/Dungeon/DungeonRoomPresenter.cs` | It makes the first room readable before authored room gates, spawn cues, lighting, VFX, and reward reveal exist. | P0-D accepts the room shell and combat feel, or an authored room prefab replaces it. | Replace with authored room presentation; keep tint only as dev diagnostic. | Yes, if prototype tint/shell is the final room read. | 2026-06-04: Registered for automation sweep. |
| TD-06 | MVP screen/camera/layout values and viewport QA copy | Temporary MVP values/diagnostic | `GameDesign/ProductionDocs/11_PlayableScreenPresentationSpec.md`, `Assets/02.Scripts/UI/PlayableScreenLayoutController.cs`, `Assets/02.Scripts/UI/PanelCameraRenderTarget.cs`, `Assets/02.Scripts/UI/PlayableLoopHud.cs` | Temporary values and render/input status copy unblocked P0-B camera/readability validation before final art direction. | P0-B accepted the current camera/layout/input checkpoint on 2026-06-04; revisit presentation only for regression or P1 production UI. | Keep accepted layout values as first-slice defaults. Hide or remove viewport QA copy from the normal player HUD before production presentation hardening. | Yes, only if QA copy remains visible in the production HUD at alpha. | 2026-06-05: P0-B accepted; removed from active tuning priority and QA copy registered for later removal. |
| TD-07 | Runtime prototype item restore and item registry gap | Prototype/Fallback | `Assets/02.Scripts/Items/SimpleInventory.cs`, `Assets/02.Scripts/Items/ItemDefinitionRegistry.cs`, `Assets/02.Scripts/Shared/GameSaveDataDiagnostics.cs`, item/save docs | It kept saved runtime rewards usable before a production registry existed. | D0-C registry, schema-v3 migration, and unresolved-id quarantine are complete. | Closed for authored items. Preserve unknown records but block gameplay actions until an explicit id migration resolves them. | No for authored items; legacy runtime snapshots remain quarantined data. | 2026-06-09: Replaced opportunistic lookup/snapshot restoration with the production registry and explicit migration diagnostics. |
| TD-08 | Fixed-slot ground actor projection and blockout presentation | Temporary MVP bridge | `Assets/02.Scripts/GroundDefense/Runtime/GroundDefenseActorRuntime.cs`, `Assets/02.Scripts/GroundDefense/UI/GroundDefenseCombatPresenter.cs`, `Assets/01.Scenes/Gameplay.unity` | It proved that continuous frontline telemetry can drive readable individual hits, defeats, travel, and wall contacts without manual wave lists. | Phase D D1-A formula-driven ground scaling is ready to add pooled actors/archetype data, or final actor content work begins. | Preserve the useful event/telemetry contract; replace fixed slots and scene blockouts with pooled prefabs, archetype data, real targeting/death, and reusable feedback. Do not polish the current blockout. | Yes, if fixed slots/blockout objects remain the normal production combat path at alpha. | 2026-06-06: Kept frozen; replacement is tied to D1-A instead of another blockout pass. |

## Automation Run Notes

- 2026-06-04: Created the register and paired it with `Tools/Automation/Get-PrototypeDebtInventory.ps1`. Current debt cleanup is tracked but does not displace P0-B camera/readability or the remaining Phase C P0 combat/normal-path gates.
- 2026-06-05: P0-B camera/layout values were accepted as first-slice defaults. Added TD-08 so the accepted P0-C behavior bridge cannot silently turn into final fixed-slot defense architecture. The next active production gate is P0-D dungeon prefab combat.
- 2026-06-06: TD-02 advanced without deleting the fallback prematurely. The normal spawned-enemy path now validates the melee prefab contract and NavMesh placement before `CombatRoom` can accept tracked enemies; the retirement trigger remains the focused P0-D Play Mode pass. The scan is now 77 markers across 17 files (`Debug 52`, `Fallback 7`, `Prototype 18`), down from 79 because duplicate per-component warning calls were consolidated.
- 2026-06-06: User accepted P0-D. TD-02 is now kept only at the dev/test edge; normal `Gameplay` disables prototype combat simulation and the verification harness enforces that production-scene contract.
- 2026-06-06: Stagnation audit promoted Phase D and prohibited standalone debt cleanup as the default next run. TD-01 remains at the dev/test edge; TD-03/TD-07 integrate with the Phase D item registry; TD-04 integrates with Phase D itemization; TD-08 remains frozen until formula-driven ground scaling.
- 2026-06-07: D0-A depth progression added no new prototype/debug/fallback path. The inventory remains 77 markers across 17 files (`Debug 52`, `Fallback 7`, `Prototype 18`); save schema v2 and HUD depth controls are production progression work, not new registered debt.
- 2026-06-08: D0-B depth balance bands added no new prototype/debug/fallback marker. The inventory remains 77 markers across 17 files (`Debug 52`, `Fallback 7`, `Prototype 18`); the formula model, runtime stat scaling, depth-scaled rewards, and export/check path are production systems.
- 2026-06-09: D0-C advances TD-03 and closes the authored-item portion of TD-07. Normal `Gameplay` disables runtime fallback rewards; schema-v3 registry migration reconnects canonical/legacy ids; unresolved records are visible but cannot provide equipment or salvage value. The scan is 78 markers across 17 files (`Debug 54`, `Fallback 7`, `Prototype 17`); the net +1 comes from explicit unresolved-load/salvage warnings while one old snapshot-restore prototype marker was removed.
