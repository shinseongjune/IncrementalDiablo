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
5. Avoid selecting debt cleanup ahead of Phase C P0 gates unless the cleanup directly unblocks camera/readability, combat feel, normal reward/inventory/crafting flow, or build verification.
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
| TD-01 | OnGUI debug HUDs and smoke test buttons | Debug | `Assets/02.Scripts/Dungeon/UI/DungeonDebugHud.cs`, `Assets/02.Scripts/Items/UI/InventoryDebugHud.cs`, `Assets/02.Scripts/Dungeon/DungeonLoopSmokeTest.cs` | They provide fast Play Mode regression checks while normal Canvas/TMP paths are still being accepted. | After P0-F full-loop acceptance, normal path should not require these surfaces. | Keep at edge, then build-gate, hide from normal scene, or move to test-only scene. | No, unless normal play still depends on them after P0-F. | 2026-06-04: Registered for automation sweep. |
| TD-02 | Prototype combat simulation fallback | Fallback | `Assets/02.Scripts/Dungeon/CombatRoom.cs` | It kept the early dungeon loop testable before spawned enemies were reliable. It is already blocked when `EnemySpawner` reports setup blockers. | P0-D accepts `PF_DungeonEnemy_Melee` spawn, activation, NavMesh chase/attack, HP feedback, and reward clear path. | Replace in normal `Gameplay`; keep only as dev/test fallback if still useful. | Yes, if active in the normal room path after P0-D/P0-F. | 2026-06-04: Registered for automation sweep. |
| TD-03 | Runtime prototype loot reward fallback | Fallback | `Assets/02.Scripts/Items/LootDropper.cs`, `Assets/02.Scripts/Items/ItemDefinition.cs` | It prevents dead-end tests when authored item tables are empty, and the HUD exposes `LastRewardSource`. | P1-B confirms authored tier-1 reward/equip/salvage/reroll flow and item registry expectations. | Keep as empty-table safety during Phase C; before alpha, disable for production scenes or fail loudly when authored tables are missing. | Yes, if production scenes can silently use prototype rewards. | 2026-06-04: Registered for automation sweep. |
| TD-04 | Prototype Rare affix reroll | Prototype | `Assets/02.Scripts/Items/ItemInstance.cs`, `Assets/02.Scripts/UI/CraftingOverlayPresenter.cs` | It proves the first real material sink and UI feedback path without waiting for a full affix system. | P0-E/P1-B confirms normal crafting flow, then Phase D itemization starts affix pool/tag/weight work. | Promote the player-facing sink, replace prototype affix generation with authored data. | Yes, if still the only affix system at alpha. | 2026-06-04: Registered for automation sweep. |
| TD-05 | Prototype dungeon room visuals and tint | Prototype/Fallback | `Assets/02.Scripts/Dungeon/DungeonRoomPresenter.cs` | It makes the first room readable before authored room gates, spawn cues, lighting, VFX, and reward reveal exist. | P0-D accepts the room shell and combat feel, or an authored room prefab replaces it. | Replace with authored room presentation; keep tint only as dev diagnostic. | Yes, if prototype tint/shell is the final room read. | 2026-06-04: Registered for automation sweep. |
| TD-06 | MVP screen/camera/layout values and viewport QA copy | Temporary MVP values/diagnostic | `GameDesign/ProductionDocs/11_PlayableScreenPresentationSpec.md`, `Assets/02.Scripts/UI/PlayableScreenLayoutController.cs`, `Assets/02.Scripts/UI/PanelCameraRenderTarget.cs`, `Assets/02.Scripts/UI/PlayableLoopHud.cs` | Temporary values and render/input status copy unblocked P0-B camera/readability validation before final art direction. | P0-B accepted the current camera/layout/input checkpoint on 2026-06-04; revisit presentation only for regression or P1 production UI. | Keep accepted layout values as first-slice defaults. Hide or remove viewport QA copy from the normal player HUD before production presentation hardening. | Yes, only if QA copy remains visible in the production HUD at alpha. | 2026-06-05: P0-B accepted; removed from active tuning priority and QA copy registered for later removal. |
| TD-07 | Runtime prototype item restore and item registry gap | Prototype/Fallback | `Assets/02.Scripts/Items/SimpleInventory.cs`, `Assets/02.Scripts/Shared/GameSaveDataDiagnostics.cs`, item/save docs | It keeps saved prototype rewards from becoming dead inventory before a production item registry exists. | Phase D item-definition registry/drop-table export/import begins. | Replace with real registry and migration rules; keep diagnostics for invalid saves. | Yes, if save/load still depends on unresolved runtime prototype definitions at alpha. | 2026-06-04: Registered for automation sweep. |
| TD-08 | Fixed-slot ground actor projection and blockout presentation | Temporary MVP bridge | `Assets/02.Scripts/GroundDefense/Runtime/GroundDefenseActorRuntime.cs`, `Assets/02.Scripts/GroundDefense/UI/GroundDefenseCombatPresenter.cs`, `Assets/01.Scenes/Gameplay.unity` | It proved that continuous frontline telemetry can drive readable individual hits, defeats, travel, and wall contacts without manual wave lists. | Ground-defense production resumes after P0-D/P0-F, or final actor content/prefab work begins. | Preserve the useful event/telemetry contract; replace fixed slots and scene blockouts with pooled prefabs, archetype data, real targeting/death, and reusable feedback. Do not polish the current blockout. | Yes, if fixed slots/blockout objects remain the normal production combat path at alpha. | 2026-06-05: User accepted P0-C behavior and explicitly froze further temporary presentation work. |

## Automation Run Notes

- 2026-06-04: Created the register and paired it with `Tools/Automation/Get-PrototypeDebtInventory.ps1`. Current debt cleanup is tracked but does not displace P0-B camera/readability or the remaining Phase C P0 combat/normal-path gates.
- 2026-06-05: P0-B camera/layout values were accepted as first-slice defaults. Added TD-08 so the accepted P0-C behavior bridge cannot silently turn into final fixed-slot defense architecture. The next active production gate is P0-D dungeon prefab combat.
