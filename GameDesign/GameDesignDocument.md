# Incremental Diablo — Game Design Document

## Product promise

IncrementalDiablo is a low-cost, offline-first PC incremental action RPG. The player runs an automatic dark-fantasy frontline, then personally enters compact dungeons to take risks, fight directly, collect equipment, and improve both the hero and the defense.

```text
automatic defense income
-> pre-run dungeon risk/reward choice
-> direct-control combat
-> loot, equipment, salvage, crafting
-> a clearer and stronger next objective
```

The target is a sellable loop, not a technology demo. The immediate target is a two-hour repeatable slice; the 900+ hour ambition is a long-horizon constraint after the short loop proves its decisions and sinks.

The release-gate source of truth is `ProductionDocs/13_ReleaseReadinessAndProductionGates.md`. The daily implementation queue is `ProductionDocs/10_PlayableLoopMvpAutomationPlan.md`.

## Player fantasy and boundaries

### Automatic ground defense

- The ground layer is a continuous frontline with Frontline Level and Hold/Push direction.
- It should read as a dark-fantasy battlefield: defenders, enemies, attacks, reinforcements, wall damage, and a protected side are visible.
- It remains automatic. The player does not select units, issue movement or focus-fire commands, manage workers/production queues, place towers freely, or author manual waves.
- `DefenseRuntimeState` remains authoritative for pressure, wall state, resources, progression, save/load, and offline behavior.
- Loading defense state rebuilds visual actors from the restored authoritative state; visual actors must not keep damaging the wall while defense is not running.

### Direct-control dungeon action

- Dungeons provide the active RPG contrast: player-controlled movement, attacks, danger, failure, and reward handling.
- A run must make its threat/reward contract visible before entry, then resolve that choice visibly at completion or failure.
- Dungeon growth should come from reusable contract/encounter rules and data, not short hand-authored ladders.

### Item and economy direction

- Equipment must produce understandable build choices for the hero and eventually the defense.
- Salvage, duplicate conversion, crafting materials, rerolls, loadouts, and filtering are preferred answers to inventory bloat.
- Drops need clear denominators, reusable tables, sinks, save behavior, and balance exports.
- The D2 reference pack can inform pacing and sink structure, but this project must not copy trading, ladder, multiplayer, or alt-character assumptions.

## Current accepted baseline

- Ground defense has an accepted actual NavMesh battlefield with readable faction/attack ownership, wall damage, reinforcement, formula-driven density, and accepted camera composition.
- Dungeon depth progression, depth threat/reward bands, E1-A contract choice, normal-player contract buttons, E1-C encounter core, defense save/load visual rebuild, item registry/migration, duplicate conversion, save/load, no-save recovery guidance, reward overlays, salvage, the accepted authored Rare affix pool, first fresh-save `Next:` guidance, and schema-v6 HUD settings persistence exist.
- The next product gap is focused first-session recovery QA so the accepted contract/item/encounter loop can be understood without developer explanation. A broader settings menu still needs a scoped UI/product decision.

Accepted baselines reopen only for regressions or explicit contract changes. They are not default polishing work.

## Current production priorities

1. Finish `E2-A`: fresh-save recovery QA now that the item/contract/encounter decision loop has first-pass HUD guidance and the current HUD settings persist.
2. Future presentation pass: authored boss silhouettes, room geometry, and VFX after the teachable loop exists.

## Design rules

- Prefer player-visible decisions, rewards, sinks, failure/recovery, and reusable scaling rules.
- Do not spend consecutive production runs on debug UI, camera values, smoke tests, or documentation without an active blocker.
- Do not introduce a second simulation, reward economy, or save model for visual presentation.
- Keep normal-player text concise. Remove diagnostics and review-only labels after their validation purpose ends.
- Every major system needs purpose, feedback, failure state, reward/sink link, save/load intent, scalable rule, balance knobs, verification, and a current owner document.

## Verification standard

`Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` verifies build, structural scene contracts, missing scripts, balance exports, prototype inventory, documentation freshness, and local automation health. A passing harness means structural safety; it does not prove gameplay feel or product completion.

Use a focused Play Mode path whenever a task changes combat readability, player input, screen composition, camera behavior, or UI interaction.
