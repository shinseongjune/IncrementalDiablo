# Dungeon Expedition System Spec

## 2026-06-05 P0-D Production Priority

- P0-D is the next active Phase C gate after ground-defense behavior readability was accepted.
- The normal `Gameplay` path must prove `PF_DungeonEnemy_Melee` spawn, `Running` activation, chase/attack, player click attacks, HP/death/result feedback, room clear, and reward continuity without prototype simulation.
- Prefer production-persistent enemy prefab, combat, and feedback work over another temporary diagnostic or room-tint layer.

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
