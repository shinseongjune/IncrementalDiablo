# Unity Scene And Prefab Setup Guide

## 2026-06-19 E0-B Camera Composition Prep

No autonomous camera/framing value is chosen here. The local harness now verifies that the existing `RawImage_DungeonViewport` and `RawImage_DefenseViewport` each keep a `PanelCameraRenderTarget` with a wired source camera and target image, so the manual composition pass can focus on visual judgment instead of silent scene-wiring drift.

Focused manual pass:

1. Open `Gameplay`, enter Play Mode, and start in `DefenseFocus` with the E0-A3 review override off.
2. If either viewport appears blank after camera or panel edits, select its `PanelCameraRenderTarget` and run `Render Target/Apply Now`.
3. Tune only editor-authored presentation values first: defense camera position, angle, orthographic size, and panel crop.
4. Target the reference orientation before deeper presentation work: enemy formation should read from the top/far side of the screen, pressure should travel downward/toward the lower contact area, and defender/tower/wall ownership should read near the lower protected side.
5. Compare against `GameDesign/References/2026-05-17_FinalGameplayScreenConcept.png`: enemy formation should read on the far side, defender/tower/wall ownership should read near the protected side, and the contact line should stay legible without HUD text.
6. Run `E0-A3 Review/Use Full Role Mix Band`, then repeat the check in `DefenseFocus` and after starting a dungeon so the defense view is compressed.
7. Run `E0-A3 Review/Clear Frontline Level Override` before judging normal progression or save/load.

Fixed for this pass: no unit selection, movement orders, focus fire, manual wave rows, new presentation abstraction, or save-state change. Adjustable by the reviewer: camera position/angle/orthographic size, panel crop, unit spacing, badge scale/height, base radius, sprite scale, and attack-line width/duration. If the top-to-bottom reference read requires moving `EnemySpawnAnchor` or `WallAnchor`, stop and record that as a separate scene-composition decision before editing.

## 2026-06-19 E0-A3 Formula-Driven Battle Scale Handoff

No new GameObject, prefab, or Inspector reference is required. `Gameplay > DefenseRoot > GroundDefenseNavMeshBattlefield` keeps the accepted runtime ground/NavMesh and derives visual force scale from the existing `DefenseDirector` Frontline profile. It also exposes a non-save E0-A3 review override so higher Frontline bands can be inspected without editing the real save/progression state.

Focused Play Mode validation, now accepted on 2026-06-19:

1. Open `Gameplay`, enter Play Mode, and start in `DefenseFocus` with `GroundDefenseNavMeshBattlefield > Use Review Frontline Level Override` off.
2. Confirm the baseline still reads like the accepted E0-A2 proof: two defenders, three enemies, cutout actors, faction bases/badges, attacker-to-target hit lines, target recoil, death/reinforcement, enemy wall hits, and visible wall-health loss.
3. Select `Gameplay > DefenseRoot > GroundDefenseNavMeshBattlefield` and run the component context menu `E0-A3 Review/Use Full Role Mix Band`. This sets the review visual profile to Frontline Level 21 only for the battlefield presentation.
4. Confirm the HUD summary shows `E0-A3 review scale` with Level 21, the higher band, added force count, role tier, and bounded respawn cadence. If the line does not appear, the review override is not active.
5. Confirm the higher band adds bounded enemy count and shield/runner role variety without hiding the defender/enemy ownership markers or source-target hit lines.
6. Confirm reinforcements feel more frequent but still leave enough time to see approach, stop-at-range, hit, target reaction, death, and wall pressure.
7. Start a dungeon to compress the defense panel and repeat the added count/role/cadence read at side-panel scale.
8. Run `E0-A3 Review/Clear Frontline Level Override` before ending the review or before judging normal progression.
9. Reject future regression or camera-composition passes if density turns the panel into unreadable motion, if role badges overlap enough to hide bodies, if attacker and target cannot be named, or if wall damage becomes understandable only through HUD text.

Fixed for this accepted gate: automatic NavMesh actors, no unit selection/movement/focus-fire, no manual wave rows, authoritative `DefenseRuntimeState`, and formula-derived force scaling. Adjustable for E0-B camera/reference composition: camera position/angle/orthographic size, panel crop, badge scale/height, base radius, sprite scale, attack-line width/duration, unit spacing, and only then force/role/cadence values if the camera pass exposes a readability regression.

## 2026-06-16 E0-A2 Readable Ownership Handoff

No new scene GameObject, prefab, or Inspector reference is required. `Gameplay > DefenseRoot > GroundDefenseNavMeshBattlefield` generates the added visuals at runtime.

Focused Play Mode validation:

1. Open `Gameplay`, enter Play Mode, and start in `DefenseFocus`.
2. Confirm the enemy and defender sprites no longer read as dark rectangular cards. The character body should be visible against the battlefield ground.
3. Confirm every generated actor has a ground faction base and shape-coded badge: blue defender shield versus red enemy threat marker.
4. Watch one defender/enemy melee exchange. Confirm the attacker stops, faces the target, a short attacker-to-target line appears, and the target flashes/recoils.
5. Allow defenders to fall. Confirm surviving enemies move to the wall, their wall hit line points to the wall, and the wall health visibly drops.
6. Confirm death/reinforcement still works as accepted on 2026-06-15 and that no inert duplicates remain.
7. Start a dungeon to compress the defense panel, then repeat identity, attack-line, target-reaction, and wall-hit checks at side-panel scale.

2026-06-17 result: the user confirmed this works well enough in Play Mode. No repeat of this E0-A2 ownership check is required unless actor identity, ownership line readability, target reaction, death/reinforcement, or wall-hit feedback changes.

Fixed for the accepted gate: existing anchors, generated ground/NavMesh, two defenders, three enemies, shared character stack, automatic targeting, no unit commands, cutout role-sheet sprites, faction base/badge markers, and short attack ownership lines. E0-A3 proved on 2026-06-19 that added density does not obscure the accepted reads. Adjustable during E0-B camera/reference composition: badge size/height, base radius, sprite scale, attack-line width/duration, force count, role mix, reinforcement cadence, and camera framing.

## 2026-06-15 NavMesh Ground Battle Handoff

`Gameplay > DefenseRoot` now has `GroundDefenseNavMeshBattlefield` enabled. `GroundDefenseLanePresenter`, `GroundDefenseActorRuntime`, `GroundDefenseEnemyPool`, `GroundDefenseBattlefieldView`, and `GroundDefenseCombatPresenter` are disabled.

Focused Play Mode validation:

1. Open `Gameplay`, enter Play Mode, and select `DefenseFocus`.
2. Confirm a dark ground surface appears between `Enemy Spawn Anchor` and `Wall Anchor`.
3. Confirm three enemies spawn on the hostile side and two defenders spawn near the wall.
4. Confirm enemies use NavMesh movement toward defenders, defenders move to intercept, and both stop near attack range while exchanging hit feedback.
5. Allow defenders to die. Confirm surviving enemies continue to the visible wall and attack it on cadence.
6. Watch the wall presentation and defense HUD. Confirm wall health decreases and breach can occur through these visible hits.
7. Confirm defeated defenders and enemies disappear and reinforce after their configured delays without leaving inert duplicates.
8. Start a dungeon to compress the defense panel and repeat the movement, combat, and wall-path check.

Fixed for this gate: existing enemy/wall anchors, runtime-generated ground/NavMesh, two defenders, three enemies, shared character component stack, automatic targeting, and no unit commands. Adjustable after review: ground width, unit spacing, movement speed, attack range/cooldown, respawn delay, sprite scale, and camera framing. Do not reposition anchors or change panel composition unless the live view proves a specific crop or pathing defect.

2026-06-15 result: the user confirmed this behavior works. No repeat of this foundation check is required unless movement, targeting, respawn, or wall damage changes. The 2026-06-16 handoff above is the current recognizable friendly/enemy and attack-source readability check.

## 2026-06-15 E0-A1 Sprite Rendering Handoff

No new GameObject or Inspector reference is required.

1. Keep `Gameplay > DefenseRoot > GroundDefenseBattlefieldView > Presentation Stage = Static Grammar`.
2. Enter Play Mode in `DefenseFocus` and pause.
3. Confirm the enemy and defender are visible as full-color cutout sprites and face each other.
4. Confirm the tower and wall read as larger rooted structures, not small blocks.
5. Confirm no wall health bar appears during this static proof and the zone bands remain secondary.
6. Start a dungeon to compress the defense panel, pause, and repeat the same four-noun check.
7. Reject the pass if any noun still reads as a rectangle/capsule/block or if cropping removes the weapon/building silhouette. Do not enable `Automatic Battle`.

The rendering implementation now uses `Sprite.Create` and `SpriteRenderer`; the old custom role quad/material path is no longer the validation target.

## 2026-06-14 E0-A1 Static Grammar Handoff

`Gameplay > DefenseRoot > GroundDefenseBattlefieldView` is now wired for the first ordered gate:

1. Keep `Presentation Stage = Static Grammar`.
2. Keep `Enemy Staging Percent = 0.28`, `Contact Line Percent = 0.72`, and `Battlefield Width = 4.8` for the first check.
3. The component generates `Zone_EnemyStaging`, `Zone_Approach`, `Line_Contact`, `Zone_FriendlyDefense`, `Enemy_GrammarProof`, `Defender_GrammarProof`, `Foundation_Tower`, and `Foundation_Wall` at runtime.
4. Do not switch to `Automatic Battle`, enable legacy pressure actors/pulses, or change pooled capacity for this check.
5. Enter Play Mode in `DefenseFocus`, pause, and confirm one enemy, one defender, tower, wall, ground zones, and contact line are all identifiable without HUD diagnostics.
6. Start a dungeon so the defense view compresses, pause again, and repeat the same noun check.
7. If the proof fails, record which object overlaps, crops out, appears ungrounded, or has ambiguous faction/scale. Do not compensate with faster motion or more units.

Fixed values for this gate: one visible enemy, one visible defender, no visible projectile, no casualty/reinforcement, and continuous background `DefenseRuntimeState`. Adjustable after review: zone width/color, unit scale/height offset, contact-line position, and tower/wall offsets. The ordered zone relationship and one-unit/one-building proof are fixed.

Validation result for the initial quad path: failed. The zones and contact line rendered, but the intended role-sheet figures/structures were not recognizable in the panel. The 2026-06-15 sprite handoff above replaces that rendering path; use it for the next proof.

## 2026-06-13 Next Ground Battlefield Authoring Gate

The current automatic battlefield failed Play Mode readability. Do not proceed by increasing counts, changing colors, or tuning projectile speed. The next Unity authoring pass must build and validate RTS concepts in this order.

### 1. Author stable battlefield zones

Under `DefenseRoot`, create or clearly identify authored transforms with these roles:

- `Zone_EnemyStaging`
- `Zone_Approach`
- `Line_Contact`
- `Zone_FriendlyDefense`
- `Structure_Tower`
- `Structure_Wall`

The camera must show their spatial order. Enemy travel must cross the ground from `Zone_EnemyStaging` toward `Line_Contact`; it must not read as generic top-to-bottom screen motion.

### 2. Unit visual contract

Before pooling/density, validate one enemy and one defender.

- Root sits on the ground plane.
- Add a visible ground shadow or footprint.
- Body faces its opponent.
- Weapon/role silhouette is readable at the actual panel scale.
- Enemy and friendly treatments differ by more than color.
- Walking, idle/contact, attack windup, hit reaction, and death are separate visible states.

Do not approve camera-facing cutouts merely because the source art is recognizable when viewed alone. They must read as actors occupying the battlefield.

### 3. Building visual contract

- Tower has a persistent foundation, tower body, and explicit `Muzzle` transform.
- Wall/citadel is the largest protected object and owns its health/damage feedback.
- Unit scale must remain clearly smaller than tower and wall scale.
- Buildings remain fixed while units move around/in front of them.

### 4. One deterministic attack proof

Temporarily use one enemy, one defender, one tower, and one wall at low cadence.

1. Enemy enters from `Zone_EnemyStaging`.
2. Enemy reaches `Line_Contact` and stops.
3. Defender visibly winds up and strikes; enemy visibly reacts.
4. Tower visibly aims/winds up.
5. Projectile leaves `Structure_Tower/Muzzle`, travels to the same enemy, and impacts it.
6. Enemy dies or resumes movement.
7. If it reaches the wall, its wall attack lands on `Structure_Wall`.

Do not restore multiple archetypes, rapid spawns, or reinforcements until this path is readable without HUD diagnostics.

### 5. Required review evidence

- One paused screenshot proving unit/building/zone distinction.
- One short observation of a complete melee exchange.
- One short observation of tower muzzle -> projectile -> enemy impact.
- The same proof in `DefenseFocus` and the compressed defense panel.

## 2026-06-13 E0-A Automatic Battlefield Handoff

`Gameplay > DefenseRoot` is already wired, but the presentation failed its focused Play Mode check. The values below describe the rejected technical checkpoint and must not be treated as an acceptance recipe.

Current component wiring:

1. `GroundDefenseActorRuntime`
   - `Actor Archetypes`: Grunt, Shield, Runner.
   - `Actor Capacity`: `8`.
   - This remains transient presentation state; do not add save fields for actor slots.
2. `GroundDefenseEnemyPool`
   - `Prewarm Per Archetype`: `3`.
   - `Max Instances`: `16`.
3. `GroundDefenseBattlefieldView`
   - `Enemy Spawn Anchor`: `Enemy Spawn Anchor`.
   - `Wall Anchor`: `Wall Anchor`.
   - `Attack Origin`: `Attack Origin`.
   - `Readability Sheet`: `GroundDefense_ReadabilitySheet`.
   - Starting formation values: `Contact Line Percent = 0.72`, `Enemy Lane Spacing = 0.72`, `Defender Count = 3`, `Defender Spacing = 0.92`, `Defender Line Gap = 0.72`.
   - Starting action values: `Projectile Capacity = 5`, `Projectile Speed = 12`, `Projectile Arc Height = 0.45`, `Melee Lunge Seconds = 0.18`, `Melee Lunge Distance = 0.42`, `Defender Death Seconds = 0.45`, `Reinforcement Seconds = 0.85`, `Casualty Cooldown Seconds = 2.5`.
4. `GroundDefenseCombatPresenter`
   - `Battlefield View`: the same `DefenseRoot` component.
   - `Use Production Battlefield`: enabled.
   - `Pressure Actors`: empty and `Show Pressure Actors`: disabled.
   - `Wall Contact Object/Renderer`: empty.
   - `Attack Pulses`: empty and `Show Attack Pulses`: disabled.
5. `GroundDefenseLanePresenter`
   - `Show Enemy Flow Markers`: disabled.
6. `PlayableLoopHud`
   - `Show Ground Combat Diagnostics`: disabled.

Fixed behavior:

- Enemy travel maps into formation lanes, then converges on one contact line before the wall.
- Actual `GroundDefenseActorRuntime.ActorHit` events trigger defender melee or a tower projectile.
- Enemy defeat/recycle uses the existing actor runtime and pool.
- Wall damage can trigger one defender casualty and later reinforcement.
- Wall health/hit/breach feedback belongs to the wall, not a detached flash.

Adjustable only after the Play Mode check:

- `Contact Line Percent`, lane/defender spacing, and defender size may be adjusted if actors overlap or the panel crop hides the source-target relationship.
- Projectile speed/arc and lunge distance may be adjusted if attacks are unreadable, but they must remain tied to a visible attacker and target.
- Do not change camera framing, wall/tower position, or overall panel composition autonomously unless the current crop makes the check impossible.

Historical Play Mode path that failed:

1. Open `Assets/01.Scenes/Gameplay.unity` and enter Play Mode in `DefenseFocus`.
2. Start defense and wait until several Grunt/Shield/Runner actors are active.
3. Confirm enemies occupy multiple lanes and meet the three defenders before the wall.
4. Confirm at least one defender lunge and one projectile visibly originate from the defender line/tower and point at an enemy.
5. Confirm an enemy loses health, shows defeat, disappears, and a later enemy reuses the pool.
6. Allow pressure to damage the wall. Confirm one defender falls, a replacement enters from the wall side, and wall health/hit/breach feedback appears on the wall.
7. Start a dungeon and confirm the compressed defense panel still preserves the same relationships without unreadable overlap.

## 2026-06-12 Direction-Only Ground Battlefield Handoff

This section supersedes the older pulse/flash and billboard acceptance instructions below. Those older sections remain only as implementation history.

- Composition reference: `GameDesign/References/2026-05-17_FinalGameplayScreenConcept.png`.
- Unit/structure role reference: `Assets/06.Art/Sprites/GroundDefense/GroundDefense_ReadabilitySheet.png`.
- Build one fixed isometric defense battlefield with the citadel/wall at the protected edge, fixed tower positions near the wall, friendly squads in front, and enemy formations entering from the far side.
- Required actor relationships:
  1. Enemy formations move toward the wall.
  2. Defender squads intercept them at a visible contact line.
  3. Melee units stop and play attacks against an actual opposing unit.
  4. Towers/ranged units launch projectiles from a visible muzzle/origin to a visible target.
  5. Hits reduce visible health and trigger target-bound reaction.
  6. Death removes/recycles the unit through a death action.
  7. Reinforcements enter from the correct faction side.
  8. Enemies that pass the contact line attack the wall; damage appears on the wall.
- Do not create or tune generic `Attack Pulses`, unattached wall flashes, moving pressure-marker rows, or isolated portrait-like battlefield billboards as the production solution.
- Do not add unit selection, movement commands, focus-fire controls, production queues, worker economy, or free tower placement.
- Fixed positions are authored in Unity. Exact lane width, camera framing, squad spacing, contact-line location, projectile arc, unit scale, and structure placement require editor judgment and must be validated against the reference image.
- `DefenseRuntimeState` remains the progression/reward/breach authority. Scene actors visualize its pressure and defense rates; they do not own a separate wave campaign or save ladder.

## 2026-06-11 Ground Progression Wiring

- No new scene object or Inspector reference is required for D1-A. The existing `DefenseDirector` automatically evaluates `GroundDefenseBalanceModel` from `DefenseRuntimeState.FrontlineLevel`.
- Do not restore the old `pressureGrowthPerLevel`, `pressureCapacityGrowthPerLevel`, `progressRequiredGrowthPerLevel`, or `rewardGrowthPerLevel` Inspector fields. Tune the shared constants and regenerate `GameDesign/Balance/GroundDefenseBalance.csv`.
- The current `DefenseHud` and `PlayableLoopHud` automatically show active band, pressure/defense/reward multipliers, next band level, and latest milestone result.
- Short runtime check when ground progression changes: at Frontline Level 1 confirm Band 1 and `x1` baseline; after any code/test-save route reaches Level 11, confirm Band 2, the Gold/Scrap milestone message, and no duplicate reward after save/load. This is a regression path, not a new visual-layout approval.
- Phase E E0-A will require a separate prefab/pool handoff. Do not add more scene-authored fixed pressure slots in the meantime.

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
5. Add dungeon/item/save buttons for `Previous Dungeon Depth`, `Next Dungeon Depth`, `Start Dungeon`, `Claim Reward`, `Equip Latest`, `Salvage Latest`, `Save`, and `Load`, then assign them to the matching button slots.
6. Let `Auto Find References` stay enabled for the first pass. If a scene has multiple heroes or inventories later, wire `DefenseDirector`, `ExpeditionDirector`, `SimpleInventory`, `ItemSalvageService`, `EquipmentSlots`, `CharacterStats`, `CurrencyWallet`, and `DefenseSaveManager` explicitly.
7. Keep `DungeonDebugHud` and `InventoryDebugHud` in the scene only as smoke-test fallback. Normal Phase B testing should use `PlayableLoopHud` first.
8. After wiring, Play Mode check: start/repair/toggle the frontline, buy one defense upgrade, start dungeon, wait for clear, claim reward if needed, equip latest or salvage it, save, load, and confirm the message/action-hint lines and button interactability guide the next action.

2026-05-14 PlayableLoopHud feedback update:

1. Make the `Dungeon` TMP text field tall enough for 4 lines. It now shows expedition state, elapsed time, reward state, last expedition result, room state, room timer, and prototype hero/enemy health.
2. `Claim Reward` can stay unavailable while a run is active. If the dungeon clears and `ExpeditionDirector > Grant Reward On Expedition Clear` is enabled, the reward is granted automatically and the button is only a status/confirmation action.
3. If pressing `Start Dungeon` shows `Room: unavailable`, the HUD found `ExpeditionDirector` but did not find `CombatRoom`. In that case keep `CombatRoom` on `DungeonRoot` or wire it directly into `PlayableLoopHud`.
4. If the dungeon appears to be stuck in `Running`, check the `Room:` line first. `Starting` means countdown, `Running` means prototype combat is ticking, and `Cleared`/`Failed` means the room already resolved.

2026-06-07 Phase D dungeon-depth controls:

1. Current `Gameplay > Canvas_Gameplay > Panel_PlayableLoopHud` already contains `Button_DungeonDepthPrevious` and `Button_DungeonDepthNext`, wired to `PlayableLoopHud.previousDungeonDepthButton` and `nextDungeonDepthButton`.
2. The first-pass anchors are fixed scene-safe values: previous button `x 0.17-0.215`, next button `x 0.22-0.265`, both `y 0.12-0.165`. They sit above the existing dungeon-start row and left of the action-hint region.
3. The Dungeon TMP line shows active `Depth` and `Selected/HighestUnlocked`. `Depth -` disables at depth 1; `Depth +` disables at the highest unlocked depth; both disable while a run is active.
4. Focused Play Mode check: start at selected/highest `1/1`, clear Depth 1, confirm `Depth 2 unlocked`, press `Depth +`, start and confirm active Depth 2, fail once and confirm highest stays 2, then Save/Load and confirm selected/highest remain `2/2`.
5. Fixed behavior is the saved unlock ladder and one-step clear advancement. Button size, final art, and wording may be refined in Phase E only if readability is poor; do not block D0-B formula work on ornate presentation.

2026-05-15 PlayableLoopHud ground-action update:

1. `PlayableLoopHud` now exposes ground-defense controls for `Start Defense`, `Repair Wall`, `Toggle Hold/Push`, `Upgrade Wall`, `Upgrade Tower`, and `Upgrade Defenders`.
2. The summary label now includes pressure, progress, and Wall/Tower/Defender levels so the player can see why a ground upgrade matters before entering another dungeon.
3. Add an optional `Action Hint` TMP text field if the layout has room. The HUD writes the next recommended action there, including repair, upgrade, dungeon reward, equip, salvage, and missing-reference blockers.
4. Phase B layout should now treat `PlayableLoopHud` as the normal combined loop panel. `DefenseHud`, `DungeonDebugHud`, and `InventoryDebugHud` should remain fallback/debug surfaces only.

2026-06-03 PlayableLoopHud defense-alert update:

1. No new TMP field is required. `PlayableLoopHud` can write `Defense alert: ...` into the existing summary text and can prioritize the alert in the existing action-hint text.
2. Inspector defaults: keep `Show Defense Alert In Summary` enabled, `Prioritize Defense Alert During Dungeon` enabled, `Low Wall Health Percent` at `0.35`, and `High Pressure Percent` at `0.75` for the first pass.
3. Fixed intent: breach, low wall health, wall damage per second, high pressure, or damaged-wall state must be visible while the player is in `DungeonFocus` or a dungeon run is active.
4. Play Mode check: start the frontline, enter a dungeon, then wait or tune pressure until the wall takes damage or pressure passes the threshold. Confirm the summary/action hint names the defense alert without hiding the dungeon state, then repair or recover through the existing defense buttons.

2026-05-26 playable screen focus handoff:

1. The current `Gameplay` scene already has the first screen-focus bridge: `PlayableScreenLayoutController`, `Panel_DefenseSide`, and `Panel_DungeonViewport` are present and wired for the MVP defense/dungeon split.
2. If rebuilding the scene from scratch, create a parent main-play-area object between the global top bar and bottom action bar. Suggested name: `Panel_MainPlayArea`. Under it, create or assign `Panel_DefenseSide` and `Panel_DungeonViewport` as RectTransforms, attach `PlayableScreenLayoutController` to a nearby UI controller object, then wire those two RectTransforms.
3. Starting values: keep `Starting Focus` as `DefenseFocus`, `Dungeon Focus Dungeon Width` at `0.70`, `Defense Panel On Right` enabled, `Entry Duration Seconds` at `0.38`, and `Exit Duration Seconds` at `0.32`. These are MVP temporary values from `11_PlayableScreenPresentationSpec.md`, not final art direction.
4. Fixed intent: `DefenseFocus` should make the defense panel fill the main play area; `DungeonFocus` should make the dungeon panel fill the left 70% and compress defense to the right 30%. The controller only changes anchors and active overlay objects. It does not choose final camera angle, ornate frame density, object scale, or panel art.
5. The current `Gameplay` scene already has `Panel_InventoryOverlay`, `Panel_CraftingOverlay`, and `Panel_RewardOverlay` wired into `PlayableScreenLayoutController`. If rebuilding the scene from scratch, create those panels, keep them inactive by default, and wire them into the controller. Their exact content, item list density, tooltip placement, and art treatment are adjustable in Unity.
6. If `PlayableLoopHud > Sync Screen Focus With Dungeon` is enabled, the HUD auto-finds `PlayableScreenLayoutController`: `Start Dungeon` requests `DungeonFocus`, and room clear/fail requests `DefenseFocus`.
7. Add bottom-action-bar buttons for `OpenInventoryOverlay`, `OpenCraftingOverlay`, `OpenRewardOverlay`, and `CloseOverlay` if the bottom action bar has room. Prefer wiring these to the matching `PlayableLoopHud` button slots so interactability follows the controller's overlay wiring state; the same methods also exist on `PlayableScreenLayoutController` for direct scene tests.
8. Play Mode check: start in `DefenseFocus`, press `Start Dungeon`, confirm the dungeon panel becomes dominant and defense stays visible, clear or fail the room, confirm the view returns to `DefenseFocus`, then open/close any wired overlay and confirm it returns to the previous gameplay focus.
9. Manual visual review required: split ratio, side-panel crop, camera framing, overlay size, text density, and final Diablo-like UI treatment are user/Unity Editor decisions.
10. Automation-side check: run `.\Tools\Automation\Invoke-IncrementalDiabloChecks.ps1` from the repo root. The current `Gameplay` scene should pass the required scene-contract checks and the optional overlay wiring check; future scenes may warn about optional overlays until those GameObjects are authored.

2026-06-04 dungeon render-target panel handoff:

Current `Gameplay` status as of 2026-06-05: `RawImage_DungeonViewport`, `Camera_DungeonPanel`, `PanelCameraRenderTarget`, and `DungeonViewportInputRouter` are already present and statically wired. The steps below are now rebuild/repair instructions, not the default next production task.

1. Under `Gameplay > Canvas_Gameplay > PlayableScreenLayoutController > Panel_DungeonViewport`, create a child `RawImage_DungeonViewport`.
2. Stretch `RawImage_DungeonViewport` to the full dungeon panel: anchors `x 0-1`, `y 0-1`, offsets all `0`. This value is fixed for the first pass because the click router assumes the whole image is the interactive dungeon view. Later decorative frames can wrap around it, but should not cover it with raycast-enabled UI unless intended.
3. Add a scene camera named `Camera_DungeonPanel` near the current dungeon room view. Starting intent: it should frame the hero, the first melee enemy, the room floor/clickable surface, and the clear/reward space in one readable shot. Adjustable values are position, rotation, FOV/orthographic size, culling mask, and clipping planes; fixed intent is that this camera is the source for the dungeon panel.
4. If the project already has a suitable dungeon gameplay camera, it can be reused instead of creating `Camera_DungeonPanel`. Do not reuse a defense-only camera for this field.
5. Add `PanelCameraRenderTarget` to `RawImage_DungeonViewport` or to a nearby helper object named `DungeonViewportRenderTarget`.
6. In `PanelCameraRenderTarget`, assign `Source Camera = Camera_DungeonPanel`, `Target Image = RawImage_DungeonViewport`, keep `Create Runtime Texture` enabled, keep `Match Image Rect` enabled, keep `Fallback Size` at `1280 x 720`, keep `Render Scale` at `1.0`, and keep `Depth Buffer Bits` at `24`. Use an explicit `RenderTexture` only if you want a named project asset for profiling or reuse.
7. Add `DungeonViewportInputRouter` to `RawImage_DungeonViewport`.
8. In `DungeonViewportInputRouter`, assign `Viewport Image = RawImage_DungeonViewport`, `Viewport Camera = Camera_DungeonPanel`, `Player = Hero` object with `PlayerController`, and `Screen Layout = PlayableScreenLayoutController`. If the router lives on the same object as `PanelCameraRenderTarget`, it can inherit the viewport camera from that render target during reference resolution, but explicit assignment remains clearer in the Inspector. Keep `Require Dungeon Focus` enabled so the dungeon panel does not accept combat clicks while defense or overlays own the screen.
9. On the hero `PlayerController`, keep `Ignore Clicks Over UI` enabled. Fixed intent: UI clicks should not also run the old `Camera.main` screen-ray path. The `DungeonViewportInputRouter` will send the correct camera ray explicitly.
10. Make sure the dungeon floor/clickable room surface and enemies are included in `PlayerController > Click Mask`. If self/friendly colliders are hit first, the controller now skips them and can still use a ground hit behind them; if there is no ground/clickable surface behind them, the click is ignored instead of moving onto the hero.
11. Play Mode check for P0-B: start in `DefenseFocus`, press `Start Dungeon`, confirm the dungeon panel shows the rendered camera view, and read the HUD Dungeon line `Viewport:` status. It should report a bound or ready render target and ready/routed input instead of missing camera, missing RawImage, or missing PlayerController. Click the floor inside the panel and confirm the hero moves, click the spawned enemy and confirm attack/chase starts, hold Shift and click inside the panel to confirm stationary attack behavior, click the hero/self collider and confirm it does not create a bad self-target or odd movement command, then open/close reward/inventory/crafting overlays and confirm panel clicks are ignored while overlays own focus.
12. P0-B can be marked `Done` only after the user confirms readability: dungeon camera framing, defense side-panel crop, 70/30 split, overlay occlusion, routed clicks, and defense-alert text are acceptable for MVP. If the rendered panel is correct but camera framing feels wrong, record the exact camera values and keep the issue as Unity Editor visual tuning, not a code blocker.

2026-06-04 dungeon render-target validation fixes:

1. Saved active dungeon state is allowed, but it must not spawn enemies behind an unrelated screen. If Play Mode is stopped while `ExpeditionDirector.State` is `Running`, the next Play Mode load should restore `DungeonFocus` automatically through `PlayableLoopHud`.
2. Revalidation path: start a dungeon, wait until the spawned enemy is active, stop Play Mode before the room resolves, start Play Mode again, and confirm the dungeon panel appears instead of leaving the enemy active behind `DefenseFocus`.
3. Shift-click target behavior: when Shift-clicking an enemy outside attack range, the hero should face and keep a stationary target command alive until the target enters range or the player gives another command. A ground Shift-click with no target still plays one in-place attack and clears.
4. Revalidation path: with the enemy visible in the dungeon panel, Shift-click the enemy at or just inside attack range and confirm its HP drops. Then Shift-click it from slightly outside range and confirm the command does not disappear after one swing before the enemy can move into range.

2026-06-05 dungeon viewport diagnostics update:

1. `PlayableLoopHud` can auto-find the dungeon `PanelCameraRenderTarget` and `DungeonViewportInputRouter` by preferring objects whose image, camera, or GameObject name contains `Dungeon`.
2. When the dungeon viewport is relevant, the Dungeon HUD line includes `Viewport: render ... / input ...`. This is a Play Mode QA aid for P0-B, not final HUD copy.
3. `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` now statically checks the `Gameplay` scene for `RawImage_DungeonViewport`, `Camera_DungeonPanel`, `PanelCameraRenderTarget`, `DungeonViewportInputRouter`, and their core serialized references before the manual camera/readability judgment.

2026-05-27 playable overlay button handoff:

1. `PlayableLoopHud` now has optional slots named `Open Inventory Overlay Button`, `Open Crafting Overlay Button`, `Open Reward Overlay Button`, and `Close Overlay Button`.
2. These buttons call `OpenInventoryOverlay`, `OpenCraftingOverlay`, `OpenRewardOverlay`, and `CloseOverlay` on `PlayableLoopHud`.
3. If `PlayableScreenLayoutController` has no matching overlay GameObject reference, the open button stays disabled and the controller will report that the overlay is not wired instead of entering an invisible overlay state.
4. Keep `Panel_InventoryOverlay`, `Panel_CraftingOverlay`, and `Panel_RewardOverlay` inactive by default after wiring them to the controller. The controller activates only the selected overlay.
5. Starting visual intent: overlays sit above the current defense/dungeon focus and return to the previous gameplay focus when closed. Exact overlay size, item-list density, tooltip placement, and ornamentation remain Unity-authored values.

2026-05-28 inventory overlay content handoff:

1. Use the existing `Gameplay` `Panel_InventoryOverlay`, or create one as an inactive child above the current focus panels when rebuilding the scene. Attach `InventoryOverlayPresenter` to that panel or a child controller object.
2. Assign TMP labels for `Header Text`, `Item List Text`, `Selected Item Text`, `Materials Text`, and `Message Text`. The script only fills text; exact list density, font size, scroll treatment, icon art, and tooltip placement remain Unity-authored.
3. Add buttons for `Previous Item`, `Next Item`, `Select Latest`, `Equip Selected`, `Salvage Selected`, and `Close Overlay`, then assign them to the matching presenter fields. The presenter also wires listeners automatically at runtime.
4. Leave `Auto Find References` enabled for the first pass. For production scenes with multiple inventories or heroes, explicitly assign `SimpleInventory`, `ItemSalvageService`, `EquipmentSlots`, `CurrencyWallet`, and `PlayableScreenLayoutController`.
5. Wire `Panel_InventoryOverlay` into `PlayableScreenLayoutController > Inventory Overlay`, then wire a bottom-action-bar button to `PlayableLoopHud.OpenInventoryOverlay` and a close button to either `InventoryOverlayPresenter.CloseOverlay` or `PlayableLoopHud.CloseOverlay`.
6. Play Mode check: clear a dungeon room, open the inventory overlay, confirm the latest reward appears in the list, select it, equip or salvage it, confirm hero stats or wallet/materials update, then close the overlay and confirm focus returns to the previous gameplay state.
7. This completes the code/content side for the first inventory overlay only. `Panel_RewardOverlay` and `Panel_CraftingOverlay` now have their own content presenter notes below.

2026-05-29 reward overlay content handoff:

1. Use the existing `Gameplay` `Panel_RewardOverlay`, or create one as an inactive child above the current focus panels when rebuilding the scene. Attach `RewardOverlayPresenter` to that panel or a child controller object.
2. Assign TMP labels for `Header Text`, `Reward Text`, `Item Detail Text`, `Materials Text`, and `Message Text`. The script only fills text; exact reveal animation, icon art, font size, and frame treatment remain Unity-authored.
3. Add buttons for `Claim Reward`, `Open Inventory`, `Equip Reward`, `Salvage Reward`, and `Close Overlay`, then assign them to the matching presenter fields. The presenter wires listeners automatically at runtime.
4. Leave `Auto Find References` enabled for the first pass. For production scenes with multiple inventories or heroes, explicitly assign `ExpeditionDirector`, `LootDropper`, `SimpleInventory`, `ItemSalvageService`, `EquipmentSlots`, `CurrencyWallet`, and `PlayableScreenLayoutController`.
5. Wire `Panel_RewardOverlay` into `PlayableScreenLayoutController > Reward Overlay`, then wire a bottom-action-bar button to `PlayableLoopHud.OpenRewardOverlay`. Keep `PlayableLoopHud > Open Reward Overlay On Dungeon Clear` enabled for the normal player path. The presenter can move from reward reveal to `InventoryOverlay` through its `Open Inventory` button.
6. Play Mode check: clear a dungeon room and confirm the reward overlay opens automatically. Confirm the reward state and loot source are visible, confirm the latest reward item details appear, equip or salvage the reward, optionally open inventory from the reward overlay, then close the overlay and confirm focus returns to `DefenseFocus`.
7. This completes the code/content side for the first reward overlay only. `Panel_CraftingOverlay` now has its own content presenter below, and final reward reveal animation/art remains Unity-authored.
8. Current `Gameplay` first-pass placement: `Panel_RewardOverlay` uses the centered overlay frame `x 0.18-0.82`, `y 0.18-0.86`; `Header` uses `x 0.04-0.42`, `y 0.885-0.965`; `Reward` uses `x 0.04-0.43`, `y 0.205-0.84`; `Item Detail` uses `x 0.465-0.755`, `y 0.62-0.84`; `Materials` uses `x 0.465-0.965`, `y 0.355-0.585`; `Message` uses `x 0.04-0.755`, `y 0.06-0.17`; the right action buttons use `x 0.785-0.965` with vertical slots from top to bottom: Claim Reward, Open Inventory, Equip Reward, Salvage, Close.

2026-05-31 automatic reward-overlay flow handoff:

1. `PlayableLoopHud` now opens `RewardOverlay` automatically after a cleared room when the overlay is wired and `Open Reward Overlay On Dungeon Clear` is enabled.
2. The HUD uses `PlayableScreenLayoutController.TryOpenOverlayAfterGameplayFocus(RewardOverlay, DefenseFocus)` for the room-clear path. This applies the post-run defense layout before the overlay opens, so closing the reward overlay returns to `DefenseFocus`.
3. Manual reward claiming from `PlayableLoopHud.ClaimPendingReward()` uses the same overlay handoff after a successful claim.
4. Fixed intent: clear reward review should be a normal player-facing flow, not a debug-only follow-up. Adjustable values remain reward reveal animation, item icon treatment, text density, button art, and ornate frame style.

2026-05-30 crafting overlay content handoff:

1. The current `Gameplay` `Panel_CraftingOverlay` already has `CraftingOverlayPresenter`, first-pass TMP labels, and first-pass buttons attached. If rebuilding the scene, create an inactive `Panel_CraftingOverlay` above the current focus panels and attach `CraftingOverlayPresenter` to that panel or a child controller object.
2. Assign TMP labels for `Header Text`, `Item List Text`, `Selected Item Text`, `Materials Text`, `Result Text`, and `Message Text`. The script only fills text; exact text density, scroll treatment, icon art, and frame treatment remain Unity-authored.
3. Add buttons for `Previous Item`, `Next Item`, `Select Latest`, `Reroll Affix`, `Salvage Selected`, and `Close Overlay`, then assign them to the matching presenter fields. The presenter wires listeners automatically at runtime.
4. Leave `Auto Find References` enabled for the first pass. For production scenes with multiple inventories or heroes, explicitly assign `SimpleInventory`, `ItemSalvageService`, `EquipmentSlots`, `CurrencyWallet`, and `PlayableScreenLayoutController`.
5. Wire `Panel_CraftingOverlay` into `PlayableScreenLayoutController > Crafting Overlay`, then wire a bottom-action-bar button to `PlayableLoopHud.OpenCraftingOverlay` and a close button to either `CraftingOverlayPresenter.CloseOverlay` or `PlayableLoopHud.CloseOverlay`.
6. Current `Gameplay` first layout pass: use the same centered overlay frame as inventory/reward, `x 0.18-0.82`, `y 0.18-0.86`. `Header` uses `x 0.04-0.96`, `y 0.88-0.965`; item rows use `x 0.04-0.47`, `y 0.27-0.84`; selected item details use `x 0.51-0.96`, `y 0.57-0.84`; materials/reroll cost uses `x 0.51-0.96`, `y 0.38-0.55`; current affixes/result uses `x 0.51-0.96`, `y 0.20-0.36`; message uses `x 0.04-0.96`, `y 0.145-0.19`; bottom actions from left to right are Previous, Next, Latest, Reroll, Salvage, Close. Fixed intent: reroll must read as a Rare item material sink; adjustable values are font size, row count, icon art, button positions, and ornate frame density.
7. Play Mode check: if the inventory has no Rare item, the current `Gameplay > DungeonRoot > LootDropper` should give the next authored reward as a Rare through its first-Rare pacing rule. Salvage one Rare first if `AlterStone` is still missing, claim the next Rare, open crafting, confirm the newest rerollable Rare is auto-selected, confirm reroll cost and reroll status are shown, press `Reroll Affix`, confirm `Gold + Essence + AlterStone` are spent, confirm a new affix appears instead of the same prototype affix repeating, confirm the Result panel shows the spent cost plus before/after affix summary, equip the item if needed and confirm hero stats refresh, salvage a spare item, close the overlay, and confirm focus returns to the previous gameplay state.
8. This completes the code/content side for the first crafting overlay only. Full affix pools, affix locking, item-level upgrades, scroll/icon polish, and final itemization balance remain future work.

2026-05-31 crafting reroll-candidate handoff:

1. `CraftingOverlayPresenter` now has `Prefer Reroll Candidate On Enable`. Keep it enabled for normal play so the overlay selects the newest rerollable Rare instead of forcing the player to hunt through recent Normal/Magic drops.
2. The header reports inventory count, Rare count, and `Reroll ready` count. The item-list `R` marker still identifies each rerollable row.
3. `Materials` now includes a next-step hint. If `AlterStone` is missing, the first-pass hint tells the player to salvage one spare Rare before rerolling the next Rare.
4. `Result` now includes `Reroll status`, which should read `ready` when the selected Rare can be rerolled and should name the missing requirement otherwise.
5. After a successful reroll, `Result` also keeps the last reroll summary for the selected item: spent materials plus the previous affix state and new affix.
6. The current prototype reroll avoids repeating the selected item's saved affix when another slot-valid candidate exists, so a paid reroll should produce a visibly different affix line during validation.
7. No new scene placement is required. Text density and row count are still adjustable in Unity if the new guidance crowds the panel.

2026-05-28 reference-layout cleanup handoff:

1. `Gameplay > Canvas_Gameplay > PlayableScreenLayoutController` is the main play area, not the full screen. Its anchors are `x 0-1`, `y 0.18-0.92`.
2. In the controller's child space, `Panel_DungeonViewport` starts at `x 0-0.7`, `y 0-1`; `Panel_DefenseSide` starts at `x 0.7-1`, `y 0-1`. Runtime focus changes still come from `PlayableScreenLayoutController`.
3. `Panel_PlayableLoopHud` is a transparent full-screen overlay (`x 0-1`, `y 0-1`) that only positions text/buttons into the reference bands.
4. Top global/status band: `Text_Resources` uses `x 0.08-0.42`, `y 0.925-0.99`; `Text_Dungeon` uses `x 0.20-0.50`, `y 0.825-0.90` as the dungeon title/status block.
5. Defense status/control band: `Text_Summary` uses `x 0.715-0.985`, `y 0.705-0.905`; defense buttons use two columns around `x 0.735-0.967`, `y 0.485-0.675`.
6. Bottom action bar: hero stats at `x 0.025-0.165`, latest loot at `x 0.555-0.715`, action messages at `x 0.275-0.530`, dungeon/item/save buttons from `x 0.18-0.633`, and inventory/crafting/reward buttons from `x 0.735-0.959`.
7. Inventory overlay frame: `Panel_InventoryOverlay` uses `x 0.18-0.82`, `y 0.18-0.86`; item list on the left (`x 0.04-0.43`), selected/material details in the middle (`x 0.465-0.965`), and item action buttons on the right (`x 0.785-0.965`).
8. Reward overlay frame: `Panel_RewardOverlay` uses `x 0.18-0.82`, `y 0.18-0.86`; reward summary on the left (`x 0.04-0.43`), item/material preview in the middle (`x 0.465-0.965`), and reward action buttons on the right (`x 0.785-0.965`).
9. Crafting overlay frame now starts from the same centered frame, `x 0.18-0.82`, `y 0.18-0.86`; item rows are on the left, selected item/current affixes/materials are on the right, and item actions are in the bottom row. Final row density and icon treatment remain Unity-authored.
10. Dark bronze button backgrounds need bright TMP label colors, preferably white or near-white, before the layout is considered ready for Play Mode review.
11. If a future automation or manual pass changes layout, report the exact parent, anchor ranges, button order, and Play Mode validation path rather than saying only that the UI was adjusted.

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
3. `Gameplay > DungeonRoot > LootDropper` is wired to a prototype per-clear split of 78% Normal, 20% Magic, and 2% Rare. This is a short-term authored-reward bridge, not final long-term drop pacing. To keep crafting validation reachable, the current scene also enables `Guarantee Rare When Inventory Has No Rare` and sets `Max Weighted Non Rare Rewards Before Rare` to `6`.
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

2026-06-06 P0-D NavMesh spawn contract:

1. Keep `Gameplay > DungeonRoot > EnemySpawner > Snap Spawn Points To Nav Mesh` enabled. The current starting radius is `2`.
2. The assigned melee prefab must contain `Health`, `CharacterActor` with Team `Enemy`, `EnemyAIController`, an enabled `NavMeshAgent`, and an enabled collider that receives panel-camera clicks.
3. Before instantiating the room group, `EnemySpawner` resolves every assigned point onto nearby NavMesh. If one point cannot resolve, it creates no partial encounter and reports `Path setup blocked`.
4. A placement blocker means the spawn transform is outside the baked walkable area, the NavMesh is stale/missing, or the sample radius is too small. Move the spawn point onto the room floor or rebake the current `NavMesh Surface`; increase the radius only when the intended point is already visually close to the walkable floor.
5. Focused Play Mode acceptance: press `Start Dungeon`; verify no attack during `Starting`; inspect `EnemySpawner.LastSpawnMessage` for `on NavMesh`; during `Running`, verify `Path tracked enemies`, chase, enemy damage to the hero, routed enemy click and Shift-click damage, HP reaching zero, `All tracked enemies defeated`, authored reward overlay continuity, then press `Start Dungeon` again and repeat the spawn/clear path.
6. 2026-06-06 accepted result: the user confirmed this path works. `Gameplay > DungeonRoot > CombatRoom > Simulate When No Enemies` is now disabled; a future setup failure must remain visible instead of silently using calculation combat.

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

2026-06-05 discrete ground actor runtime handoff:

1. Current `Gameplay > DefenseRoot` is already wired with `GroundDefenseActorRuntime` and the existing `GroundDefenseCombatPresenter`.
2. The actor runtime uses the three existing `Pressure Actors` as reusable slots. Current first-pass values are `Actor Capacity = 3`, `Actor Max Health = 12`, `Pressure Per Spawn = 8`, `Damage Per Hit = 3`, `Minimum Running Actors = 1`, `Base Advance Per Second = 0.10`, `Pressure Advance Per Second = 0.12`, and `Hit Feedback Seconds = 0.16`.
3. `GroundDefenseCombatPresenter > Actor Runtime` points to the same `DefenseRoot` component. The presenter now uses each runtime slot's active state, hit feedback, and travel percentage instead of looping every pressure actor independently.
4. Fixed intent: pressure generation creates actor slots, measured defense clearing produces discrete hits and defeats, and measured wall damage allows surviving actors to complete wall-contact events. The continuous `DefenseRuntimeState` remains authoritative for progression, rewards, breach, and save/load.
5. P0-C acceptance: the user confirmed on 2026-06-05 that the behavior appears to work. The Play Mode path above is now regression-only, not a request for another visual tuning pass.
6. Freeze rule: do not tune the current placeholder count, colors, movement speed, spacing, silhouette, or camera composition. When ground combat production resumes, replace the fixed scene slots with pooled enemy prefabs, archetype data, real targeting/death handling, and reusable combat feedback.

2026-06-12 pooled ground actor replacement handoff (historical implementation bridge; superseded by the direction-only handoff above):

1. Current `Gameplay > DefenseRoot` already has `GroundDefenseActorRuntime`, `GroundDefenseEnemyPool`, `GroundDefenseBattlefieldView`, and `GroundDefenseCombatPresenter` wired.
2. `Actor Archetypes` and `Prewarm Archetypes` reference `GDA_Enemy_Grunt`, `GDA_Enemy_Shield`, and `GDA_Enemy_Runner`. All three reuse `PF_GroundDefenseEnemy_Grunt` as a pooled component shell while their archetype data selects a different region of `GroundDefense_ReadabilitySheet.png`.
3. Role intent is fixed: Grunt is the common baseline (`12` HP, weight `4`), Shield Breaker is slow and durable (`24` HP, weight `1`), and Bone Runner is fragile and fast (`7` HP, weight `2`). Tune these through archetype assets, not manual wave rows.
4. `Actor Capacity` starts at `8`; `Prewarm Per Archetype` starts at `3`; `Max Instances` starts at `16`. These are pooling/visibility limits, not authored wave counts.
5. `GroundDefenseBattlefieldView` references `Camera_DefensePanel`, `WallAnchor`, `AttackOrigin`, and the readability sheet. It creates the stone wall at `WallAnchor`, the crossbow tower at `AttackOrigin`, and the defender between them. Serialized sizes/offsets are starting values and may be adjusted only if the panel has overlap or crop issues.
6. Current implementation note: `GroundDefenseCombatPresenter` still owns pulse-derived bolt presentation. It is not the production target and should be removed from the normal player path when actual attackers/projectiles are implemented.
7. Reusable boundary: preserve the authoritative `DefenseRuntimeState`, archetype stats, pooling, health, defeat, and wall-contact event concepts where useful.
8. Do not request acceptance for billboard orientation, bolt targeting, or isolated role recognition. The next acceptance path begins only after visible squads meet and fight inside one coherent battlefield.

## 4. 프리팹 목록

### 지상 디펜스 프리팹

| 프리팹 | 구성 컴포넌트 | 역할 |
| --- | --- | --- |
| `PF_GameSystems` | CurrencyWallet, DefenseUpgradeModel, DefenseDirector | 지속 전선 숫자 시뮬레이션 |
| `PF_DefenseHud` | DefenseHud | UI |
| `PF_GroundDefenseLane` | GroundDefenseLanePresenter + scene-authored anchors/markers | Phase C 지상 전선 시각 브리지 |
| `PF_GroundDefenseCombatFeedback` | GroundDefenseCombatPresenter + GroundDefenseBattlefieldView | E0-A runtime bridge: formation mapping, event-driven melee/projectiles, casualty/reinforcement, and wall-bound feedback; legacy pulse/flash references are disabled |
| `PF_GroundDefenseEnemy_Grunt` | GroundDefenseEnemyView + reusable pooled shell; archetype data supplies role art/stats | Phase E 풀링 지상 적 프리팹 |
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
| GroundDefenseEnemyArchetype | 지상 적 프리팹, 역할 텍스처/UV/크기, 체력, 압박 비용, 이동, 피격, 처치, 벽 접촉 피드백 값을 재사용 가능한 데이터로 보관 |
| GroundDefenseEnemyPool | 아키타입 프리팹을 사전 생성하고 비활성 인스턴스를 재사용 |
| GroundDefenseBillboardUtility | 투명 시트의 UV 영역을 런타임 sprite로 만들고 방어 패널 카메라를 향하게 함 |
| GroundDefenseEnemyView | 풀링 적의 역할 실루엣, 체력바, 피격 tint, 처치 축소, 벽 접촉 크기 피드백을 표현 |
| GroundDefenseBattlefieldView | `WallAnchor`/`AttackOrigin` 기준으로 성벽, 전선 수비병, 석궁탑 실루엣을 생성 |
| GroundDefenseCombatPresenter | 현재 구현에서는 pooled actors, wall flash, pulse-derived bolts, diagnostics를 갱신한다. 이 presentation 책임은 E0-A에서 실제 squad attack/projectile/wall damage 컴포넌트로 교체하고, 필요한 runtime telemetry만 유지한다. |
| GroundDefenseActorRuntime | 연속 전선 압박/방어/벽 피해율을 아키타입 기반 개별 압박 적 상태의 체력, 이동, 피격, 처치, 벽 접촉 이벤트로 변환 |
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

| InventoryOverlayPresenter | Player-facing inventory overlay content, item selection, equip selected, salvage selected, material preview, and close-overlay handoff |
| RewardOverlayPresenter | Player-facing reward overlay content, pending/claimed reward state, loot source, reward item details, claim/open-inventory/equip/salvage controls, and close-overlay handoff |
| CraftingOverlayPresenter | Player-facing crafting overlay content, item selection, salvage selected, Rare affix reroll material sink, current-affix preview, and close-overlay handoff |

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

### 2026-06-08 Dungeon Depth Scaling Setup

- No new scene object or prefab component is required. Keep `PF_DungeonEnemy_Melee` wired with `CharacterStats`, `Health`, `EnemyAIController`, `CombatDriver`, `CharacterActor`, an enabled `NavMeshAgent`, and an enabled collider.
- `EnemySpawner` reads the active depth from its linked `CombatRoom`, evaluates `DungeonDepthBalanceModel`, and applies runtime max-health and attack-damage multipliers to each instantiated enemy.
- Do not author separate enemy prefabs or room lists for each depth. Base prefab stats remain the depth-1 baseline; formulas own long-tail scaling.
- Accepted Play Mode evidence on 2026-06-08: the user completed the Depth 1 versus Depth 2 comparison, including enemy HP/damage and the Depth 2 reward level/power path. Repeat only for regression after a scaling-contract change.

### 2026-06-09 Item Registry Setup

- `Gameplay > GameSystems > SimpleInventory` must reference `Assets/05.ScriptableObjects/Items/ItemDefinitionRegistry.asset` in `Definition Registry`.
- Add every production-authored `ItemDefinition` to that registry before placing it in a reward table. IDs must stay unique and stable.
- When retiring or renaming an id, add an `Id Migrations` entry from the old id to the registered replacement before shipping the content change.
- Keep `Gameplay > DungeonRoot > LootDropper > Create Prototype Reward When Table Empty` disabled. The fallback remains available only for isolated dev/test scenes.
- No visual placement or camera judgment is involved. The local harness checks the registry asset, scene reference, and fallback setting.

### 2026-06-10 Duplicate Conversion Setup

- No new GameObject or visual layout is required.
- `Gameplay > DungeonRoot > LootDropper > Salvage Service` references `Gameplay > GameSystems > ItemSalvageService`.
- Keep `Auto Convert Inferior Duplicates` enabled. The rule only converts the same registered definition when an owned copy has equal-or-higher level and rolled power.
- Keep `Auto Find Salvage Service` enabled as a fallback, but prefer the explicit scene reference above.
- The reward overlay shows `Reward converted` plus the gained materials. The compact dungeon HUD shows `Reward auto-converted` and the expedition result records the conversion.
- Static verification checks the scene reference, enabled policy, comparison guardrails, payout path, and UI event path. No subjective camera or layout review is required.

Unity 세팅 완료 기준:

- `SampleScene`/`Gameplay`에서 지속 전선과 던전 루프가 같은 런타임 안에 있다.
- Frontline Level, Pressure, Wall Health, Gold/Scrap이 표시된다.
- Hold/Push 버튼이 동작한다.
- 강화 버튼이 실제 수치에 영향을 준다.
- 시각 단계에서는 적이 성벽으로 계속 이동한다.
- 던전 버튼이 최소한 결과 로그를 반환하고, 던전 화면을 보는 중에도 지상 전선이 멈추지 않는다.
