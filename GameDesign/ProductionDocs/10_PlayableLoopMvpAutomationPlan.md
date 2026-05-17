# Playable Loop MVP Automation Plan

작성일: 2026-05-07
문서 목적: daily automation이 실제 플레이 루프 MVP를 닫은 뒤에도 디버그 보조 작업에 머물지 않고, 플레이어가 보는 실제 게임 제작으로 계속 전진하도록 고정하는 작업 큐

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

Daily automation은 매 실행 시작 시 이 문서를 읽고, **현재 phase의 task queue**에서 가장 앞에 있는 미완료/막힌 항목을 우선 선택한다.

자동화는 작업 후 이 문서를 개선해야 한다.

- 완료된 항목은 `Done`으로 표시한다.
- 새로 발견한 막힌 연결부는 `Discovered` 섹션에 추가한다.
- 구현 중 범위가 커지면 작은 다음 작업으로 쪼갠다.
- Unity Editor 수동 배치가 필요한 경우 정확한 GameObject/Component/Inspector 연결 순서를 남긴다.
- Git commit/push는 하지 않는다. 변경이 검증되어 올릴 만하면 보고서에서 "사용자 확인 필요: 커밋/푸시 요청"으로 요청한다.
- 한 phase의 최소 검증이 끝났으면 디버그 HUD, smoke test, 진단 helper만 더 늘리는 작업을 기본 선택지로 삼지 않는다. 다음 phase의 **플레이어 가시 작업**으로 바로 넘어간다.

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
| MVP-01 | 지상전이 Play Mode에서 Gold/Scrap을 생성한다. | Done |
| MVP-02 | 던전 시작 명령이 있고, 런 상태가 Ready/Running/Cleared/Failed로 바뀐다. | Done |
| MVP-03 | 던전 방 1개에서 영웅과 적의 전투가 자동 또는 간단 직접 조작으로 끝난다. | Done (debug) |
| MVP-04 | 적 또는 방 클리어가 보상을 지급한다. | Done |
| MVP-05 | 보상 아이템이 `SimpleInventory`에 들어간다. | Done |
| MVP-06 | 장비 장착이 영웅 스탯에 반영된다. | Done |
| MVP-07 | 장비 분해가 인벤토리에서 아이템을 제거하고 재료를 지급한다. | Done (debug) |
| MVP-08 | 재료나 보상이 지상 방어 강화에 다시 쓰인다. | Done (debug) |
| MVP-09 | 저장/로드가 지상전, 재화, 인벤토리의 최소 상태를 유지한다. | Done |
| MVP-10 | 한 화면 또는 임시 HUD에서 현재 루프 상태를 확인할 수 있다. | Done (debug) |

## Post-MVP Phase Runway

This document must not stop at the first MVP. When the current phase is complete, the automation must promote the next phase and keep moving toward a fuller game loop instead of polishing the same small surface forever.

| Phase | Entry condition | Exit condition | Default next work |
| --- | --- | --- | --- |
| Phase A - Playable Loop MVP | Completed 2026-05-14 after Play Mode smoke-test confirmation. | MVP-01 through MVP-10 are `Done`; Play Mode can show ground reward -> dungeon -> loot -> equip/salvage -> save/load at debug quality. | Closed unless a regression appears. |
| Phase B - 30-Minute Retention Slice | Completed 2026-05-17 after user confirmation that the normal player HUD slice was already done. | A new player can play about 30 minutes with at least three meaningful upgrade decisions, one failure/recovery moment, and no dev-console-only step. | Closed unless a regression appears. |
| Phase C - First Real Game Slice | Current phase. The first session reads correctly, but too much of the loop is still hidden simulation or prototype fallback. | One player-facing runtime slice contains a visible ground-defense lane, one direct-control dungeon room with at least one real enemy prefab, and authored item assets/definitions feeding the reward loop without relying on debug-only surfaces for the normal path. | Replace hidden simulations and runtime-only fallbacks with scene/prefab-driven gameplay, direct-control combat, authored item assets, and player-visible presentation. |
| Phase D - Long-Horizon Systems Foundation | Phase C is done. The project now reads as a game, but long-term scaling is not proven. | Formula-driven dungeon tiers, ground scaling, item rarity/material sinks, and save migration hooks exist without hand-authored content ladders. | Add generated tiers, item/drop pacing tables, material sinks, balance validation scripts, and extensible save schemas. |
| Phase E - Early Access Readiness Slice | Phase D is done. Systems scale, but the game is not yet release-shaped. | A 2-4 hour repeatable slice is playable with stable UI, recoverable failure, basic settings, readable onboarding, QA checklist, and no known progression blocker. | Add usability polish, error handling, content breadth, performance checks, settings, and release-scope triage. |

## Phase Promotion Rule

- If every completion criterion in the current phase is `Done`, update `Current phase` in the progress tracker, mark the first actionable task of the next phase as `Next`, and continue from that task.
- If the next phase is too broad, split the first risky item into one P0 task that can be completed in one automation run.
- If all listed phases are done or stale, add a `Next Production Phase Proposal` section with 2-3 options, a recommendation, and user-confirmation needs. Do not spend the run on filler cleanup.
- If a phase cannot be advanced because it requires Unity Editor/manual gameplay judgment, document the exact manual check and choose the next safe code/docs task that still moves the playable loop.

## Visible Game Production Rule

Phase B를 닫은 뒤부터는 "검증 가능한 간이 게임"을 더 오래 다듬는 것이 기본값이 아니다.

- smoke test, debug HUD, 진단 helper는 회귀를 막거나 다음 실제 구현을 열어줄 때만 추가한다.
- 한 번의 실행이 보조 작업만 했다면, 다음 실행은 실제 회귀/빌드 차단이 없는 한 **플레이어가 눈으로 보고 조작하는 변화**를 만들어야 한다.
- Phase C의 기본 질문은 "무엇을 더 측정할까?"가 아니라 "지금 어떤 placeholder를 실제 게임 요소로 바꿀까?"다.
- 우선 교체 대상은 숨은 전투 시뮬레이션, 런타임 임시 아이템, 보이지 않는 전선 표현이다.
- 첫 실제 게임 제작 우선순위는 직접 조작 던전 방, 실제 적 프리팹/AI, authored item assets/definitions, 지상 전선 시각화다.

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
| Current phase | Phase C - First Real Game Slice |
| Last meaningful movement | 2026-05-17: Phase B was closed by user confirmation, then the first dungeon encounter was corrected so enemies belong to a room lifecycle instead of free-roaming forever: hidden until room start, active during combat, explicit cleared/failed feedback on resolution. |
| Next unlock | Turn the current encounter bridge into the first actual room slice: visible room bounds/presentation plus one enemy prefab path that feels like a dungeon encounter rather than a loose scene actor. |
| Loop coverage | Phase A debug loop and Phase B player HUD slice are confirmed. Phase C has begun with one direct-control encounter path, but visible ground-defense action, authored item assets, and a room that reads spatially as a room are still open. |
| Known blockers | The new direct-control encounter still needs Play Mode feel validation; authored item assets/drop tables, production item-definition registry, visible ground-defense action, and longer pacing remain open. |

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

상태: Done

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
- User Play Mode confirmation on 2026-05-14 closed this P1 task at debug quality.

2026-05-14 smoke-test path progress note:

- `DungeonLoopSmokeTest` now exercises the current debug-quality playable loop from the existing `DungeonDebugHud`: start/clear dungeon, grant loot, equip the latest item, save, validate the saved JSON, clear live equipped flags, load, and confirm the saved equipped item is restored into `EquipmentSlots`.
- This does not replace full Unity restart verification. It reduces the normal Play Mode check to one button plus a separate restart pass and makes failed links report a specific blocker.
- User Play Mode confirmation on 2026-05-14 proved the smoke-test path works at debug quality. Do not keep selecting this task unless a save/load regression appears.

2026-05-14 publication note:

- Published the one-button smoke-test path as `b5bc121 Add one-button playable loop smoke test`.
- The automation target moved from Phase A to Phase B to avoid repeating save-file validation helper work.

## 4.1 Phase B Task Queue

### P0. 최소 플레이어 HUD 브리지

상태: Done

목표:
OnGUI 디버그 패널에 의존하지 않고, Canvas/TMP/Button 기반의 최소 플레이어 HUD에서 핵심 루프 상태와 행동을 다룬다.

완료 기준:

- `PlayableLoopHud`가 지상 전선, 재화, 던전 상태, 최신 아이템, 영웅 스탯, 최근 메시지를 표시한다.
- 버튼으로 지상 방어 시작/수리/Hold-Push 전환/강화, 던전 시작, 보상 수령, 최신 아이템 장착/분해, 저장/로드를 실행할 수 있다.
- 다음 행동 또는 막힌 이유가 `Message`나 선택적 `Action Hint` 텍스트로 표시된다.
- Unity 씬 연결 순서가 문서화되어 다음 수동 씬 작업자가 바로 배치할 수 있다.
- OnGUI 디버그 HUD는 검증/비상용으로 남기되, 정상 루프 설명은 `PlayableLoopHud` 중심으로 이동한다.

추천 파일:

- `Assets/02.Scripts/UI/PlayableLoopHud.cs`
- `GameDesign/ProductionDocs/06_UnitySceneAndPrefabSetupGuide.md`
- `GameDesign/ProductionDocs/09_BaseScriptUsageGuide.md`

2026-05-14 진행 메모:

- `PlayableLoopHud` 코드가 추가되어 Canvas/TMP/Button에 붙일 수 있는 player-facing HUD 브리지를 시작했다.
- 아직 `SampleScene` Canvas 배치는 하지 않았다. 다음 확인은 Unity Editor에서 TMP 라벨과 버튼을 연결하는 것이다.

2026-05-15 진행 메모:

- `PlayableLoopHud`에 지상 방어 버튼 메서드와 슬롯을 추가했다: `Start Defense`, `Repair Wall`, `Toggle Hold/Push`, `Upgrade Wall`, `Upgrade Tower`, `Upgrade Defenders`.
- 요약 라벨에 pressure/progress/upgrade levels를 추가했고, 다음 행동 힌트가 repair, upgrade, dungeon reward, equip, salvage, missing-reference blocker를 안내한다.
- `06_UnitySceneAndPrefabSetupGuide.md`와 `09_BaseScriptUsageGuide.md`의 연결 절차를 6개 버튼 기준에서 12개 버튼 + 선택적 `Action Hint` 기준으로 갱신했다.
- 2026-05-15 사용자 Play Mode 확인으로 `Gameplay` 씬의 HUD 연결이 완료됐고, 일반 루프가 OnGUI 디버그 패널 없이 동작하는 것을 확인했다. 이 브리지 작업은 더 이상 반복 선택하지 않는다.

### P0. 첫 10-20분 루프 패스

상태: Done

목표:
Phase A의 "작동하는 루프"를 Phase B의 "짧게 플레이 가능한 루프"로 바꾼다.

완료 기준:

- 신규 플레이어가 디버그 버튼 없이 지상전 보상, 던전 1회, 장착/분해, 저장/로드를 이해할 수 있다.
- 최소 3개의 의미 있는 결정이 있다: 지상 강화, 던전 재도전, 아이템 장착/분해.
- 실패 또는 막힘이 발생했을 때 다음 행동이 화면에 드러난다.

## 4.2 Phase C Task Queue

### P0. 첫 실제 던전 방 만들기

상태: Next

목표:
프로토타입 계산 전투에 기대지 않고, 플레이어가 직접 클릭 조작으로 들어가 싸우는 던전 방 1개를 만든다.

완료 기준:

- `Gameplay` 런타임 안에서 영웅이 지면 클릭 이동과 적 클릭 공격을 수행한다.
- 최소 1종의 실제 적 프리팹이 존재하고, 적이 영웅을 추적/공격한다.
- 일반 플레이 경로는 숨은 `CombatRoom` 시뮬레이션만으로 끝나지 않고, 보이는 적 사망으로 방 클리어가 난다.
- 클리어/실패 결과가 기존 보상/귀환 루프와 연결된다.
- 필요한 씬/프리팹 수동 작업이 있으면 정확한 연결 절차를 문서화한다.

2026-05-17 진척 메모:

- `Gameplay`의 기존 `Player`/`Enemy` 오브젝트를 첫 실제 방의 전투원으로 연결했다.
- `EnemyAIController`가 적의 플레이어 추적/근접 공격을 맡고, `CombatRoom`은 첫 패스에서 Player/Enemy를 자동 탐색해 계산형 전투보다 실제 `Health` 기반 전투를 우선한다.
- 사용자의 피드백대로 이 상태만으로는 "방"도 "클리어"도 아니었다. 적이 장면에 상시 존재하며 공격 피드백도 약했기 때문이다.
- 후속 수정으로 적은 방 시작 전에는 비활성, 전투 중에만 활성, 해소 뒤에는 다시 비활성으로 바뀌고, HUD는 현재 HP와 `Room/Dungeon cleared` 메시지를 보여 준다.
- 아직 실제 방 경계/프리팹/배치가 없으므로 이 항목은 `Next` 상태를 유지한다.

### P0. 지상 전선 시각 프로토타입

상태: Planned

목표:
숫자 압박 모델만 보이던 지상 전선을, 적이 성벽으로 밀려오고 방어가 자동으로 대응하는 장면으로 바꾼다.

완료 기준:

- 적이 화면에서 지속적으로 전진한다.
- 포탑 또는 병력이 자동으로 적을 공격한다.
- 압박 증가와 성벽 손상이 숫자뿐 아니라 장면에서도 읽힌다.
- 기존 `DefenseDirector` 수치 루프와 시각 오브젝트가 분리되지 않고 함께 움직인다.

### P1. 첫 authored item 세트

상태: Planned

목표:
런타임 임시 아이템 fallback에만 기대지 않고, 실제 `ItemDefinition` 에셋 몇 개가 던전 보상으로 떨어지기 시작한다.

완료 기준:

- 무기/방어구/장신구에서 최소 1개씩 실제 아이템 정의 에셋이 있다.
- 일반 플레이 경로에서 보상은 authored definition을 우선 사용한다.
- save/load 후에도 definition 재연결 경로가 분명하다.
- prototype fallback은 비상 경로로만 남고, 일반 플레이 설명은 authored item 기준으로 바뀐다.

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

- 2026-05-15 첫 10분 플레이 패스에서 던전 재도전 버그를 발견했다. 1회 클리어 뒤 다시 `Start Dungeon`을 누르면 `ExpeditionDirector`는 새 런을 `Running`으로 시작했지만, `CombatRoom`은 이전 0번 방의 `Cleared` 상태를 보고 같은 방 재시작을 막아서 경과 시간만 증가했다. 이 재시도 차단 로직은 제거했고, 후속 Play Mode 확인에서는 두 번째 던전도 즉시 `Starting -> Running -> Cleared`로 다시 흘러야 한다.
- 2026-05-16 QA 게이트에서 전선 돌파 후 보상이 0%라면 Gold를 모두 쓴 플레이어가 수리비를 다시 벌지 못해 진행이 잠길 수 있는 소프트락을 발견했다. 초기 프로토타입은 돌파 중 보상을 25%로 낮추되 0으로 만들지 않도록 수정했고, 후속 10-20분 패스에서는 돌파 후 수리 회복이 화면에서 명확히 읽히는지 함께 확인해야 한다.
- 2026-05-17 QA 게이트에서 `Gameplay` 씬 안에 `Player`와 `Enemy`가 이미 있었지만 `CombatRoom`이 둘을 추적하지 않아 일반 플레이 경로가 계속 숨은 계산 전투로만 흘러가는 간극을 발견했다. 첫 수정은 적을 붙이는 데는 성공했지만, 사용자의 피드백대로 적이 그냥 따라오다 멈추고 방/클리어 감각이 전혀 없었다. 후속 수정에서 적을 room lifecycle에 묶고, 현재 HP와 명시적 clear/fail 메시지를 노출했다.
