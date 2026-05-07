# Items Crafting Economy Spec

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
Rare 장비 -> 더 많은 Scrap + Essence
```

이 값은 장기 밸런스 확정값이 아니라 프로토타입 기준이다. D2 참고 자료에서는 아이템 품질 판정이 Normal/Magic/Rare 이상으로 나뉘고, 큐브 조합이 Magic/Rare 아이템과 보석/룬을 다시 성장 재료로 순환시킨다. 이 게임은 거래가 없으므로 중복 장비가 죽은 드랍이 되지 않게 분해를 먼저 열어 둔다. 다만 `AlterStone`은 옵션 변형의 핵심 재료라서 초반 분해에서 쉽게 풀지 않는다.

2026-05-06 구현 메모: `ItemEconomyModel`은 `AlterStone`을 Normal/Magic 분해에서는 지급하지 않고, Rare 장비도 `baseTier >= 4`부터만 소량 지급한다. 이 값은 장기 밸런스 목표가 아니라 프로토타입 페이싱 규칙이다. D2 참고 자료의 Magic/Rare 아이템과 보석/룬 순환 구조를 그대로 복제하지 않고, 거래가 없는 싱글 플레이에서 중복 Rare가 느린 옵션 변형 재료가 되도록 축약했다.

2026-05-07 implementation note: `ItemInstance` and `SimpleInventory` now cover the first runtime inventory slice. An item can be rolled from an `ItemDefinition`, receive a stable instance id, carry rarity/level/power/durability/affix-roll placeholders, and be exported through `InventorySaveData`. This is still a foundation, not the final loot loop: drop tables, item-definition lookup after loading, affix mutation, crafting UI, and inventory UI remain future work.

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

현재 코드에서는 `ItemDefinition.CanRerollAffix`와 `ItemDefinition.AffixRerollCost`로 Rare 장비의 변형 가능 여부와 비용을 미리 계산한다. 비용은 `Gold + Essence + AlterStone`이고 `baseTier`에 따라 증가한다. 실제 옵션 1개를 바꾸는 런타임 `ItemInstance` 변형은 인벤토리/아이템 인스턴스 구현 이후에 연결한다.

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
