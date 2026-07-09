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

## 2026-06-29 E2-A settings persistence status

- Save schema v6 adds `UiSettingsSaveData` for the current HUD text-density and first-session guide settings.
- `PlayableLoopHud` can snapshot and apply compact HUD text, detailed balance text, diagnostic status text, first-session guide, and first recovery-save emphasis.
- `DefenseSaveManager` saves and restores those settings with the same local JSON recovery point as frontline, dungeon, inventory, and equipment.
- Normal save feedback now says the recovery point includes HUD settings.
- This is not a full settings menu or layout pass. Adding visible settings controls needs a separate product/UI decision so the normal HUD does not become crowded again.

## 2026-06-30 E2-A no-save recovery guidance status

- `Load` stays clickable when `DefenseSaveManager` exists, even before a save file exists, so a first-session player can receive the no-save guidance instead of being blocked by a disabled control.
- The no-save message still explains the playable path: start frontline, choose a contract, clear a room, then save after handling the reward.
- The first recovery-save `Next:` hint now includes HUD settings in the recovery point, matching the schema-v6 save behavior.
- This does not add a settings menu, move HUD controls, or change layout.

## 2026-07-01 E2-A no-save report consistency

- `DefenseSaveManager.NoSaveRecoveryGuidance` now owns the no-save recovery copy.
- `PlayableLoopHud.LoadGame()` reuses that copy when `Load` is clicked before a first save, and the harness checks the save-manager and HUD tokens together.
- User-confirmed recovery guidance on 2026-07-01 accepts the scoped E2-A onboarding/settings/recovery path. Reopen only for a first-session recovery, no-save load, or settings-restore regression.
- This does not add a full settings menu. Visible settings controls remain a separate product/UI decision.

## 2026-07-02 E2-B presentation target

- The next player-facing gap is goal comparison clarity: help the player name why the selected contract, latest reward, or next upgrade is better for the current goal.
- Prefer overlay or contextual copy over a busier always-on HUD. The default compact HUD should stay readable.
- Do not add diagnostic labels, broad item content, new economy denominators, or a settings menu as part of this target.

## 2026-07-03 E2-B contract comparison status

- `DungeonContractModel.FormatGoalComparisonText(...)` turns the selected contract and the other offered contract into one normal-player `Goal:` line.
- `PlayableLoopHud` shows that line in the compact contract block, contract select/refresh messages, and first-session `Next:` hint before dungeon entry.
- The first implemented comparison scope was contract tradeoff only: safer clear/recovery versus higher reward-depth risk. Latest item and defense-upgrade comparison status is tracked in the later E2-B sections below.
- This does not add a new HUD panel, settings menu, diagnostic text, contract economy denominator, item table, scene object, or layout change.
- Production evidence accepted 2026-07-03: A/B/refresh changed the `Goal:` text, the compact `Next:` hint stayed readable, and starting the selected run preserved the chosen contract consequence.

## 2026-07-04 E2-B latest item comparison status

- `PlayableLoopHud` now adds a compact `Compare:` phrase to the latest Item line when the reward item is resolved.
- The comparison uses existing same-slot equipment state and saved item power: empty slot, equipped item, positive power delta, sidegrade, or equipped item higher.
- The normal `Next:` hint now names the practical reward choice: equip an upgrade, fill an empty slot, treat a sidegrade as affix/material choice, or keep the stronger equipped item and salvage the spare.
- This changes no drop odds, reward count, reward denominator, salvage yield, save schema, scene object, layout, or settings menu.
- Production evidence accepted 2026-07-05: latest reward item guidance was user-confirmed after the guide-off priority hardening pass.

## 2026-07-05 E2-B latest item action priority status

- `PlayableLoopHud.TryBuildLatestItemDecisionHint(...)` now checks unresolved or unequipped latest reward items before ready-state contract guidance.
- This keeps the reward equip/salvage decision visible even if the player disabled the first-session guide through the saved HUD setting.
- It does not change item scoring, item drops, salvage yields, reward denominators, save schema, scene wiring, or HUD layout.
- Production evidence accepted 2026-07-05: the latest item action guidance was user-confirmed, including the reward decision staying ahead of next-contract guidance.

## 2026-07-06 E2-B defense upgrade comparison status

- `PlayableLoopHud.BuildDefenseUpgradeComparisonText()` now names an affordable Wall, Tower, or Defenders upgrade before ready-state contract guidance once the latest reward no longer needs an equip/salvage decision.
- The comparison uses existing `DefenseUpgradeModel` costs and effect deltas only: stressed wall/pressure favors Wall, otherwise Tower and Defenders compare their next DPS gain.
- This changes no upgrade cost, defense formula, save schema, scene wiring, HUD layout, item drop, reward denominator, or salvage value.
- This pass is part of the accepted 2026-07-08 E2-B defense-upgrade comparison path. Reopen only for a regression.

## 2026-07-07 E2-B defense upgrade shortfall status

- The defense-upgrade comparison now keeps stressed wall/pressure goals on Wall even when Wall cannot yet be bought.
- If Wall is the correct next goal but is not affordable, the compact `Next:` line shows the missing Gold/Scrap for Wall instead of recommending a cheaper Tower or Defenders purchase.
- This still changes no upgrade cost, defense formula, save schema, scene wiring, HUD layout, item drop, reward denominator, or salvage value.
- This pass is part of the accepted 2026-07-08 E2-B defense-upgrade comparison path. Reopen only for a regression.

## 2026-07-08 E2-B defense upgrade return guidance status

- Defense upgrade button feedback now mirrors the comparison contract instead of ending at `Wall upgraded.`.
- If an upgrade is unaffordable, the click message reports the missing Gold/Scrap shortfall through the same missing-resource formatter used by the Wall `Next:` hint.
- If an upgrade succeeds, the click message routes the player back to Hold/Push or the next contract, so the purchase has a visible next step without adding a panel or changing layout.
- This still changes no upgrade cost, defense formula, save schema, scene wiring, HUD layout, item drop, reward denominator, or salvage value.
- Production evidence accepted 2026-07-08: the post-purchase message and compact `Next:` line remained readable after buying the named upgrade, and save/load restored the upgraded levels. Reopen only for a comparison-copy, upgrade-button, or save/load regression.

## 2026-07-09 E3-A first-session QA and settings scope

- The R3 first-session QA checklist now lives in `06_UnitySceneAndPrefabSetupGuide.md`. It combines the accepted E2-A/E2-B paths into one fresh-save validation route instead of reopening another HUD comparison micro-slice.
- The recommended future settings menu scope is limited to two normal-player controls first: text density (`compact`/`detailed`) and first-session guide (`on`/`off`), backed by the existing schema-v6 `UiSettingsSaveData`.
- Detailed balance text and diagnostic status text stay code/QA toggles unless the user explicitly chooses a debug/settings scope. They should not appear in normal player captures by default.
- No scene controls, HUD placement, camera, room geometry, boss silhouette, or visual composition changed in this pass. Those remain user-approved visual-authoring scope.

## Text policy

- Normal-player text states consequences and actions, not implementation state.
- Default HUD text should fit a normal play read: one compact frontline block, one compact dungeon block, one item line, one hero line, and one `Next:` line.
- Persisted text-density choices may switch between compact and detailed status, but diagnostic status text must stay off for normal player captures unless a QA pass explicitly enables it.
- Keep `Render Target`, `input router`, scene-component names, review scales, and other diagnostics out of the normal HUD.
- Avoid temporary labels that imply unavailable systems or accepted work still needs review.

## Manual visual validation

Use Play Mode when changing layout, camera, input, or combat readability:

1. Enter defense focus and verify automatic battle readability.
2. Enter dungeon focus and verify the viewport image and input.
3. Clear or fail a room and verify reward/recovery flow.
4. Open/close each wired overlay and confirm focus returns predictably.

The structural harness checks serialized wiring; it does not approve visual feel or text hierarchy.
