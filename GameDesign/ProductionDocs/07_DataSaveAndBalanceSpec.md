# Data Save And Balance Spec

작성일: 2026-05-03
문서 목적: 데이터 구조, 저장 구조, 초기 밸런스 기준 정의

## 1. 데이터 설계 원칙

하드코딩을 줄이고, 반복 테스트를 쉽게 만들기 위해 데이터는 세 층으로 나눈다.

```text
Definition Data = 변하지 않는 설계 데이터
Runtime State = 플레이 중 변하는 상태
Save Data = 종료 후 보존해야 하는 상태
```

지상 디펜스는 수동 웨이브 목록이 아니라 공식 기반 지속 전선을 기본으로 한다.

## 2. ScriptableObject 후보

### GroundDefense

| 데이터 | 필드 |
| --- | --- |
| FrontlineScalingDefinition | basePressure, pressureGrowth, pressureCapacity, rewardGrowth |
| DefenseEnemyDefinition | id, hp, damage, moveSpeed, pressureWeight |
| DefenseUpgradeDefinition | id, targetType, level, costGold, costScrap, value |
| TowerDefinition | id, damage, attackRate, range |

MVP 숫자 프로토타입에서는 `DefenseDirector` Inspector 수치로 충분하다. ScriptableObject는 밸런스가 늘어날 때 분리한다.

### Dungeon

| 데이터 | 필드 |
| --- | --- |
| DungeonDefinition | id, displayName, depth, recommendedPower, roomList, rewardTable |
| RoomDefinition | id, roomType, enemyGroups, clearReward |
| DungeonEnemyDefinition | id, stats, aiType, dropTable |
| BossDefinition | id, stats, patternList, rewardTable |

### Items

| 데이터 | 필드 |
| --- | --- |
| ItemDefinition | id, displayName, slot, baseTier, basePowerRange, tags |
| AffixDefinition | id, statId, minValue, maxValue, tags, weight |
| CraftRecipeDefinition | id, inputCurrencies, inputItems, outputRule |
| LootTableDefinition | id, entries, weights |

## 3. Runtime State

런타임 상태는 씬 안에서 변하지만, 저장하기 전까지는 메모리에 있다.

### DefenseRuntimeState

```text
state
mode
frontlineLevel
wallCurrentHp
wallMaxHp
enemyPressure
enemyPressureCapacity
frontlineProgress
frontlineProgressRequired
totalElapsed
levelElapsed
isDamaged
```

### DungeonRuntimeState

```text
currentDungeonId
currentRoomIndex
heroCurrentHp
temporaryLoot
isExpeditionRunning
```

### InventoryRuntimeState

```text
items
equippedItemIds
currencies
```

## 4. Save Data

저장 루트:

```text
GameSaveData
  version
  savedAtUtc
  playTimeSeconds
  currencies
  defense
  dungeon
  hero
  inventory
  unlocks
```

### DefenseSaveData

```text
frontlineLevel
frontlineMode
wallLevel
towerLevel
defenderLevel
wallCurrentHp
enemyPressure
frontlineProgress
isDamaged
lastOfflineUtc
```

### DungeonSaveData

```text
state
dungeonId
depth
totalRooms
currentRoomIndex
roomsCompleted
elapsedSeconds
rewardPending
lastResult
```

### HeroSaveData

```text
level
experience
baseStats
currentHp
equippedItemInstanceIds
```

### InventorySaveData

```text
itemInstances
nextItemInstanceId
```

### ItemInstanceSaveData

```text
instanceId
definitionId
displayName
slot
rarity
level
rolledPower
affixRolls
durability
equipped
```

2026-05-07 implementation note: `GameSaveData` now includes `hero` and `inventory` sections. `DefenseSaveManager` still owns the local JSON file, but it will also save/load `SimpleInventory` when that component exists in the scene. Loaded items keep ids and rolled values; reconnecting them to `ItemDefinition` assets is a later item registry task.

2026-05-08 implementation note: `GameSaveData` now includes a `dungeon` section. `ExpeditionDirector` writes `DungeonSaveData` for the current prototype run state, and `DefenseSaveManager` saves/loads it when an `ExpeditionDirector` exists in the scene. This is still run-state persistence only; combat room results, rewards, and item-definition lookup remain separate MVP tasks.

2026-05-12 implementation note: `HeroSaveData.equippedItemInstanceIds` is now written from the inventory/equipment state. After `InventorySaveData` loads, `SimpleInventory` first tries to reconnect saved definition ids through its known `ItemDefinition` registry, then `DefenseSaveManager` asks it to restore equipped items into `EquipmentSlots` so their modifiers affect `CharacterStats` again. `LootDropper` registers authored reward definitions with the inventory on scene load. At this point runtime prototype-only items kept their equipped flag but still needed the 2026-05-14 snapshot-power bridge below before they could restore a stat effect without a live definition.

2026-05-14 implementation note: equipped `ItemInstance` objects now contribute definition modifiers, saved affix-roll modifiers, and a small prototype rolled-power modifier by slot. This lets runtime prototype-only equipment restore a debug-quality stat effect from saved slot/rarity/rolledPower even when its live `ItemDefinition` asset is not available after restart. This is a prototype bridge, not the final production item registry or drop-balance model.

## 5. 저장 시점

저장한다:

- Frontline Level 상승 후
- 전선 돌파 후
- Hold/Push 전환 후
- 수리/강화 후
- 장비 획득 후
- 장비 장착/해제 후
- 제작/강화/분해 후
- 씬 전환 전
- 게임 종료 시

MVP에서는 JSON 로컬 저장으로 충분하다.

## 6. 오프라인 보상 계산

입력:

```text
lastOfflineUtc
nowUtc
frontlineLevel
frontlineMode
defensePower
wallCurrentHp
enemyPressure
```

절차:

1. 오프라인 시간 계산.
2. 최대 계산 시간을 적용한다.
3. 현재 단계의 압박 생성량과 방어력을 계산한다.
4. Hold/Push 모드에 따라 보상과 진행도를 계산한다.
5. 성벽 체력 또는 압박 한계가 무너지면 그 지점에서 정지한다.
6. 보상과 손상 상태를 저장한다.

제한:

```text
MVP 오프라인 최대 계산 시간 = 8시간
```

## 7. 초기 밸런스 기준

### 지상 전선

초기값 예시:

| 항목 | 값 |
| --- | --- |
| Wall HP Lv1 | 100 |
| Tower DPS Lv1 | 8 |
| Defender DPS Lv1 | 5 |
| Base Pressure Per Second | 10 |
| Pressure Growth Per Level | 1.12 |
| Push Pressure Multiplier | 1.3 |
| Base Gold Per Minute | 30 |
| Base Scrap Per Minute | 4 |

Frontline Lv.1 목표:

```text
아무 강화 없이 Hold 가능
```

Frontline Lv.3 목표:

```text
첫 Push 성공 가능, 이후 성벽 손상 경험
```

Frontline Lv.8 목표:

```text
포탑 또는 병력 강화 필요
```

### 던전

초기값 예시:

| 항목 | 값 |
| --- | --- |
| Hero HP | 100 |
| Hero Attack | 12 |
| Basic Enemy HP | 35 |
| Basic Enemy Attack | 6 |
| Boss HP | 250 |
| Boss Attack | 12 |

첫 던전 목표:

```text
기본 장비로 클리어 가능하지만,
보스 패턴을 맞으면 위험하다.
```

## 8. 전투력 계산

정확한 밸런스 전까지 단순 계산을 사용한다.

### DefensePower

```text
DefensePower =
  TowerDps
  + DefenderDps
  + WallPressureReduction
```

### IncomingPressure

```text
IncomingPressure =
  basePressure * pressureGrowth ^ (frontlineLevel - 1)
```

Push 상태에서는 압박이 더 강해진다.

```text
if mode == Push:
  IncomingPressure *= pushPressureMultiplier
```

오프라인 판정과 UI 추천에 사용한다. 실제 시각 전투는 이후 이 숫자 모델과 연결한다.

## 9. 밸런스 조정 원칙

조정 순서:

1. Hold가 너무 빨리 무너지는지 확인한다.
2. Push로 첫 단계가 너무 늦게 오르는지 확인한다.
3. 강화 비용을 조정한다.
4. 보상량을 조정한다.
5. 압박 성장률을 조정한다.
6. 마지막으로 플레이타임 목표를 조정한다.

먼저 비용/보상을 만지고, 적 스탯은 나중에 만진다. 적 스탯을 자주 바꾸면 전투 감각이 흔들린다.

## 10. 완료 기준

- 저장/로드 후 Gold/Scrap/Frontline Level/강화 상태가 유지된다.
- 오프라인 보상이 계산된다.
- 오프라인 중 돌파가 예상되면 손상 상태가 저장된다.
- 장비 인스턴스가 저장된다.
- 초기 밸런스로 20-30분 테스트가 가능하다.
