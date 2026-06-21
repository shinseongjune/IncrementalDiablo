# Prototype Debt Register

## Purpose

Track only temporary, debug, fallback, or prototype behavior that still exists. A retired surface belongs in the short retirement record, not in the active queue.

## Automation Contract

1. Run `Tools/Automation/Get-PrototypeDebtInventory.ps1 -SummaryOnly` when changing prototype/debug/fallback behavior.
2. Add a row only for a live surface that could affect normal production behavior.
3. Give each row an owner, replacement trigger, target decision, alpha-blocker state, and last action.
4. Delete a replacement path once its production replacement is accepted; disabled code is not an acceptable long-term retirement state.
5. Treat an `Alpha blocker` row as a real blocker before alpha/early-access hardening.

## Current Register

| ID | Surface | Kind | Owner/files | Target decision | Alpha blocker | Last action |
| --- | --- | --- | --- | --- | --- | --- |
| TD-01 | OnGUI smoke-test HUDs and buttons | Debug | `Assets/02.Scripts/Dungeon/UI/DungeonDebugHud.cs`, `Assets/02.Scripts/Items/UI/InventoryDebugHud.cs`, `Assets/02.Scripts/Dungeon/DungeonLoopSmokeTest.cs` | Keep at the dev/test edge; never make normal play depend on it. | No | 2026-06-06: normal Canvas path accepted independently. |
| TD-02 | Dungeon combat simulation fallback | Fallback | `Assets/02.Scripts/Dungeon/CombatRoom.cs` | Keep only as explicit dev/test safety; production scenes fail visibly when real enemy wiring breaks. | No while disabled in production scenes | 2026-06-06: real spawned-enemy path accepted. |
| TD-03 | Runtime fallback loot | Fallback | `Assets/02.Scripts/Items/LootDropper.cs`, `ItemDefinition.cs` | Keep only for empty-table development safety; normal scenes must never silently award fallback loot. | No while production fallback is disabled | 2026-06-09: registry/migration production path established. |
| TD-04 | Prototype Rare affix reroll | Prototype | `Assets/02.Scripts/Items/ItemInstance.cs`, `Assets/02.Scripts/UI/CraftingOverlayPresenter.cs` | Replace with authored affix data: tags, weights, slot rules, clear stat text, migration. | Yes | 2026-06-21: assigned to E1-B. |
| TD-05 | Tint-led dungeon room presentation | Prototype | `Assets/02.Scripts/Dungeon/DungeonRoomPresenter.cs` | Replace with reusable encounter/elite/boss presentation and readable room consequences. | Yes | 2026-06-21: assigned to E1-C. |
| TD-06 | Temporary screen/camera layout values | Temporary MVP values | `PlayableScreenLayoutController.cs`, `PanelCameraRenderTarget.cs` | Keep accepted defaults until a production UI pass; do not expose implementation diagnostics in normal HUD text. | No | 2026-06-21: viewport/review diagnostics removed from normal player text. |

## Retired record

| ID | Retired surface | Decision | Evidence |
| --- | --- | --- | --- |
| TD-08 | Legacy ground lane, actor projection, pooling/presentation stack, review-only density override, and player-facing review diagnostics | Delete | 2026-06-21: actual NavMesh battlefield was accepted; legacy scripts, prefab/assets, scene components, review controls, and stale HUD text were removed. The harness now requires those scene components to be absent. |

## Current production link

- E1-A must not add a new prototype contract path; selected contract data and active-run/save state must be production-owned.
- E1-B retires TD-04 through an authored affix pool.
- E1-C retires TD-05 through reusable encounter presentation.
- A green harness is structural verification. It does not close a debt row without normal-path evidence.
