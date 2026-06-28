# Release Readiness And Production Gates

## 1. Current Product Contract

IncrementalDiablo is a low-cost, offline-first PC incremental action RPG. Its sellable identity is not a generic idle clicker or a controllable RTS:

```text
Automatic continuous frontline earns basic resources
-> player chooses a dungeon risk/reward contract
-> direct-control dungeon combat earns equipment and rare materials
-> equipment/crafting changes hero and defense capability
-> a clear next obstacle creates the next session goal
```

The accepted E0 ground battlefield and E0-B camera composition are a baseline, not an active polishing queue. Reopen them only for a regression that breaks actor identity, attack ownership, wall damage, or the accepted top/far-to-lower/protected composition.

The 900+ hour target is a long-horizon design constraint. It is not an Early Access, alpha, or first-sellable-slice completion criterion. No automation may use a distant hour target to justify building large content breadth before the shorter gates below pass.

## 2. Objective Production Assessment

| Area | Verified foundation | Missing for a sellable slice | Production implication |
| --- | --- | --- | --- |
| Ground defense | Automatic frontline, visible autonomous battle, formula bands, save/offline loop | A player-facing long-session reason to return to the defense beyond numeric level growth | Do not add camera or actor polish by default; connect future rewards and contracts to meaningful strategy only. |
| Dungeon | Direct-control one-room combat, depth unlocks, depth threat/reward bands, authored reward path | Encounter variety, a pre-run decision, a boss/elite identity, and repeatable objectives | Prioritize reusable encounter and contract rules over hand-authored room lists. |
| Items and crafting | Registry/migration, basic rarity, duplicate conversion, salvage, first reroll sink | Authored affix pool, build identities, clear upgrade comparison, scalable drop export | Treat the prototype reroll as an alpha blocker, not as a content-complete item system. |
| UX and onboarding | Playable focus states, overlays, save recovery, alert bridge | First-session teaching, settings, release-grade text/art pass | Add after the repeatable slice proves its decisions; do not use layout polish to substitute for content. |
| Verification | Build, scene contracts, balance exports, debt scan, freshness checks | Explicit product-gate checks and honest compiler-warning visibility | A green harness means safe structure, not a completed product gate. |

## 3. What Counts As Production Movement

An automation task counts as movement only when it advances one release gate with all five items below:

1. **Player delta:** a player sees, chooses, earns, risks, or retains something new in the normal path.
2. **Loop link:** the change connects to a reward, sink, failure/recovery, or next-goal loop.
3. **Scale rule:** content is data/formula/reusable-template driven, not a one-off ladder unless explicitly scoped as authored showcase content.
4. **Persistence:** save/load behavior is defined or explicitly unchanged.
5. **Evidence:** a focused harness check, deterministic export, or short Play Mode path can prove the changed contract.

Documentation, harness, diagnostics, and visual tuning are supporting work. They are valid only when they unblock an active product task, correct a false completion state, or fix a regression. They are never the default work item after a gate has accepted user evidence.

## 4. Release Gates

### Gate R0 — First Loop Evidence (complete)

The player can earn ground resources, enter direct dungeon combat, receive a reward, equip or salvage it, and save/load the state. This is evidence of viability, not a sellable duration claim.

### Gate R1 — Two-Hour Repeatable Slice

The player can complete several runs without repeating the exact same decision pattern. Required outcomes:

- an explicit pre-run risk/reward choice;
- at least three reusable dungeon contract rules;
- visible contract effects on threat and rewards;
- one contract result saved through an active run and explained on completion;
- duplicate handling and salvage remain understandable;
- no accepted ground or dungeon combat path regresses.

`E1-A` is accepted from the focused Play Mode path as of 2026-06-25. `E1-B` is accepted from the focused Play Mode path as of 2026-06-26. `E1-C` is accepted from the focused Play Mode path as of 2026-06-27, so the repeatable slice now has contract choice, authored item-affix outcome, and reusable elite/boss encounter variety. `E2-A` has started with fresh-save guidance and first recovery-save copy; settings persistence and recovery QA remain.

### Gate R2 — Ten-Hour Alpha Loop

- authored affix data replaces the prototype reroll (`TD-04`) and is verified through the normal crafting overlay;
- at least one reusable encounter/elite or boss rule replaces the tint-only room presentation (`TD-05`);
- players can name why one item, contract, or upgrade is better for their current goal;
- the first-session path teaches Hold/Push, contract choice, direct combat, reward handling, and recovery;
- save migration and negative-path recovery are verified against the production item/contract data.

### Gate R3 — Early Access Candidate

- R1 and R2 pass without debug HUDs, prototype fallbacks, or review-only controls on the normal path;
- alpha-blocker debt is closed or explicitly replaced with a tested production path;
- settings, first-session onboarding, basic accessibility/readability, and a reproducible QA checklist exist;
- a fresh save can play the intended first session without developer explanation;
- builds report no untriaged compiler warnings and no known progression dead end.

### Post-Early-Access Long Horizon

Only after R3 should the project expand toward multi-theme depth, broader item bases, unique effects, automation layers, and the 900+ hour horizon. Each expansion must add a reusable rule, a sink, and a verification/export path before increasing content counts.

## 5. Current Ordered Work

| ID | Status | Product task | Completion evidence |
| --- | --- | --- | --- |
| E1-A | Done / P0 | Formula-driven dungeon contract choice. Before each expedition, show two generated choices from a starter set of three contracts. Each choice has a transparent threat modifier and reward-depth modifier, applies to the active run, and persists in the save. | Accepted 2026-06-25 through focused Play Mode contract A/B/refresh -> run -> reward -> save/load evidence, including restored defense state. |
| E1-B | Done / P0 | Replace the prototype Rare reroll with a small authored affix pool: tags, weights, slot rules, and clear stat text. | Accepted 2026-06-26 through focused reward -> equip -> reroll -> save/load evidence. `RareAffixPool.csv` is the deterministic export, and TD-04 is retired. |
| E1-C | Done / P1 | Add reusable dungeon encounter variety: one elite rule and one boss/encounter rule that preserves direct-control value. | Accepted 2026-06-27 through focused Play Mode next encounter -> start -> elite/boss active text -> clear/fail -> reward -> save/load evidence. `DungeonEncounterModel`, schema-v5 selected/active encounter save fields, encounter HP/damage/reward-depth modifiers, HUD text, and `DungeonEncounterBalance.csv` are complete. |
| E2-A | In progress / P1 | First-session onboarding, settings, and recovery handoff after R1 decisions are real. | Fresh-save HUD guidance and first recovery-save copy are implemented. Settings persistence and failure/recovery QA remain. |

## 6. Economy and Balance Guardrails

- Keep the existing 78/20/2 per-clear rarity table as a baseline, not a final economy claim.
- Any change to reward count, rarity, contract reward multiplier, or drop source must first state its denominator (`per-clear`, `per-kill`, or both) and produce a Unity export row.
- Use the local D2 reference pack only to inform pacing, pool separation, and sink structure. Do not copy trading, ladders, alt characters, or Treasure Class complexity into this single-player project.
- The depth and frontline exports prove monotonic math only. They do not prove fun, affordability, retention, or a playable depth-1000 experience.
- A clamp at `1,000,000,000` is a runtime safety boundary, not a content target. Revisit curve shape before levels approach a flat clamp region.

## 7. Automation Rules Derived From This Plan

1. Read this document and `10_PlayableLoopMvpAutomationPlan.md` before selecting work.
2. Work only on the first `Next` product task unless a build/regression blocker prevents it.
3. Closed gates are regression-only. A prior user acceptance is sufficient evidence unless a changed contract invalidates it.
4. Do not spend two consecutive automation runs on documentation, static checks, camera values, debug UI, or validation helpers.
5. A green harness must be reported as structural verification, never as player-value progress by itself.
6. Each report must name the release gate moved, the player-visible delta, and the next evidence needed.
