# Production Document Map

작성일: 2026-05-03
문서 목적: 제작에 필요한 문서 목록, 논리적 근거, 읽는 순서, 유지 규칙 정리
기준 프로젝트: `D:\Unity\IncrementalDiablo`

## 1. 왜 문서를 나누는가

이 프로젝트는 두 게임을 억지로 합치는 것이 아니라, 역할이 다른 두 루프를 연결하는 게임이다.

```text
지상 디펜스 = 자동으로 막고 돈/기본 재료를 쌓는 곳
지하 던전 = 영웅이 장비와 희귀 재료를 캐는 곳
제작/장비 = 두 루프를 서로 강화하게 만드는 연결부
```

따라서 제작 문서는 다음 질문에 답해야 한다.

1. 이 게임의 핵심 재미가 무엇인지 설명할 수 있는가?
2. 플레이어가 실제로 어떤 버튼을 누르고 어떤 결과를 보는지 알 수 있는가?
3. Unity에서 어떤 씬, 프리팹, 컴포넌트를 만들어야 하는지 지시할 수 있는가?
4. 각 시스템이 어떤 데이터를 읽고 어떤 저장값을 남기는지 정해져 있는가?
5. 첫 구현 순서가 명확해서 범위가 새지 않는가?

이 기준에 따라 문서를 8개로 나눈다.

## 2. 문서 목록과 논리적 근거

| 순서 | 문서 | 필요한 이유 | 주요 독자 |
| --- | --- | --- | --- |
| 00 | `00_DocumentMap.md` | 전체 문서 체계와 읽는 순서를 고정한다. 문서가 늘어나도 어디를 봐야 하는지 잃지 않게 한다. | 개발자, 협업자 |
| 01 | `01_GamePillarsAndMVP.md` | 게임의 핵심 기둥과 MVP 범위를 못 박는다. 아이디어가 다시 오펜스/RTS/복잡한 타워디펜스로 새는 것을 막는다. | 개발자, 학원 피드백 |
| 02 | `02_CoreLoopAndPlayerFlows.md` | 플레이어가 실제로 무엇을 누르고 어떤 화면을 오가는지 정의한다. UI와 씬 전환의 기준이 된다. | 개발자, UI 구현 |
| 03 | `03_GroundDefenseSystemSpec.md` | 지상 디펜스를 구현 가능한 규칙으로 쪼갠다. 지속 전선, Frontline Level, Hold/Push, 압박, 보상, 수리, 오프라인 진행의 기준이 된다. | 시스템 구현 |
| 04 | `04_DungeonExpeditionSystemSpec.md` | 던전 크롤링의 조작, 자동/직접 전투, 방 구조, 실패/보상 규칙을 정한다. 기존 캐릭터 코드의 다음 목표가 된다. | 전투/던전 구현 |
| 05 | `05_ItemsCraftingEconomySpec.md` | 지상과 지하를 연결하는 장비/재화/제작 규칙을 정한다. 장기 플레이가 단순 숫자 상승으로 흐르지 않게 한다. | 아이템/밸런스 구현 |
| 06 | `06_UnitySceneAndPrefabSetupGuide.md` | Unity에서 어떤 씬, 폴더, 프리팹, 컴포넌트를 만들지 단계별로 지시한다. 개발자가 그대로 따라 세팅할 수 있어야 한다. | Unity 세팅 |
| 07 | `07_DataSaveAndBalanceSpec.md` | ScriptableObject, 런타임 상태, 저장 데이터, 초기 밸런스 값을 분리한다. 하드코딩을 줄이고 테스트를 쉽게 만든다. | 데이터/저장 구현 |
| 08 | `08_ImplementationRoadmap.md` | 실제 작업 순서와 완료 기준을 제시한다. 지금 당장 무엇부터 만들지 결정한다. | 개발 일정 관리 |

## 3. 기존 문서와의 관계

기존 문서는 유지한다.

| 기존 문서 | 역할 |
| --- | --- |
| `GameDesignDocument.md` | 외부 설명용 상위 기획서 |
| `GroundBattleFlowBlueprint.md` | 지상 디펜스 방향 전환의 기준 문서 |
| `ScriptFolderStructure.md` | 현재 스크립트 폴더 구성 원칙 |

새 ProductionDocs는 기존 문서를 대체하지 않는다. 대신 구현에 필요한 더 구체적인 기준을 제공한다.

충돌이 생기면 우선순위는 다음과 같다.

```text
ProductionDocs의 시스템 상세 문서
→ GroundBattleFlowBlueprint.md
→ GameDesignDocument.md
→ 대화 중 아이디어
```

## 4. 읽는 순서

새로 참여한 개발자나 미래의 내가 읽을 순서:

1. `00_DocumentMap.md`
2. `01_GamePillarsAndMVP.md`
3. `02_CoreLoopAndPlayerFlows.md`
4. 구현할 시스템에 따라 `03`, `04`, `05` 중 해당 문서
5. Unity 세팅 전 `06_UnitySceneAndPrefabSetupGuide.md`
6. 저장/밸런스 작업 전 `07_DataSaveAndBalanceSpec.md`
7. 오늘 할 일을 정할 때 `08_ImplementationRoadmap.md`

## 5. 문서 작성 규칙

모든 제작 문서는 다음 형식을 따른다.

```text
목적
확정 규칙
제외 규칙
플레이어 입력
게임 반응
Unity 구현 단위
완료 기준
```

새 아이디어가 나와도 바로 구현하지 않는다. 먼저 해당 문서의 `제외 규칙`과 충돌하는지 확인한다.

## 6. 이번 제작 기준의 최종 요약

```text
지상은 막고 돈을 쌓는다.
지하는 영웅이 장비를 캔다.
제작은 두 루프를 연결한다.
MVP는 세 루프가 작게라도 한 바퀴 도는 상태다.
```
