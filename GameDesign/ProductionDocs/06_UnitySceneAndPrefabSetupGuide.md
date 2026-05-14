# Unity Scene And Prefab Setup Guide

작성일: 2026-05-03
문서 목적: Unity 씬, 폴더, 프리팹, 컴포넌트 세팅 지시서

## 1. 기본 원칙

이 문서는 Unity 에디터에서 무엇을 만들지 지시하기 위한 문서다. 구현자는 이 문서를 보고 씬과 프리팹을 만들 수 있어야 한다.

원칙:

- MVP는 적은 씬으로 시작한다.
- 씬보다 프리팹과 데이터로 확장한다.
- 기존 `Assets/02.Scripts` 구조를 유지한다.
- 캐릭터는 `CharacterActor` 허브와 컴포넌트 조합을 유지한다.
- 지상 디펜스는 수동 웨이브가 아니라 지속 전선으로 구현한다.
- 지상 디펜스와 지하 던전은 시스템 폴더를 분리한다.

## 2. 추천 Assets 폴더 구조

현재 구조를 유지하면서 아래 폴더를 추가한다.

```text
Assets/
  01.Scenes/
    Bootstrap.unity
    DefensePrototype.unity
    DungeonPrototype.unity
  02.Scripts/
    Bootstrap/
    Character/
      Controllers/
      Core/
      Stats/
    GroundDefense/
      Runtime/
      UI/
    Dungeon/
      Runtime/
      Data/
      UI/
    Items/
      Runtime/
      Data/
      UI/
    Save/
    Shared/
    UI/
  03.Characters/
    Hero/
    Enemies/
  04.Prefabs/
    GroundDefense/
    Dungeon/
    UI/
  05.ScriptableObjects/
    GroundDefense/
    Dungeon/
    Items/
    Balance/
  06.Art/
    Sprites/
    Materials/
    VFX/
```

MVP에서 꼭 필요한 폴더만 먼저 만든다.

우선 생성:

```text
Assets/02.Scripts/GroundDefense/Runtime
Assets/02.Scripts/GroundDefense/UI
Assets/02.Scripts/Shared
Assets/02.Scripts/Save
Assets/04.Prefabs
```

## 3. 씬 구성

### Bootstrap 씬

역할:

- 게임 전체 진입점
- 저장 데이터 로드
- 공통 매니저 생성
- 첫 화면으로 이동

MVP에서는 `SampleScene`을 임시 Bootstrap으로 사용해도 된다. 다만 최종적으로는 별도 `Bootstrap.unity`를 만든다.

필수 오브젝트:

```text
GameBootstrap
SaveManager
SceneLoader
AudioManager, MVP에서는 생략 가능
```

### DefensePrototype 씬

역할:

- 지상 전선 플레이
- 지속 보상/강화 UI 테스트
- Hold/Push와 Frontline Level 상승 테스트

숫자 프로토타입 필수 오브젝트:

```text
GameSystems
  CurrencyWallet
  DefenseUpgradeModel
  DefenseDirector

Canvas_Defense
  DefenseHud
```

시각 프로토타입 이후 추가 오브젝트:

```text
DefenseRoot
  DefenseWall
  TowerBattery
  DefenderSquad

SpawnPoint_Enemy
WallPoint
Camera
```

### DungeonPrototype 씬

역할:

- 영웅 직접 조작
- 적 AI
- 방 클리어
- 보상 드랍

필수 오브젝트:

```text
DungeonRoot
  ExpeditionDirector
  CombatRoom
  LootDropper

HeroSpawnPoint
EnemySpawnPoints
RoomBounds
Camera
Canvas_Dungeon
```

2026-05-10 기준 최소 방 결과 + 보상 테스트:

1. 빈 오브젝트 `DungeonRoot`를 만든다.
2. `GameSystems`에 `SimpleInventory`를 붙인다.
3. `DungeonRoot`에 `ExpeditionDirector`, `CombatRoom`, `LootDropper`를 붙인다.
4. `LootDropper > Inventory`에 `GameSystems`의 `SimpleInventory`를 연결한다.
5. 실제 장비 에셋이 아직 없으면 `LootDropper > Create Prototype Reward When Table Empty`를 켜 둔다.
6. 아직 영웅/적 프리팹이 없으면 `CombatRoom > Simulate When No Enemies`를 켜 둔다.
7. Play Mode에서 `ExpeditionDirector.StartExpedition()`을 호출한다.
8. `CombatRoom`이 시작 카운트다운 뒤 프로토타입 체력/DPS 계산으로 `CompleteRoom()` 또는 `FailExpedition()`을 호출하는지 Inspector에서 확인한다.
9. 클리어 시 `SimpleInventory.Count`가 1 증가하고 `ExpeditionDirector.rewardPending`이 꺼지는지 확인한다.
10. 실제 적 프리팹을 붙인 뒤에는 `CombatRoom > Hero Health`와 `Enemy Healths`에 각 `Health` 컴포넌트를 연결해서 생존 판정 기반으로 바꾼다.

이 fallback 보상은 프로토타입 전용이다. 실제 밸런스 단계에서는 `LootDropper > Reward Definitions`에 작성된 `ItemDefinition` 에셋을 넣고, 별도 드랍 테이블/품질 판정으로 교체한다.

2026-05-11 기준 임시 루프 HUD 테스트:

1. `GameSystems`에 `DungeonDebugHud`와 `InventoryDebugHud`를 붙인다.
2. `DungeonDebugHud > Expedition`에는 `DungeonRoot`의 `ExpeditionDirector`, `Combat Room`에는 `CombatRoom`, `Loot Dropper`에는 `LootDropper`, `Inventory`에는 `GameSystems`의 `SimpleInventory`를 연결한다.
3. `InventoryDebugHud > Inventory`에는 `SimpleInventory`, `Salvage Service`에는 `ItemSalvageService`, `Equipment Slots`에는 `Player`의 `EquipmentSlots`, `Wallet`에는 `CurrencyWallet`을 연결한다.
4. `SampleScene`은 위 연결이 이미 들어간 상태다.
5. Play Mode에서 왼쪽 상단 `Dungeon Loop Debug` 패널의 `Start Dungeon`을 누른다.
6. 자동 전투가 끝나기를 기다리거나 `Force Clear`를 눌러 방 클리어와 보상 지급을 확인한다.
7. 왼쪽 하단 `Inventory Loop Debug` 패널에서 Inventory count와 최신 아이템 이름을 확인한다.
8. `Equip Latest`를 눌러 최신 아이템이 장착 플래그와 `EquipmentSlots`에 반영되는지 확인한다.
9. `Salvage Latest`를 눌러 인벤토리에서 아이템이 빠지고 Scrap/Essence/AlterStone 보상이 지갑에 들어가는지 확인한다.
10. 이 HUD는 production UI가 아니라 Play Mode smoke test용 OnGUI 도구다. 최종 UI 프리팹을 만들 때는 같은 버튼 흐름을 일반 Canvas/TMP UI로 옮긴다.

2026-05-14 save/load smoke test update:

1. In Play Mode, use `Dungeon Loop Debug` -> `Start Dungeon`, then wait for a clear or press `Force Clear`.
2. Use `Inventory Loop Debug` -> `Equip Latest`, then confirm `Hero Stats` changes.
3. Use `Dungeon Loop Debug` -> `Save`, then `Validate Saved File`. This checks the JSON written under `Application.persistentDataPath`.
4. Use `Dungeon Loop Debug` -> `Load`, then `Validate Snapshot`. This checks the live runtime state after loading.
5. Restart Play Mode and repeat `Validate Snapshot` to confirm the persisted inventory count, equipped item ids, and prototype snapshot-power stat bridge survive a full session restart.
6. This is still a debug HUD smoke test. It does not replace the later production inventory UI or authored item-definition registry.

## 4. 프리팹 목록

### 지상 디펜스 프리팹

| 프리팹 | 구성 컴포넌트 | 역할 |
| --- | --- | --- |
| `PF_GameSystems` | CurrencyWallet, DefenseUpgradeModel, DefenseDirector | 지속 전선 숫자 시뮬레이션 |
| `PF_DefenseHud` | DefenseHud | UI |
| `PF_DefenseWall` | DefenseWall, HealthBarUI | 시각 단계 성벽 체력 |
| `PF_TowerBattery` | TowerBattery | 시각 단계 자동 공격 |
| `PF_DefenderSquad` | DefenderSquad | 시각 단계 병력 전투력 |
| `PF_DefenseEnemy_Grunt` | DefenseEnemy, EnemyMover | 시각 단계 기본 적 |
| `PF_DefenseEnemy_Shield` | DefenseEnemy, EnemyMover | 시각 단계 탱커 적 |
| `PF_DefenseEnemy_Runner` | DefenseEnemy, EnemyMover | 시각 단계 빠른 적 |

MVP 숫자 검증은 `PF_GameSystems`와 `PF_DefenseHud`만으로 시작한다.

### 던전 프리팹

| 프리팹 | 구성 컴포넌트 | 역할 |
| --- | --- | --- |
| `PF_Hero` | CharacterActor, CharacterMotor, CombatDriver, Health, CharacterStats, PlayerController | 플레이어 영웅 |
| `PF_DungeonEnemy_Melee` | CharacterActor, CharacterMotor, CombatDriver, Health, CharacterStats, EnemyAIController | 근접 적 |
| `PF_DungeonEnemy_Ranged` | 위와 동일 + 원거리 공격 설정 | 원거리 적 |
| `PF_DungeonBoss` | CharacterActor, BossAIController | 보스 |
| `PF_CombatRoom` | CombatRoom, EnemySpawner | 방 |
| `PF_LootChest` | LootDropper 또는 LootContainer | 보상 |

### UI 프리팹

| 프리팹 | 역할 |
| --- | --- |
| `PF_MainTabsHud` | 지상/던전/장비/영웅 탭 |
| `PF_DefensePanel` | 지상 전선 상태 |
| `PF_DungeonPanel` | 던전 선택 |
| `PF_InventoryPanel` | 장비/제작 |
| `PF_ToastMessage` | 보상/실패 메시지 |

## 5. 컴포넌트 책임

### GroundDefense

| 스크립트 | 책임 |
| --- | --- |
| CurrencyWallet | 재화 보관과 소비 |
| ResourceId | 재화 종류 |
| ResourceAmount | 재화/수량 묶음 |
| FrontlineMode | Hold/Push 구분 |
| DefenseState | Idle/Holding/Pushing/Breached 상태 |
| DefenseRuntimeState | 단계, 압박, 성벽, 진행도 상태 |
| DefenseDirector | 지속 압박, 보상, 단계 상승, 돌파 판정 |
| DefenseUpgradeModel | 강화 레벨과 비용 |
| DefenseHud | 버튼과 표시 갱신 |
| DefenseEnemy | 시각 단계 지상 적 스탯과 피격 |
| EnemyMover | 시각 단계 성벽 방향 이동 |
| DefenseWall | 시각 단계 성벽 체력과 손상 |
| TowerBattery | 시각 단계 자동 타겟팅/공격 |
| DefenderSquad | 시각 단계 병력 전투력 |

### Dungeon

| 스크립트 | 책임 |
| --- | --- |
| ExpeditionDirector | 던전 진행 |
| CombatRoom | 방 클리어 조건 |
| EnemySpawner | 방 적 생성 |
| EnemyAIController | 적 행동 |
| BossAIController | 보스 패턴 |
| LootDropper | 보상 생성 |
| DungeonResult | 원정 결과 |

### Items

| 스크립트 | 책임 |
| --- | --- |
| ItemDefinition | 장비 정적 데이터 |
| ItemInstance | 장비 인스턴스 |
| AffixDefinition | 옵션 정적 데이터 |
| Inventory | 아이템 보관 |
| EquipmentService | 장착/해제 |
| CraftingService | 제작/강화/분해/변형 |
| ItemRoller | 옵션 굴림 |

### Save

| 스크립트 | 책임 |
| --- | --- |
| SaveManager | 저장/로드 |
| GameSaveData | 저장 루트 |
| DefenseSaveData | 지상 저장 |
| HeroSaveData | 영웅 저장 |
| InventorySaveData | 인벤토리 저장 |

## 6. Unity 에디터 세팅 순서

### Step 1. 숫자 프로토타입 배치

1. `SampleScene` 또는 `DefensePrototype`을 연다.
2. 빈 오브젝트 `GameSystems`를 만든다.
3. `CurrencyWallet`, `DefenseUpgradeModel`, `DefenseDirector`를 붙인다.
4. `CurrencyWallet`에 초기 Gold/Scrap을 넣는다.
5. Play 버튼으로 Frontline Level, Pressure, Gold/Scrap이 변하는지 확인한다.

### Step 2. HUD 연결

1. Canvas를 만든다.
2. TextMeshPro Text를 만든다.
3. 버튼을 만든다.
4. Canvas 안의 빈 오브젝트에 `DefenseHud`를 붙인다.
5. `DefenseHud`에 Text/Button 슬롯을 연결한다.

추천 표시:

```text
State / Mode
Frontline Level
Wall Health
Pressure / Progress
Gold / Scrap
Upgrade Levels
Upgrade Costs
```

추천 버튼:

```text
Start
Repair
Toggle Hold/Push
Wall Upgrade
Tower Upgrade
Defender Upgrade
```

### Step 3. 시각 전투 확장

숫자 감각이 맞으면 다음을 추가한다.

1. `DefenseWall` 오브젝트 배치.
2. `TowerBattery` 오브젝트 배치.
3. `DefenseEnemy` 프리팹 제작.
4. 적이 오른쪽에서 왼쪽으로 계속 이동하게 구현.
5. 실제 적 개체 수와 `EnemyPressure`를 연결한다.

## 7. 씬 전환 목표

MVP 초기에는 씬 전환 없이 하나의 씬에서 탭만 바꿔도 된다.

추천 첫 구현:

```text
SampleScene 또는 DefensePrototype 하나에서
지상 전선 수치/전투 구현
→ 던전은 버튼 클릭 시 결과 계산
→ 이후 DungeonPrototype 씬 분리
```

이유:

- 첫 목표는 루프 검증이다.
- 씬 전환과 로딩 구조를 너무 일찍 만들면 범위가 늘어난다.

## 8. 완료 기준

Unity 세팅 완료 기준:

- `DefensePrototype` 씬에서 지속 전선이 돌아간다.
- Frontline Level, Pressure, Wall Health, Gold/Scrap이 표시된다.
- Hold/Push 버튼이 동작한다.
- 강화 버튼이 실제 수치에 영향을 준다.
- 시각 단계에서는 적이 성벽으로 계속 이동한다.
- 던전 버튼이 최소한 결과 로그를 반환한다.
