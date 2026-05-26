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
- 지상 디펜스와 지하 던전은 시스템 폴더와 화면 패널을 분리하지만, 플레이어용 런타임은 하나의 살아 있는 게임 루프로 유지한다.
- 던전 화면을 보고 있어도 지상 전선 시뮬레이션은 멈추지 않는다. 반대로 지상 화면을 보고 있어도 던전 런 상태와 보상 대기는 유지되어야 한다.

## 2. 추천 Assets 폴더 구조

현재 구조를 유지하면서 아래 폴더를 추가한다.

```text
Assets/
  01.Scenes/
    SampleScene.unity 또는 Gameplay.unity
    Bootstrap.unity
    DefensePrototype.unity    # 선택: 지상 단독 테스트 샌드박스
    DungeonPrototype.unity    # 선택: 던전 단독 테스트 샌드박스
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

### 플레이어용 런타임 씬

역할:

- 지상 전선, 던전, 인벤토리, 저장 매니저가 동시에 살아 있는 실제 게임 플레이 공간
- 탭, 패널, 카메라, 오브젝트 활성화로 화면만 전환
- 어느 패널을 보고 있어도 다른 시스템의 시간/상태가 유지됨

MVP/Phase B에서는 `SampleScene`을 이 결합 런타임 씬으로 사용한다. 나중에 이름을 바꾼다면 `Gameplay.unity`가 적합하다.

권장 계층:

```text
GameSystems
  CurrencyWallet
  DefenseUpgradeModel
  DefenseDirector
  DefenseSaveManager
  SimpleInventory
  ItemSalvageService

DefenseRoot
  DefenseWall
  TowerBattery
  DefenderSquad

DungeonRoot
  ExpeditionDirector
  CombatRoom
  LootDropper
  Hero
  DungeonRoomContainer

Canvas_Gameplay
  Panel_PlayableLoopHud
  Panel_Defense
  Panel_Dungeon
  Panel_Inventory
```

중요한 규칙:

- `GameSystems`는 씬 전환 느낌을 주는 UI 탭 변경 중에도 꺼지면 안 된다.
- `DefenseDirector`는 던전 화면을 보고 있을 때도 계속 `Update()`로 전선을 진행한다.
- 던전 직접 조작 화면이 전체 화면을 차지하더라도 지상 전선은 숫자 시뮬레이션으로 계속 진행한다.
- 플레이어가 둘을 "같이 본다"는 뜻은 항상 두 전투를 풀 비주얼로 동시에 보여준다는 뜻이 아니라, 한 화면의 HUD/탭에서 둘의 현재 상태와 할 일을 즉시 확인할 수 있다는 뜻이다.

### Bootstrap 씬

역할:

- 게임 전체 진입점
- 저장 데이터 로드
- 공통 매니저 생성
- 첫 화면으로 이동

MVP에서는 `SampleScene`을 임시 Bootstrap/Gameplay 겸용으로 사용해도 된다. 별도 `Bootstrap.unity`는 나중에 메인 메뉴, 로딩, 설정, 지속 매니저가 필요해질 때 만든다.

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

주의: 이 씬은 선택적인 단독 테스트 샌드박스다. 실제 플레이어용 루프는 `SampleScene`/`Gameplay` 안에서 던전과 함께 돌아야 한다.

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

주의: 이 씬은 선택적인 단독 테스트 샌드박스다. 던전을 테스트하기 위해 지상 전선 런타임을 언로드하는 구조로 쓰지 않는다. 플레이어용 던전은 `SampleScene`/`Gameplay` 안의 `DungeonRoot` 또는 나중의 additive 시각 레이어로 붙인다.

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
10. 실제 적 프리팹을 붙인 뒤에는 첫 패스에서 `CombatRoom > Auto Find Tracked Combatants`가 `PlayerController`와 `CharacterTeam.Enemy`를 자동으로 찾는다. 방/영웅이 여러 개가 되면 `Hero Health`와 `Enemy Healths`를 명시적으로 연결해서 생존 판정 대상을 고정한다.

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
4. Use `Dungeon Loop Debug` -> `Load`, then `Validate Snapshot`. Manual `Load` restores the saved snapshot exactly; offline catch-up is only applied when the game loads on startup.
5. Restart Play Mode and repeat `Validate Snapshot` to confirm the persisted inventory count, equipped item ids, and prototype snapshot-power stat bridge survive a full session restart.
6. If you are testing rollback behavior, press `Load` before the next auto-save tick. The current prototype still uses one shared save file, so the 15-second auto-save can overwrite the earlier manual snapshot.
7. This is still a debug HUD smoke test. It does not replace the later production inventory UI or authored item-definition registry.

2026-05-14 Phase B minimal player HUD bridge:

1. Create a normal Canvas panel named `Canvas_PlayableLoop`.
2. Add a child object named `Panel_PlayableLoopHud` and attach `PlayableLoopHud`.
3. Add TMP text fields for `Summary`, `Resources`, `Dungeon`, `Latest Item`, `Hero Stats`, `Message`, and optionally `Action Hint`, then assign them to the matching `PlayableLoopHud` label slots. If `Action Hint` is not assigned, the next-action hint is appended to `Message`.
4. Add ground buttons for `Start Defense`, `Repair Wall`, `Toggle Hold/Push`, `Upgrade Wall`, `Upgrade Tower`, and `Upgrade Defenders`, then assign them to the matching button slots.
5. Add dungeon/item/save buttons for `Start Dungeon`, `Claim Reward`, `Equip Latest`, `Salvage Latest`, `Save`, and `Load`, then assign them to the matching button slots.
6. Let `Auto Find References` stay enabled for the first pass. If a scene has multiple heroes or inventories later, wire `DefenseDirector`, `ExpeditionDirector`, `SimpleInventory`, `ItemSalvageService`, `EquipmentSlots`, `CharacterStats`, `CurrencyWallet`, and `DefenseSaveManager` explicitly.
7. Keep `DungeonDebugHud` and `InventoryDebugHud` in the scene only as smoke-test fallback. Normal Phase B testing should use `PlayableLoopHud` first.
8. After wiring, Play Mode check: start/repair/toggle the frontline, buy one defense upgrade, start dungeon, wait for clear, claim reward if needed, equip latest or salvage it, save, load, and confirm the message/action-hint lines and button interactability guide the next action.

2026-05-14 PlayableLoopHud feedback update:

1. Make the `Dungeon` TMP text field tall enough for 4 lines. It now shows expedition state, elapsed time, reward state, last expedition result, room state, room timer, and prototype hero/enemy health.
2. `Claim Reward` can stay unavailable while a run is active. If the dungeon clears and `ExpeditionDirector > Grant Reward On Expedition Clear` is enabled, the reward is granted automatically and the button is only a status/confirmation action.
3. If pressing `Start Dungeon` shows `Room: unavailable`, the HUD found `ExpeditionDirector` but did not find `CombatRoom`. In that case keep `CombatRoom` on `DungeonRoot` or wire it directly into `PlayableLoopHud`.
4. If the dungeon appears to be stuck in `Running`, check the `Room:` line first. `Starting` means countdown, `Running` means prototype combat is ticking, and `Cleared`/`Failed` means the room already resolved.

2026-05-15 PlayableLoopHud ground-action update:

1. `PlayableLoopHud` now exposes ground-defense controls for `Start Defense`, `Repair Wall`, `Toggle Hold/Push`, `Upgrade Wall`, `Upgrade Tower`, and `Upgrade Defenders`.
2. The summary label now includes pressure, progress, and Wall/Tower/Defender levels so the player can see why a ground upgrade matters before entering another dungeon.
3. Add an optional `Action Hint` TMP text field if the layout has room. The HUD writes the next recommended action there, including repair, upgrade, dungeon reward, equip, salvage, and missing-reference blockers.
4. Phase B layout should now treat `PlayableLoopHud` as the normal combined loop panel. `DefenseHud`, `DungeonDebugHud`, and `InventoryDebugHud` should remain fallback/debug surfaces only.

2026-05-26 playable screen focus handoff:

1. On the main gameplay Canvas, create a parent main-play-area object between the global top bar and bottom action bar. Suggested name: `Panel_MainPlayArea`.
2. Under it, create or assign `Panel_DefenseSide` and `Panel_DungeonViewport` as RectTransforms. Attach `PlayableScreenLayoutController` to a nearby UI controller object, then wire those two RectTransforms.
3. Starting values: keep `Starting Focus` as `DefenseFocus`, `Dungeon Focus Dungeon Width` at `0.70`, `Defense Panel On Right` enabled, `Entry Duration Seconds` at `0.38`, and `Exit Duration Seconds` at `0.32`. These are MVP temporary values from `11_PlayableScreenPresentationSpec.md`, not final art direction.
4. Fixed intent: `DefenseFocus` should make the defense panel fill the main play area; `DungeonFocus` should make the dungeon panel fill the left 70% and compress defense to the right 30%. The controller only changes anchors and active overlay objects. It does not choose final camera angle, ornate frame density, object scale, or panel art.
5. Optional overlays: create `Panel_InventoryOverlay`, `Panel_CraftingOverlay`, and `Panel_RewardOverlay`, keep them inactive by default, and wire them into the controller. Their exact content, item list density, tooltip placement, and art treatment are adjustable in Unity.
6. If `PlayableLoopHud > Sync Screen Focus With Dungeon` is enabled, the HUD auto-finds `PlayableScreenLayoutController`: `Start Dungeon` requests `DungeonFocus`, and room clear/fail requests `DefenseFocus`.
7. Add buttons later for `OpenInventoryOverlay`, `OpenCraftingOverlay`, `OpenRewardOverlay`, and `CloseOverlay` if the bottom action bar has room. These are normal Unity button methods on `PlayableScreenLayoutController`.
8. Play Mode check: start in `DefenseFocus`, press `Start Dungeon`, confirm the dungeon panel becomes dominant and defense stays visible, clear or fail the room, confirm the view returns to `DefenseFocus`, then open/close any wired overlay and confirm it returns to the previous gameplay focus.
9. Manual visual review required: split ratio, side-panel crop, camera framing, overlay size, text density, and final Diablo-like UI treatment are user/Unity Editor decisions.

2026-05-17 first real dungeon-room bridge:

1. `Gameplay`의 `Enemy`에는 `EnemyAIController`를 붙여 실제 플레이어를 추적/공격하게 한다.
2. `CombatRoom`은 첫 패스에서 플레이어와 `CharacterTeam.Enemy`를 자동 탐색하고, 새 런이 시작될 때 추적 중인 전투원을 다시 채워 반복 테스트가 가능하게 한다.
3. `Manage Tracked Enemy Activity`를 켜면 적은 방 시작 전에는 비활성, 전투 중에는 활성, 방 해소 뒤에는 다시 비활성으로 바뀐다. 그래야 적이 전역 씬 몹처럼 떠돌지 않고 던전 방 소속으로 읽힌다.
4. `PlayableLoopHud`는 추적 전투원을 발견하면 `wait` 대신 적을 클릭해 싸우라는 힌트, 현재 HP, 그리고 clear/fail 메시지를 보여 준다.
5. 여러 방/여러 영웅 구조로 확장할 때는 자동 탐색 대신 명시적 참조 또는 스포너 기반 연결로 바꾼다.

2026-05-18 visible room shell handoff:

1. `DungeonRoomPresenter`는 최종 방 아트가 아니라, authored room prefab이 생기기 전까지 공간을 읽게 해 주는 **prototype/fallback 보조 컴포넌트**다.
2. 첫 수동 배치 때는 `Gameplay > DungeonRoot`에 `DungeonRoomPresenter`를 붙이고, authored room prefab이 아직 없을 때만 `Auto Build Prototype Fallback Visuals`를 임시 확인용으로 켠다.
3. `Prototype Debug Tint`는 개발 중 상태 확인용 옵션이다. 최종 게임의 방 상태는 통째 색을 바꾸는 방식이 아니라 문 잠금/해제, 적 등장 연출, 보상 오브젝트, 조명/VFX, UI 같은 authored 피드백으로 읽히게 한다.
4. authored 방 비주얼을 붙였으면 기본적으로 `Apply Prototype State Tint`는 꺼 둔다. 상태 전환 검증이 필요할 때만 잠깐 켜고, 최종 비주얼 판단에는 섞지 않는다.
5. `Floor Renderer`와 `Boundary Renderers`는 임시 tint 확인이 필요할 때만 연결하면 된다. 최종 room prefab이 자체 연출을 가지면 이 presenter 자체도 debug/fallback 용도로만 남거나 제거될 수 있다.
6. 다음 프리팹 단계에서는 현재 씬의 `Enemy`를 그대로 두는 대신 `PF_DungeonEnemy_Melee`를 만들고, 이후 스포너가 그 프리팹을 방 시작 시 생성하도록 전환한다.

의도한 첫 시작값:

- 권장 방 비율: 가로가 세로보다 조금 넓은 직사각형
- 임시 시작 크기: `16 x 12`
- 경계 표현: 실제 높은 벽보다, 첫 패스에서는 플레이 공간을 읽게 하는 낮은 경계선
- 임시 경계 높이: `0.45`
- 바닥 표식 두께: `0.05`
- 배치 의도: 플레이어와 적이 서로를 인지한 뒤 1-2초 안에 교전권으로 들어오되, 클릭 이동으로 한 번은 거리 조절을 체감할 수 있는 여유를 남긴다.
- 조정 가능 항목: 최종 방 크기, 벽 두께/높이, 재질, 장식, 카메라 각도
- 지켜야 할 항목: 방이 시작 전에도 공간으로 읽히고, 전투 중/클리어 후 상태 변화가 눈에 보여야 한다.

2026-05-19 prefab enemy spawn handoff:

1. Create or duplicate the first melee enemy prefab as `Assets/04.Prefabs/Dungeon/PF_DungeonEnemy_Melee.prefab`.
2. Required components on the prefab: `CharacterActor`, `CharacterStats`, `Health`, `CharacterMotor`, `CombatDriver`, `EquipmentSlots`, `NavMeshAgent`, `EnemyAIController`, and a collider that can receive player clicks.
3. Set `CharacterActor > Team` to `Enemy`. This is fixed for the prefab-spawned combat path.
4. Suggested initial enemy stat targets: Max Health `35-60`, Attack Damage `4-8`, Attack Range `1.4-2.0`, Attack Cooldown `1.0-1.8`, Move Speed `2.5-3.8`. These are prototype feel values, not long-term balance targets.
5. On `Gameplay > DungeonRoot` or the authored room object, add `EnemySpawner` beside `CombatRoom`.
6. Wire `EnemySpawner > Combat Room` to the room's `CombatRoom`, assign `Enemy Prefab` to `PF_DungeonEnemy_Melee`, and keep `Spawn On Room Start` enabled.
7. Add 1-3 empty spawn transforms under the room, named `EnemySpawnPoint_01`, `EnemySpawnPoint_02`, etc., then assign them to `EnemySpawner > Spawn Points`.
8. Starting spatial intent: put the first spawn point ahead of the hero rather than directly on top of the hero, close enough for contact within roughly `1-2` seconds after the room starts. The exact distance, angle, camera read, silhouette, and cover/wall relation are adjustable in-editor.
9. Fixed values: the spawned enemy must be owned by `EnemySpawner`, registered into `CombatRoom`, and inactive until `CombatRoom` reaches `Running`. Adjustable values: spawn count, spawn point positions, enemy prefab art/scale, NavMeshAgent radius, room size, camera angle, and final encounter composition.
10. Play Mode check: press `Start Dungeon`; during `Starting`, spawned enemies should not attack yet; during `Running`, the prefab should activate, chase the hero, take click attacks, and clear the room through `All tracked enemies defeated`.

2026-05-20 authored tier-1 reward handoff:

1. `Assets/05.ScriptableObjects/Items` now contains the first six authored dungeon reward definitions: three Normal items, two Magic items, and one Rare ring.
2. `LootDropper` now reads `Reward Table` before the legacy uniform `Reward Definitions` array. Use `Reward Table` for normal authored rewards so rarity pacing does not require duplicate asset references.
3. `Gameplay > DungeonRoot > LootDropper` is wired to a prototype per-clear split of 78% Normal, 20% Magic, and 2% Rare. This is a short-term authored-reward bridge, not final long-term drop pacing.
4. Keep `Create Prototype Reward When Table Empty` enabled only as a safety fallback. The normal Phase C path should grant one of the authored `ItemDefinition` assets.
5. Play Mode check: clear the first room, confirm the HUD/latest item line shows one of the authored item names, equip or salvage it, then save/load and confirm the saved item reconnects through `SimpleInventory` known definitions.
6. D2 pacing note: this split is intentionally more conservative than the D2-derived `lp_n_act1_tier1` table because the current Unity room grants one guaranteed item per clear. Revisit it when per-kill/material/no-drop lanes exist.

2026-05-21 Phase C fallback-guard handoff:

1. `Gameplay > DungeonRoot > EnemySpawner` is already wired to `Assets/04.Prefabs/Dungeon/PF_DungeonEnemy_Melee.prefab` and one spawn point.
2. `CombatRoom` now blocks prototype simulation while `EnemySpawner` reports a setup blocker. This prevents a broken prefab path from clearing the room through hidden prototype combat.
3. `PlayableLoopHud > Dungeon` now shows `Path tracked enemies`, `Path prototype simulation`, `Path setup blocked`, or `Path waiting for enemies`.
4. `PlayableLoopHud > Dungeon` also shows the loot source: authored table, legacy list, or prototype fallback.
5. Play Mode check for the current gate: press `Start Dungeon`; during `Starting`, the spawned enemy should not attack; during `Running`, the HUD should show `Path tracked enemies`; after clear, the loot line should show authored table and the latest item should be one of the six tier-1 assets.
6. If the HUD shows `Path setup blocked`, fix the named prefab/spawn/Health setup issue before judging combat feel. Do not accept a prototype-simulation clear as Phase C completion.

2026-05-22 visible ground lane presenter handoff:

1. Add a scene object under `Gameplay > DefenseRoot` named `GroundDefenseLane`.
2. Add `GroundDefenseLanePresenter` to `GroundDefenseLane`.
3. Wire `Defense` to the scene `DefenseDirector`, or leave `Auto Find Defense` enabled for the first pass.
4. Create two empty anchors: `EnemySpawnAnchor` at the enemy-entry side of the lane and `WallAnchor` at the citadel/wall side. Assign them to the presenter. The presenter only reads these positions; the exact lane length, camera angle, and silhouette are editor-authored values.
5. Add a simple marker object named `EnemyPressureMarker` and assign it. It moves from spawn toward wall as `EnemyPressure / EnemyPressureCapacity` rises.
6. Add a second marker named `PushProgressMarker` and assign it. It moves from wall toward spawn as `FrontlineProgress / FrontlineProgressRequired` rises while Push is active.
7. Optional: add thin child transforms for `WallHealthFill` and `PressureFill`. Their local X scale is multiplied by wall-health percent and pressure percent, so set their full-size scale in the editor before Play Mode.
8. Optional: assign renderers for wall, pressure, and progress. The presenter changes `_BaseColor`/`_Color` through a material property block for Idle/Hold/Push/warning/breached states without editing shared materials.
9. Optional: assign small TMP labels for state, pressure, progress, and wall health if the normal HUD is not visible while tuning the lane.
10. Starting values: place `EnemySpawnAnchor` and `WallAnchor` far enough apart that the pressure marker movement is readable at the gameplay camera's normal zoom; use simple blockout shapes first. Adjustable values are lane length, marker art, fill thickness, colors, and label placement. Fixed values are the data source (`DefenseDirector.Runtime`) and the mapping: pressure approaches the wall, Push progress moves outward, wall health shrinks, and breach state must be visible.
11. Play Mode check: start defense, toggle Hold/Push, wait for pressure/progress changes, then force or tune toward a breach. Confirm the marker positions, fill scales, labels, and state objects change with the same values shown in `PlayableLoopHud`.

2026-05-23 visible ground lane enemy-flow update:

1. The presenter now auto-finds `Renderer` components on the assigned `EnemyPressureMarker` and `PushProgressMarker` transforms when `Auto Resolve Marker Renderers` is enabled. Existing marker objects in `Gameplay > DefenseRoot` should therefore recolor by Hold/Push/warning/breach state without assigning renderer fields manually.
2. To show continuous enemy pressure, add 2-5 simple scene-authored marker objects under `DefenseRoot`, name them `EnemyFlowMarker_01`, `EnemyFlowMarker_02`, etc., and assign their transforms to `GroundDefenseLanePresenter > Enemy Flow Markers`.
3. Fixed mapping: assigned flow markers move from `EnemySpawnAnchor` toward `WallAnchor`; the active marker count rises with `EnemyPressure / EnemyPressureCapacity`; all assigned flow markers remain active while breached.
4. Adjustable values: marker count, mesh/sprite/art, size, height above the lane, material, lane length, camera framing, and final silhouette. These are visual authoring choices and should be tuned in Unity.
5. Suggested first pass: use 3 small, clearly visible placeholder markers. Keep `Minimum Running Enemy Markers` at `1` and `Enemy Flow Cycles Per Second` near `0.18` until the lane is readable; tune speed only after the camera framing is settled.
6. Play Mode check: start defense, confirm at least one flow marker moves while Holding/Pushing, toggle Push, wait for pressure changes, and confirm marker color/count reads consistently with `PlayableLoopHud` pressure and wall/breach state.

2026-05-24 ground combat feedback handoff:

1. Add `GroundDefenseCombatPresenter` to `Gameplay > DefenseRoot` or to the same `GroundDefenseLane` object used for lane presentation.
2. Wire `Defense` to the scene `DefenseDirector`, or leave `Auto Find Defense` enabled for the first pass.
3. Reuse the same `EnemySpawnAnchor` and `WallAnchor` used by `GroundDefenseLanePresenter`. Fixed mapping: pressure actors move from spawn to wall, and breached state parks all active actors at the wall.
4. Create 2-5 scene-authored placeholder objects named `PressureActor_01`, `PressureActor_02`, etc., and assign their transforms to `Pressure Actors`. Suggested first pass: 3 readable, enemy-like placeholders. Adjustable values are count, mesh/sprite/art, height, scale, material, spacing, and silhouette.
5. Create one small flash object at the wall, named `WallContactFlash`, and assign it to `Wall Contact Object` or assign its renderer to `Wall Contact Renderer`. The presenter activates it when `WallHealth` drops or the frontline breaches. Adjustable values are flash art, scale, material, and exact offset from the wall; fixed intent is that wall damage must be visible.
6. Create 1-4 small projectile/slash objects named `DefenseAttackPulse_01`, etc., and assign them to `Attack Pulses`. Create an `AttackOrigin` transform near the tower/defender silhouette and assign it. Fixed mapping: pulses move from `AttackOrigin` toward the leading active pressure actor; active pulse count scales with the larger of `DefenseUpgradeModel.TotalDefensePower` and the measured pressure-cleared-per-second feedback from `DefenseRuntimeState`.
7. Suggested starting values: `Minimum Running Actors = 1`, `Pressure Actor Cycles Per Second = 0.16`, `Wall Contact Flash Seconds = 0.22`, `Wall Contact Scale Multiplier = 1.2`, `Attack Pulse Cycles Per Second = 0.85`, `Defense Power Per Visible Pulse = 8`. Optional color values can be tuned later: pressure actors default red, turn warmer under fire, and use the wall-contact color near breach/contact.
8. `PlayableLoopHud` auto-finds `GroundDefenseCombatPresenter` and shows `Ground combat visuals: ...` in the frontline summary when the component is present. If the line says `missing lane anchors` or `anchors only`, the scene wiring is not ready. If wired, the line should include `pressure +incoming/-cleared/s` and `wall damage/s` values.
9. Play Mode check: start defense, confirm pressure actors advance; toggle Push and confirm actor count changes with pressure; confirm attack pulse count/intensity rises when the `pressure -cleared/s` value is high; tune or wait until wall health drops and confirm a wall flash plus `wall /s` value. This does not judge final art, only that the real runtime state produces visible combat feedback.

## 4. 프리팹 목록

### 지상 디펜스 프리팹

| 프리팹 | 구성 컴포넌트 | 역할 |
| --- | --- | --- |
| `PF_GameSystems` | CurrencyWallet, DefenseUpgradeModel, DefenseDirector | 지속 전선 숫자 시뮬레이션 |
| `PF_DefenseHud` | DefenseHud | UI |
| `PF_GroundDefenseLane` | GroundDefenseLanePresenter + scene-authored anchors/markers | Phase C 지상 전선 시각 브리지 |
| `PF_GroundDefenseCombatFeedback` | GroundDefenseCombatPresenter + pressure actors + wall contact flash + attack pulses | Phase C 지상 전투 피드백 브리지 |
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
| GroundDefenseLanePresenter | `DefenseDirector.Runtime`을 읽어 scene-authored 지상 전선 앵커, 압박/진행 마커, 자동 marker renderer, 선택 enemy-flow marker, 성벽/압박 fill, 상태 오브젝트, 색상, 라벨을 갱신 |
| GroundDefenseCombatPresenter | `DefenseDirector.Runtime`을 읽어 scene-authored pressure actors, wall-contact flash, tower/defender attack pulses를 갱신하고 최근 압박/방어/벽 피해율을 포함한 `LastCombatMessage`를 HUD/Inspector에 노출 |
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

1. 실제 플레이 루프를 만들 때는 `SampleScene`/`Gameplay`를 연다. `DefensePrototype`은 지상 단독 수치 확인이 필요할 때만 쓴다.
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

플레이어용 구조는 씬을 갈아끼우는 방식이 아니라, 하나의 살아 있는 런타임에서 탭/패널/카메라만 전환하는 방식이 기본이다.

추천 첫 구현:

```text
SampleScene/Gameplay 하나에서
GameSystems는 계속 켜 둔다
→ 지상 전선은 계속 자동 진행
→ 던전은 같은 씬의 DungeonRoot에서 시작/진행/보상 처리
→ UI는 지상/던전/장비 탭으로 화면만 전환
```

이유:

- 지상과 던전은 서로 보상과 재화를 주고받는 하나의 루프다.
- 던전 화면으로 들어갔다고 지상 전선이 멈추면 이 게임의 방치/지속 전선 감각이 깨진다.
- 씬 전환과 로딩 구조를 너무 일찍 만들면 저장, 매니저 생명주기, 참조 복구 범위가 불필요하게 커진다.

나중에 씬을 여러 개 로드해야 한다면 다음처럼 additive presentation layer로 쓴다.

```text
Bootstrap 또는 GameplayCore 씬
  GameSystems, SaveManager, CurrencyWallet, DefenseDirector, ExpeditionDirector 유지

+ DefenseView additive 씬
  지상 전선 비주얼과 카메라

+ DungeonView additive 씬
  던전 방 비주얼, 적, 카메라
```

이 경우에도 규칙은 같다.

- core/system 씬은 언로드하지 않는다.
- additive 씬은 비주얼, 배치, 카메라, UI 레이아웃을 싣는 용도다.
- 던전 additive 씬을 로드해도 `DefenseDirector`는 계속 살아 있고, 지상 상태는 HUD로 표시된다.
- 지상 additive 씬을 보고 있어도 `ExpeditionDirector`의 보상 대기/실패/진행 상태는 유지된다.

### 7.1 MVP presentation target reference

Before implementing production UI panels, camera viewports, RenderTextures, or focus transitions, use `11_PlayableScreenPresentationSpec.md` as the current MVP reference.

MVP temporary presentation values:

- Start in `DefenseFocus`.
- On dungeon entry, compress defense into the right-side panel and slide/expand the dungeon viewport into the main play area.
- Keep the global top bar and bottom action bar fixed during the transition.
- Use a `70%` dungeon / `30%` defense split in `DungeonFocus`.
- Treat `0.38` seconds as the first-pass entry transition duration and `0.32` seconds as the first-pass exit duration.
- Do not reload scenes or pause `DefenseDirector` just to switch focus.
- Exact camera framing, object scale, panel crop, and ornate frame density remain Unity Editor feel-tuning tasks.

## 8. 완료 기준

Unity 세팅 완료 기준:

- `SampleScene`/`Gameplay`에서 지속 전선과 던전 루프가 같은 런타임 안에 있다.
- Frontline Level, Pressure, Wall Health, Gold/Scrap이 표시된다.
- Hold/Push 버튼이 동작한다.
- 강화 버튼이 실제 수치에 영향을 준다.
- 시각 단계에서는 적이 성벽으로 계속 이동한다.
- 던전 버튼이 최소한 결과 로그를 반환하고, 던전 화면을 보는 중에도 지상 전선이 멈추지 않는다.
