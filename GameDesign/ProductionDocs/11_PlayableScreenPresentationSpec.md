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
- The focused Play Mode validation was accepted on 2026-06-25. Do not repeat contract-button acceptance work unless a regression changes the contract/save/reward path.
- Do not add review labels or diagnostic component names for this pass; contract text should state player consequences only.

## 2026-06-26 E1-B crafting presentation status

- Rare affix reroll result text now uses authored affix display names and stat text through `ItemEconomyModel.FormatAffixRoll(...)`.
- The crafting overlay keeps the same normal action path: select a Rare item, spend `Gold + Essence + AlterStone`, show the before -> after affix summary, and refresh equipped stats when needed.
- Production evidence accepted 2026-06-26: a rewarded Rare can be equipped, rerolled, saved, and loaded with the authored affix text still coherent. Reopen only for UI/stat/save regressions.

## 2026-06-27 E1-C encounter presentation status

- The Dungeon line now includes `Next encounter` before entry and `Encounter active` during a run or reward-pending state.
- Encounter text names the reusable profile and states the direct player consequences: normal/elite/boss kind, HP multiplier, damage multiplier, and reward-depth offset.
- `CombatRoom`, `EnemySpawner`, clear/fail result text, and reward text keep the same active encounter name so a run does not silently change identity across combat, reward, or save/load.
- Production evidence accepted 2026-06-27: next encounter -> active elite/boss -> clear/fail -> reward -> save/load remained coherent in Play Mode.
- This is the first production-owned encounter presentation pass. It does not author boss silhouettes, room geometry, spawn placement, camera framing, or new VFX. Those remain manual visual-authoring decisions after onboarding/recovery makes the accepted loop teachable.

## 2026-06-28 E2-A first-session guide status

- `PlayableLoopHud` now uses compact first-session `Next:` guidance to route a fresh player through start frontline, compare contracts, run/fail/reward, equip or salvage, and create the first recovery save.
- Normal status text is compact by default: it keeps current action, wall/pressure/progress, selected contract, next/active encounter, reward state, latest item, and hero basics. Detailed balance multipliers, seeds, loot-source diagnostics, screen-layout state, and last-result logs are hidden from the default HUD and remain code-toggleable for QA.
- Missing first-save load attempts no longer surface the raw save path in normal HUD text; they explain the playable path to create a save.
- Manual save feedback now states that the recovery point covers frontline, dungeon, inventory, and equipment.
- This is onboarding copy only. It does not add a settings menu, visual layout pass, camera change, tutorial overlay, or new scene object.

## Text policy

- Normal-player text states consequences and actions, not implementation state.
- Default HUD text should fit a normal play read: one compact frontline block, one compact dungeon block, one item line, one hero line, and one `Next:` line.
- Keep `Render Target`, `input router`, scene-component names, review scales, and other diagnostics out of the normal HUD.
- Avoid temporary labels that imply unavailable systems or accepted work still needs review.

## Manual visual validation

Use Play Mode when changing layout, camera, input, or combat readability:

1. Enter defense focus and verify automatic battle readability.
2. Enter dungeon focus and verify the viewport image and input.
3. Clear or fail a room and verify reward/recovery flow.
4. Open/close each wired overlay and confirm focus returns predictably.

The structural harness checks serialized wiring; it does not approve visual feel or text hierarchy.
