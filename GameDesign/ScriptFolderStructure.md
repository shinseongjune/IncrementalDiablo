# Live Code Ownership

| 영역 | 주요 소유자 | 책임 |
| --- | --- | --- |
| 전선 권한 | `GroundDefense/Runtime/DefenseRuntimeState`, `DefenseDirector`, `DefenseSaveManager` | 전선 진행, 벽, 자원, 저장/불러오기 |
| 전선 전투 | `GroundDefenseNavMeshBattlefield`, `GroundDefenseNavMeshUnit` | 자동 전투 시각화와 벽 피해 전달 |
| 던전 | `Dungeon/ExpeditionDirector`, `CombatRoom`, `EnemySpawner`, `DungeonContractModel`, `DungeonEncounterModel` | 직접 전투 실행, 계약, 조우, 보상 |
| 아이템 | `Items/ItemDefinitionRegistry`, `LootDropper`, `SimpleInventory`, `ItemEconomyModel`, `ItemSalvageService`, `EquipmentSlots` | 전리품, 장비, 분해, 제작 재료 |
| Hero 전투 | `Character/Core`, `CombatDriver`, `Health`, `CombatAnimationDriver`, 이동 컴포넌트 | 이동, 공격, 피해, 사망. `CombatAnimationDriver`는 이를 `MoveSpeed`/`Attack`/`Hit`/`Death` Animator 계약으로 표현할 뿐 권한을 갖지 않는다. |
| UI | `UI/PlayableLoopHud`, `PlayableScreenLayoutController`, Overlay Presenter | 플레이어 행동과 결과 표시 |

새 기능은 기존 권한 경로를 확장한다. 화면 전용 상태, 두 번째 저장 모델, 두 번째 보상 경제를 만들지 않는다.
