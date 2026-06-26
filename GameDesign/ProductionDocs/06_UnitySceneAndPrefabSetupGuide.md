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
