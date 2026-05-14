# Playable Loop MVP Automation Plan

작성일: 2026-05-07
문서 목적: daily automation이 실제 플레이 루프 MVP를 향해 매일 가장 앞의 연결부를 구현하도록 고정하는 작업 큐

## 1. MVP 목표

이 문서의 목표는 완성품이 아니라, Unity Play Mode에서 다음 흐름이 한 번이라도 끊기지 않고 보이는 것이다.

```text
Ground defense earns Gold/Scrap
-> player starts a dungeon expedition
-> one room combat resolves
-> loot or materials are awarded
-> item enters inventory
-> player equips or salvages it
-> rewards improve hero or ground defense
-> save/load preserves the loop state
```

첫 MVP는 작아도 된다. 방은 1개, 적은 1종, 아이템은 3-6개, UI는 디버그형이어도 된다. 다만 각 시스템은 실제 게임 루프 안에서 서로 연결되어야 한다.

## 2. 자동화 운영 규칙

Daily automation은 매 실행 시작 시 이 문서를 읽고, `MVP Task Queue`에서 가장 앞에 있는 미완료/막힌 항목을 우선 선택한다.

자동화는 작업 후 이 문서를 개선해야 한다.

- 완료된 항목은 `Done`으로 표시한다.
- 새로 발견한 막힌 연결부는 `Discovered` 섹션에 추가한다.
- 구현 중 범위가 커지면 작은 다음 작업으로 쪼갠다.
- Unity Editor 수동 배치가 필요한 경우 정확한 GameObject/Component/Inspector 연결 순서를 남긴다.
- Git commit/push는 하지 않는다. 변경이 검증되어 올릴 만하면 보고서에서 "사용자 확인 필요: 커밋/푸시 요청"으로 요청한다.

### 2.1 Headless Unity 검증 안전 규칙

2026-05-13 자동화 중 Unity 6000.4.4f1 batchmode 검증이 라이선싱/ILPP 단계에서 멈추며 `Unity.ILPP.Trigger.exe` 시스템 오류 팝업을 반복 생성했다. 재발 방지를 위해, 원인이 별도로 해결되기 전까지 daily automation은 무인 Unity batchmode 컴파일을 실행하지 않는다.

기본 검증 순서는 다음으로 제한한다.

- `dotnet build .\IncrementalDiablo.sln -v:minimal`
- `git diff --check`
- 변경 파일 범위와 씬 YAML 직렬화 필드 확인
- 필요한 경우 사용자에게 Unity Editor Play Mode 수동 검증 절차를 정확히 요청

Unity Editor 검증이 꼭 필요하면 먼저 실행 중인 `Unity.exe`, `Unity.ILPP.*`, `Bee*`, `UnityAutoQuitter.exe` 프로세스가 없는지 확인하고, 사용자가 명시적으로 허락한 짧은 검증만 수행한다. `-noUpm`을 붙인 무인 batchmode 재시도는 금지한다.

## 3. 완료 판정

Playable loop MVP는 다음 조건을 모두 만족하면 완료로 본다.

| ID | 완료 조건 | 현재 상태 |
| --- | --- | --- |
| MVP-01 | 지상전이 Play Mode에서 Gold/Scrap을 생성한다. | Mostly Done |
| MVP-02 | 던전 시작 명령이 있고, 런 상태가 Ready/Running/Cleared/Failed로 바뀐다. | Code Done |
| MVP-03 | 던전 방 1개에서 영웅과 적의 전투가 자동 또는 간단 직접 조작으로 끝난다. | Code Done |
| MVP-04 | 적 또는 방 클리어가 보상을 지급한다. | Code Done |
| MVP-05 | 보상 아이템이 `SimpleInventory`에 들어간다. | Code Done |
| MVP-06 | 장비 장착이 영웅 스탯에 반영된다. | Code Done |
| MVP-07 | 장비 분해가 인벤토리에서 아이템을 제거하고 재료를 지급한다. | Partial |
| MVP-08 | 재료나 보상이 지상 방어 강화에 다시 쓰인다. | Partial |
| MVP-09 | 저장/로드가 지상전, 재화, 인벤토리의 최소 상태를 유지한다. | Partial |
| MVP-10 | 한 화면 또는 임시 HUD에서 현재 루프 상태를 확인할 수 있다. | Partial |

## Post-MVP Phase Runway

This document must not stop at the first MVP. When the current phase is complete, the automation must promote the next phase and keep moving toward a fuller game loop instead of polishing the same small surface forever.

| Phase | Entry condition | Exit condition | Default next work |
| --- | --- | --- | --- |
| Phase A - Playable Loop MVP | Current phase. Ground/inventory foundations exist, but dungeon loop is not visible yet. | MVP-01 through MVP-10 are `Done`; Play Mode can show ground reward -> dungeon -> loot -> equip/salvage -> save/load at debug quality. | Finish P0/P1 tasks below. |
| Phase B - 30-Minute Retention Slice | Phase A is done. The loop exists but may be shallow. | A new player can play about 30 minutes with at least three meaningful upgrade decisions, one failure/recovery moment, and no dev-console-only step. | Replace debug HUD with minimal player HUD, tune early pacing, repeat dungeon attempts, clarify failure/reward feedback. |
| Phase C - Long-Horizon Systems Foundation | Phase B is done. The first session works, but long-term scaling is not proven. | Formula-driven dungeon tiers, ground scaling, item rarity/material sinks, and save migration hooks exist without hand-authored content ladders. | Add generated tiers, item/drop pacing tables, material sinks, balance validation scripts, and extensible save schemas. |
| Phase D - Early Access Readiness Slice | Phase C is done. Systems scale, but the game is not yet release-shaped. | A 2-4 hour repeatable slice is playable with stable UI, recoverable failure, basic settings, readable onboarding, QA checklist, and no known progression blocker. | Add usability polish, error handling, content breadth, performance checks, settings, and release-scope triage. |

## Phase Promotion Rule

- If every completion criterion in the current phase is `Done`, update `Current phase` in the progress tracker, mark the first actionable task of the next phase as `Next`, and continue from that task.
- If the next phase is too broad, split the first risky item into one P0 task that can be completed in one automation run.
- If all listed phases are done or stale, add a `Next Production Phase Proposal` section with 2-3 options, a recommendation, and user-confirmation needs. Do not spend the run on filler cleanup.
- If a phase cannot be advanced because it requires Unity Editor/manual gameplay judgment, document the exact manual check and choose the next safe code/docs task that still moves the playable loop.

## No-Stagnation Rules

Each automation run must satisfy at least one of these:

- change at least one completion criterion toward `Done`;
- implement or verify a missing system link in the playable loop;
- fix a blocker that prevents Play Mode or build verification;
- add a missing verification path that allows the next run to implement safely.

Docs-only work is allowed only when it directly unblocks code work, records required Unity scene setup, captures a major design decision, or updates this plan after real implementation. Do not repeat the same category of minor cleanup in two consecutive runs. Every report must state what moved closer to visible gameplay.

## Progress Tracker

| Field | Current value |
| --- | --- |
| Current phase | Phase A - Playable Loop MVP |
| Last meaningful movement | 2026-05-14: equipped `ItemInstance` objects now restore debug-quality stat effects after save/load, and `DefenseSaveManager` can validate the persisted JSON save file separately from the live runtime snapshot. |
| Next unlock | In Play Mode, run Start Dungeon -> Force Clear -> Equip Latest -> Save -> Validate Saved File -> Load -> Validate Snapshot, then restart Play Mode once to confirm both authored-definition equipment and prototype snapshot-power equipment restore visibly. |
| Loop coverage | Ground reward: mostly present; dungeon state: code foundation done; room combat: code done; loot-to-inventory: code done with prototype fallback; equip/salvage feedback: temporary HUD code done; save/load: partial plus snapshot validator, saved-file validator, debug HUD controls, and prototype equipment stat restore; HUD: debug OnGUI done, production UI pending. |
| Known blockers | Real dungeon enemy/prefab feel, inventory HUD, authored item assets/drop tables, production item-definition registry, and gameplay feel still need Unity Play Mode review after code foundations are connected. |

## 4. MVP Task Queue

### P0. 던전 런 상태 연결

상태: Done

목표:
`DungeonRunState`와 `ExpeditionDirector`를 추가해서 던전 시작/완료/실패를 코드에서 표현한다.

완료 기준:

- `StartExpedition()` 호출로 런 상태가 `Running`이 된다.
- `CompleteRoom()` 또는 `FailExpedition()` 호출로 상태가 바뀐다.
- 최소 런타임 데이터가 `GameSaveData`에 들어갈 수 있는 구조를 가진다.
- 아직 실제 방 프리팹이 없어도 코드 단위로 빌드된다.

추천 파일:

- `Assets/02.Scripts/Dungeon/ExpeditionDirector.cs`
- `Assets/02.Scripts/Dungeon/DungeonRunState.cs`
- `Assets/02.Scripts/Shared/GameSaveData.cs`
- `GameDesign/ScriptFolderStructure.md`

2026-05-08 완료 메모:

- `ExpeditionDirector`가 `Ready`, `Running`, `Cleared`, `Failed` 상태와 `StartExpedition()`, `CompleteRoom()`, `FailExpedition()` 호출을 제공한다.
- `DungeonSaveData`가 `GameSaveData`에 추가되었고, `DefenseSaveManager`가 씬의 `ExpeditionDirector`를 찾아 저장/로드한다.
- 실제 전투, 보상, HUD 버튼은 다음 P0 작업 범위로 남긴다.

### P0. 방 1개 전투 결과 만들기

상태: Code Done

목표:
`CombatRoom`이 영웅/적 전투 결과를 받거나 간단 계산으로 클리어/실패를 결정한다.

완료 기준:

- 방 시작 시 적 카운트 또는 enemy health가 생긴다.
- 적 사망/방 클리어 이벤트가 발생한다.
- 실패 시 런 상태가 `Failed`가 된다.
- 수동 Unity 배치가 필요한 경우 빈 씬 세팅 순서를 문서화한다.

추천 파일:

- `Assets/02.Scripts/Dungeon/CombatRoom.cs`
- `Assets/02.Scripts/Character/Controllers/EnemyAIController.cs`
- `Assets/02.Scripts/Character/Controllers/AutoCombatController.cs`

2026-05-09 코드 완료 메모:

- `CombatRoom`이 실행 중인 `ExpeditionDirector`를 찾아 방을 시작하고, 시작 카운트다운 뒤 전투 상태로 들어간다.
- 실제 `Health` 참조가 연결되어 있으면 영웅 사망/모든 적 사망으로 실패/클리어를 판정한다.
- 아직 적 프리팹이 없으면 프로토타입 영웅 체력/DPS와 적 체력/DPS 계산으로 방 결과를 만든다.
- 클리어/실패 결과는 `ExpeditionDirector.CompleteRoom()` 또는 `ExpeditionDirector.FailExpedition()`으로 전달된다.
- 실제 씬 배치와 전투 체감은 Unity Play Mode에서 확인해야 한다.

### P0. 던전 보상을 인벤토리에 연결

상태: Code Done

목표:
방 클리어나 보스 클리어가 `ItemDefinition` 기반 보상을 만들고 `SimpleInventory`에 넣는다.

완료 기준:

- 보상 생성 코드가 `ItemInstance`를 만든다.
- `SimpleInventory.Count`가 증가한다.
- 보상 지급 실패 시 이유가 로그로 남는다.
- 아이템 정의 에셋이 없으면 임시 정의/테스트 경로가 명확하다.

추천 파일:

- `Assets/02.Scripts/Items/LootDropper.cs`
- `Assets/02.Scripts/Items/SimpleInventory.cs`
- `Assets/02.Scripts/Dungeon/ExpeditionDirector.cs`

2026-05-10 코드 완료 메모:

- `LootDropper`가 `ItemDefinition` 배열에서 보상을 고르거나, 정의 에셋이 아직 없으면 런타임 프로토타입 `ItemDefinition`을 만들어 `ItemInstance`를 생성한다.
- 프로토타입 fallback은 MVP 전용이다. D2식 품질 판정 순서를 축약해 Rare는 낮은 기본 확률, Magic은 중간 확률, 나머지는 Normal로 둔다.
- `ExpeditionDirector`는 최종 방 클리어 후 `LootDropper.TryGrantClearReward()`를 호출하고, 성공하면 `rewardPending`을 해제한다. 실패하면 `rewardPending`을 유지하고 실패 이유를 `lastResult`/로그에 남긴다.
- `SampleScene`의 `GameSystems`에는 `SimpleInventory`와 `ItemSalvageService`, `DungeonRoot`에는 `LootDropper`가 추가되어 Start Dungeon 버튼으로 보상 지급까지 헤드리스/Play Mode 확인이 가능하다.
- 저장 후 원본 `ItemDefinition` 연결이 없는 프로토타입 아이템도 죽은 인벤토리가 되지 않도록, `ItemSalvageService`는 저장된 slot/rarity/level 스냅샷으로 최소 분해 보상을 계산할 수 있다.
- 실제 드랍 테이블, 장비 에셋, 아이템 registry lookup, 인벤토리 HUD는 다음 작업 범위다.

### P0. 임시 루프 HUD

상태: Code Done

목표:
Play Mode에서 지상전/던전/인벤토리 상태를 한 화면에서 확인하고 버튼으로 다음 행동을 실행한다.

완료 기준:

- Start Dungeon 버튼이 있다.
- Inventory count와 마지막 획득 아이템 이름이 보인다.
- Equip 또는 Salvage 테스트 버튼이 있다.
- 지상전 재화/강화 결과가 기존 HUD 또는 임시 HUD로 보인다.

추천 파일:

- `Assets/02.Scripts/Dungeon/UI/DungeonDebugHud.cs`
- `Assets/02.Scripts/Items/UI/InventoryDebugHud.cs`
- `GameDesign/ProductionDocs/06_UnitySceneAndPrefabSetupGuide.md`

2026-05-11 코드 완료 메모:

- `DungeonDebugHud`가 Play Mode 화면에 던전 상태, 방 진행, 보상 대기 여부, 최근 결과, 전투 방 상태, 인벤토리 수량을 표시한다.
- `DungeonDebugHud`에서 `Start Dungeon`, `Force Clear`, `Force Fail`, `Grant Pending Reward`를 눌러 던전 루프를 Inspector 없이 확인할 수 있다.
- `InventoryDebugHud`가 인벤토리 수량, 최근 아이템, Gold/Scrap/Essence/AlterStone을 표시한다.
- `InventoryDebugHud`에서 최근 아이템 장착, 최근 아이템 분해, 장착 플래그 해제를 누를 수 있다.
- `SampleScene`의 `GameSystems`에 두 HUD가 붙어 있어 빈 수동 배치 없이 Play Mode에서 바로 보인다.
- 아직 production UI가 아니라 OnGUI 기반 임시 HUD다. 다음 P1은 저장/로드 후 인벤토리와 장착 플래그가 끊기지 않는지 검증하고, 로드 후 `ItemDefinition` 연결이 사라지는 registry 문제를 명시적으로 다루는 것이다.

### P1. 인스턴스 장착과 저장 연결

상태: Code Done

목표:
`SimpleInventory`의 `ItemInstance`를 실제 장착 슬롯과 연결한다.

완료 기준:

- 인벤토리 아이템을 장착하면 이전 장비와 교체된다.
- `EquipmentSlots` 또는 새 장착 서비스가 `ItemInstance`의 definition/modifier를 사용한다.
- 저장/로드 후 장착 상태가 유지된다.
- 로드 후 `ItemDefinition` 에셋 연결이 필요한 경우 item registry 계획을 함께 남긴다.

추천 파일:

- `Assets/02.Scripts/Character/Core/EquipmentSlots.cs`
- `Assets/02.Scripts/Items/SimpleInventory.cs`
- `Assets/02.Scripts/Shared/GameSaveData.cs`

2026-05-12 코드 완료 메모:

- `EquipmentSlots`가 이제 `ItemDefinition` 직접 장착뿐 아니라 `ItemInstance` 장착도 받을 수 있고, 장착된 인스턴스 ID를 내보낼 수 있다.
- `SimpleInventory.TryEquip(...)`가 같은 슬롯의 이전 장착 플래그를 교체하고 `EquipmentSlots`에 실제 definition/modifier를 연결한다. `knownDefinitions`와 `LootDropper` 보상 정의 등록으로 저장된 definition id를 다시 live asset으로 연결할 수 있다.
- `DefenseSaveManager`가 저장 시 `hero.equippedItemInstanceIds`를 기록하고, 로드 후 인벤토리의 장착 상태를 `EquipmentSlots`에 복원한다.
- 로드된 아이템에 live `ItemDefinition`이 없으면 장착 플래그는 보존하지만 스탯에는 반영하지 못한다. 이 경우 로그 경고를 남기며, 실제 해결은 item-definition registry 작업 범위다.

### P1. 저장/로드 검증 루프

상태: Partial

목표:
실제 Play Mode에서 지상전 + 던전 런 + 인벤토리 저장이 깨지지 않는지 확인한다.

완료 기준:

- JSON 저장 파일에 currencies/defense/hero.equippedItemInstanceIds/inventory가 들어간다.
- Play Mode 종료 후 다시 시작해 인벤토리 count와 live-definition 장착 상태가 유지된다.
- runtime prototype-only 아이템처럼 정의 에셋 lookup이 아직 안 되는 경우 그 한계를 보고서와 문서에 명확히 적는다.

2026-05-13 코드 진행 메모:

- `GameSaveDataDiagnostics`가 저장 스냅샷의 currencies, defense, dungeon, inventory, hero.equippedItemInstanceIds 일관성을 검사한다.
- `DefenseSaveManager.CreateSaveDataSnapshot()`과 `TryValidateCurrentSaveData()`로 저장 파일을 실제로 쓰기 전에 현재 루프 상태를 점검할 수 있다.
- `DungeonDebugHud`에 Save/Load/Validate Save 버튼을 추가해 Play Mode에서 던전 보상, 인벤토리, 장착 상태를 한 자리에서 저장 검증할 수 있게 했다.
- 아직 실제 Play Mode 재시작 검증은 남아 있다. runtime prototype-only 아이템은 이제 saved slot/rarity/rolledPower 기반의 prototype power modifier로 장착 스탯을 복원하지만, production item registry와 실제 드롭 테이블은 여전히 별도 작업이다.

2026-05-14 code progress note:

- `ItemInstance` now contributes live definition modifiers, saved affix-roll modifiers, and a small prototype rolled-power modifier by slot.
- `EquipmentSlots` and `SimpleInventory.RestoreEquipment(...)` can re-equip saved runtime prototype items even when their live `ItemDefinition` is not resolved, so the save/load loop no longer drops all equipment stat effects for prototype rewards.
- Actual Play Mode restart verification is still required, and the rolled-power mapping is a prototype bridge until authored item assets, real drop tables, and a production item registry exist.

2026-05-14 saved-file validation progress note:

- `DefenseSaveManager.TryValidateSavedFile(...)` now reads the JSON file from `Application.persistentDataPath`, parses it, and runs the same structural diagnostics used by the live snapshot validator.
- `DefenseSaveManager.TryLoad()` now refuses structurally invalid save files instead of applying a broken snapshot to the live scene.
- `DungeonDebugHud` now separates `Validate Snapshot` from `Validate Saved File`, so Play Mode can confirm both the in-memory loop state and the persisted disk state without opening the JSON manually.
- The P1 task remains `Partial` until Play Mode restart verification proves the saved file reloads visibly after a full session restart.

## 5. 2주 목표 일정

이 일정은 약속된 출시 일정이 아니라 자동화 작업 순서를 고정하기 위한 예상이다.

| 기간 | 목표 | 기대 결과 |
| --- | --- | --- |
| Day 1-2 | 던전 런 상태와 시작/완료/실패 코드 | 버튼 또는 임시 호출로 던전 상태 변화 확인 |
| Day 3-4 | 방 1개 전투 결과 | 적 또는 계산 전투가 클리어/실패를 만든다 |
| Day 5-6 | 보상과 인벤토리 연결 | 방 클리어 후 아이템/재료가 들어온다 |
| Day 7-8 | 임시 HUD 연결 | 한 화면에서 지상전/던전/아이템 루프 확인 |
| Day 9-10 | 장착/분해/강화 순환 | 장비 선택이 스탯 또는 재화 흐름에 영향을 준다 |
| Day 11-12 | 저장/로드와 문서 보정 | 루프 상태가 세션 사이에 유지된다 |
| Day 13-14 | 플레이 테스트와 작은 튜닝 | 10-20분 동안 루프가 끊기지 않는다 |

## 6. 지금 하지 않을 것

MVP 전까지 다음은 보류한다.

- 여러 던전 테마
- 고급 인벤토리 UI
- 수십 개 이상의 수동 아이템 테이블
- Legendary/Unique/Set 등급
- 복잡한 affix mutation
- 자동분해/필터 전체 시스템
- Steam/네트워크/클라우드 저장
- 완성형 밸런스

## 7. Discovered

아직 없음.
