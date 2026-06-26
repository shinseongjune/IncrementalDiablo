# Data Save And Balance Spec

## 2026-06-21 Production Validation Boundary

- The depth and frontline exports prove deterministic monotonic formulas. They do not prove a fun session length, affordable sinks, or a playable maximum-level economy.
- The `1,000,000,000` multiplier clamp is a runtime safety boundary, not a target content tier. Curve shape must be reviewed before normal progression approaches a clamp plateau.
- New run choices such as E1-A dungeon contracts require a stable id, active-run save field, migration/default rule, deterministic export, and explicit reward denominator before implementation is called complete.
- 2026-06-25 update: save schema v4 adds dungeon contract offer/selection/active fields. `DefenseSaveManager` migrates older saves by generating a default two-contract offer from selected depth and seed, then preserving an active contract for running or reward-pending clears.

## 2026-06-25 Dungeon Contract Balance Export

`DungeonContractModel` is the runtime source of truth for the first E1-A contract set.

- Stable ids: `steady_clear`, `ravenous_pact`, `blood_price`.
- Offer generation: two choices from `BuildOffer(selectedDepth, contractOfferSeed)`, deterministic and save-backed.
- Threat denominator: active dungeon run enemy HP/damage multipliers.
- Reward denominator: one guaranteed per-clear item reward. Risk contracts increase reward by passing a higher reward depth into the existing clear-reward path, not by changing rarity odds or reward count.
- `Tools/Automation/Export-DungeonContracts.ps1` reads the C# starter set, verifies the baseline plus at least one risk/reward contract, and exports `GameDesign/Balance/DungeonContractBalance.csv`.

## 2026-06-26 Rare Affix Balance Export

`ItemEconomyModel.AuthoredRareAffixes` is the runtime source of truth for the first E1-B Rare affix pool.

- Stable ids: `rare_wounding_edge`, `rare_quickened_edge`, `rare_vital_plating`, `rare_runner_plate`, `rare_swift_band`, `rare_runner_band`.
- Coverage: two authored affixes each for Weapon, Armor, and Ring.
- Reroll denominator: `per-paid Rare affix reroll`. The pool does not change dungeon reward count, rarity odds, duplicate conversion, salvage yield, or contract reward-depth offsets.
- Roll formula: `ceil(base_value + item_level * per_item_level + rolled_power * per_rolled_power)`.
- Weighting: slot-valid candidates are weighted by each profile's `weight`; the selected item's current affix id is excluded when the slot has another candidate.
- Save behavior: no schema change. `ItemAffixRoll.affixId` plus `modifier` already persist in `ItemInstanceSaveData`; older saved ids remain readable as legacy ids until the next paid reroll replaces them.
- `Tools/Automation/Export-RareAffixes.ps1` reads the C# pool, verifies unique ids, positive weights, per-slot coverage, slot tags, and exports `GameDesign/Balance/RareAffixPool.csv`.

## 2026-06-11 Ground Defense Balance Model

`GroundDefenseBalanceModel` is the runtime source of truth for D1-A. It uses ten-level bands instead of authored wave rows.

```text
b = floor((frontlineLevel - 1) / 10)
s = (frontlineLevel - 1) % 10

IncomingPressure = 1.65^b * (1 + 0.07s)
DefenseOutput = 1.45^b * (1 + 0.025s)
PressureCapacity = 1.4^b * (1 + 0.04s)
ProgressRequired = 1.5^b * (1 + 0.05s)
Reward = 1.5^b * (1 + 0.05s)
```

- Frontline Level 1 is exactly `x1` for every lane, preserving the existing first-level timing and baseline.
- Every multiplier is monotonic and clamped to `1..1,000,000,000`.
- Defense output multiplies the player's current Wall/Tower/Defender-derived output; it does not replace upgrade choices.
- Band 2 and later starts grant `Gold 120 * 1.6^(band-2)` and `Scrap 16 * 1.6^(band-2)`, saturated to integer limits.
- Milestone rewards need no new save field because they are granted only while crossing a band boundary; the wallet is already saved.
- `Tools/Automation/Export-GroundDefenseBalance.ps1` reads the C# constants, verifies Frontline Levels 1-1000, and exports `GameDesign/Balance/GroundDefenseBalance.csv`.

## 2026-06-08 Dungeon Depth Balance Model

`DungeonDepthBalanceModel` is the runtime source of truth for D0-B. It uses ten-depth milestone bands instead of authored per-depth rows.

```text
b = floor((depth - 1) / 10)
s = (depth - 1) % 10

EnemyHealth = 1.8^b * (1 + 0.08s)
EnemyDamage = 1.5^b * (1 + 0.05s)
RewardPower = 1.55^b * (1 + 0.055s)
MaterialYield = 1.3^b * (1 + 0.03s)
```

- Depth 1 is exactly `x1` for every lane.
- Every multiplier is monotonic and clamped to `1..1,000,000,000` to prevent invalid float growth from corrupting runtime values.
- Enemy movement, range, and cooldown are intentionally excluded from this first balance pass.
- Reward items store source depth in the existing `ItemInstance.level` field and store the final integer power roll. No save schema migration is required.
- `Tools/Automation/Export-DungeonDepthBalance.ps1` reads the C# constants directly, validates depth 1 and monotonic growth, and exports depth 1-100 to `GameDesign/Balance/DungeonDepthBalance.csv`.
- Tuning order: compare survivability first, then reward power, then material yield. Do not change rarity odds in the same pass because one guaranteed reward per clear has a different denominator from D2 per-kill drop math.

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
selectedDepth
highestUnlockedDepth
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

2026-05-14 saved-file validation note: `DefenseSaveManager` can now validate the persisted JSON save file separately from the current runtime snapshot, and `TryLoad()` refuses structurally invalid save files before applying them to the live scene. The debug dungeon HUD exposes both checks as `Validate Snapshot` and `Validate Saved File`, which makes the MVP save/load smoke test cover the actual disk file before requiring a full Play Mode restart.

2026-06-07 Phase D dungeon progression note: save schema v2 adds `DungeonSaveData.selectedDepth` and `highestUnlockedDepth`. `DefenseSaveManager` migrates a v1 save by using its prior active `depth` as the initial selected/highest value, then `GameSaveDataDiagnostics` requires active and selected depth to remain within `1..highestUnlockedDepth`. This is the first explicit save migration hook for long-horizon dungeon progression.

2026-06-09 Phase D item identity note: save schema v3 adds a production item-id migration stage without changing the serialized `ItemInstanceSaveData` shape. `DefenseSaveManager` asks the scene's `ItemDefinitionRegistry` to remap legacy ids before validation and inventory restore. Canonical ids reconnect to live assets; unknown ids remain serialized and visible but are quarantined from equip/salvage/reroll. `LastLoadReport`, save diagnostics, HUD text, and overlay text expose resolved/remapped/unresolved counts so content deletion or id drift cannot silently become snapshot-based gameplay power.

2026-06-25 E1-A contract note: save schema v4 adds `contractOfferSeed`, `offeredContractIdA`, `offeredContractIdB`, `selectedContractId`, `activeContractId`, and `lastContractSummary` to `DungeonSaveData`. Save diagnostics require valid offered/selected ids and require `activeContractId` for running or reward-pending contract resolution.

2026-06-25 defense restore note: loading a save now emits `DefenseDirector.SaveDataApplied`, rebuilds `GroundDefenseNavMeshBattlefield` from the restored authoritative `DefenseRuntimeState`, and stops visual actors from attacking while the restored state is not running. Manual saves also reset the auto-save timer so a player-triggered checkpoint is not immediately overwritten by the next auto-save tick.

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
