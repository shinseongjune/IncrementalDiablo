# Production Document Map

## Read order

1. `13_ReleaseReadinessAndProductionGates.md` — product contract, release gates, and what counts as real movement.
2. `10_PlayableLoopMvpAutomationPlan.md` — current queue, closed gates, blockers, and verification rule.
3. The relevant system spec: `03` ground defense, `04` dungeon, `05` items/economy, or `07` data/save/balance.
4. `06_UnitySceneAndPrefabSetupGuide.md` and `09_BaseScriptUsageGuide.md` only when Unity setup or script ownership changes.
5. `11_PlayableScreenPresentationSpec.md` only when player-visible screen flow changes.
6. `12_PrototypeDebtRegister.md` when adding, retaining, or removing a debug/prototype/fallback surface.

`GameDesignDocument.md` is the concise product overview. `ScriptFolderStructure.md` maps live code ownership. Neither is a historical task queue.

## Source-of-truth rules

| Need | Source |
| --- | --- |
| What is being sold and which gate is next | `13_ReleaseReadinessAndProductionGates.md` |
| What an automation should do today | `10_PlayableLoopMvpAutomationPlan.md` |
| Ground-defense rules | `03_GroundDefenseSystemSpec.md` |
| Dungeon contracts, combat, failure, and reward rules | `04_DungeonExpeditionSystemSpec.md` |
| Items, crafting, drops, and sinks | `05_ItemsCraftingEconomySpec.md` |
| Save schema and balance/export rules | `07_DataSaveAndBalanceSpec.md` |
| Unity object/component setup | `06_UnitySceneAndPrefabSetupGuide.md` |
| Script/component responsibilities | `09_BaseScriptUsageGuide.md` |
| Screen state and normal-player UI | `11_PlayableScreenPresentationSpec.md` |
| Temporary/prototype retirement decisions | `12_PrototypeDebtRegister.md` |

If documents conflict, use the newest explicit product decision in `13`, then the current queue in `10`, then the system spec. Do not revive an old route merely because it appears in an older note.

## Document hygiene

- Keep only current contracts, accepted baselines, current queue, setup instructions, and decisions that still constrain implementation.
- Delete superseded prototypes, closed-gate test scripts, review-only text, and historic implementation diaries instead of preserving them as active guidance.
- A completed gate is regression-only. Its evidence belongs in the concise baseline, not in a new work queue.
- `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` validates structural contracts; it never replaces player-visible acceptance.
