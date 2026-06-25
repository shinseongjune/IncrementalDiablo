# Playable Screen Presentation Specification

## Player-facing screen contract

The normal screen must explain the current action loop without developer text:

```text
defense state and resources
-> choose a dungeon depth and contract
-> direct-control combat view
-> reward, inventory, equip/salvage/crafting
-> return to the next clear objective
```

`PlayableScreenLayoutController` owns safe focus transitions. `PlayableLoopHud` owns meaningful state/action text. Overlay presenters own detailed item, reward, and crafting content.

## Focus states

| State | Player purpose | Required result |
| --- | --- | --- |
| Defense focus | Read automatic defense, resources, alerts, and dungeon entry | Defense viewport is visible; no unit-control affordance or debug text. |
| Dungeon focus | Direct-control expedition combat | Dungeon viewport/input are active; defense remains a compact status context. |
| Reward overlay | Understand and claim a dungeon result | Player can inspect, equip, salvage, or continue to inventory. |
| Inventory overlay | Compare and manage held equipment | Selected item, wallet/materials, and meaningful actions are readable. |
| Crafting overlay | Spend materials on a clear item operation | Costs, valid target, outcome, and next action are clear. |

## Defense presentation

- The accepted view places enemy pressure toward the far/top side and defenders plus the protected wall toward the lower side.
- Actors, attack ownership, death/reinforcement, and wall damage must remain readable in the defense viewport.
- This is automatic defense. Do not add unit selection, order buttons, production controls, manual-wave controls, or review-only visual scale labels.
- Camera composition is an accepted baseline and changes only for visual regression or an explicit new feature requiring a manual visual decision.

## Dungeon presentation

- Before entry, show the selected depth, two generated contract offers, selected contract, and readable threat/reward-depth effect.
- During a run, keep direct-control input, enemy state, failure, and reward consequence legible.
- On clear, open the normal reward path; do not require debug HUD actions.
- On failure, state the outcome and return path without silently losing valid saved progression.

## 2026-06-25 E1-A presentation status

- `PlayableLoopHud` now has script fields and methods for contract A, contract B, and offer refresh buttons.
- The Dungeon text can display the generated offers, selected contract, active contract, threat multipliers, and reward-depth result.
- `Gameplay` now wires compact normal-player `Contract A`, `Contract B`, and `Refresh` buttons to the new `PlayableLoopHud` fields.
- Remaining evidence is focused Play Mode validation of choice -> run -> reward/save behavior and whether the compact controls are readable enough in the live screen.
- Do not add review labels or diagnostic component names for this pass; contract text should state player consequences only.

## Text policy

- Normal-player text states consequences and actions, not implementation state.
- Keep `Render Target`, `input router`, scene-component names, review scales, and other diagnostics out of the normal HUD.
- Avoid temporary labels that imply unavailable systems or accepted work still needs review.

## Manual visual validation

Use Play Mode when changing layout, camera, input, or combat readability:

1. Enter defense focus and verify automatic battle readability.
2. Enter dungeon focus and verify the viewport image and input.
3. Clear or fail a room and verify reward/recovery flow.
4. Open/close each wired overlay and confirm focus returns predictably.

The structural harness checks serialized wiring; it does not approve visual feel or text hierarchy.
