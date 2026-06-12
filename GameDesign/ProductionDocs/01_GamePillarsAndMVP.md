# Game Pillars And MVP

## 2026-06-12 Direction Override: RTS-Readable Automatic Defense

This section overrides older wording that could be read as rejecting all RTS influence.

- Ground defense **should use classic RTS visual language**: grouped units, a visible frontline, melee contact, ranged volleys, fixed defensive structures, deaths, reinforcements, and an isometric dark-fantasy battlefield.
- Ground defense **should not use classic RTS micromanagement**: no individual unit selection, movement orders, focus-fire commands, production queues, resource-worker control, or free tower placement.
- Player agency stays incremental and strategic: upgrade wall/tower/squads/traps, choose composition or priority policies, repair, and switch Hold/Push.
- The screen composition reference is `GameDesign/References/2026-05-17_FinalGameplayScreenConcept.png`.
- The current unit/structure silhouette reference is `Assets/06.Art/Sprites/GroundDefense/GroundDefense_ReadabilitySheet.png`.
- Abstract pulses, moving debug markers, isolated billboard cards, and combat diagnostic text are not acceptable final player-facing combat. Every visible attack and damage event should belong to a visible attacker, target, projectile/contact, death, or damaged structure.
- The continuous formula-driven frontline remains authoritative so the system can support 900+ hours without authored wave ladders.

작성일: 2026-05-03
문서 목적: 게임의 핵심 기둥, 제외 범위, MVP 완료 기준 확정

## 1. 게임 한 줄 정의

디아블로식 던전 크롤링과 방치형 지상 디펜스 성장이 결합된 PC 핵앤슬래시 증분 RPG.

## 2. 핵심 기둥

### Pillar 1. 지상은 막고 돈을 쌓는 곳이다

지상 디펜스는 RTS처럼 보이는 자동 전투를 사용하지만, RTS식 유닛 직접 조작·생산 관리·점령전·타워 배치 퍼즐은 아니다.

확정:

- 성채는 절대 함락되지 않는다.
- 적은 성채 앞 방어선으로 몰려온다.
- 플레이어는 성벽, 포탑, 병력을 강화한다.
- 전선이 버티는 동안 금화와 기본 제작 재료를 계속 얻는다.
- 방어 실패 시 전선이 돌파되고 방어선이 손상된다.

제외:

- 생산건물 점령
- 진군/출정 명령
- 지상 돌파
- RTS식 유닛 조작
- 포탑 위치 퍼즐
- 숨겨진 생산 효율 감소

### Pillar 2. 지하는 장비를 캐는 곳이다

지하 던전은 게임의 핵심 감성이다. 던전은 지상 디펜스보다 느리고 읽혀야 한다.

확정:

- 영웅은 던전에서 직접/자동 전투를 수행한다.
- 기본은 자동 전투 가능, 어려운 보스전은 직접 조작으로 이득을 볼 수 있다.
- 던전 보상은 장비, 희귀 제작 재료, 변형 재료다.
- 던전 실패 시 미확정 보상 일부와 경험치 일부를 잃는다.

제외:

- 던전 화면이 방치형 숫자 폭발처럼 보이는 것
- 몬스터와 이펙트가 너무 많아 상황이 안 보이는 것
- 모든 전투를 직접 조작해야 하는 구조

### Pillar 3. 제작은 두 루프를 연결한다

지상은 돈과 기본 재료를 제공하고, 지하는 장비와 희귀 재료를 제공한다. 제작은 이 둘을 합쳐 성장으로 바꾼다.

확정:

- 지상 재화만으로는 좋은 장비를 완성할 수 없다.
- 던전 보상만으로는 장기 성장을 유지하기 어렵다.
- 지상 재화는 베이스 아이템, 강화 비용, 수리 비용, 병력/포탑 성장에 쓰인다.
- 던전 재료는 장비 옵션, 변형, 고급 제작에 쓰인다.

### Pillar 4. 장기 플레이는 손제작 콘텐츠가 아니라 반복 가능한 성장 구조로 만든다

목표 플레이타임이 길다고 해서 수백 시간 분량의 수작업 스테이지를 만들지 않는다.

확정:

- Frontline Level
- 던전 깊이/난이도
- 장비 옵션
- 제작 목표
- 자동화 해금
- 반복 가능한 보스/던전 보상

위 요소로 장기 플레이를 만든다.

## 3. MVP 정의

MVP는 "최소 기능 제품"이 아니라, 이 프로젝트에서는 다음 의미로 쓴다.

```text
핵심 루프가 실제로 한 바퀴 돌아서 이 게임을 계속 만들 가치가 있는지 판단할 수 있는 가장 작은 버전
```

MVP는 출시판이 아니다. 900시간 콘텐츠도 아니다. 재미 검증용 첫 완성 루프다.

## 4. MVP 플레이 목표

MVP 플레이어는 20-40분 안에 다음을 경험해야 한다.

1. 지상에서 끊임없이 몰려오는 적 압박을 자동으로 막는다.
2. 버티는 동안 금화와 기본 재료가 쌓인다.
3. 성벽/포탑/병력을 강화한다.
4. 영웅을 지하 던전에 보낸다.
5. 던전에서 장비나 희귀 재료를 얻는다.
6. 장비/재료로 영웅 또는 지상 방어를 강화한다.
7. 강화 후 더 높은 Frontline Level 또는 더 깊은 던전에 도전한다.

## 5. MVP 포함 범위

### 지상 디펜스

| 항목 | MVP 수량 |
| --- | --- |
| 성채 | 1 |
| 방어선/성벽 | 1 |
| 레인 | 1 |
| 포탑 | 1종 |
| 병력 | 1종 |
| 적 | 3종 |
| Frontline Level | 공식 기반 자동 상승 |
| 대형 침공/관문 이벤트 | MVP에서는 선택 |
| 지상 재화 | 금화, 철조각 |

### 지하 던전

| 항목 | MVP 수량 |
| --- | --- |
| 던전 테마 | 1 |
| 방 프리팹 | 3-5개 |
| 일반 적 | 2종 |
| 엘리트 적 | 1종 |
| 보스 | 1종 |
| 장비 부위 | 무기, 갑옷, 반지 |
| 장비 등급 | 일반, 마법, 희귀 |
| 던전 재화 | 정수, 변형석 |

### 제작/장비

| 항목 | MVP 수량 |
| --- | --- |
| 장비 베이스 | 8-12개 |
| 옵션 | 12-20개 |
| 제작 기능 | 제작, 강화, 분해 |
| 변형 기능 | 1종만 |
| 저장 | 로컬 세이브 |

## 6. MVP 제외 범위

MVP에서 제외한다.

- 지상 오펜스
- 지상 생산건물 점령
- 다중 레인
- 실시간 멀티플레이
- 거래소
- 시즌
- 복잡한 스토리
- 여러 직업
- 고급 스킬 트리
- 세트 아이템
- 고유 아이템
- 네트워크 세이브
- Steam 업적

## 7. 성공 기준

MVP 성공 기준:

| 기준 | 설명 |
| --- | --- |
| 루프 이해 | 처음 보는 사람이 지상과 지하의 관계를 설명할 수 있다. |
| 성장 체감 | 20분 안에 방어 또는 영웅이 강해졌다고 느낀다. |
| 선택 발생 | 돈을 방어에 쓸지, 장비 제작에 쓸지 고민이 생긴다. |
| 막힘 발생 | 특정 Frontline Level이나 던전에서 한 번 막히고, 강화 후 다시 넘는다. |
| 반복 욕구 | 다음 Frontline Level/다음 던전 보상을 보고 싶어진다. |

## 8. 실패 기준

MVP 실패 기준:

- 지상 디펜스가 그냥 숫자만 오르고 재미가 없다.
- 던전이 지상과 연결되지 않고 별개 게임처럼 느껴진다.
- 장비를 얻어도 무엇이 달라졌는지 모른다.
- 플레이어가 다음 목표를 모르고 멈춘다.
- 직접 조작이 필수 노동처럼 느껴진다.

이 경우 콘텐츠를 늘리지 말고 루프를 다시 고친다.
