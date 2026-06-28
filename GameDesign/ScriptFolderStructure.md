# Script Folder Structure

## Live ownership map

| Area | Folder / primary scripts | Responsibility |
| --- | --- | --- |
| Ground-defense authority | `Assets/02.Scripts/GroundDefense/Runtime/DefenseRuntimeState.cs`, `DefenseDirector.cs`, `DefenseSaveManager.cs`, `GroundDefenseBalanceModel.cs` | Continuous frontline, wall/resources/progression, save/load/offline state, formula balance, save-apply notification. |
| Ground-defense live battle | `Assets/02.Scripts/GroundDefense/Runtime/GroundDefenseNavMeshBattlefield.cs`, `GroundDefenseNavMeshUnit.cs`, `Assets/02.Scripts/GroundDefense/UI/GroundDefenseBillboardUtility.cs` | Autonomous NavMesh actors, visual faction/role readability, target ownership, death/reinforcement, authoritative wall damage, save-load visual rebuild. |
| Dungeon | `Assets/02.Scripts/Dungeon/ExpeditionDirector.cs`, `EnemySpawner.cs`, `DungeonDepthBalanceModel.cs`, `DungeonContractModel.cs`, `DungeonEncounterModel.cs` | Direct-control expedition state, enemy spawning, formula depth bands, generated contract choices, reusable elite/boss encounter rules, failure/reward handoff. |
| Items | `Assets/02.Scripts/Items/ItemDefinitionRegistry.cs`, `LootDropper.cs`, `SimpleInventory.cs`, `ItemEconomyModel.cs`, `ItemSalvageService.cs` | Authored item identity, rewards, duplicate conversion, inventory, salvage, authored Rare affix pool, material sinks. |
| UI | `Assets/02.Scripts/UI/PlayableLoopHud.cs`, `PlayableScreenLayoutController.cs`, `PanelCameraRenderTarget.cs`, `DungeonViewportInputRouter.cs` | Normal player HUD, first-session guidance/recovery copy, focus/overlays, viewport render bridge, dungeon viewport input. |
| Overlay UI | `Assets/02.Scripts/UI/*OverlayPresenter.cs` | Inventory, reward, and crafting actions/content. |
| Shared | `Assets/02.Scripts/Shared/GameSaveData.cs`, `GameSaveDataDiagnostics.cs` | Save schema and explicit migration/diagnostics, including dungeon contract ids. |
| Automation | `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1`, balance/contract/encounter/affix exports, prototype inventory | Structural verification, deterministic balance checks, debt visibility. |

## Retired ground-defense path

The superseded presentation stack, its prefab/assets, review-only level control, and normal-player diagnostics were deleted after the actual NavMesh battlefield was accepted. Do not recreate compatibility fallbacks. If a future feature needs role data or pooling, design it against the live NavMesh contract.

## Ownership boundaries

- Ground battle visuals never own rewards, frontline progression, wall authority, or save data.
- Ground battle visuals rebuild from `DefenseDirector.SaveDataApplied` and must stop attacks whenever the restored defense state is not running.
- Dungeon contracts must extend `ExpeditionDirector`, `DungeonContractModel`, and save data; they must not create a second depth/reward system.
- Dungeon encounters must extend `ExpeditionDirector`, `DungeonEncounterModel`, and save data; they must not create a hand-authored room ladder or second reward path.
- Rare affixes must extend `ItemEconomyModel.AuthoredRareAffixes`, `ItemAffixRoll`, and the existing crafting cost path; they must not create a second item mutation or save model.
- Player-facing UI shows actions, consequences, first-session next steps, and recovery meaning, not review/debug/render wiring status.
- New systems need a primary owner, data location, persistence statement, balance knobs, and a harness or Play Mode verification path.

## Automation notes

`Invoke-IncrementalDiabloChecks.ps1` verifies the active NavMesh defense battle and requires the removed legacy components to be absent from `Gameplay.unity`. It also checks named dungeon/defense viewport render bridges, the `PlayableLoopHud` depth/contract button references, save/item/contract/encounter/affix contracts, balance exports, prototype inventory, and production-document freshness. A passing run is structural verification, not gameplay acceptance.
