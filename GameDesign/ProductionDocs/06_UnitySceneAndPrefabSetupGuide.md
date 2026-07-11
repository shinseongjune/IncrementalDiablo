# Unity Scene and Prefab Setup Guide

## Scope

This guide describes only live `Gameplay` scene contracts. It intentionally excludes deleted presentation prototypes, review overrides, and obsolete scene recipes.

## Gameplay scene: required live roots

| Scene object / feature | Required live component or contract | Notes |
| --- | --- | --- |
| `DefenseRoot` | `DefenseDirector`, `GroundDefenseNavMeshBattlefield` | Automatic NavMesh battle; no legacy lane/pool/presenter components. |
| `Camera_DefensePanel` | Defense viewport source camera | Enemy approach is far/top; wall and defenders are lower/protected. Treat these anchors as accepted unless a regression is reported. |
| `RawImage_DefenseViewport` | `RawImage` + `PanelCameraRenderTarget` | Must reference the defense camera and its own RawImage. |
| `Camera_DungeonPanel` | Dungeon viewport source camera | Feeds direct-control dungeon view. |
| `RawImage_DungeonViewport` | `RawImage` + `PanelCameraRenderTarget` + `DungeonViewportInputRouter` | Router must reference the RawImage, viewport camera, player, and screen layout controller. |
| Gameplay Canvas | `PlayableLoopHud`, `PlayableScreenLayoutController` | Normal player flow only; no viewport diagnostics or review labels. |
| Dungeon root | `ExpeditionDirector`, `EnemySpawner`, player, reward/inventory/crafting bridges | Must maintain direct combat -> reward -> inventory/salvage/crafting flow. |

## Deterministic setup rules

1. Keep `DefenseRoot` free of the superseded ground-presentation stack. It was replaced and deleted; the harness verifies its absence.
2. Keep `GroundDefenseNavMeshBattlefield` wired to `DefenseDirector`, `Camera_DefensePanel`, enemy spawn anchor, wall anchor, and `GroundDefense_ReadabilitySheet`.
3. Keep `PlayableLoopHud` wired to depth navigation and normal overlays. Do not add status fields for camera/render/input checks unless a temporary QA path is explicitly needed outside normal play.
4. Keep both named viewport `RawImage` objects connected through `PanelCameraRenderTarget`; the harness validates the serialized bridges.
5. When editing a scene externally while Unity is open, reopen or reload `Gameplay` before Play Mode.

## E1-A contract-button wiring

The E1-A contract core is implemented in scripts and `Gameplay` now contains three normal-player contract controls under the Dungeon status area:

- `Button_DungeonContractA` / label `Contract A`
- `Button_DungeonContractB` / label `Contract B`
- `Button_DungeonContractRefresh` / label `Refresh`

Required `PlayableLoopHud` Inspector fields:

| Field | Intended action |
| --- | --- |
| `Select Contract A Button` | Assigned to `Button_DungeonContractA`; calls `PlayableLoopHud.SelectFirstDungeonContract()` at runtime for the first offered contract. |
| `Select Contract B Button` | Assigned to `Button_DungeonContractB`; calls `PlayableLoopHud.SelectSecondDungeonContract()` at runtime for the second offered contract. |
| `Refresh Dungeon Contract Button` | Assigned to `Button_DungeonContractRefresh`; calls `PlayableLoopHud.RefreshDungeonContractOffer()` at runtime to advance the deterministic offer seed before a run. |

Validation path:

1. Open `Gameplay`.
2. If Unity reports an external scene-file change, reload `Gameplay` before Play Mode; do not discard unrelated unsaved scene edits.
3. Enter Play Mode and confirm the Dungeon text shows two offers.
4. Click `Contract A`, `Contract B`, and `Refresh`; confirm selection/result text changes and buttons disable while a run is active.
5. Keep `Start Dungeon Button` unchanged. Starting a run uses the currently selected contract.
6. Start the run, clear or fail, save/load, and confirm the active contract/result text persists.

## E1-A regression validation path

Use this path only when contract selection, save/load, or defense restore regresses. E1-A was accepted on 2026-06-25:

1. Open `Gameplay`; reload the scene if Unity reports external file changes.
2. Enter Play Mode and read the top-left Dungeon line. Expected: two contract offers and one selected contract are visible.
3. Click `Refresh`, then `Contract A` or `Contract B`. Expected: the selected contract line changes.
4. Start the defense if it is stopped, let the wall/progress visibly change, click `Save`, then immediately change defense mode or wait for a visible wall/progress difference.
5. Click `Load`. Expected: the Frontline line reports the saved FL/state/mode/wall/progress, the defense battlefield rebuilds from that restored state, and visual units do not keep attacking if the restored state is not running.
6. Start a dungeon run from the selected contract, finish or fail it, then save/load once more. Expected: contract result/reward state and defense state both remain coherent.

Do not move camera anchors, dungeon room geometry, or viewport proportions for this wiring pass unless the user explicitly approves a layout pass.

## E1-B regression validation path

Use this path only when authored Rare affix reroll, stat refresh, or save/load regresses. E1-B was accepted on 2026-06-26:

1. Open `Gameplay`; reload the scene if Unity reports external file changes.
2. Enter Play Mode and earn or select a Rare item through the normal dungeon/reward/inventory path.
3. Equip the Rare item, then open the crafting overlay and select the same item.
4. Confirm the reroll cost is `Gold + Essence + AlterStone`, then press `Reroll`.
5. Expected: the result text shows an authored affix name such as `Wounding Edge`, `Vital Plating`, `Swift Band`, or `Runner Band`, plus a clear stat modifier. If the item already had an authored affix and another slot-valid candidate exists, the new affix id should change.
6. Save and load. Expected: the item remains resolved, the authored affix id/text and modifier remain coherent, and equipped stat text refreshes after load/equip.

## E1-C encounter regression validation path

Use this path only when reusable encounter text, threat/reward effects, or save/load regresses. E1-C was accepted on 2026-06-27:

1. Open `Gameplay`; reload the scene if Unity reports external file changes.
2. Enter Play Mode and read the Dungeon line. Expected: `Next encounter` names `Crypt Skirmish`, `Elite Guard`, or `Tomb Warden` with HP, damage, and reward-depth effects.
3. Select/refresh a contract, then start a dungeon. Expected: the start message and Dungeon line name the active encounter and keep the selected contract visible.
4. Clear or fail the room. Expected: the result text includes the same active encounter, and reward depth includes the contract plus encounter reward-depth offset.
5. Save and load during a ready state and, if practical, during a running or reward-pending state. Expected: selected/active encounter ids remain valid and the loaded Dungeon line still names the correct encounter.
6. Repeat starts until `Elite Guard` or `Tomb Warden` appears. Expected: the threat multipliers change without adding manual room-list or camera/layout changes.

Do not move room geometry, spawn anchors, cameras, or HUD placement for this validation unless the user explicitly asks for a visual-authoring pass.

## E2-A fresh-save guide regression path

E2-A was accepted from the 2026-07-01 user-confirmed recovery guidance check. Use this path only when first-session onboarding/recovery regresses:

1. Back up or temporarily move the existing local save outside Unity if you need a true fresh-save path.
2. Open `Gameplay`; reload the scene if Unity reports external file changes.
3. Enter Play Mode. Expected: the normal `Next:` line tells the player to start the frontline, then compare the two contracts and start the selected dungeon.
4. Click `Load` before saving. Expected: the button is clickable, and the HUD says there is no save yet and explains the frontline -> contract -> reward -> save path, not the raw persistent-data file path.
5. Start the frontline, select/refresh a contract, start a dungeon, then clear or fail the room. Expected: the `Next:` line explains reward claim or failure recovery.
6. After a reward, equip or salvage the latest item, then click `Save`. Expected: the `Next:` line and save message say the recovery point covers frontline, dungeon, inventory, equipment, and HUD settings.

This path validates the accepted first-session copy and recovery flow. It does not require a new scene object or layout change.

## E2-A/E3-A HUD settings persistence validation path

Use this path when verifying the schema-v6 settings persistence slice or the E3-A HUD settings quick toggles:

1. Open `Gameplay`; reload the scene if Unity reports external file changes.
2. Enter Play Mode and find the two bottom-row HUD settings quick toggles: `Text: Compact/Detailed` and `Guide: On/Off`.
3. Click `Text: Compact/Detailed`. Expected: the button label flips between compact and detailed, and the message says to save to keep the setup.
4. Click `Guide: On/Off`. Expected: the button label flips between guide on and off, and the normal `Next:` line respects the selected guide state.
5. Click `Save`. Expected: the message says the recovery point covers HUD settings.
6. Change either setting again, then click `Load`. Expected: the saved HUD text-density/guide state returns, and the load message includes `HUD settings restored`.

These are the only approved E3-A HUD settings quick toggles. `ToggleDetailedBalanceText()` and `ToggleDiagnosticStatusText()` remain QA/code toggles unless a separate debug settings surface is approved.

## E2-B contract comparison regression path

Use this path only when the accepted contract comparison copy regresses. It was user-accepted on 2026-07-03:

1. Open `Gameplay`; reload the scene if Unity reports external file changes.
2. Enter Play Mode and start the frontline if it is stopped.
3. Read the compact Dungeon contract block. Expected: it shows two offered contracts, the selected contract, and one `Goal:` line that explains safer clear/recovery versus higher reward-depth risk.
4. Click `Contract A`, `Contract B`, and `Refresh`. Expected: the selected contract and `Goal:` line change together, and the `Next:` hint repeats the practical reason to start, switch, or refresh before entering.
5. Start the selected dungeon. Expected: the active contract still appears in the Dungeon line, the run uses the selected contract/encounter path, and no diagnostic labels or new settings controls appear.

This path validates player-facing copy density and choice clarity. It does not require moving HUD objects, adding scene controls, changing item drops, or changing contract economy values.

## E2-B latest item comparison regression path

Use this path only when the accepted E2-B reward-item comparison regresses. It was user-confirmed on 2026-07-05:

1. Open `Gameplay`; reload the scene if Unity reports external file changes.
2. Enter Play Mode, start the frontline if needed, choose any contract, and clear a dungeon room until a normal reward item reaches the latest Item line.
3. Expected Item line: it includes one compact `Compare:` phrase such as empty slot, equipped, positive Power delta, sidegrade, or equipped item higher.
4. Expected `Next:` line: it tells the player whether to equip the latest item, fill an empty slot, treat it as an affix/material sidegrade, or keep the current item and salvage the spare.
5. Optional priority check: turn `Show First Session Guide` off on `PlayableLoopHud` during Play Mode, then confirm the same unequipped latest item still produces the item decision `Next:` hint before next-contract guidance.
6. Equip or salvage the latest item, then save and load. Expected: inventory/equipment state and the comparison/action hint remain coherent after load.

This path validates reward decision clarity only. It does not require moving HUD objects, changing item drops, changing salvage yields, adding a settings menu, or changing scene wiring.

## E2-B defense upgrade comparison regression path

Use this path only when the accepted E2-B defense-upgrade comparison regresses. It was user-confirmed on 2026-07-08:

1. Open `Gameplay`; reload the scene if Unity reports external file changes.
2. Enter Play Mode, start the frontline if needed, and make sure the latest reward item is either equipped, salvaged, or absent so item handling no longer owns the `Next:` line.
3. Shortfall check: while wall health or pressure is stressed and Wall is not yet affordable, read the compact `Next:` line. Expected: it says how much Gold/Scrap is still missing for Wall, not a cheaper Tower/Defenders detour.
4. Let Gold/Scrap accumulate until at least one Wall, Tower, or Defenders upgrade button is interactable.
5. Expected `Next:` line: it names one affordable upgrade, states the reason in current terms such as wall/pressure relief or higher DPS gain, and then routes back to Push or the next contract.
6. Buy the named upgrade. Expected: the compact Frontline upgrade levels update, the click feedback says to Hold/Push or start the next contract, and the `Next:` line returns to the next useful loop step without diagnostic labels.
7. Save and load. Expected: Wall/Tower/Defenders levels and the upgraded-state HUD copy remain coherent after load.

This path validates goal-comparison copy only. It does not require changing upgrade costs, defense formulas, scene layout, camera composition, item drops, reward denominators, or save schema.

## E3-A first-session QA regression checklist

Use this checklist only for a reproducible E3-A regression check after E2-B acceptance and after the HUD settings quick toggles are in the scene. It does not block E3-B combat model/animation, E3-C combat behavior, E3-D authored map, or E3-E complete-session production. The route is fresh-save -> no-save Load -> contract -> run -> reward -> equip/salvage -> defense-upgrade -> HUD settings -> save/load:

1. Back up or temporarily move the existing local save so the run starts from a true fresh save.
2. Open `Gameplay`; reload the scene if Unity reports external file changes.
3. Enter Play Mode and click `Load` before saving. Expected: the button is clickable, and the HUD shows no-save Load guidance for frontline -> contract -> reward -> save instead of a raw file path.
4. Start the frontline. Expected: the compact Frontline line shows wall/pressure/progress and the `Next:` line points to contract choice or the next useful loop step.
5. Click `Contract A`, `Contract B`, and `Refresh`. Expected: the selected contract and `Goal:` line change together without diagnostic labels.
6. Start the selected dungeon and clear or fail the room. Expected: the Dungeon line keeps the active contract/encounter readable; failure explains recovery, while a clear routes to reward claim.
7. Claim the reward, then equip or salvage the latest item. Expected: the Item `Compare:` line and `Next:` hint resolve before new-contract or defense-upgrade guidance takes over.
8. Let Gold/Scrap accumulate until a named Wall/Tower/Defenders upgrade is affordable, then buy it. Expected: upgrade levels update, missing-resource copy is short when unaffordable, and successful purchase routes back to Hold/Push or the next contract.
9. Click `Text: Compact/Detailed` and `Guide: On/Off`, then click `Save`. Change either setting again and click `Load`. Expected: the saved HUD setting returns, the load report includes `HUD settings restored`, and frontline/dungeon/inventory/equipment state remains coherent.
10. Open and close the Inventory, Crafting, and Reward overlays. Expected: focus returns predictably, normal HUD text stays compact, and diagnostic status remains hidden.

This checklist is accepted E3-A regression evidence. The approved visible settings scope is only the two HUD settings quick toggles for text density and first-session guide. It does not approve a full settings menu, debug settings surface, boss silhouettes, room geometry, camera movement, VFX, reward denominator changes, or save-schema changes.

## Visual-authoring boundary

Do not autonomously change room size, camera framing, HUD placement, object scale, silhouette, or composition based on source text alone. For a necessary manual pass, provide:

- exact GameObject and component;
- fixed versus adjustable fields and starting values/ranges;
- intended spatial relationship and feedback;
- a short Play Mode validation path.

The accepted defense composition is not a routine tuning target. Revisit it only when a visual regression is reported.

## Manual validation paths

### Defense regression check

1. Open `Gameplay` and enter Play Mode.
2. Confirm far-side enemies approach lower-side defenders/wall without player unit control.
3. Confirm one readable attacker-to-target exchange, wall hit, death/reinforcement, and no player-facing diagnostic/review text.
4. Exit Play Mode without saving scene changes unless deliberately changing a scene contract.

### Dungeon viewport/input check

1. Enter Dungeon focus from the normal HUD flow.
2. Confirm `RawImage_DungeonViewport` receives the dungeon camera image.
3. Click within the viewport and confirm the player receives the intended world input.
4. Clear a room and confirm normal reward flow, not a debug-only follow-up.

## Verification

Run `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` after scene/prefab/script wiring changes. It checks missing scripts, live/deleted component contracts, named viewport bridges, and build-safe serialized references; it does not replace the manual visual checks above.
