# Base Script Usage Guide

## Ground-defense ownership

| Component | Responsibility | Do not use it for |
| --- | --- | --- |
| `DefenseRuntimeState` | Authoritative pressure, wall, state, resources, progression, save/load, offline state | Visual-only actor state or a second economy. |
| `DefenseDirector` | Starts/ticks the runtime and receives battlefield wall damage | Unit control or authored waves. |
| `GroundDefenseBalanceModel` | Formula bands, balance knobs, deterministic export | Hand-authored ladders. |
| `GroundDefenseNavMeshBattlefield` | Builds live ground/NavMesh actors, faction readability, autonomous force, reinforcement, and wall bridge | Player orders, review-only level switching, or a separate simulation. |
| `GroundDefenseNavMeshUnit` | Autonomous targeting, movement, combat, death, reinforcement, and attacker-to-target feedback | Persisting unit positions or rewards. |
| `GroundDefenseBillboardUtility` | Runtime role sprites and readable faction treatment | A second presentation stack. |

The deleted ground-presentation stack is not an alternative implementation. It must not be restored without an explicit new production decision.

## Dungeon and progression ownership

| Component | Responsibility | Contract to preserve |
| --- | --- | --- |
| `ExpeditionDirector` | Dungeon state, room outcome, reward handoff, save recovery | Active contracts must persist when E1-A is added. |
| `DungeonDepthBalanceModel` | Formula-driven depth threat/reward bands and export | No manual depth ladder as the default scaling solution. |
| `EnemySpawner` | Spawned dungeon enemies | Failure and completion must remain visible. |
| `LootDropper` / `SimpleInventory` | Rewards, inventory, duplicate conversion, salvage link | Production scenes must not silently use fallback rewards. |
| `ItemDefinitionRegistry` | Authored definitions and migration IDs | Unknown saved IDs remain visible/quarantined. |
| `ItemEconomyModel` / `ItemSalvageService` | Salvage and material sinks | Drop changes state a denominator and preserve a sink. |

## Normal-player UI ownership

| Component | Responsibility |
| --- | --- |
| `PlayableLoopHud` | Current frontline/dungeon state, meaningful actions, depth selection, and normal status. |
| `PlayableScreenLayoutController` | Defense/dungeon focus and overlay visibility safety. |
| `PanelCameraRenderTarget` | Camera-to-`RawImage` viewport bridge. |
| `DungeonViewportInputRouter` | Converts dungeon viewport clicks to player input. |
| `InventoryOverlayPresenter`, `RewardOverlayPresenter`, `CraftingOverlayPresenter` | Player-facing item/reward/crafting content and actions. |

Keep QA diagnostics, render binding state, review labels, and temporary test controls out of normal HUD text once their validation purpose is closed.

## E1-A implementation boundary

The next implementation adds a pre-run dungeon contract. Put contract definitions in reusable data, keep the selected contract in expedition/save state, expose clear threat/reward text before entry, and apply it to the active run and reward result. Do not solve it with hidden multipliers, a hard-coded one-off room, or a replacement depth system.

## Verification

- Run `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` after changing a live script or wiring contract.
- Add focused checks for new data/save contracts.
- Use Play Mode for player input, reward flow, combat feedback, or presentation changes.
