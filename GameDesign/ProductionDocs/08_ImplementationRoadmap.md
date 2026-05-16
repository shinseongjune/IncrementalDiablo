# Implementation Roadmap

작성일: 2026-05-03
문서 목적: 실제 구현 순서, 완료 기준, 작업 분해

## 1. 구현 원칙

첫 목표는 완성판이 아니라 루프 검증이다.

```text
지상 전선이 계속 싸우며 돈과 기본 재료를 만든다.
돈과 재료로 성벽/포탑/병력을 강화한다.
영웅이 던전에서 장비와 희귀 재료를 얻는다.
장비와 재료가 다시 지상/던전을 강하게 만든다.
```

이 네 문장이 실제 게임에서 작동하면 첫 단계는 성공이다.

### 프로토타입 탈출 규칙

루프 검증이 끝난 뒤에도 디버그형 보조 화면과 계산용 fallback만 계속 키우지 않는다.

- 첫 연결이 확인되면 다음 목표는 **보이는 게임**이다.
- 현재 프로젝트에서 제품 관점의 다음 공백은 `Phase 2. 지상 디펜스 시각 프로토타입`과 `Phase 4. 던전 직접 조작 MVP`다.
- 이후 작업은 가능한 한 숨은 시뮬레이션을 실제 장면, 실제 적, 실제 입력, 실제 아이템 에셋으로 치환해야 한다.
- 검증 helper는 필요한 만큼만 두고, 같은 종류의 내부 보조 작업을 연속해서 쌓지 않는다.

즉, 첫 루프가 증명된 뒤의 질문은 "이걸 더 잘 측정할까?"가 아니라 "이제 무엇을 실제 게임으로 바꿀까?"여야 한다.

## 2. Phase 0. 문서 기준 고정

목표:

- 기획 방향을 디펜스 중심으로 고정.
- 생산건물/진군/돌파 규칙 제거.
- 수동 웨이브 목록 대신 지속 전선과 자동 단계 상승 구조로 정리.
- 구현 문서 세트 준비.

완료 기준:

- `ProductionDocs` 문서가 존재한다.
- Unity 세팅 문서가 있다.
- MVP 범위가 명확하다.

## 3. Phase 1. 지속 전선 숫자 프로토타입

목표:

Unity 씬에서 실제 적 오브젝트 없이도 지속 전선이 돌아가게 한다.

작업:

1. `CurrencyWallet` 작성.
2. `DefenseRuntimeState` 작성.
3. `FrontlineMode` 작성.
4. `DefenseDirector` 작성.
5. `DefenseUpgradeModel` 작성.
6. `DefenseHud` 작성.
7. Hold/Push 전환 구현.
8. 압박, 성벽 피해, 단계 상승, 지속 보상 구현.

완료 기준:

- Play 버튼 후 전선 전투가 계속 진행된다.
- Gold/Scrap이 시간 단위로 쌓인다.
- Hold에서는 현재 단계 파밍이 가능하다.
- Push에서는 위험이 커지지만 Frontline Level이 오른다.
- 방어력이 부족하면 성벽이 손상되거나 전선이 돌파된다.
- 강화하면 이전에 막힌 압박을 더 잘 버틴다.

예상 파일:

```text
Assets/02.Scripts/Shared/CurrencyWallet.cs
Assets/02.Scripts/Shared/ResourceAmount.cs
Assets/02.Scripts/Shared/ResourceId.cs
Assets/02.Scripts/GroundDefense/Runtime/FrontlineMode.cs
Assets/02.Scripts/GroundDefense/Runtime/DefenseState.cs
Assets/02.Scripts/GroundDefense/Runtime/DefenseRuntimeState.cs
Assets/02.Scripts/GroundDefense/Runtime/DefenseDirector.cs
Assets/02.Scripts/GroundDefense/Runtime/DefenseUpgradeModel.cs
Assets/02.Scripts/GroundDefense/UI/DefenseHud.cs
```

## 4. Phase 2. 지상 디펜스 시각 프로토타입

목표:

2D/고정 화면에서 적이 계속 성벽으로 이동하고 포탑/병력이 자동으로 막는 장면을 만든다.

작업:

1. `DefensePrototype` 씬 생성.
2. 적 스폰 지점과 성벽 배치.
3. `DefenseEnemy` 구현.
4. `EnemyMover` 구현.
5. `TowerBattery` 구현.
6. 적 체력/사망 처리.
7. 성벽 피해 표현.
8. 숫자 압박 모델과 실제 적 개체 수를 연결.

완료 기준:

- 적이 오른쪽에서 왼쪽으로 계속 온다.
- 포탑과 병력이 자동으로 적을 공격한다.
- 적이 죽으면 사라진다.
- 적 압박이 커지면 성벽이 손상된다.
- 전투가 끊기지 않는다.

## 5. Phase 3. 저장/로드

목표:

재화, Frontline Level, 강화 상태, 성벽 상태가 게임을 껐다 켜도 유지된다.

작업:

1. `GameSaveData` 작성.
2. `SaveManager` 작성.
3. JSON 저장/로드 구현.
4. 지상 데이터 저장.
5. 재화 저장.
6. 오프라인 시간 기록.
7. 오프라인 보상/손상 계산.

완료 기준:

- 저장 후 재실행해도 Gold/Scrap/Frontline Level/강화 상태가 유지된다.
- 오프라인 보상 계산이 된다.
- 오프라인 중 돌파가 예상되면 그 지점에서 계산이 멈춘다.

## 6. Phase 4. 던전 직접 조작 MVP

목표:

기존 캐릭터 구조를 이용해 방 하나와 보스 하나를 클리어한다.

작업:

1. `DungeonPrototype` 씬 생성.
2. 영웅 프리팹 정리.
3. 적 프리팹 생성.
4. `EnemyAIController` 작성.
5. `CombatRoom` 작성.
6. 보스 프리팹 생성.
7. 방 클리어 판정.
8. 보상 생성.

완료 기준:

- 영웅이 이동/공격한다.
- 적이 영웅을 공격한다.
- 보스를 처치하면 보상이 생성된다.
- 실패하면 성채로 돌아온다.

## 7. Phase 5. 아이템/장비 연결

목표:

던전 보상이 실제 장비가 되고, 장비가 영웅 또는 지상 방어에 영향을 준다.

작업:

1. `ItemDefinition` 작성.
2. `ItemInstance` 작성.
3. `Inventory` 작성.
4. `EquipmentService` 작성.
5. 장착 UI 최소 구현.
6. 장비 스탯을 `CharacterStats`에 반영.
7. 지상 보너스를 `DefenseUpgradeModel` 또는 별도 보너스 모델에 반영.

완료 기준:

- 던전에서 얻은 장비를 장착할 수 있다.
- 영웅 공격력/체력이 오른다.
- 지상 보너스 옵션이 포탑/성벽에 반영된다.

## 8. Phase 6. 제작/강화

목표:

지상 보상과 던전 보상을 같이 써서 장비를 강화한다.

작업:

1. `CraftingService` 작성.
2. 제작 UI 최소 구현.
3. 장비 강화 구현.
4. 분해 구현.
5. 옵션 변형 1종 구현.

완료 기준:

- Gold/Scrap으로 베이스 제작.
- Essence로 장비 강화.
- 장비 분해로 재료 회수.
- 강화 장비로 이전 막힘을 넘을 수 있다.

## 9. Phase 7. 30분 플레이 테스트

목표:

처음부터 30분 동안 루프가 돌아가는지 확인한다.

테스트 체크리스트:

| 시간 | 체크 |
| --- | --- |
| 5분 | 첫 지속 보상 확인 |
| 10분 | 첫 Hold/Push 전환 판단 |
| 15분 | 첫 수리 또는 강화 |
| 20분 | 첫 던전 클리어 |
| 30분 | 강화 후 이전 압박 구간 해결 |

성공 기준:

- 다음 행동이 항상 보인다.
- 재화가 어디 쓰이는지 이해된다.
- 장비를 얻으면 체감된다.
- 지상과 지하가 따로 놀지 않는다.

## 10. 지금 하면 안 되는 일

당장 하지 않는다.

- 수동 웨이브 20개 만들기
- 새 직업 추가
- 화려한 던전 생성기
- 고급 인벤토리 UI
- 네트워크 기능
- Steam 연동
- 여러 던전 테마
- 복잡한 타워디펜스 배치
- 지상 영웅 직접 조작

## 11. 첫 구현 티켓 목록

| ID | 작업 | 완료 기준 |
| --- | --- | --- |
| GD-001 | GroundDefense 폴더 생성 | Runtime/UI 폴더 존재 |
| GD-002 | CurrencyWallet 작성 | Gold/Scrap을 더하고 소비할 수 있음 |
| GD-003 | DefenseRuntimeState 작성 | 단계/압박/성벽/진행도 상태 보유 |
| GD-004 | FrontlineMode 작성 | Hold/Push 구분 가능 |
| GD-005 | DefenseDirector 작성 | 지속 압박, 보상, 단계 상승, 돌파 판정 동작 |
| GD-006 | DefenseUpgradeModel 작성 | 성벽/포탑/병력 강화 수치 계산 |
| GD-007 | DefenseHud 작성 | Frontline Level/압박/재화/버튼 표시 |
| GD-008 | 간단 저장 구현 | Gold/Scrap/Frontline Level/강화 저장 |
| DG-001 | DungeonPrototype 씬 생성 | 영웅과 적 배치 |
| DG-002 | EnemyAIController 작성 | 적이 영웅 추적/공격 |
| IT-001 | ItemDefinition 작성 | 장비 정의 가능 |
| IT-002 | Inventory 작성 | 아이템 보관 가능 |
