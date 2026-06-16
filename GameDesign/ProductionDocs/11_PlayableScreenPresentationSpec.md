# Playable Screen Presentation Spec

## 2026-06-16 E0-A2 Readable Ownership

- The accepted defense view keeps the accepted NavMesh battle behavior and changes only actor readability and hit ownership.
- Role-sheet actors should render as cutout sprites rather than dark rectangular cards. Each actor also has a faction base and a shape-coded badge: defender shield versus enemy threat marker.
- Every successful melee or wall hit should briefly show an attacker-to-target line, while the target flashes/recoils. The viewer should be able to name the attacker, target, and hit without relying on HUD diagnostics.
- User Play Mode feedback accepted this path on 2026-06-17. Treat friendly/enemy identity, approach, stop-at-range, attack source, target reaction, death/reinforcement, enemy wall hit, and visible wall-health loss as cumulative evidence unless a later change regresses them.
- E0-A3 may add formula-driven density, but the added count/roles/cadence must not hide the accepted ownership read at actual panel scale.

## 2026-06-15 Live NavMesh Battle Contract

- The defense panel must now explain itself through real behavior: hostile actors enter across visible ground, friendly actors intercept them, both exchange attacks, defeated actors leave, reinforcements return, and surviving hostiles attack the wall.
- Region bands, contact-line rectangles, health-bar-like lines, and stationary proof objects are removed from the normal acceptance path.
- Full and compressed panel acceptance requires readable source, movement destination, target choice, attack contact, death, reinforcement side, and wall damage. HUD text may confirm wall health but cannot substitute for the visible event.
- Current visuals may use the prepared sprite sheet, but the sprites are attached to actual NavMesh actors rather than floating presentation cards.
- Camera framing and final art remain editor-sensitive. The fixed behavioral contract is enemy -> defender -> wall and defender -> enemy, with no individual unit controls.
- 2026-06-15 Play Mode result: the behavioral contract passed, but the lack of recognizable friendly/enemy models still made faction identity difficult. The 2026-06-16 readable-ownership path above was accepted on 2026-06-17.

## 2026-06-15 E0-A1 Sprite Panel Repair

- Enemy, defender, tower, and wall cells now render through `SpriteRenderer` rather than generated UV/material quads.
- The defender faces the enemy. The wall health bar is hidden in `StaticGrammar`, and zone bands use lower alpha so object silhouettes own the read.
- Full and compressed panel acceptance is unchanged: a paused screenshot must identify all four nouns without labels, motion, health bars, or diagnostic copy.
- This static sprite panel repair was superseded by the accepted actual NavMesh battlefield. E0-A2 now validates readable ownership on real actors instead.

## 2026-06-14 E0-A1 Static Panel Contract

- `Gameplay` now selects `GroundDefenseBattlefieldStage.StaticGrammar`.
- The full and compressed defense panels should show one hostile unit in the enemy staging zone, one friendly unit behind the contact line, a fixed tower and wall on the protected side, three ordered ground zones, and grounded footprints/foundations.
- Runtime pooled enemies, melee/projectile events, casualties, and reinforcements are intentionally hidden for this gate. The underlying automatic defense simulation, Hold/Push state, wall health, resources, and progression continue normally.
- User evidence failed this initial quad-rendered contract. Spatial bands and a contact line were visible, but the nouns were not. The 2026-06-15 sprite repair above is the current validation candidate.
- Acceptance still requires one paused screenshot per panel state without diagnostic text. Do not count colored zone rectangles as unit/building readability.

## 2026-06-13 RTS Visual Grammar After Failed Validation

The current panel failed because motion existed without readable nouns or ownership. The next presentation pass must establish the following before adding spectacle.

### Paused-frame test

Without animation, labels, or diagnostic copy, one screenshot must make these immediately identifiable:

- hostile unit;
- friendly unit;
- tower or ranged building;
- wall/citadel;
- enemy staging side;
- contact line;
- protected side.

Faction color alone is not enough. Use silhouette, ground footprint/shadow, body scale, facing, weapon shape, authored zone, and building foundation together.

### Motion test

- Enemy entry must read as movement from an enemy staging zone across ground toward a known contact line.
- Units stop when fighting. They do not continuously slide past targets or move like falling screen-space objects.
- Spawn cadence starts deliberately slow. The viewer must see one unit complete spawn -> approach -> attack/hit or death before density rises.
- Formation density is added only after a one-enemy/one-defender proof works in `DefenseFocus` and the compressed defense panel.

### Attack ownership test

- Melee: visible attacker body winds up and strikes a visible target at contact.
- Tower/ranged: the visible tower aims or winds up; the projectile leaves its muzzle, crosses the battlefield, and impacts a visible enemy.
- Projectile color, trail, and impact are secondary. A projectile-like object near the wall with no visible owner/target is invalid.
- Structure damage must visibly land on the wall. Enemy projectiles or melee strikes must not be confused with friendly tower fire.

### Presentation rejection checklist

Reject the pass immediately when any are true:

- enemies look like objects spawning at the top and falling to the bottom;
- units, tower, and wall share the same card/billboard language or similar scale;
- an attack can be seen but the attacker or target cannot be named;
- movement speed or spawn frequency prevents one complete action from being observed;
- combat is understandable only by watching health bars or diagnostic text;
- the full defense view works but the compressed defense panel loses source-target relationships.

## 2026-06-13 Ground Battlefield Runtime Pass

- The normal defense panel now places pooled enemies in multiple lanes that converge on one contact line, with three friendly defenders between the enemy formation and the protected wall.
- A measured actor hit produces either a defender lunge at that target or a projectile launched from the visible tower. Legacy scene-authored attack pulses are disabled.
- Enemy defeats recycle through the existing pool. Sustained wall damage can knock out one defender, then show that slot reinforcing from the wall side.
- The wall itself shows health loss, hit emphasis, and breach color; the unattached wall flash is disabled.
- Moving pressure markers and normal-player ground-combat diagnostics are disabled. Pressure, wall health, Hold/Push, and alerts remain in the functional HUD.
- User validation failed: the formation read as rapid top-to-bottom movement and the projectile near the wall had no clear owner or purpose. Treat this section as an implementation-history checkpoint, not accepted presentation.

## 2026-06-12 RTS-Readable Automatic Defense Presentation

- The defense view should reproduce the visual hierarchy of `GameDesign/References/2026-05-17_FinalGameplayScreenConcept.png`: enemy formations at the far side, an active contact line in the middle, fixed friendly towers and squads near the protected wall, and the citadel/wall as the final readable defense object.
- `Assets/06.Art/Sprites/GroundDefense/GroundDefense_ReadabilitySheet.png` supplies the current role silhouettes. Treat it as a concept/reference sheet whose figures belong in the battlefield, not as six independent billboard cards.
- The normal player view must show actual combat actions: approach, melee contact, projectile launch and travel, hit reaction, health loss, death, reinforcement, wall attack, and breach danger.
- Remove generic `pulse` vocabulary and abstract pulse objects from the production presentation. Small impact VFX are allowed only when attached to a real projectile, attack, hit, death, or structure-damage event.
- Unit density should create RTS spectacle without destroying readability. Distinct faction colors, silhouettes, spacing, facing, health bars on damaged/selected-priority units, and depth ordering should make the battle understandable at a glance.
- The defense side panel remains automatic and decision-light. It exposes repair, Hold/Push, upgrades, composition/priority decisions, and alerts, but never individual unit selection or movement controls.
- The current pooled billboard/bolt implementation is not a presentation acceptance candidate. E0-A must be reworked toward the battlefield sequence above before Play Mode approval.

## 2026-06-11 Ground Progression Feedback

- The frontline summary now shows the active formula band, pressure multiplier, defense-output multiplier, reward multiplier, next band level, and latest milestone result.
- `DefenseHud` also shows effective defense output plus current Gold/Scrap per minute so an upgrade or band change has a visible numeric result.
- This is functional progression feedback. Do not create a new panel or adjust camera/layout composition for D1-A; Phase E E0-A owns the production actor replacement.

## 2026-06-08 Dungeon Depth Balance Feedback

- The Dungeon HUD line shows the selected depth's balance band while Ready and the active depth's band while Running.
- The compact line exposes enemy HP, enemy damage, reward power, and material-yield multipliers so depth selection communicates a real risk/reward change before the player presses Start.
- This is functional progression feedback, not final typography or panel-density authoring. Do not create another HUD field or layout pass for D0-B unless the added line clips or becomes unreadable in Play Mode.

Created: 2026-05-24
Status: MVP temporary values. This is a production reference for implementation, not final art direction.

## 1. Purpose

This document stores the current target for the main playable screen, camera ownership, HUD regions, and the transition between ground-defense focus and dungeon focus.

It exists so future implementation work does not treat the current ground-defense marker/presenter work as the final defense screen. The target is a single live gameplay screen where dungeon action can become dominant while ground defense stays visible, readable, and mechanically alive.

Source reference:

- `GameDesign/References/2026-05-17_FinalGameplayScreenConcept.md`
- `GameDesign/References/2026-05-17_FinalGameplayScreenConcept.png`

## 2. Core Decision

Use a dual-panel focus layout inside one live runtime.

- Ground defense and dungeon are not separate mutually exclusive game modes.
- `DefenseDirector` and `ExpeditionDirector` continue running while the visible focus changes.
- The player can focus on dungeon combat without losing sight of frontline pressure, wall damage, repair needs, and Hold/Push state.
- Inventory and crafting open as overlays, not permanent always-open management panes.

## 3. MVP Screen States

| State | MVP use | Main visible shape |
| --- | --- | --- |
| `DefenseFocus` | Default state after load and between expeditions. | Ground-defense view fills the main play area. Dungeon entry/status can be shown as a compact control area. |
| `DungeonFocus` | Active direct-control dungeon play. | Dungeon viewport becomes dominant. Ground defense compresses into a persistent side panel. |
| `InventoryOverlay` | Item review, equip, salvage. | Overlay above the current focus. It must not stop the live runtime unless a later pause rule explicitly says so. |
| `CraftingOverlay` | Upgrade, craft, salvage, reroll. | Overlay above the current focus. It should share the same inventory entry points. |
| `DefenseAlert` | Breach, wall low, repair needed, pressure spike. | Temporary visual priority on the defense side panel plus top/bottom alert feedback. |
| `RewardOverlay` | Dungeon clear reward, rare item reveal, material summary. | Short modal or toast layer; should not become the normal inventory screen. |

## 4. MVP Temporary Layout Values

Primary target resolution is PC 16:9. These are starting values for implementation and can be tuned after Unity Play Mode review.

| Region | DefenseFocus MVP value | DungeonFocus MVP value | Notes |
| --- | ---: | ---: | --- |
| Global top bar height | 8% screen height | 8% screen height | Fixed across focus changes. |
| Bottom action bar height | 18% screen height | 18% screen height | Fixed across focus changes. |
| Main play area height | 74% screen height | 74% screen height | Remaining area between top and bottom bars. |
| Defense view width | 100% main area | 30% main area | In DungeonFocus this becomes the persistent side panel. |
| Dungeon view width | 0% hidden/minimized | 70% main area | Dungeon slides in and owns direct-control input. |
| Minimum defense side panel width | 360 px | 360 px | If screen is too narrow, collapse to alert/status strip instead of unreadable controls. |
| Minimum dungeon viewport width | 900 px | 900 px | Below this, reduce side-panel controls before shrinking the dungeon too far. |

Preferred side-panel position: right side.

Reason: the saved final screen concept already uses a dungeon-dominant center/left view with the defense panel on the right, and this keeps click-combat space visually dominant.

## 5. MVP Transition Values

Dungeon entry transition:

1. Player starts direct-control dungeon entry from `DefenseFocus`.
2. Global top bar and bottom action bar remain fixed.
3. Defense view compresses horizontally into the right-side defense panel.
4. Dungeon viewport slides in from offscreen left or center-left and expands to the remaining main area.
5. Dungeon input becomes primary when the transition reaches 70% progress or when the dungeon viewport is fully visible.

Dungeon exit transition:

1. Reward/failure feedback appears first.
2. Dungeon viewport slides or collapses away.
3. Defense panel expands back to the full main play area.
4. Defense input becomes primary after the panel expansion finishes.

MVP timing:

| Parameter | Temporary value |
| --- | ---: |
| Entry duration | 0.38 seconds |
| Exit duration | 0.32 seconds |
| Easing | cubic ease in/out |
| Input lock during transition | 0.15 seconds max |
| Alert interrupt | Defense alerts may flash during transition, but should not cancel dungeon entry unless the wall is already breached. |

## 6. Camera Ownership

MVP camera rule:

- In `DefenseFocus`, the main camera/frame belongs to ground defense.
- In `DungeonFocus`, the main camera/frame belongs to dungeon combat.
- The compressed defense panel may use either a second camera viewport, a RenderTexture, or a UI-driven summarized lane view. The first implementation may choose the simplest safe method, but must preserve the same runtime data mapping.

MVP temporary camera intent:

| View | Starting intent | Adjustable values |
| --- | --- | --- |
| Defense main view | Wide readable frontline: enemy approach, wall/citadel, defenders/tower feedback, pressure direction. | Camera angle, orthographic size/FOV, lane length, object scale, marker count, panel crop. |
| Defense side panel | Compact status plus small live battlefield read. It should show pressure direction, wall health, attack/impact feedback, and breach danger. | Whether it is live camera, RenderTexture, or UI abstraction; panel crop; alert emphasis. |
| Dungeon main view | Diablo-like direct-control combat: hero, enemies, click path, attack range, loot labels, exits, and room boundary must be readable. | Camera angle, zoom, room scale, navmesh feel, label size, VFX intensity. |

Do not final-tune visual feel headlessly. Exact camera framing, object scale, and panel crop require Unity Editor Play Mode review.

## 7. HUD Content Map

Global top bar:

- Account/session level or hero level
- Gold
- Scrap
- Essence or core crafting material
- Rare reroll/crafting material when unlocked
- Frontline Level
- Dungeon depth/floor/room
- Alert icons
- Settings/options entry

Defense side panel:

- Frontline Level
- Frontline progress
- Enemy pressure
- Hold/Push state
- Wall/citadel health
- Repair needed/repair button
- Wall/Tower/Defender upgrade levels
- Breach or low-wall warning
- Ground combat visual status only while the presenter bridge is still being validated

Dungeon viewport:

- Hero and enemies
- Health bars or hit feedback
- Room state
- Click destination/path readability
- Loot labels
- Exit/clear/failure feedback

Bottom action bar:

- Hero HP orb or HP block
- Potion/consumable slots
- Skill slots
- Latest loot strip
- Inventory button
- Crafting button
- Context action prompt/message
- Optional secondary resource orb if the hero resource system exists

## 8. Input And Focus Rules

In `DefenseFocus`:

- Defense repair, Hold/Push, and upgrades are primary.
- Dungeon start/entry is available.
- Dungeon direct-control input is inactive.

In `DungeonFocus`:

- Click movement, targeting, skills, potions, and loot interaction are primary.
- If the dungeon view is shown through a UI `RawImage` or RenderTexture, clicks inside that image must be routed through the camera that rendered the image, not through `Camera.main`.
- If a saved or loaded expedition is already `Running`, the playable screen must restore `DungeonFocus` instead of letting dungeon enemies run behind `DefenseFocus`.
- Defense side panel allows only high-level actions: repair, Hold/Push toggle, and critical upgrade if readable.
- No RTS-like unit micromanagement is allowed from the defense panel.
- Defense alert clicks may shift focus or open a compact repair/upgrade prompt, but should not steal combat input unexpectedly.

Overlay rules:

- Inventory/crafting overlays should open from the bottom action bar.
- Tooltips must avoid covering the hero, enemies, wall-health alert, or rare item reveal.
- Closing an overlay returns to the previous focus state.

## 9. Implementation Notes For MVP

Recommended first implementation shape:

1. Add a lightweight presentation state enum such as `PlayableScreenFocus`.
2. Add a UI controller that owns panel anchors/rects and transitions between `DefenseFocus` and `DungeonFocus`.
3. Keep existing gameplay directors alive. Do not reload scenes to switch focus.
4. Use authored RectTransform anchors for the top bar, bottom action bar, dungeon viewport, and defense side panel.
5. Drive MVP transitions through RectTransform sizes/anchors first, then bind a gameplay camera into the authored panel through camera viewport, RenderTexture, or `PanelCameraRenderTarget`.
6. Keep current debug HUDs as fallback only; the normal player path should move toward Canvas/TMP production UI.

Suggested names:

- `PlayableScreenFocus`
- `PlayableScreenLayoutController`
- `Panel_GlobalTopBar`
- `Panel_BottomActionBar`
- `Panel_DungeonViewport`
- `Panel_DefenseSide`
- `Panel_InventoryOverlay`
- `Panel_CraftingOverlay`
- `Panel_RewardOverlay`

2026-06-07 Phase D depth-selection addition:

- The normal combined HUD now includes compact `Depth -` and `Depth +` controls near the dungeon-start row.
- The Dungeon status text exposes active depth plus selected/highest-unlocked depth, so the player can see the ladder without opening a debug HUD or separate menu.
- Depth selection is disabled while an expedition is running. Clearing the current highest unlocks one next depth; failure does not change the ladder.
- These are conservative first-pass controls for the progression axis. A later dedicated dungeon-selection panel may replace them when multiple dungeon definitions/themes exist, but the saved progression contract should remain.

2026-05-26 implementation bridge:

- `PlayableScreenFocus` now exists with `DefenseFocus`, `DungeonFocus`, `InventoryOverlay`, `CraftingOverlay`, and `RewardOverlay`.
- `PlayableScreenLayoutController` now exists as the first code bridge for this spec. It expects authored RectTransforms for defense and dungeon panels, applies the MVP 70/30 dungeon-focus split by normalized anchors, and toggles overlay GameObjects without reloading scenes.
- `PlayableLoopHud` can auto-find the layout controller. With `Sync Screen Focus With Dungeon` enabled, starting a dungeon requests `DungeonFocus`, and room clear/fail requests `DefenseFocus`.
- This bridge intentionally leaves final panel art, camera framing, split-ratio review, overlay content, and text density to Unity Editor authoring.

2026-05-27 overlay-control bridge:

- `PlayableScreenLayoutController` now reports whether each optional overlay object is wired and will not enter an overlay state when the target GameObject is missing.
- `PlayableLoopHud` now exposes optional button slots for inventory, crafting, reward, and close-overlay actions. It disables those buttons until the corresponding overlay references are present.
- The HUD summary can show the current screen focus and transition progress, giving Play Mode validation a visible text checkpoint alongside the panel movement.
- This still does not author the actual overlay panel contents, tooltip positions, item-list density, or final UI art.

2026-05-28 inventory overlay content bridge:

- `InventoryOverlayPresenter` now provides the first content script for `Panel_InventoryOverlay`.
- It can show item rows, selected-item details, wallet/materials, salvage preview, Rare affix-reroll cost preview when supported by the item definition, and action messages.
- It exposes button-safe Previous/Next/Latest/Equip/Salvage/Close methods and can close through `PlayableScreenLayoutController`, so the overlay returns to the previous gameplay focus.
- This still leaves RectTransform composition, list density, tooltip placement, scroll behavior, icon art, and ornate frame treatment to Unity Editor authoring.

2026-05-29 reward overlay content bridge:

- `RewardOverlayPresenter` now provides the first content script for `Panel_RewardOverlay`.
- It can show pending/claimed dungeon reward state, loot source, latest reward item details, wallet/material preview, claim pending reward, open inventory, equip reward, salvage reward, and close-overlay actions.
- It exposes button-safe Claim Reward/Open Inventory/Equip Reward/Salvage Reward/Close methods and can close through `PlayableScreenLayoutController`, so the overlay returns to the previous gameplay focus.
- `Gameplay` now has a first deterministic RectTransform pass for this overlay: the frame uses `x 0.18-0.82`, `y 0.18-0.86`; summary content occupies the left column, item/material preview occupies the middle, and reward actions occupy a right-side button stack.
- This still leaves reveal animation, icon art, rare-item treatment, and ornate frame density to Unity Editor authoring. Crafting overlay scene content still needs its own Unity Editor wiring pass.

2026-05-30 crafting overlay content bridge:

- `CraftingOverlayPresenter` now provides the first content script for `Panel_CraftingOverlay`.
- It can show item rows, selected item details, wallet/materials, current affixes, salvage preview, Rare reroll cost, salvage selected, reroll selected Rare affix, and close-overlay actions.
- The reroll path spends `ItemDefinition.AffixRerollCost` and replaces the selected Rare item's saved prototype affix roll. This is the first real material sink behind the crafting overlay, not just a preview.
- `Gameplay` now has a first deterministic RectTransform pass for this overlay: the frame uses `x 0.18-0.82`, `y 0.18-0.86`; item rows occupy the left column, selected/material/affix details occupy the right column, and Previous/Next/Latest/Reroll/Salvage/Close actions occupy a bottom button row.
- This still leaves exact text density, scroll behavior, icon art, ornate frame treatment, and Play Mode validation to Unity Editor authoring.

2026-05-31 automatic reward-overlay bridge:

- `PlayableLoopHud` now opens the wired `RewardOverlay` automatically when a dungeon room resolves as cleared and `openRewardOverlayOnDungeonClear` is enabled.
- `PlayableScreenLayoutController.TryOpenOverlayAfterGameplayFocus(...)` applies the intended return gameplay focus before showing an overlay. The reward-clear path opens over `DefenseFocus`, so closing the reward overlay returns to the post-run defense screen instead of a partial dungeon transition.
- Manual reward claims through the normal HUD also open the reward overlay after a successful claim when possible.
- This changes presentation flow only. It does not choose final reward animation, icon art, rare-item treatment, panel density, or ornate frame style.

2026-05-31 crafting overlay usability bridge:

- `CraftingOverlayPresenter` now prefers the newest rerollable Rare item when opened.
- The crafting overlay now exposes a reroll-ready count, reroll status, and material guidance so the first Rare reroll path can be validated without relying on external notes.
- This still leaves final text density, scroll behavior, item icons, ornate frame treatment, and long-term crafting UX to Unity Editor authoring.

2026-06-01 crafting reroll result feedback:

- After a successful Rare affix reroll, the Result region now keeps a last-reroll line for the selected item.
- The line includes the spent material cost and a `before -> after` affix summary, giving Play Mode validation a local proof that the material sink fired and the item changed.
- This does not change final panel layout, icon treatment, reveal animation, affix pools, or long-term crafting UX.

2026-06-02 crafting reroll anti-repeat validation:

- The current prototype Rare reroll avoids repeating the selected item's saved affix when another slot-valid candidate exists.
- This supports the existing Result-region validation: a paid reroll should show both material spend and a changed affix line.
- This does not change panel layout, icon treatment, reveal animation, authored affix pools, or long-term crafting UX.

2026-06-03 defense alert HUD feedback:

- `PlayableLoopHud` now derives a defense alert from breach, low wall health, wall damage per second, high pressure, or damaged-wall state.
- The summary line can show `Defense alert: ...` without a new TMP field, and the action hint prioritizes severe alerts plus high pressure during `DungeonFocus` or an active dungeon run.
- Default first-pass thresholds are low wall at `35%` health and high pressure at `75%` capacity. This does not add final alert animation, icon art, camera changes, or defense-side composition.

2026-06-04 overlay event reliability:

- `RewardOverlayPresenter`, `InventoryOverlayPresenter`, and `CraftingOverlayPresenter` now resynchronize their event subscriptions when auto-found references appear or change during refresh.
- Reward grants, inventory changes, wallet material changes, salvage payouts, and equipped-stat refreshes should update overlay text immediately instead of relying only on the periodic refresh interval.
- This does not change overlay placement, panel density, item icon treatment, reward reveal art, crafting cost, or item mutation rules.

2026-06-04 camera panel input bridge:

- `PanelCameraRenderTarget` provides the first reusable bridge for rendering a scene camera into a UI `RawImage`. It can use an explicitly assigned `RenderTexture` or create a runtime texture sized from the image rect.
- `DungeonViewportInputRouter` provides the matching input bridge. It converts a click inside the `RawImage` into normalized viewport coordinates and sends the resulting ray from the assigned dungeon camera into `PlayerController.HandlePrimaryClickRay(...)`.
- `PlayerController` now ignores duplicate world-click handling when the pointer is over UI, while still allowing explicit routed rays from a UI panel. It also sorts click hits by distance, attacks valid enemy targets, and skips self/friendly/damageable actor colliders as movement surfaces.
- This is code support only. The visual result still depends on Unity Editor authoring: the dungeon camera angle, culling/layers, `RawImage` placement, split ratio, and panel crop must be judged in Play Mode.

2026-06-04 saved-running and stationary attack validation:

- `PlayableLoopHud` now syncs screen focus from expedition state changes, including load-time restoration. A saved `Running` dungeon should put the screen back into `DungeonFocus` so spawned enemies are visible.
- Shift-clicking a target is still a stationary attack, not chase movement. If the target is outside attack range, the command remains active and keeps facing/swinging until the target becomes hittable or another command replaces it.
- These fixes do not decide final save-resume UX. Later production may still add an explicit "resume expedition" prompt, but the MVP rule is that an active dungeon cannot run invisibly behind the wrong focus.

2026-06-05 dungeon viewport QA bridge:

- The current `Gameplay` scene has the first static dungeon viewport bridge: `RawImage_DungeonViewport`, `Camera_DungeonPanel`, `PanelCameraRenderTarget`, and `DungeonViewportInputRouter`.
- `PlayableLoopHud` can show `Viewport: render ... / input ...` in the Dungeon line when a dungeon run or `DungeonFocus` makes the panel relevant. P0-B is accepted; this remains regression-only QA copy and must not be treated as final screen text or receive a dedicated polish pass.
- `DungeonViewportInputRouter` can inherit its viewport camera from a same-object `PanelCameraRenderTarget`, reducing Inspector mismatch risk when the render and click router live on the same `RawImage`.
- The automation harness checks the static scene bridge and core serialized references. Camera framing, defense side-panel crop, routed click feel, overlay occlusion, and alert readability were accepted for the current MVP checkpoint and should be reopened only for regressions or the later production presentation pass.

2026-06-09 unresolved item feedback:

- Inventory, reward, crafting, and compact HUD item summaries label an unknown saved definition id as `UNRESOLVED`/`Unresolved`.
- Equip, salvage, and reroll actions are disabled for that quarantined record. The item remains listed so a content-id migration can recover it without silently deleting player data.
- Loading through `PlayableLoopHud` includes the schema/item migration summary in the normal message area. This is production error feedback, not a new overlay layout or visual-polish track.

2026-06-10 duplicate conversion feedback:

- When `LootDropper` auto-converts a dominated same-definition reward, `RewardOverlayPresenter` shows `Reward converted`, the authored loot source, the material payout, and the updated wallet.
- The consumed candidate is not replaced by the previous inventory item in the reward detail area, and equip/salvage actions remain disabled for that resolved reward event.
- `PlayableLoopHud` reports `Reward auto-converted`, while `ExpeditionDirector.LastResult` records the item name and gained materials.
- No RectTransform, camera, icon, animation, or ornate-treatment change is required for D0-D.

2026-05-28 scene layout cleanup pass:

- `Gameplay` now uses the reference-image screen bands as the first deterministic layout pass: top global bar `y 0.925-0.99`, main play area `y 0.18-0.92`, and bottom action bar `y 0.04-0.175`.
- `PlayableScreenLayoutController` is constrained to the main play area (`x 0-1`, `y 0.18-0.92`) so the dungeon/defense split no longer sits under the global bar or action bar.
- `DungeonFocus` still uses the controller's `70%` dungeon / `30%` defense split. In editor defaults, `Panel_DungeonViewport` occupies `x 0-0.7`, and `Panel_DefenseSide` occupies `x 0.7-1` inside the main play area.
- `Panel_PlayableLoopHud` is a transparent full-screen overlay. Its labels and buttons are anchored into the same reference bands instead of using loose center positions.
- `Panel_InventoryOverlay` uses a centered overlay frame (`x 0.18-0.82`, `y 0.18-0.86`), with item list on the left, selected item/material details in the middle, and action buttons on the right.
- `Panel_RewardOverlay` now follows the same centered frame (`x 0.18-0.82`, `y 0.18-0.86`): header `x 0.04-0.42`, `y 0.885-0.965`; reward summary `x 0.04-0.43`, `y 0.205-0.84`; item detail `x 0.465-0.755`, `y 0.62-0.84`; materials `x 0.465-0.965`, `y 0.355-0.585`; message `x 0.04-0.755`, `y 0.06-0.17`; actions `x 0.785-0.965`.
- `Panel_CraftingOverlay` now follows the same centered frame (`x 0.18-0.82`, `y 0.18-0.86`): header `x 0.04-0.96`, `y 0.88-0.965`; item list `x 0.04-0.47`, `y 0.27-0.84`; selected item `x 0.51-0.96`, `y 0.57-0.84`; materials `x 0.51-0.96`, `y 0.38-0.55`; affix/result `x 0.51-0.96`, `y 0.20-0.36`; message `x 0.04-0.96`, `y 0.145-0.19`; actions `y 0.045-0.115` from left to right: Previous, Next, Latest, Reroll, Salvage, Close.
- Dark bronze gameplay buttons must use bright TMP label colors (white or near-white) so the text remains readable in Play Mode.

## 10.1 Layout Handoff Rule

Whenever future work changes visible UI placement, the handoff must include:

- The target screen state: `DefenseFocus`, `DungeonFocus`, or a named overlay.
- The parent object and child objects changed.
- Anchor ranges as normalized `x min-max` and `y min-max` values, plus any fixed offsets.
- The placement order: parent panel first, text regions second, buttons third, content wiring fourth.
- Which values are fixed by design versus which values are meant for Unity Editor visual tuning.
- The Play Mode path used to judge it.

## 10. MVP Acceptance Check

The presentation slice is acceptable for MVP when:

- The game starts in `DefenseFocus`.
- Starting a dungeon visibly compresses defense and brings in the dungeon viewport.
- Restarting Play Mode from a saved `Running` dungeon restores the dungeon viewport instead of leaving enemies active behind the defense screen.
- The top and bottom HUD bars remain stable during the transition.
- Dungeon click/control input works after the transition, including clicks routed from a `RawImage`/RenderTexture panel through the dungeon camera.
- The HUD Dungeon line can expose whether the dungeon viewport render target and input router are ready while P0-B is under validation.
- Defense pressure, wall health, Hold/Push, and breach state remain visible during dungeon play.
- A defense alert can be noticed in summary/action-hint feedback while in dungeon focus.
- Inventory or crafting can open and close without losing the previous focus state.
- No scene reload is required to move between focus states.

## 11. Known Open Decisions

These require user or Unity Editor review before final production values:

- Final split ratio: keep MVP `70/30` or move toward `68/32`.
- Whether the defense side panel should always be on the right. Current recommendation: right.
- Whether the compressed defense view should be a live camera viewport, RenderTexture, or UI abstraction.
- Final camera angle and zoom for both defense and dungeon.
- Exact ornate frame density and Diablo-like UI art treatment.
- Whether severe defense breach should auto-return the player to `DefenseFocus` or only alert them.
