# Dungeon Expedition System Spec

## 2026-06-21 Current Production Boundary

- The accepted normal path is one direct-control room with a real spawned enemy, depth progression, reward continuity, and retry. It is a foundation, not proof of a repeatable dungeon game.
- E1-A contract choice, E1-B authored Rare affix reroll, E1-C reusable encounter variety, and E2-A onboarding/settings/recovery are accepted. E2-B contract comparison copy is accepted from the 2026-07-03 user-confirmed check; current E2-B work is latest reward item comparison after dungeon rewards resolve, including `Next:` priority when a reward still needs equip/salvage handling.
- Boss breadth, multi-room sequences, and a new calculation-only auto-expedition path remain deferred until the accepted encounter route is teachable from a fresh save.

## 2026-06-27 E1-C Encounter Core

- `DungeonEncounterModel` owns the first reusable encounter set: `crypt_skirmish` baseline, `elite_guard` elite rule, and `tomb_warden` boss-style rule.
- `BuildEncounter(selectedDepth, encounterSeed, selectedContractId)` chooses the next encounter without a hand-authored room ladder. Depth 5 and later milestone depths force the boss-style rule; the seed/contract path can surface elite/boss variation across repeated runs.
- `ExpeditionDirector` stores selected and active encounter ids. Starting a run copies selected encounter into active run state, increments the next encounter seed, and keeps the active id through running/reward-pending saves.
- Encounter HP/damage multipliers are applied inside `GetEffectiveDepthBalance(...)` alongside depth and contract multipliers. Encounter reward-depth offset stacks with the selected contract and still uses the existing `LootDropper.TryGrantClearReward(depth)` denominator.
- `PlayableLoopHud`, `CombatRoom`, and `EnemySpawner` now name the next/active encounter in normal status/result text. This is player-facing consequence text, not a debug-only route.
- `Tools/Automation/Export-DungeonEncounters.ps1` exports `GameDesign/Balance/DungeonEncounterBalance.csv` and verifies baseline, elite, boss, denominator, and multiplier coverage.
- Production evidence accepted 2026-06-27: next encounter text -> start run -> elite/boss active text -> clear/fail -> reward -> save/load in Play Mode. Reopen E1-C only for encounter/save/reward regressions or a deliberate visual-authoring pass.

## 2026-06-28 E2-A First-Session Guide

- `PlayableLoopHud` now explains the accepted dungeon loop as normal next-step copy: compare contracts, start the selected run, finish or fail, claim the reward, then equip or salvage.
- A failed room now routes the player back to defense recovery instead of sounding like a silent dead end.
- The first manual save is described as a recovery point for frontline, dungeon, inventory, equipment, and HUD settings.
- 2026-06-29 update: schema v6 persists current HUD text-density and first-session guide settings through `UiSettingsSaveData`. This adds no new dungeon save field and does not change contract, encounter, reward, or room-resolution behavior.
- 2026-07-02 status: E2-A is accepted from the 2026-07-01 user-confirmed recovery guidance check. Reopen only for first-session recovery, no-save load, settings-restore, or reward/recovery text regressions.

## 2026-06-25 E1-A Contract Core

- `DungeonContractModel` now owns the starter set of three reusable contracts: one baseline and two risk/reward choices. `BuildOffer(selectedDepth, contractOfferSeed)` deterministically exposes two choices without a hand-authored depth ladder.
- `ExpeditionDirector` owns offered, selected, and active contract ids. Starting a run copies the selected contract into active run state; clear/failure/result text names the active contract.
- Contract threat applies through `ExpeditionDirector.GetEffectiveDepthBalance(...)`, which feeds `CombatRoom` and `EnemySpawner` enemy HP/damage multipliers without creating a second combat system.
- Contract reward uses a reward-depth offset and still calls the existing `LootDropper.TryGrantClearReward(depth)` path. This keeps the denominator as one guaranteed per-clear item reward and preserves authored tables, duplicate conversion, salvage, and item save behavior.
- `DungeonContractModel.FormatGoalComparisonText(...)` owns E2-B selected-vs-alternative contract guidance. It explains safer clear/recovery versus higher reward-depth risk without changing contract math, reward denominator, or save data.
- `DungeonLoopSmokeTest` now selects a non-default contract before starting its clear path and blocks if the run starts without an active contract.
- `Gameplay` now wires normal-player contract A, contract B, and refresh buttons into `PlayableLoopHud`; the harness checks these scene references.
- Production evidence accepted 2026-06-25: choice A/B/refresh -> start run -> clear/fail -> reward -> save/load in Play Mode, including restored defense state. Reopen E1-A only for contract/save/reward regressions.

## 2026-06-06 Phase D Direction

- The user confirmed the normal `Gameplay` path works through `PF_DungeonEnemy_Melee` spawn, `Running` activation, chase/attack, player attacks, HP/death/result feedback, room clear, reward continuity, and retry.
- `EnemySpawner` now rejects incomplete melee prefabs and spawn points that cannot resolve onto nearby NavMesh, so inert `Health` records cannot be accepted as real tracked combat.
- Normal `Gameplay` disables `CombatRoom` prototype simulation. Keep calculation combat only as an isolated dev/test fallback, not as a silent production path.
- Phase C is closed from cumulative accepted evidence. Do not run another broad acceptance pass unless a regression changes one of these contracts.
- D0-A code and scene wiring are implemented. `DungeonSaveData` stores `selectedDepth` and `highestUnlockedDepth`; `ExpeditionDirector` starts the selected depth, unlocks exactly one next depth only when the current highest is cleared, and does not advance on failure.
- `DefenseSaveManager` writes schema v2 and migrates v1 dungeon data by treating the prior active depth as both selected and highest unlocked. `GameSaveDataDiagnostics` rejects selected/active depths outside the unlocked range.
- `Gameplay` exposes `Depth -` and `Depth +` buttons through `PlayableLoopHud`, and the Dungeon text shows active depth plus selected/highest unlocked progress.
- The user confirmed the focused Play Mode path on 2026-06-07: clear -> one-depth unlock -> select/start next depth, failure without further advancement, and save/load restoration all work. D0-A is complete.
- D0-B runtime wiring is implemented and accepted in Play Mode. `DungeonDepthBalanceModel` maps every depth into a ten-depth milestone band.
- For depth `d`, let `b = floor((d - 1) / 10)` and `s = (d - 1) % 10`. Spawned enemy health uses `1.8^b * (1 + 0.08s)` and attack damage uses `1.5^b * (1 + 0.05s)`.
- `EnemySpawner` applies those multipliers to the spawned prefab's `CharacterStats` before `CombatRoom` refills and activates it. Movement speed, attack range, and cooldown remain unchanged so depth scaling does not silently rewrite combat feel.
- The isolated prototype simulation uses the same health/damage profile, but normal `Gameplay` still disables that fallback.
- The normal Dungeon HUD line shows the selected or active band plus threat, reward-power, and material-yield multipliers. The user confirmed the focused Depth 1/2 enemy HP/damage and Depth 2 reward level/power comparison on 2026-06-08.

작성일: 2026-05-02  
문서 목적: 지하 던전 크롤링, 자동/직접 전투, 실패/보상 규칙 정의

## 1. 시스템 목적

지하 던전은 이 게임의 핵심 장비 파밍 공간이다. 지상 디펜스가 재화를 쌓는 곳이라면, 던전은 영웅이 위험을 감수하고 더 좋은 장비와 희귀 재료를 얻는 곳이다.

## 2. 던전 기본 구조

MVP 던전은 방 단위로 구성한다.

```text
입구 방
→ 일반 전투 방
→ 엘리트 방
→ 보스 방
→ 보상/귀환
```

플레이어용 런타임에서는 던전 때문에 지상 전선 씬을 언로드하지 않는다. MVP/Phase B는 `SampleScene`/`Gameplay` 안의 `DungeonRoot`에서 방 프리팹을 순서대로 로드하거나, 처음에는 같은 공간을 재사용한다. `DungeonPrototype`은 던전만 빠르게 확인하는 선택적 테스트 샌드박스일 뿐, 실제 플레이 흐름의 기본 구조가 아니다.

나중에 직접 조작 던전 비주얼이 커지면 additive 씬을 쓸 수 있다. 이때도 `GameSystems`, `DefenseDirector`, `ExpeditionDirector`, 저장 매니저는 유지하고, additive 씬은 던전 방 배치/카메라/적 비주얼만 싣는다.

## 3. 플레이 방식

던전에는 두 입장 방식이 있다.

| 방식 | 설명 | MVP 포함 |
| --- | --- | --- |
| 직접 입장 | 플레이어가 영웅을 조작한다. | 포함 |
| 자동 원정 | 전투력 기반 결과 계산 또는 간단 자동 전투. | 포함, 단순형 |

### 직접 입장

조작 기준:

- 지면 클릭: 이동
- 적 클릭: 사거리 밖이면 접근 후 1회 공격
- 적 클릭: 사거리 안이면 1회 공격
- Shift+클릭: 제자리 공격
- 스킬 키: 스킬 사용

중요:

```text
한 번 클릭은 한 번 명령이다.
자동으로 끝없이 추격/공격하지 않는다.
```

### 자동 원정

MVP 자동 원정은 간단한 계산형으로 시작한다.

```text
영웅 전투력
장비 상태
던전 난이도
보스 위험도
```

이 값을 비교해 성공/실패와 보상을 결정한다.

나중에 실제 자동 전투 AI로 교체할 수 있다.

## 4. 던전 상태 머신

```text
Ready
→ Entering
→ RoomRunning
→ RoomClear
→ NextRoom
→ BossRunning
→ Completed

RoomRunning
→ Failed
→ ReturnToCitadel
```

## 5. 방 종류

| 방 | 역할 |
| --- | --- |
| EntranceRoom | 시작, 튜토리얼 |
| CombatRoom | 일반 적 전투 |
| EliteRoom | 강한 적, 좋은 보상 |
| BossRoom | 던전 클리어 관문 |
| RewardRoom | 보상 확인, 귀환 |

MVP에서는 `CombatRoom`, `BossRoom`만 있어도 된다.

## 6. 적 구성

MVP 적:

| 적 | 역할 |
| --- | --- |
| SkeletonGrunt | 기본 근접 적 |
| SkeletonArcher | 원거리 압박 |
| EliteGuard | 강한 일반 적 |
| TombBoss | 첫 보스 |

적은 복잡한 AI보다 명확한 역할이 중요하다.

기본 AI:

```text
영웅 감지
→ 접근
→ 사거리 안이면 공격
→ 죽으면 드랍 판정
```

보스 AI:

```text
기본 공격
→ 예고 장판
→ 돌진 또는 광역 공격
→ 반복
```

보스 패턴은 직접 조작의 이득을 보여주는 장치다.

## 7. 실패 규칙

던전 실패 조건:

- 영웅 체력 0
- 제한 시간 초과, MVP에서는 생략 가능
- 플레이어 귀환 선택

실패 결과:

| 항목 | 처리 |
| --- | --- |
| 확정 보상 | 유지 |
| 미확정 보상 | 일부 또는 전부 손실 |
| 경험치 | 획득분 일부 손실 |
| 장비 내구도 | 손상 |
| 영웅 상태 | 성채 복귀 |

MVP 단순 규칙:

```text
보스 처치 전 실패 = 이번 원정 아이템 보상 없음, 경험치 50%만 획득
보스 처치 후 귀환 = 보상 확정
```

## 8. 보상 규칙

던전 보상은 지상 보상과 다르다.

| 보상 | 용도 |
| --- | --- |
| 장비 | 영웅 강화 |
| 희귀 재료 | 장비 강화/변형 |
| 정수 | 고급 제작 |
| 베이스 아이템 | 제작 시작점 |

MVP 보상:

- 일반 장비
- 마법 장비
- 희귀 장비
- 정수
- 변형석

## 9. 던전 난이도

던전 난이도는 깊이로 표현한다.

```text
던전 깊이 1
던전 깊이 2
던전 깊이 3
...
```

깊이가 오르면:

- 적 체력 증가
- 적 공격력 증가
- 엘리트 등장 확률 증가
- 장비 등급/옵션 품질 상승

## 10. 기존 코드와 연결

현재 캐릭터 구조를 유지한다.

| 기존 컴포넌트 | 사용 |
| --- | --- |
| CharacterActor | 영웅/적 허브 |
| CharacterMotor | 이동 |
| CombatDriver | 공격 실행 |
| Health | 현재 체력 |
| CharacterStats | 계산 스탯 |
| PlayerController | 직접 조작 |

현재 구현:

| 컴포넌트 | 역할 |
| --- | --- |
| EnemyAIController | 적 AI |
| ExpeditionDirector | 방 진행 관리 |
| CombatRoom | 방 단위 전투 |
| LootDropper | 보상 생성 |

추가 예정:

| 컴포넌트 | 역할 |
| --- | --- |
| AutoCombatController | 자동 원정/자동 전투 |

## 11. Unity 구현 순서

1. `SampleScene`/`Gameplay` 안에 `DungeonRoot`를 둔다.
2. `DungeonRoot` 아래에 영웅 프리팹, 방 컨테이너, 기본 적 프리팹을 배치한다.
3. `EnemyAIController`를 추가한다.
4. `CombatRoom`이 적 생존 수를 감지하게 구현한다.
5. 방 클리어 시 보상 후보를 생성한다.
6. 보스 방을 추가한다.
7. 보스 처치 시 보상을 확정한다.
8. 실패 시 보상 손실/귀환 처리를 한다.
9. 자동 원정은 전투력 계산형으로 먼저 구현한다.
10. 던전 비주얼이 커져 별도 로딩이 필요해질 때만 additive `DungeonView` 씬을 검토한다.

## 12. 완료 기준

- 직접 조작으로 방 하나를 클리어할 수 있다.
- 보스를 처치하면 장비 보상이 나온다.
- 실패하면 보상 일부가 사라진다.
- 장비 장착 후 영웅이 강해진다.
- 던전 보상이 지상 강화 또는 제작에 쓰인다.

## 13. 2026-05-21 Current Implementation Note

- The first Phase C room path now distinguishes three combat paths in code/HUD: tracked enemies, prototype simulation, and setup blocked.
- `EnemySpawner` setup blockers stop `CombatRoom` from using prototype simulation, so a missing prefab or missing spawned `Health` cannot silently clear the dungeon.
- `LootDropper` records whether the clear reward came from the authored weighted table, the legacy definition list, or prototype fallback.
- The current `Gameplay` scene already wires `PF_DungeonEnemy_Melee`, one spawn point, and the authored tier-1 reward table. The remaining completion gate is Play Mode validation of feel, activation timing, click combat, reward grant, equip/salvage, and save/load.

## 14. 2026-06-06 Prefab Spawn Contract

- The melee prefab path requires `Health`, `CharacterActor` on the Enemy team, `EnemyAIController`, an enabled `NavMeshAgent`, and an enabled collider for player clicks.
- `EnemySpawner` resolves every intended spawn position with `NavMesh.SamplePosition` before creating any enemies. If one intended point has no valid NavMesh within the configured radius, the room stays setup-blocked instead of spawning an inert tracked target or falling back to prototype simulation.
- Current `Gameplay` enables `Snap Spawn Points To Nav Mesh` with a `2` unit sample radius. This is a placement safety margin, not a substitute for a correctly baked room NavMesh.
- P0-D Play Mode confirmation is accepted. Reopen this path only for a regression in activation timing, chase/attack, routed click damage, HP/death/result feedback, authored reward continuity, or clear-then-restart behavior.
