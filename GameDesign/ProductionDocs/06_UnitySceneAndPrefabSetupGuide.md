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
