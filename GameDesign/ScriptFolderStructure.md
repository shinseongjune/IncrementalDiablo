# Script Folder Structure

## Live ownership map

| Area | Folder / primary scripts | Responsibility |
| --- | --- | --- |
| Ground-defense authority | `Assets/02.Scripts/GroundDefense/Runtime/DefenseRuntimeState.cs`, `DefenseDirector.cs`, `DefenseSaveManager.cs`, `GroundDefenseBalanceModel.cs` | Continuous frontline, wall/resources/progression, save/load/offline state, formula balance. |
| Ground-defense live battle | `Assets/02.Scripts/GroundDefense/Runtime/GroundDefenseNavMeshBattlefield.cs`, `GroundDefenseNavMeshUnit.cs`, `Assets/02.Scripts/GroundDefense/UI/GroundDefenseBillboardUtility.cs` | Autonomous NavMesh actors, visual faction/role readability, target ownership, death/reinforcement, authoritative wall damage. |
| Dungeon | `Assets/02.Scripts/Dungeon/ExpeditionDirector.cs`, `EnemySpawner.cs`, `DungeonDepthBalanceModel.cs` | Direct-control expedition state, enemy spawning, formula depth bands, failure/reward handoff. |
| Items | `Assets/02.Scripts/Items/ItemDefinitionRegistry.cs`, `LootDropper.cs`, `SimpleInventory.cs`, `ItemEconomyModel.cs`, `ItemSalvageService.cs` | Authored item identity, rewards, duplicate conversion, inventory, salvage, material sinks. |
| UI | `Assets/02.Scripts/UI/PlayableLoopHud.cs`, `PlayableScreenLayoutController.cs`, `PanelCameraRenderTarget.cs`, `DungeonViewportInputRouter.cs` | Normal player HUD, focus/overlays, viewport render bridge, dungeon viewport input. |
| Overlay UI | `Assets/02.Scripts/UI/*OverlayPresenter.cs` | Inventory, reward, and crafting actions/content. |
| Shared | `Assets/02.Scripts/Shared/GameSaveData.cs`, `GameSaveDataDiagnostics.cs` | Save schema and explicit migration/diagnostics. |
| Automation | `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1`, balance exports, prototype inventory | Structural verification, deterministic balance checks, debt visibility. |

## Retired ground-defense path

The superseded presentation stack, its prefab/assets, review-only level control, and normal-player diagnostics were deleted after the actual NavMesh battlefield was accepted. Do not recreate compatibility fallbacks. If a future feature needs role data or pooling, design it against the live NavMesh contract.

## Ownership boundaries

- Ground battle visuals never own rewards, frontline progression, wall authority, or save data.
- Dungeon contracts must extend `ExpeditionDirector` and save data; they must not create a second depth/reward system.
- Player-facing UI shows actions and consequences, not review/debug/render wiring status.
- New systems need a primary owner, data location, persistence statement, balance knobs, and a harness or Play Mode verification path.

## Automation notes

`Invoke-IncrementalDiabloChecks.ps1` verifies the active NavMesh defense battle and requires the removed legacy components to be absent from `Gameplay.unity`. It also checks named dungeon/defense viewport render bridges, save/item contracts, balance exports, prototype inventory, and production-document freshness. A passing run is structural verification, not gameplay acceptance.
