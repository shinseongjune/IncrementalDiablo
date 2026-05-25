# Ground Defense System Spec

2026-05-23 Phase C visual bridge note:

- `GroundDefenseLanePresenter` now auto-resolves renderers from assigned pressure/progress marker transforms and colors those markers by runtime state.
- Optional scene-authored `Enemy Flow Markers` move from `EnemySpawnAnchor` to `WallAnchor`; active marker count rises with pressure and all assigned markers stay visible on breach.
- This is a visual bridge only. Enemy art, marker count, lane length, camera framing, and final composition remain manual Unity authoring decisions.

2026-05-24 Phase C ground-combat feedback note:

- `GroundDefenseCombatPresenter` is the first code bridge from marker-only lane presentation toward readable ground combat.
- It does not invent waves or hand-authored encounters. It reads the existing continuous `DefenseDirector.Runtime` values and drives scene-authored pressure actors, wall-contact feedback, and tower/defender attack pulses.
- Scene placement, silhouette, marker art, pulse scale, camera framing, and final composition remain manual Unity authoring decisions.

2026-05-25 Phase C runtime-combat telemetry note:

- `DefenseRuntimeState` exposes last-tick incoming pressure, pressure cleared by defense, wall damage, and push progress as per-second feedback values.
- `GroundDefenseCombatPresenter` uses those values to color pressure actors, scale visible attack-pulse count, and report `pressure +/-/s` plus `wall /s` in `LastCombatMessage`.
- This improves Play Mode validation of the existing continuous frontline simulation without adding manual waves, hand-authored enemy lists, or final art assumptions.

작성일: 2026-05-03
문서 목적: 지상 디펜스를 끊임없는 전선 전투와 자동 단계 상승 구조로 정의한다.

## 1. 시스템 목적

지상 디펜스는 플레이어가 접속하지 않아도 계속 싸움이 이어지는 장기 성장 기반이다. 여기서 중요한 감각은 "스테이지를 하나씩 시작하고 끝낸다"가 아니라 "성채 앞 전선에서 적이 계속 밀려오고, 방어선이 버티며 돈과 기본 재료를 번다"이다.

최종 한 줄:

```text
성채 앞 전선에서 끝없이 몰려오는 적 압박을 버티고, 방어선을 강화해서 더 높은 Frontline Level까지 밀어붙인다.
```

## 2. 핵심 방향

이 시스템은 수동 작성 웨이브 목록을 사용하지 않는다.

사용하지 않는 것:

- `WaveDefinition`을 1, 2, 3, 4... 식으로 계속 만드는 방식
- 매 전투가 시작/종료로 끊기는 스테이지 클리어 구조
- 플레이어가 전투 중 병력을 직접 이동시키는 RTS식 조작

사용하는 것:

- 전투는 계속 흐른다.
- 적 압박은 공식으로 계속 생성된다.
- 단계는 `Frontline Level`로 존재한다.
- `Hold`는 현재 단계에서 안정 파밍한다.
- `Push`는 위험을 높이고 다음 단계 진행도를 채운다.

## 3. 화면 구성

MVP 화면은 한 줄 레인이다.

```text
적 스폰 지점                                      성채
    ↓                                             ↓
오른쪽 ------------------------------------------- 왼쪽

[Enemy Spawn] ---> continuous enemies ---> [Defenders / Wall / Towers] | [Citadel]
```

카메라는 고정이다. 플레이어는 전투 중 유닛을 움직이지 않는다. 플레이어가 하는 일은 수리, 강화, Hold/Push 전환, 던전 파밍으로 방어선을 간접 강화하는 것이다.

## 4. 핵심 오브젝트

| 오브젝트 | Unity 이름 예시 | 역할 |
| --- | --- | --- |
| DefenseDirector | `DefenseDirector` | 지속 전선 시뮬레이션, 단계 상승, 보상, 돌파 판정 |
| DefenseRuntimeState | `DefenseRuntimeState` | 현재 전선 상태, 단계, 압박, 진행도, 성벽 체력, 최근 압박/방어/벽 피해 피드백 |
| DefenseUpgradeModel | `DefenseUpgradeModel` | 성벽/포탑/병력 레벨과 방어 수치 계산 |
| CurrencyWallet | `CurrencyWallet` | Gold/Scrap/Essence/AlterStone 보관과 소비 |
| DefenseHud | `DefenseHud` | 단계, 압박, 진행도, 재화, 수리/강화 버튼 표시 |
| DefenseEnemy | `DefenseEnemy` | 시각 프로토타입 단계에서 실제 적 개체 |
| TowerBattery | `TowerBattery` | 시각 프로토타입 단계에서 자동 포탑 공격 |
| DefenseWall | `DefenseWall` | 시각 프로토타입 단계에서 성벽 피격 표현 |
| GroundDefenseCombatPresenter | `GroundDefenseCombatPresenter` | `DefenseDirector.Runtime` 기반 압박 적, 벽 피격, 공격 펄스 피드백 |

## 5. 상태 구조

```text
Idle
→ Holding
→ Pushing

Holding/Pushing
→ Breached
→ WaitingForRepairOrUpgrade
→ Holding 또는 Pushing
```

| 상태 | 설명 |
| --- | --- |
| Idle | 아직 전선 시뮬레이션이 시작되지 않음 |
| Holding | 현재 Frontline Level에서 안정적으로 파밍 |
| Pushing | 압박이 더 강하지만 다음 Frontline Level 진행도가 오름 |
| Breached | 적 압박이 한계에 도달했거나 성벽 체력이 0 |
| WaitingForRepairOrUpgrade | 수리 또는 강화 후 다시 시작해야 하는 상태 |

## 6. 주요 수치

| 수치 | 의미 |
| --- | --- |
| FrontlineLevel | 현재 전선 단계. 손으로 만들지 않고 공식으로 계속 증가한다. |
| EnemyPressure | 현재 방어선에 쌓인 적 압박. 방어력이 부족하면 증가한다. |
| EnemyPressureCapacity | 버틸 수 있는 압박 한계. 이 값에 닿으면 돌파된다. |
| DefensePower | 포탑과 병력의 초당 방어력 합산. |
| WallHealth | 성벽 체력. 압박이 쌓이면 깎인다. |
| FrontlineProgress | Push 중 다음 단계로 넘어가기 위한 진행도. |

## 7. 진행 공식

MVP는 실제 적 오브젝트 없이 숫자로 먼저 검증한다.

```text
incomingPressure = basePressure * pressureGrowth ^ (frontlineLevel - 1)
if mode == Push:
    incomingPressure *= pushPressureMultiplier

enemyPressure += incomingPressure * deltaTime
enemyPressure -= defensePower * deltaTime
enemyPressure = clamp(enemyPressure, 0, pressureCapacity)
```

압박이 남아 있으면 성벽이 피해를 받는다.

```text
wallDamage = enemyPressure * wallDamagePerPressureSecond * deltaTime
```

Push 상태에서 압박을 0으로 유지할 수 있으면 전선 진행도가 오른다.

```text
surplusDefense = max(0, defensePower - incomingPressure)
progressPerSecond = basePushProgress + surplusDefense * surplusProgressMultiplier
```

진행도가 요구치를 채우면 단계가 오른다.

```text
if frontlineProgress >= progressRequired:
    frontlineLevel += 1
    frontlineProgress = 0
```

## 8. Hold / Push 규칙

| 모드 | 목적 | 위험 | 보상 | 단계 진행 |
| --- | --- | --- | --- | --- |
| Hold | 현재 단계 안정 파밍 | 낮음 | 기본 | 없음 |
| Push | 다음 단계로 밀기 | 높음 | 약간 높음 | 있음 |

Hold는 방치형 안정감을 준다. Push는 "이제 다음 구간을 넘을 수 있나?"라는 목표를 만든다.

## 9. 실패 규칙

실패는 숨겨진 효율 감소가 아니라 화면에서 바로 이해되는 손상으로 처리한다.

조건:

```text
WallHealth <= 0
또는 EnemyPressure >= EnemyPressureCapacity
```

결과:

- 전선 상태가 `Breached`가 된다.
- 지속 보상은 크게 줄지만 0이 되지는 않는다. 수리비가 없는 상태에서도 복구를 기다릴 수 있어야 한다.
- 성벽 수리가 필요하다.
- Frontline Level은 유지한다.
- 플레이어는 수리/강화 후 다시 Hold 또는 Push를 선택한다.

## 10. 보상 규칙

보상은 전투가 계속 흐르는 만큼 시간 단위로 누적된다.

```text
goldPerSecond = baseGoldPerMinute / 60 * rewardGrowth ^ (frontlineLevel - 1)
scrapPerSecond = baseScrapPerMinute / 60 * rewardGrowth ^ (frontlineLevel - 1)
```

Push 중에는 위험을 감수하므로 보상 배율을 조금 높일 수 있다.

```text
if mode == Push:
    reward *= pushRewardMultiplier
```

돌파 후에는 진행은 멈추지만 최소 복구 수입은 남긴다.

```text
if state == Breached:
    reward *= breachedRewardMultiplier
```

`breachedRewardMultiplier`는 초기 프로토타입에서 25%를 사용한다. 실패를 의미 있게 만들되, Gold를 모두 쓴 플레이어가 수리비를 다시 벌 방법까지 잃어버리는 소프트락은 허용하지 않는다.

## 11. 오프라인 진행

오프라인 계산도 같은 공식을 사용한다.

1. 저장된 `lastPlayedAt`과 현재 시간을 비교한다.
2. 오프라인 시간을 적당한 상한으로 자른다.
3. 현재 Frontline Level, Hold/Push 모드, 방어력, 성벽 체력으로 압박과 보상을 계산한다.
4. 돌파가 예상되면 그 시점에서 계산을 멈춘다.
5. 획득 Gold/Scrap, 손상 여부, 현재 Level을 요약해서 보여준다.

예시:

```text
오프라인 2시간 13분
Frontline Lv. 12 유지
획득 Gold: 1,240
획득 Scrap: 88
성벽 손상: 37%
추천: 성벽 수리 또는 포탑 강화
```

## 12. Unity 구현 순서

1. `DefenseDirector`, `DefenseRuntimeState`, `DefenseUpgradeModel`, `CurrencyWallet`를 빈 `GameSystems` 오브젝트에 붙인다.
2. 숫자 HUD로 Frontline Level, Mode, WallHealth, EnemyPressure, Progress, Gold/Scrap을 표시한다.
3. Hold/Push 전환 버튼을 붙인다.
4. 성벽/포탑/병력 강화 버튼을 붙인다.
5. 숫자 시뮬레이션에서 단계 상승과 돌파가 이해되는지 확인한다.
6. `GroundDefenseLanePresenter`를 붙여 숫자 루프가 실제 배치된 앵커/마커/성벽 표시와 함께 움직이는지 확인한다. 이 단계는 시각 전투의 임시 브리지이며, 레인 크기와 카메라 구도는 Unity에서 수동 저작한다.
7. `GroundDefenseCombatPresenter`를 붙여 scene-authored 압박 적, 벽 피격 flash, 타워/수비대 공격 pulse가 같은 `DefenseDirector.Runtime` 값을 읽는지 검증한다. HUD의 `pressure +/-/s`와 `wall /s` 값이 보이는 전투 피드백과 맞는지도 함께 본다.
8. 이후 `DefenseEnemy`, `TowerBattery`, `DefenseWall`을 실제 스탯/타겟팅/처치 규칙이 있는 컴포넌트로 분리해 확장한다.

## 13. 완료 기준

MVP 완료 기준:

- 전투가 끊기지 않고 계속 진행된다.
- 적 압박이 방어력보다 강하면 성벽이 손상된다.
- Hold 상태에서는 현재 단계에서 안정 파밍할 수 있다.
- Push 상태에서는 위험이 커지지만 Frontline Level이 오른다.
- 수리/강화 후 이전에 막힌 단계를 다시 버틸 수 있다.
- Gold/Scrap이 제작 또는 던전 준비에 쓰일 수 있다.
