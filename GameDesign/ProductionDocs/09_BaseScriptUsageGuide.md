# Base Script Usage Guide

작성일: 2026-05-03
목적: 새로 추가한 기본 스크립트가 무엇을 하는지, Unity에서 어떻게 붙여서 확인하는지, 마음에 안 드는 지점을 어떻게 피드백하면 되는지 정리한다.

## 1. 이번에 만든 범위

이번 구현은 `Phase 1. 지속 전선 숫자 프로토타입`을 위한 최소 뼈대다.

2026-05-04 추가 범위: 아이템 드롭률이나 제작 비용을 정하지 않고, 장비 정의 에셋을 영웅 슬롯에 장착하면 `CharacterStats`에 스탯 보정이 반영되는 최소 기반만 추가했다.

2026-05-05 추가 범위: 인벤토리 UI 없이도 장비 정의를 `Scrap/Essence`로 바꾸는 분해 보상 계산과 `ItemSalvageService`를 추가했다. 이 값은 장기 밸런스 확정값이 아니라 중복 장비가 무의미해지는 문제를 막기 위한 프로토타입 경제 규칙이다.

2026-05-06 추가 범위: Rare 장비 분해에서 `AlterStone`을 아주 늦고 적게 회수하는 규칙과 Rare 옵션 변형 비용 계산을 `ItemEconomyModel`에 추가했다. 아직 실제 옵션을 바꾸는 인벤토리/아이템 인스턴스 기능은 없고, 지금은 비용/보상 페이싱을 테스트하기 위한 코드 기반이다.

2026-05-07 implementation scope: `ItemInstance` and `SimpleInventory` add the first runtime item-storage layer. A scene can now keep individual rolled item instances, assign stable ids, export/import inventory save data, and remove an item instance through `ItemSalvageService` when salvaging from an attached inventory. This does not yet add loot drops, inventory UI, item registry lookup after load, or actual affix mutation.

목표는 다음 한 문장이 Unity Play 모드에서 돌아가는 것이다.

```text
전선 전투가 계속 진행되고, Gold/Scrap이 시간 단위로 쌓이며, Push를 켜면 Frontline Level이 오른다.
```

아직 의도적으로 넣지 않은 것:

- 실제 적 오브젝트가 달려오는 시각 전투
- 던전 방/보스/아이템 드랍
- 던전/아이템/장비 인스턴스 저장
- 인벤토리 UI와 장비 드래그 장착
- 복잡한 제작, 옵션, 장비 장착 UI

삭제한 것:

- `WaveDefinition`
- `GroundDefense/Data` 폴더
- 수동으로 웨이브를 계속 작성해야 하는 구조

## 2. 스크립트별 설명

| 스크립트 | 위치 | 역할 | Unity에서 쓰는 법 | 피드백할 때 보면 좋은 부분 |
| --- | --- | --- | --- | --- |
| `ResourceId` | `Assets/02.Scripts/Shared/ResourceId.cs` | Gold, Scrap, Essence, AlterStone 같은 재화 종류 목록 | 직접 붙이지 않는다. 다른 스크립트가 사용한다. | 재화 이름이 너무 많거나 적은지 |
| `ResourceAmount` | `Assets/02.Scripts/Shared/ResourceAmount.cs` | 특정 재화와 수량을 한 묶음으로 표현 | 보상/비용 배열에서 보인다. | 보상/비용 표기가 이해되는지 |
| `CurrencyWallet` | `Assets/02.Scripts/Shared/CurrencyWallet.cs` | 플레이어가 가진 재화를 저장하고 더하거나 소비한다 | `GameSystems` 같은 빈 오브젝트에 붙인다. `Starting Amounts`로 초기 Gold/Scrap을 넣을 수 있다. | 시작 재화가 너무 짜거나 넉넉한지 |
| `FrontlineMode` | `Assets/02.Scripts/GroundDefense/Runtime/FrontlineMode.cs` | `Hold`와 `Push` 모드를 구분한다. | 직접 붙이지 않는다. `DefenseDirector`가 사용한다. | Hold/Push 이름이 이해되는지 |
| `DefenseState` | `Assets/02.Scripts/GroundDefense/Runtime/DefenseState.cs` | Idle, Holding, Pushing, Breached 같은 상태 목록 | 직접 붙이지 않는다. `DefenseDirector`가 사용한다. | 상태명이 직관적인지 |
| `DefenseRuntimeState` | `Assets/02.Scripts/GroundDefense/Runtime/DefenseRuntimeState.cs` | Frontline Level, 성벽 체력, 적 압박, 단계 진행도를 저장 | 직접 붙이지 않는다. `DefenseDirector` Inspector 안에서 보인다. | Pressure/Progress/WallHealth 숫자가 이해되는지 |
| `DefenseUpgradeModel` | `Assets/02.Scripts/GroundDefense/Runtime/DefenseUpgradeModel.cs` | 성벽/포탑/병력 레벨, 성벽 체력, 방어 DPS, 강화 비용을 계산 | `CurrencyWallet`와 같은 오브젝트에 붙인다. 수치 밸런스는 Inspector에서 조정한다. | 강화 비용 증가가 너무 빠른지, 강화 체감이 약한지 |
| `DefenseDirector` | `Assets/02.Scripts/GroundDefense/Runtime/DefenseDirector.cs` | 지속 압박 생성, 보상 지급, 단계 상승, 돌파 판정을 관리 | `GameSystems` 오브젝트에 붙이고 `Wallet`, `Upgrades`를 연결한다. 비워도 같은 오브젝트에서 자동 탐색한다. | Hold/Push 위험도, 보상 속도, 단계 상승 속도가 맞는지 |
| `GameSaveData` | `Assets/02.Scripts/Shared/GameSaveData.cs` | 저장 파일의 루트 데이터와 지상 방어 저장 데이터를 정의한다. | 직접 붙이지 않는다. `DefenseSaveManager`가 JSON으로 읽고 쓴다. | 저장해야 할 값이 빠졌는지 |
| `DefenseSaveManager` | `Assets/02.Scripts/GroundDefense/Runtime/DefenseSaveManager.cs` | Gold/Scrap/Frontline Level/강화/성벽 상태를 로컬 JSON으로 저장하고, 재접속 시 최대 8시간 오프라인 진행을 계산한다. | `GameSystems` 오브젝트에 붙인다. `DefenseDirector`는 비워도 자동 탐색한다. | 오프라인 보상이 너무 후하거나, 돌파 정지가 너무 가혹한지 |
| `ItemSlot` | `Assets/02.Scripts/Items/ItemSlot.cs` | Weapon, Armor, Ring 같은 MVP 장비 부위를 정의한다. | 직접 붙이지 않는다. `ItemDefinition`과 `EquipmentSlots`가 사용한다. | MVP 부위가 너무 많거나 적은지 |
| `ItemRarity` | `Assets/02.Scripts/Items/ItemRarity.cs` | Normal, Magic, Rare 등급만 우선 정의한다. | 직접 붙이지 않는다. `ItemDefinition`이 사용한다. | 초반 등급 구분이 충분한지 |
| `ItemDefinition` | `Assets/02.Scripts/Items/ItemDefinition.cs` | 장비 에셋의 ID, 이름, 슬롯, 등급, 요구 레벨, 파워 범위, 스탯 보정을 정의한다. | Project 창에서 `Create > Incremental Diablo > Items > Item Definition`으로 만든 뒤 스탯 보정을 입력한다. | 장비 한 개가 주는 스탯 체감이 과하거나 약한지 |
| `ItemEconomyModel` | `Assets/02.Scripts/Items/ItemEconomyModel.cs` | 장비 부위/등급/티어에 따라 분해 보상을 계산한다. | 직접 붙이지 않는다. `ItemDefinition.SalvageRewards`와 `ItemSalvageService`가 사용한다. | Scrap/Essence 회수량이 너무 후하거나 짠지 |
| `ItemSalvageService` | `Assets/02.Scripts/Items/ItemSalvageService.cs` | 선택한 장비 정의를 분해해 `CurrencyWallet`에 보상을 더한다. | `GameSystems` 같은 오브젝트에 붙이고 `CurrencyWallet`을 연결한다. 인벤토리 구현 전에는 테스트 버튼/임시 호출에서 사용한다. | 중복 장비가 재료 순환으로 충분히 의미가 생기는지 |
| `ItemInstance` | `Assets/02.Scripts/Items/ItemInstance.cs` | Holds one rolled runtime item with instance id, definition id, rarity, level, power, durability, and affix placeholders. | Created by `SimpleInventory.TryAdd(ItemDefinition, out ItemInstance)` or loaded from `InventorySaveData`. | Whether saved item ids and rolled power remain stable after save/load. |
| `SimpleInventory` | `Assets/02.Scripts/Items/SimpleInventory.cs` | Stores item instances, assigns stable ids, and exports/imports the inventory save slice. | Add it to `GameSystems` beside `CurrencyWallet`, `DefenseSaveManager`, and `ItemSalvageService` for prototype testing. | Capacity, duplicate-id handling, and whether salvage removes the item before paying materials. |
| `StatMod` | `Assets/02.Scripts/Character/Stats/StatMod.cs` | 특정 스탯에 Flat, PercentAdd, PercentMult 보정을 준다. Percent 값은 10 = 10%로 입력한다. | `ItemDefinition`의 Modifiers 배열에서 사용한다. | 퍼센트 입력 방식이 이해되는지 |
| `EquipmentSlots` | `Assets/02.Scripts/Character/Core/EquipmentSlots.cs` | Weapon/Armor/Ring에 장비 정의를 장착하고 `CharacterStats`로 보정을 전달한다. | 영웅 오브젝트의 `CharacterActor`와 함께 붙어 있다. 슬롯에 `ItemDefinition` 에셋을 넣으면 스탯이 바뀐다. | 장비 장착 후 공격력/체력/이동 속도 체감이 맞는지 |
| `DefenseHud` | `Assets/02.Scripts/GroundDefense/UI/DefenseHud.cs` | TMP 텍스트와 버튼을 연결해서 현재 상태와 강화 버튼을 보여준다 | Canvas 안의 HUD 오브젝트에 붙이고 Text/Button 슬롯을 연결한다. | 화면에 보이는 문구가 충분히 직관적인지 |

아이템 경제 테스트 시 `ItemDefinition.SalvageRewards`는 분해 보상 미리보기이고, `ItemDefinition.AffixRerollCost`는 Rare 장비 옵션 변형 비용 미리보기다. Normal/Magic은 변형 비용을 반환하지 않는다. Rare도 낮은 `baseTier`에서는 `AlterStone` 분해 보상이 없으므로, 초반 장비가 너무 빨리 재굴림 루프로 들어가지 않는지 확인해야 한다.

## 3. 가장 빠른 테스트 세팅

1. 씬에 빈 오브젝트를 만들고 이름을 `GameSystems`로 둔다.
2. `GameSystems`에 `CurrencyWallet`, `DefenseUpgradeModel`, `DefenseDirector`, `DefenseSaveManager`를 붙인다. Add `SimpleInventory` and `ItemSalvageService` when testing item instance save/salvage.
3. `CurrencyWallet > Starting Amounts`에 다음을 넣는다.

```text
Gold 100
Scrap 25
```

4. `DefenseDirector`에서 `Start On Play`를 켠다.
5. Play를 누르면 Frontline 전투가 계속 진행된다.
6. Gold/Scrap이 시간 단위로 늘어나는지 본다.
7. `Mode`를 Hold와 Push로 바꿔서 압박과 진행도 차이를 본다.
8. `DefenseUpgradeModel`의 Wall/Tower/Defender 레벨과 비용을 바꿔보며 체감이 맞는지 본다.

저장/로드까지 확인하려면:

1. Play 모드에서 Gold/Scrap을 조금 벌거나 강화 버튼을 눌러 상태를 바꾼다.
2. Play 모드를 끄면 `DefenseSaveManager`가 저장한다.
3. 다시 Play를 누른다.
4. Gold/Scrap, Frontline Level, Wall/Tower/Defender Level, 성벽 체력, Hold/Push 모드가 유지되는지 확인한다.
5. 저장 후 몇 분 뒤 다시 실행하면 최대 8시간 한도 안에서 오프라인 보상과 손상이 계산된다.

저장 파일은 Unity의 `Application.persistentDataPath` 아래 `incremental_diablo_save.json`으로 만들어진다. 아직 장비, 인벤토리, 던전 진행은 저장하지 않는다.

HUD까지 보고 싶다면:

1. Canvas를 만든다.
2. TextMeshPro Text를 5~7개 만든다.
3. Button을 Start, Repair, Mode Toggle, Wall Upgrade, Tower Upgrade, Defender Upgrade 용도로 만든다.
4. Canvas 안의 빈 오브젝트에 `DefenseHud`를 붙인다.
5. `DefenseHud` 슬롯에 위 Text/Button을 연결한다.

## 4. 현재 규칙의 의미

숫자 프로토타입은 실제 적을 만들지 않고 `Enemy Pressure`라는 숫자를 적 무리가 성벽으로 밀어붙이는 압박으로 쓴다.

```text
Enemy Pressure는 계속 생성된다.
Defense Power가 Enemy Pressure를 깎는다.
Enemy Pressure가 남으면 Wall Health가 깎인다.
Push 상태에서 Enemy Pressure를 0으로 유지하면 Frontline Progress가 오른다.
Frontline Progress가 가득 차면 Frontline Level이 오른다.
```

즉, 지금 단계에서 중요한 질문은 그래픽이 아니라 이것이다.

```text
전투가 끊기지 않고 계속 흐르는 느낌이 드는가?
Hold는 안정 파밍처럼 느껴지는가?
Push는 위험하지만 다음 단계로 미는 선택처럼 느껴지는가?
강화했을 때 압박을 더 잘 버티는 느낌이 드는가?
```

## 5. 피드백하기 쉬운 체크리스트

마음에 안 들면 아래처럼 말해주면 바로 고치기 쉽다.

| 보고 싶은 것 | 피드백 예시 |
| --- | --- |
| 전투 흐름 | "전투가 아직 너무 스테이지처럼 느껴져. Progress보다 압박 숫자가 더 계속 움직였으면 해." |
| Hold/Push | "Hold는 너무 심심해. 낮은 확률로 작은 위기가 있었으면 해." |
| 단계 상승 | "Push를 켜도 레벨업이 너무 느려. 첫 단계는 30초 안에 올라갔으면 해." |
| 실패 방식 | "돌파되면 그냥 멈추기보다 성벽 손상이 먼저 누적됐으면 해." |
| 강화 체감 | "포탑 강화가 너무 약해. 한 번 올렸을 때 Pressure가 확 줄었으면 해." |
| 재화 종류 | "Scrap까지는 좋은데 Essence/AlterStone은 아직 안 보였으면 해." |
| HUD 문구 | "Pressure라는 말이 직관적이지 않아. 적 압박 같은 표현이 나아." |

## 6. 다음 구현 후보

이 기본 뼈대가 괜찮으면 다음 순서는 둘 중 하나가 좋다.

1. `DefenseEnemy`, `TowerBattery`, `DefenseWall`을 추가해서 실제 적이 성벽으로 계속 움직이는 시각 프로토타입으로 확장한다.
2. 저장 데이터를 `GameSaveData`의 hero/inventory 영역까지 확장해서 던전 보상과 장비 인스턴스를 유지한다.

지금은 일부러 시스템을 작게 유지했다. 먼저 숫자로 전선 압박, Hold/Push, 단계 상승 속도를 확인하고, 그 다음 화면과 저장을 붙이는 편이 피드백하기 쉽다.

장비 기반은 아직 더 작다. 지금 가능한 것은 `ItemDefinition` 에셋을 만들어 영웅의 Weapon/Armor/Ring 슬롯에 직접 넣고, `CharacterStats.GetValue(...)` 결과가 바뀌는지 확인하는 수준이다. 분해는 `ItemDefinition` 기준의 보상 계산과 지갑 지급만 가능하다. 드롭, 인벤토리, 장비 인스턴스 저장, 제작, 희귀도별 옵션 개수는 아직 구현하지 않았다.
