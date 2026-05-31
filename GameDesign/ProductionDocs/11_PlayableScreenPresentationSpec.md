# Playable Screen Presentation Spec

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
5. Drive MVP transitions through RectTransform sizes/anchors first. Add camera viewport or RenderTexture only when the panel layout is proven.
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
- The top and bottom HUD bars remain stable during the transition.
- Dungeon click/control input works after the transition.
- Defense pressure, wall health, Hold/Push, and breach state remain visible during dungeon play.
- A defense alert can be noticed while in dungeon focus.
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
