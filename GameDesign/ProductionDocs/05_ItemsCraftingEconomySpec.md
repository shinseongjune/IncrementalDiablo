# Items Crafting Economy Spec

## 2026-06-21 Production Economy Guard

- The current 78/20/2 Normal/Magic/Rare per-clear table is a baseline, not a release-balanced economy. Any reward-count, rarity, or contract-reward change must declare its denominator and export Unity rows before D2 comparison.
- D2 reference collection is complete enough; the missing production work is Unity-side drop-balance export and validation, not more reference gathering.
- The duplicate conversion sink prevents dead drops, but it does not create build identity by itself. E1-B replaced the prototype Rare reroll (`TD-04`) with an authored affix pool and user-accepted reward -> equip -> reroll -> save/load evidence. The next item gap is not another reroll acceptance pass; it is deeper build identity after encounter variety exists.
- E1-A contract rewards must use the same item/salvage path and export denominator; they must not silently alter rarity odds or bypass duplicate conversion.
- 2026-06-25 E1-A contract reward rule: risk contracts use a reward-depth offset for the existing one guaranteed per-clear item reward. They do not change reward count, the 78/20/2 rarity table, authored weighted-table selection, duplicate conversion, salvage service, or the D2 per-kill comparison denominator.

## 2026-06-26 E1-B Authored Rare Affix Pool

- Rare reroll now calls `ItemInstance.TryApplyAuthoredAffixReroll(...)`, which rolls from `ItemEconomyModel.AuthoredRareAffixes` instead of hard-coded prototype candidates.
- The first authored pool has six entries: two each for Weapon, Armor, and Ring. Each entry has a stable id, display name, slot rule, stat, modifier type, base value, item-level scaling, rolled-power scaling, weight, and tags.
- Reroll keeps the existing denominator and sink: one paid Rare affix reroll through `Gold + Essence + AlterStone`. It does not change reward count, rarity odds, salvage yield, duplicate conversion, or the E1-A contract reward-depth rule.
- When another valid affix exists for the slot, reroll excludes the selected item's current affix id from the candidate set. This keeps a paid reroll from visibly repeating the same affix unless the slot has no alternative.
- Crafting text uses authored affix display names and clear stat text through `ItemEconomyModel.FormatAffixRoll(...)`.
- Save schema does not change. `ItemAffixRoll.affixId` and `modifier` already persist; existing older affix ids stay loaded as legacy ids and are replaced the next time the player pays for a reroll.
- `Tools/Automation/Export-RareAffixes.ps1` exports and checks `GameDesign/Balance/RareAffixPool.csv` with the denominator `per-paid Rare affix reroll`.
- Production evidence accepted 2026-06-26: in `Gameplay`, reward -> equip -> reroll -> save/load kept the authored affix id/text coherent. Reopen E1-B only for affix id, stat refresh, save/load, or crafting-cost regressions.

## 2026-07-04 E2-B Latest Item Comparison

- `PlayableLoopHud` now compares the latest resolved reward item against the currently equipped same-slot item through `EquipmentSlots.GetEquippedItem(...)` and existing inventory equipped flags.
- The comparison uses saved `ItemInstance.RolledPower` only for the first normal-player decision line: equip upgrade, fill empty slot, sidegrade, or keep the stronger equipped item and salvage the spare unless its affix matters.
- This is presentation/decision support for the accepted reward path. It changes no item definition, rarity odds, reward count, reward denominator, salvage yield, affix pool, reroll cost, duplicate-conversion rule, save schema, or D2 pacing assumption.
- Remaining evidence: focused `Gameplay` Play Mode validation of reward -> Item `Compare:` text -> `Next:` equip/salvage hint -> equip or salvage -> save/load.

## 2026-07-05 E2-B Latest Item Action Priority

- `PlayableLoopHud.TryBuildLatestItemDecisionHint(...)` keeps an unresolved or unequipped latest reward item ahead of ready-state contract guidance in the normal `Next:` line.
- This is still presentation/decision support over the existing one guaranteed per-clear reward path. It does not change item definitions, rarity odds, reward count, reward denominator, salvage yield, affix pool, reroll cost, duplicate conversion, save schema, or D2 pacing assumptions.
- Remaining evidence is the same focused `Gameplay` Play Mode path, now with an optional guide-off check so persisted HUD settings cannot hide the reward decision.

## 2026-06-10 Phase D Duplicate Conversion

- `LootDropper` rolls the candidate item before adding it to inventory. `ItemEconomyModel.TryFindAutoConversionMatch(...)` only matches an already-owned resolved item with the same canonical definition id.
- The new reward is auto-converted only when the owned match has both `Level >= candidate.Level` and `RolledPower >= candidate.RolledPower`. Any candidate that improves either axis remains in inventory for player review.
- Conversion uses `ItemSalvageService.TryConvertReward(...)` and the same depth-scaled Scrap/Essence/AlterStone calculation as manual salvage. The reward overlay and dungeon result show the material payout and do not present an older inventory item as the new reward.
- If the salvage service or wallet is unavailable, conversion is skipped and normal inventory grant is attempted. A reward is never deleted merely because the conversion path is unavailable.
- The current rule is deliberately narrow: it does not auto-score different definitions, compare affix builds, create collection bonuses, or add defender gear slots. Those are larger economy decisions.
- Save schema remains v3. Converted rewards never enter inventory; the resulting wallet materials already persist through the existing save path.
- D2 reference principle used: unwanted drops should become deterministic progress, as reflected by the reference pack's salvage-efficiency and equipment-scrap lanes. This project uses immediate single-player material conversion instead of copying D2 trading, vendor, cube, ladder, or alt-character pressure.

## 2026-06-08 Phase D Depth Reward Bands

- Dungeon depth now changes reward value without changing the existing authored rarity weights. The per-clear 78/20/2 Normal/Magic/Rare table and first-Rare pity behavior remain unchanged.
- For depth `d`, `DungeonDepthBalanceModel` uses ten-depth bands with `b = floor((d - 1) / 10)` and `s = (d - 1) % 10`.
- Rolled item power uses `1.55^b * (1 + 0.055s)`. The selected authored definition still controls slot, rarity, and base power range; `ItemInstance` stores `level = depth` and `ceil(base roll * reward multiplier)`.
- Salvage yield uses `1.3^b * (1 + 0.03s)` and rounds each existing Scrap/Essence/AlterStone result to the nearest integer. This keeps early material changes conservative while making later-depth duplicates progressively more valuable.
- Inventory, reward, and crafting overlays call the same `ItemEconomyModel.GetSalvageRewards(ItemInstance)` path as the actual salvage service, so previewed materials cannot silently disagree with the payout.
- Item level and rolled power already persist in `ItemInstanceSaveData`, so this feature adds no save-schema field. Loaded authored items reconnect to their definition and retain their source-depth material multiplier through the saved level.
- D2 reference principle used: keep content-pool/quality selection separate from item-level progression and keep the Unity denominator explicit as one guaranteed reward per clear. This is not a D2 per-kill drop-rate comparison, and the D2 `drop-balance-check` is not applicable because rarity odds did not change.
- `GameDesign/Balance/DungeonDepthBalance.csv` is the deterministic depth 1-100 export for threat, reward power, material yield, and sample item/salvage values.

작성일: 2026-05-03
문서 목적: 재화, 장비, 제작, 강화, 분해 규칙 정의

## 1. 시스템 목적

아이템/제작은 지상과 지하를 연결하는 핵심 시스템이다.

```text
지상 디펜스 → 금화/기본 재료
지하 던전 → 장비/희귀 재료
제작소 → 성장 결과
```

## 2. 재화 종류

MVP 재화:

| 재화 | 획득처 | 사용처 |
| --- | --- | --- |
| Gold | 지상 전선 지속 보상 | 수리, 강화, 제작 비용 |
| Scrap | 지상 전선 지속 보상 | 성벽/포탑/병력 강화 |
| Essence | 던전 | 장비 강화 |
| AlterStone | 던전 | 옵션 변형 |

재화는 처음부터 많이 만들지 않는다. 재화가 많으면 UI와 밸런스가 복잡해진다.

## 3. 장비 부위

MVP 장비 부위:

| 부위 | 이유 |
| --- | --- |
| Weapon | 공격 체감이 가장 큼 |
| Armor | 생존 체감 |
| Ring | 옵션 실험용 |

출시 확장 후보:

- Helmet
- Gloves
- Boots
- Amulet
- Offhand

## 4. 장비 등급

MVP 등급:

| 등급 | 옵션 수 | 역할 |
| --- | --- | --- |
| Normal | 0 | 베이스 확인 |
| Magic | 1 | 초반 성장 |
| Rare | 2-3 | 첫 파밍 목표 |

MVP 제외:

- Legendary
- Unique
- Set

## 5. 장비 데이터 구조

### ItemDefinition

정적 데이터:

```text
id
displayName
slot
baseTier
baseMinPower
baseMaxPower
allowedAffixTags
icon
```

### ItemInstance

런타임/저장 데이터:

```text
instanceId
definitionId
rarity
level
rolledPower
affixes
durability
isEquipped
```

### AffixDefinition

```text
id
displayName
statId
minValue
maxValue
tags
weight
```

## 6. 스탯 종류

MVP 스탯:

| 스탯 | 적용 |
| --- | --- |
| AttackDamage | 영웅 공격 |
| MaxHealth | 영웅 생존 |
| AttackSpeed | 영웅 공격 속도 |
| MoveSpeed | 영웅 이동 |
| DefenseWallHpBonus | 지상 성벽 |
| TowerDamageBonus | 지상 포탑 |
| DefenderDamageBonus | 지상 병력 |

지상 보너스 스탯은 장비에 일부만 붙인다. 모든 장비가 지상 보너스를 주면 선택이 흐려진다.

## 7. 제작 기능

### 제작

입력:

```text
Gold
Scrap
선택한 장비 베이스
```

출력:

```text
Normal 또는 Magic 장비
```

MVP 제작은 너무 복잡하지 않게 한다.

### 강화

입력:

```text
Gold
Essence
장비 1개
```

결과:

```text
장비 level +1
기본 수치 증가
```

강화 실패는 MVP에서 제외한다.

### 분해

입력:

```text
장비 1개
```

결과:

```text
Scrap
등급에 따라 Essence 일부
```

분해는 인벤토리 정리와 재료 회수 역할을 한다.

현재 구현된 첫 규칙:

```text
Normal 장비 -> Scrap
Magic 장비 -> Scrap + 소량 Essence
Rare 장비 -> 더 많은 Scrap + Essence + 소량 AlterStone
```

이 값은 장기 밸런스 확정값이 아니라 프로토타입 기준이다. D2 참고 자료에서는 아이템 품질 판정이 Normal/Magic/Rare 이상으로 나뉘고, 큐브 조합이 Magic/Rare 아이템과 보석/룬을 다시 성장 재료로 순환시킨다. 이 게임은 거래가 없으므로 중복 장비가 죽은 드랍이 되지 않게 분해를 먼저 열어 둔다. 다만 `AlterStone`은 옵션 변형의 핵심 재료라서 초반 분해에서 쉽게 풀지 않는다.

2026-05-30 구현 메모: `ItemEconomyModel`은 `AlterStone`을 Normal/Magic 분해에서는 지급하지 않고, Rare 장비 분해에서 최소 1개부터 소량 지급한다. 이 값은 장기 밸런스 목표가 아니라 프로토타입 페이싱 규칙이다. D2 참고 자료의 Magic/Rare 아이템과 보석/룬 순환 구조를 그대로 복제하지 않고, 거래가 없는 싱글 플레이에서 중복 Rare가 느린 옵션 변형 재료가 되도록 축약했다. 2026-05-30부터 tier 1 Rare도 reroll 재료를 낼 수 있게 바꾼 이유는 `CraftingOverlayPresenter`의 실제 reroll 비용 루프가 현재 보장 방 클리어 보상 안에서 닫혀야 하기 때문이다.

2026-05-07 implementation note: `ItemInstance` and `SimpleInventory` now cover the first runtime inventory slice. An item can be rolled from an `ItemDefinition`, receive a stable instance id, carry rarity/level/power/durability/affix-roll placeholders, and be exported through `InventorySaveData`. This is still a foundation, not the final loot loop: drop tables, item-definition lookup after loading, affix mutation, crafting UI, and inventory UI remain future work.

2026-05-10 implementation note: dungeon clear rewards can now create an `ItemInstance` through `LootDropper` and place it into `SimpleInventory`. Until authored item assets/drop tables exist, `LootDropper` may create an explicit prototype runtime definition. Because saved prototype items do not reconnect to an `ItemDefinition` asset after load, `ItemSalvageService` can now fall back to the saved item snapshot's slot/rarity/level for minimum salvage rewards. This keeps duplicate/no-trade items from becoming dead inventory during the prototype phase, but real item registry lookup is still required before production balance.

### 옵션 변형

입력:

```text
Rare 장비
AlterStone
Gold
```

결과:

```text
옵션 1개 재굴림
```

MVP에서는 플레이어가 변형할 옵션을 직접 고르는 기능은 보류해도 된다. 처음에는 마지막 옵션 1개 재굴림으로 충분하다.

현재 코드에서는 `ItemDefinition.CanRerollAffix`와 `ItemDefinition.AffixRerollCost`로 Rare 장비의 변형 가능 여부와 비용을 계산한다. 비용은 `Gold + Essence + AlterStone`이고 `baseTier`에 따라 증가한다. `CraftingOverlayPresenter`는 이 비용을 실제로 소비한 뒤 선택된 Rare `ItemInstance`의 saved `ItemAffixRoll` 1개를 authored Rare affix로 교체한다. 이 규칙은 첫 material sink이며, affix lock/upgrade crafting은 아직 별도 작업이다.

## 8. 지상과 지하 연결 방식

지상 재화가 필요한 것:

- 장비 제작 비용
- 성벽 수리
- 포탑 강화
- 병력 훈련
- 장비 강화 비용 일부

던전 재료가 필요한 것:

- 장비 강화
- 옵션 변형
- 고급 베이스 제작
- 지상 방어 보너스 업그레이드

이렇게 해야 한쪽만 플레이해서 모든 것을 해결할 수 없다.

## 9. 초기 밸런스 방향

초반 30분 목표:

| 시간 | 기대 상태 |
| --- | --- |
| 5분 | 첫 포탑 강화 |
| 10분 | 첫 전선 압박/성벽 손상과 수리 |
| 15분 | 첫 무기 제작 |
| 20분 | 첫 던전 클리어 |
| 30분 | 장비 강화 후 막혔던 Frontline Level 돌파 |

## 10. 금지 규칙

MVP에서 하지 않는다.

- 강화 실패로 장비 파괴
- 장비 영구 삭제
- 옵션 6개 이상
- 재화 10종 이상
- 경매장
- 거래
- 세트 효과

## 11. 완료 기준

- 지상 보상으로 장비를 제작할 수 있다.
- 던전 보상으로 장비를 강화할 수 있다.
- 장비 장착이 영웅 스탯에 반영된다.
- 일부 옵션이 지상 디펜스에 반영된다.
- 분해로 재료를 회수할 수 있다.

## 12. 2026-05-21 Reward Diagnostics Note

- `LootDropper` now records `LastRewardSource` so Play Mode can tell authored weighted-table rewards apart from legacy-list and prototype-fallback rewards.
- No item weight, rarity, salvage, crafting, or economy value changed in this run. The current 78/20/2 tier-1 table remains a prototype per-clear bridge until a real Unity drop-balance export exists.

## 13. 2026-05-28 Inventory Overlay Note

- `InventoryOverlayPresenter` now gives the authored inventory overlay a normal-player path for reviewing item instances, selecting a row, equipping the selected item, salvaging the selected item, and previewing salvage/reroll material values already defined by `ItemEconomyModel`.
- No item weight, rarity, salvage, reroll, or drop-balance value changed in this run. This is UI/content exposure for the existing economy model, not a new economy rule.
- The next item-economy production gap is a scalable duplicate/low-value conversion sink plus drop-table export tooling and production-grade crafting rules.

## 14. 2026-05-30 Crafting Overlay And Rare Reroll Note

- `CraftingOverlayPresenter` now exposes the first crafting overlay path for item selection, salvage preview, current affix preview, selected-item salvage, and Rare affix reroll.
- The reroll path spends `ItemDefinition.AffixRerollCost` from `CurrencyWallet` and calls `ItemInstance.TryApplyAuthoredAffixReroll(...)`, replacing the item's saved affix roll with one authored stat modifier.
- `SimpleInventory.NotifyItemsChanged()` and `EquipmentSlots.RefreshEquippedModifiers()` keep UI and equipped-stat subscribers current after the live item mutation.
- This uses the existing D2-inspired resource idea of turning rare/duplicate gear into reroll pressure, but it is not a D2 cube clone. It is a small single-player sink for the current guaranteed per-clear reward loop.
- Remaining economy gaps: Play Mode validation of the authored affix pool, affix locking, item-level upgrades, drop-balance export/import, and tuning whether early Rare reroll costs are too expensive or too cheap.
- 2026-06-01 validation feedback update: the crafting overlay now records the last successful reroll for the selected item as spent materials plus previous affix state and new affix. This changes only player-facing verification feedback, not reroll cost, salvage return, rarity pacing, or affix generation rules.
- 2026-06-02 validation reliability update: the pre-E1-B prototype reroll avoided repeating the selected item's saved affix when another slot-valid prototype candidate existed. The 2026-06-26 E1-B pool replaces that rule with authored affix ids, weights, and export validation.

## 16. 2026-06-09 Production Item Registry And Save Migration

- `ItemDefinitionRegistry.asset` is the canonical authored item identity source for the six current tier-1 definitions. Runtime reward tables no longer register definitions opportunistically into each inventory.
- Optional `ItemDefinitionIdMigration` entries map retired ids to a registered replacement before inventory restoration. Save schema v3 records that this migration stage exists.
- Unknown ids are not deleted or converted from stale slot/rarity/power snapshots. They remain visible in inventory as unresolved quarantine records, while equip, salvage, and reroll actions stay disabled until an explicit migration is authored.
- Normal `Gameplay` disables `LootDropper.createPrototypeRewardWhenTableEmpty`; an empty/invalid authored reward table now fails visibly instead of creating production-looking runtime loot.
- This changes item durability and data safety, not rarity odds, salvage yields, reroll costs, or D2 pacing. The completed D0-D path above now owns the first scalable use for dominated duplicate authored drops.

## 15. 2026-05-30 Rare Access Pacing Note

- The baseline tier-1 authored table remains 78% Normal, 20% Magic, and 2% Rare per clear, but that raw rate is too slow for validating the first crafting overlay.
- `LootDropper` now adds an early-slice access rule: if the current inventory has no Rare item, a valid Rare entry in the authored weighted table is selected before the normal weighted roll. The current `Gameplay` scene also forces a Rare after 6 weighted non-Rare rewards.
- This is not the final long-term economy target. It is a small pity/milestone rule so the first Rare and the first duplicate-Rare salvage/reroll loop can be tested without debug-only item seeding.
