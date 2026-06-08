# Base Script Usage Guide

## 2026-06-08 D0-B Depth Balance Usage

- `DungeonDepthBalanceModel.Evaluate(depth)` returns the shared band number and multipliers for enemy health, enemy damage, reward power, and material yield.
- `EnemySpawner` applies the combat multipliers automatically. Do not add depth-specific Inspector values to the enemy prefab.
- `ExpeditionDirector.TryGrantPendingReward()` passes the active depth into `LootDropper`; the resulting item stores `level = depth` and scaled rolled power.
- `ItemSalvageService` evaluates material yield from the saved item level, so a loaded reward retains its depth-origin salvage value.
- Inventory, reward, and crafting overlay salvage previews use the same `ItemInstance` calculation as the service.
- Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Automation\Export-DungeonDepthBalance.ps1` to regenerate `GameDesign/Balance/DungeonDepthBalance.csv`, or add `-CheckOnly` for validation without rewriting the CSV.

작성일: 2026-05-03
목적: 새로 추가한 기본 스크립트가 무엇을 하는지, Unity에서 어떻게 붙여서 확인하는지, 마음에 안 드는 지점을 어떻게 피드백하면 되는지 정리한다.

## 1. 이번에 만든 범위

이번 구현은 `Phase 1. 지속 전선 숫자 프로토타입`을 위한 최소 뼈대다.

2026-05-04 추가 범위: 아이템 드롭률이나 제작 비용을 정하지 않고, 장비 정의 에셋을 영웅 슬롯에 장착하면 `CharacterStats`에 스탯 보정이 반영되는 최소 기반만 추가했다.

2026-05-05 추가 범위: 인벤토리 UI 없이도 장비 정의를 `Scrap/Essence`로 바꾸는 분해 보상 계산과 `ItemSalvageService`를 추가했다. 이 값은 장기 밸런스 확정값이 아니라 중복 장비가 무의미해지는 문제를 막기 위한 프로토타입 경제 규칙이다.

2026-05-30 추가 범위: Rare 장비 분해에서 `AlterStone`을 최소 1개부터 적게 회수하는 규칙과 Rare 옵션 변형 비용 계산을 `ItemEconomyModel`에 둔다. `CraftingOverlayPresenter`가 연결되면 이 비용을 실제로 소비해 선택된 Rare 아이템의 프로토타입 affix 1개를 바꿀 수 있다.

2026-05-31 추가 범위: `PlayableLoopHud`는 방 클리어 후 보상 overlay를 자동으로 열 수 있고, `PlayableScreenLayoutController`는 overlay를 열기 전에 의도한 복귀 gameplay focus를 먼저 적용할 수 있다. 이로써 정상 던전 클리어 경로가 보상 확인, 장착, 분해 선택으로 바로 이어지면서 닫기 동작은 `DefenseFocus`로 고정된다.

2026-05-07 implementation scope: `ItemInstance` and `SimpleInventory` add the first runtime item-storage layer. A scene can now keep individual rolled item instances, assign stable ids, export/import inventory save data, and remove an item instance through `ItemSalvageService` when salvaging from an attached inventory. This does not yet add loot drops, inventory UI, item registry lookup after load, or actual affix mutation.

2026-05-08 implementation scope: `DungeonRunState` and `ExpeditionDirector` add the first dungeon run-state layer. A scene can now start a prototype expedition, complete its current room, fail the run, and save/load the run state through the existing JSON save path. This does not yet add combat room resolution, loot rewards, dungeon HUD buttons, or room prefab setup.

2026-05-09 implementation scope: `CombatRoom` adds the first one-room result layer. A scene can now start a room from a running `ExpeditionDirector`, wait through a short countdown, resolve by connected `Health` components or by prototype health/DPS simulation, and push the result back into `CompleteRoom()` or `FailExpedition()`. This does not yet add loot rewards, dungeon HUD buttons, enemy AI controllers, or finished room prefab setup.

2026-05-10 implementation scope: `LootDropper` connects the first dungeon clear reward to `SimpleInventory`. `ExpeditionDirector` now tries to grant a reward after the final room clears; if no authored `ItemDefinition` assets are assigned yet, `LootDropper` creates a clearly prototype-only runtime item using Normal/Magic/Rare odds so the loop can be tested before real drop tables exist. `SampleScene` now has `SimpleInventory`, `ItemSalvageService`, and `LootDropper` attached for a Play Mode smoke test. `ItemSalvageService` can also salvage loaded item instances from their saved slot/rarity/level snapshot when the original definition asset is not connected yet. This does not yet add inventory HUD, item-definition lookup after load, authored drop tables, affix mutation, or player-facing equip/salvage controls.

2026-05-12 implementation scope: `SimpleInventory` can equip an `ItemInstance` into `EquipmentSlots`, replace the previous item in the same slot, save equipped item ids through `HeroSaveData`, reconnect saved definition ids through known `ItemDefinition` assets, and restore equipped items after load. `LootDropper` registers authored reward definitions with the inventory. At this point runtime prototype-only items kept their equipped flag for save continuity but still needed the 2026-05-14 snapshot-power bridge below before they could restore a stat effect without a live definition.

2026-05-14 implementation scope: equipped `ItemInstance` objects now apply live definition modifiers, saved affix-roll modifiers, and a prototype rolled-power modifier by slot. This means a runtime prototype reward can still give a small Weapon/Armor/Ring stat effect after save/load even if the live `ItemDefinition` was not resolved. Authored item assets and a real item-definition registry are still required before production balance.

2026-05-14 Phase B scope: `PlayableLoopHud` adds the first Canvas/TMP/Button bridge for the full loop. It shows frontline status, resources, dungeon state, latest item, hero stats, a message line, and an optional action-hint line, then exposes player-facing button methods for ground defense, dungeon, item, save, and load actions. This is the first step away from OnGUI debug panels, but it still needs Unity Canvas wiring and layout review.

2026-05-15 Phase B scope update: `PlayableLoopHud` now includes ground-defense buttons for start, repair, Hold/Push toggle, and Wall/Tower/Defender upgrades. The single normal HUD can now cover the first Phase B decision set: improve defense, run/retry dungeon, then equip or salvage the reward.

2026-05-15 save/load clarification: manual `Load` now restores the saved snapshot exactly. Startup auto-load still applies offline progress after loading. The prototype still uses one shared save file, so the 15-second auto-save can overwrite a prior manual snapshot before a later manual load.

2026-05-17 Phase C bridge: `EnemyAIController` now gives the scene enemy a real chase/attack loop, and `CombatRoom` can auto-discover the current player plus `CharacterTeam.Enemy` actors before falling back to hidden prototype simulation. The first pass still did not read like a real room because the enemy was globally active and attacks had almost no feedback; the follow-up fix now gates tracked enemies to the room lifecycle and exposes current HP plus explicit clear/fail messaging in `PlayableLoopHud`. Prefab/spawner wiring has since been added; Play Mode combat-feel tuning remains future work.

2026-05-20 Phase C authored-reward bridge: `LootDropper` now supports a weighted `Reward Table` before the old uniform `Reward Definitions` list. `Gameplay` uses six tier-1 `ItemDefinition` assets under `Assets/05.ScriptableObjects/Items` at 78% Normal, 20% Magic, and 2% Rare per clear, with prototype runtime rewards kept only as an empty-table fallback. These are prototype pacing weights for a guaranteed room-clear reward, not final long-term drop targets.

2026-05-30 Phase C Rare validation pacing: `LootDropper` now has first-Rare/pity fields for authored weighted rewards. In `Gameplay`, `Guarantee Rare When Inventory Has No Rare` is enabled and `Max Weighted Non Rare Rewards Before Rare` is `6`, so the first crafting/reroll Play Mode check no longer waits on a raw 2% Rare roll. This is an early-slice accessibility rule; final long-term drop pacing still belongs to the later drop-balance export/import pass.

2026-05-21 Phase C fallback-guard scope: `EnemySpawner` now reports missing prefab or missing spawned `Health` setup into `CombatRoom`, and `CombatRoom` blocks prototype simulation while that setup blocker is active. `LootDropper` records `LastRewardSource`, and `PlayableLoopHud` shows combat path plus loot source so the current Play Mode check can confirm spawned enemies and authored reward-table drops are actually being used.

2026-06-06 P0-D spawn-contract scope: `EnemySpawner` now validates the complete melee combat contract before spawning and resolves all intended positions onto nearby NavMesh before instantiation. Missing Enemy team/AI/agent/click collider or an off-NavMesh spawn point becomes a visible setup blocker instead of an inert tracked enemy. `Gameplay` enables this with a `2` unit NavMesh sample radius.

2026-06-06 P0-D acceptance: the user confirmed the spawned prefab combat/reward/retry path works. Normal `Gameplay` now disables `CombatRoom > Simulate When No Enemies`; prototype calculation combat remains code-level dev/test support only.

2026-06-07 Phase D depth progression scope: `ExpeditionDirector` now separates active `Depth`, `SelectedDepth`, and `HighestUnlockedDepth`. Clearing the current highest unlocks exactly one next depth, failure does not advance, and the selected depth is used by the next `StartExpedition()`. `GameSaveData`/`DefenseSaveManager` use schema v2 with v1 migration defaults, while `PlayableLoopHud` and `Gameplay` expose `Depth -` / `Depth +` controls plus active/selected/unlocked status. Keep depth changes disabled while a run is active.

2026-05-23 Phase C visible-lane scope: `GroundDefenseLanePresenter` now auto-resolves renderers from assigned pressure/progress marker transforms and can drive optional scene-authored `Enemy Flow Markers` from `EnemySpawnAnchor` to `WallAnchor`. This makes the ground lane read more like continuous enemy pressure while still leaving lane length, marker art/count, camera framing, and composition to manual Unity authoring.

2026-05-24 Phase C ground-combat feedback scope: `GroundDefenseCombatPresenter` adds the next visual bridge for ground defense. It reads `DefenseDirector.Runtime` and drives scene-authored pressure actors, a wall-contact flash, and tower/defender attack pulses. `PlayableLoopHud` auto-finds it and shows its `LastCombatMessage` in the frontline summary when present. This is still a feedback bridge, not final enemy AI, final art, or long-term combat balance.

2026-05-25 Phase C runtime-combat telemetry scope: `DefenseRuntimeState` now records incoming pressure, pressure cleared by defense, wall damage, and push progress as last-tick per-second feedback values. `GroundDefenseCombatPresenter` uses those values to color pressure actors, scale attack pulse intensity, and show `pressure +/-/s` plus `wall /s` in `LastCombatMessage`, so Play Mode validation can tell whether the visible fight follows the real simulation.

2026-05-26 Phase C playable-screen focus scope: `PlayableScreenFocus` and `PlayableScreenLayoutController` add the first reusable code bridge for the MVP screen states from `11_PlayableScreenPresentationSpec.md`. The controller changes authored RectTransform anchors for DefenseFocus/DungeonFocus, toggles inventory/crafting/reward overlay GameObjects, and exposes button-safe methods for opening/closing overlays. `PlayableLoopHud` can auto-find it and request DungeonFocus on dungeon start, then DefenseFocus when the room resolves. This does not author final layout, camera crop, overlay content, or art style.

2026-05-27 Phase C overlay-control scope: `PlayableScreenLayoutController` now blocks overlay focus changes when the matching overlay GameObject is not wired, reports overlay availability, and keeps the previous gameplay focus intact. `PlayableLoopHud` now exposes optional inventory/crafting/reward/close overlay button slots, shows current screen focus in the summary line, and refreshes when screen focus changes. The current `Gameplay` scene has the overlay GameObjects wired; the remaining work is content/button authoring and Play Mode validation.

2026-05-28 Phase C inventory-overlay content scope: `InventoryOverlayPresenter` adds the first player-facing inventory overlay content bridge. It can fill TMP labels with item rows, selected-item details, wallet/materials, salvage preview, Rare reroll-cost preview when available, and action messages, then run Previous/Next/Latest/Equip/Salvage/Close from normal UI buttons. This does not author panel layout, item icons, scroll behavior, crafting behavior, reward reveal visuals, or final ornate UI treatment.

2026-05-29 Phase C reward-overlay content scope: `RewardOverlayPresenter` adds the first player-facing reward reveal bridge for `Panel_RewardOverlay`. It can show pending/claimed dungeon reward state, loot source, latest reward item details, wallet/material preview, claim pending reward, open inventory, equip reward, salvage reward, and close the overlay back to the previous gameplay focus. The current `Gameplay` scene also has a first deterministic reward-overlay RectTransform pass using the same centered overlay frame as inventory. This does not author reward reveal animation, item icons, rare-item treatment, or final ornate UI treatment.

2026-05-30 Phase C crafting-overlay content scope: `CraftingOverlayPresenter` adds the first player-facing crafting bridge for `Panel_CraftingOverlay`. It can list item instances, show selected item details, preview salvage and Rare reroll material costs, salvage selected items, spend `Gold + Essence + AlterStone` to reroll one prototype affix on a selected Rare item, and close back to the previous gameplay focus. The current `Gameplay` scene has the first deterministic panel layout and button wiring. This does not author final affix pools, affix locking, item-level upgrades, item icons, scroll polish, or final ornate UI treatment.

2026-05-31 Phase C reward-flow scope: `PlayableLoopHud` now opens `RewardOverlay` after a room clear when the overlay is wired and `openRewardOverlayOnDungeonClear` is enabled. `PlayableScreenLayoutController.TryOpenOverlayAfterGameplayFocus(...)` first applies `DefenseFocus`, then opens the overlay, so closing the reward panel returns to the intended post-run focus instead of a partial transition state. Manual reward claims through the HUD use the same overlay handoff when possible.

2026-05-31 Phase C crafting selection scope: `CraftingOverlayPresenter` now prefers the newest rerollable Rare item when opened, shows a reroll-ready count in the header, and writes explicit reroll status/material guidance into the overlay. If `AlterStone` is missing, the first-pass guidance points the player toward salvaging a spare Rare before rerolling the next Rare.

2026-06-01 Phase C crafting validation scope: `CraftingOverlayPresenter` now records the last successful Rare affix reroll for the selected item in the Result panel. The line includes spent materials plus the previous affix state and the new affix so Play Mode validation can confirm the spend/change without relying on memory.

2026-06-02 Phase C crafting validation scope: `ItemInstance.TryApplyPrototypeAffixReroll(...)` now avoids repeating the selected item's saved prototype affix when another candidate exists for the slot. This keeps the current paid reroll check visibly changing the affix line without adding final affix pools, locking, or long-term weighting.

2026-06-03 Phase C defense-alert scope: `PlayableLoopHud` now derives a `Defense alert` from breach, low wall health, wall damage per second, high pressure, or damaged-wall state. It can show that alert in the summary and prioritize it in the action hint during `DungeonFocus` or active dungeon runs, using first-pass thresholds of `35%` wall health and `75%` pressure capacity.

2026-06-04 Phase C overlay-event scope: `RewardOverlayPresenter`, `InventoryOverlayPresenter`, and `CraftingOverlayPresenter` now resynchronize the exact references they subscribe to whenever refresh auto-finds a missing or changed dependency. Reward grants, inventory changes, wallet material changes, and equipped-stat refreshes should therefore update the visible overlay text even if a reference is discovered after the overlay first enables.

2026-06-04 Phase C camera-panel input scope: `PanelCameraRenderTarget` can bind a gameplay camera into a UI `RawImage`, and `DungeonViewportInputRouter` can convert clicks inside that rendered image into rays from the same camera. `PlayerController` now exposes `HandlePrimaryClickRay(...)`, ignores duplicate UI-covered world clicks, and skips self/friendly actors as movement surfaces. This prepares P0-B for RenderTexture-style dungeon panels without changing final camera framing, culling, or panel art.

2026-06-04 Phase C camera-panel validation fix: `PlayableLoopHud` now reacts to `ExpeditionDirector.Changed` by syncing screen focus from the loaded or changed dungeon state, so a saved `Running` dungeon should reopen `DungeonFocus` instead of spawning enemies behind the defense screen. `PlayerController` also keeps a Shift-click stationary target command active while waiting for range, so a target click no longer clears after one out-of-range in-place swing.

2026-06-05 Phase C dungeon viewport QA scope: `PlayableLoopHud` now exposes `Viewport: render ... / input ...` diagnostics in the Dungeon line when the dungeon panel is relevant. `DungeonViewportInputRouter` can inherit its camera from a same-object `PanelCameraRenderTarget`, and the automation harness statically checks the `Gameplay` dungeon RawImage/camera/render-target/input-router bridge. P0-B is accepted; keep the static checks for regression coverage, but treat the HUD diagnostic as temporary QA copy tracked by TD-06 rather than final player-facing text.

2026-06-05 Phase C ground actor scope: `GroundDefenseActorRuntime` now owns a small reusable set of individual pressure actors with health, travel, defense hits, defeats, and wall-contact events. It consumes `DefenseRuntimeState` telemetry and does not replace the authoritative continuous frontline, rewards, breach, or save data. The user accepted this behavior for P0-C. The fixed three-slot scene presentation is frozen as a replacement target; do not add more placeholder tuning. Future ground production should preserve useful runtime events while moving to pooled prefabs, archetype data, real targeting/death, and reusable feedback.

2026-06-08 automation-plan freshness scope: `Invoke-IncrementalDiabloChecks.ps1` requires the canonical plan to name `Phase D - Long-Horizon Systems Foundation` and its current production routing. After D0-B acceptance, the token contract covers completed D0-A/D0-B plus next-task D0-C so the harness catches stale automation routing instead of preserving it. A future phase or task promotion must update this contract in the same run.

2026-06-07 D0-A verification scope: the harness requires the depth-selection buttons and their `PlayableLoopHud` references in `Gameplay`, plus source contracts for selected/highest depth, clear-based unlock, schema v2 migration, save diagnostics, and button-safe HUD actions. The user confirmed the one-step unlock, non-advancing failure, and save/load behavior in Play Mode; this path is now regression-only.

2026-05-26 automation verification scope: `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` is the safe daily verification harness while Unity batchmode remains avoided. It checks the solution build, `git diff --check`, required `Gameplay.unity` scene-contract tokens, missing script references, optional overlay wiring, automation-plan freshness, and local automation TOML health before the Korean handoff report.

목표는 다음 한 문장이 Unity Play 모드에서 돌아가는 것이다.

```text
전선 전투가 계속 진행되고, Gold/Scrap이 시간 단위로 쌓이며, Push를 켜면 Frontline Level이 오른다.
```

아직 의도적으로 넣지 않은 것:

- 최종 `DefenseEnemy`/`TowerBattery`/`DefenseWall` 스탯 전투와 완성형 지상 전투 아트
- 실제 적 오브젝트가 있는 던전 방/보스/아이템 드랍
- 실제 드랍 테이블과 장비 에셋
- 완성형 인벤토리 UI와 장비 드래그 장착
- 복잡한 제작, 옵션, 장비 장착 UI

삭제한 것:

- `WaveDefinition`
- `GroundDefense/Data` 폴더
- 수동으로 웨이브를 계속 작성해야 하는 구조

## 2. 스크립트별 설명

| 스크립트 | 위치 | 역할 | Unity에서 쓰는 법 | 피드백할 때 보면 좋은 부분 |
| --- | --- | --- | --- | --- |
| `ResourceId` | `Assets/02.Scripts/Shared/ResourceId.cs` | Gold, Scrap, Essence, AlterStone 같은 재화 종류 목록 | 직접 붙이지 않는다. 다른 스크립트가 사용한다. | 재화 이름이 너무 많거나 적은지 |
| `ResourceAmount` | `Assets/02.Scripts/Shared/ResourceAmount.cs` | 특정 재화와 수량을 한 묶음으로 표현 | 보상/비용 배열에서 보인다. | 보상/비용 표기가 이해되는지 |
| `CurrencyWallet` | `Assets/02.Scripts/Shared/CurrencyWallet.cs` | 플레이어가 가진 재화를 저장하고 더하거나 소비한다 | `GameSystems` 같은 빈 오브젝트에 붙인다. `Starting Amounts`로 초기 Gold/Scrap을 넣을 수 있다. | 시작 재화가 너무 짜거나 넉넉한지 |
| `FrontlineMode` | `Assets/02.Scripts/GroundDefense/Runtime/FrontlineMode.cs` | `Hold`와 `Push` 모드를 구분한다. | 직접 붙이지 않는다. `DefenseDirector`가 사용한다. | Hold/Push 이름이 이해되는지 |
| `DefenseState` | `Assets/02.Scripts/GroundDefense/Runtime/DefenseState.cs` | Idle, Holding, Pushing, Breached 같은 상태 목록 | 직접 붙이지 않는다. `DefenseDirector`가 사용한다. | 상태명이 직관적인지 |
| `DefenseRuntimeState` | `Assets/02.Scripts/GroundDefense/Runtime/DefenseRuntimeState.cs` | Frontline Level, 성벽 체력, 적 압박, 단계 진행도, 최근 압박/방어/벽 피해 피드백 값을 저장 | 직접 붙이지 않는다. `DefenseDirector` Inspector 안에서 보인다. | Pressure/Progress/WallHealth 숫자와 `Last...PerSecond` 전투 피드백이 화면 연출과 맞는지 |
| `DefenseUpgradeModel` | `Assets/02.Scripts/GroundDefense/Runtime/DefenseUpgradeModel.cs` | 성벽/포탑/병력 레벨, 성벽 체력, 방어 DPS, 강화 비용을 계산 | `CurrencyWallet`와 같은 오브젝트에 붙인다. 수치 밸런스는 Inspector에서 조정한다. | 강화 비용 증가가 너무 빠른지, 강화 체감이 약한지 |
| `DefenseDirector` | `Assets/02.Scripts/GroundDefense/Runtime/DefenseDirector.cs` | 지속 압박 생성, 보상 지급, 단계 상승, 돌파 판정을 관리 | `GameSystems` 오브젝트에 붙이고 `Wallet`, `Upgrades`를 연결한다. 비워도 같은 오브젝트에서 자동 탐색한다. | Hold/Push 위험도, 보상 속도, 단계 상승 속도가 맞는지 |
| `DungeonRunState` | `Assets/02.Scripts/Dungeon/DungeonRunState.cs` | Ready, Running, Cleared, Failed 던전 런 상태를 정의한다. | 직접 붙이지 않는다. `ExpeditionDirector`와 `DungeonSaveData`가 사용한다. | 상태명이 던전 HUD에 보여도 이해되는지 |
| `ExpeditionDirector` | `Assets/02.Scripts/Dungeon/ExpeditionDirector.cs` | 프로토타입 던전 런을 시작/완료/실패시키고 저장 데이터를 만든다. | `GameSystems`나 `DungeonRoot`에 붙인다. 임시 테스트는 Inspector/디버그 버튼에서 `StartExpedition()`, `CompleteRoom()`, `FailExpedition()`을 호출한다. | 시작, 클리어, 실패 흐름이 플레이어가 기대하는 던전 흐름과 맞는지 |
| `CombatRoom` | `Assets/02.Scripts/Dungeon/CombatRoom.cs` | 던전 방 시작 카운트다운, 적/영웅 생존 판정, 추적 전투원 자동 탐색, 전투원 활성 수명주기, 프로토타입 전투 계산, 클리어/실패 결과 전달을 맡는다. | `ExpeditionDirector`와 같은 오브젝트나 `DungeonRoot` 자식에 붙인다. `Expedition`을 비워도 자동 탐색한다. 첫 패스는 `Auto Find Tracked Combatants`와 `Manage Tracked Enemy Activity`를 켜서 Player/Enemy를 찾고 적을 방 시작/해소에 묶는다. 적 프리팹이 없으면 `Simulate When No Enemies`를 켜서 계산형 방 결과를 테스트한다. 단, `EnemySpawner`가 setup blocker를 보고하면 기본값에서는 prototype simulation을 멈춘다. | 클리어/실패 시간이 너무 빠르거나 느린지, 실패가 납득 가능한지. HUD `Path`가 tracked enemies인지 setup blocked인지 확인 |
| `EnemyAIController` | `Assets/02.Scripts/Character/Controllers/EnemyAIController.cs` | 근접 적이 플레이어를 추적하고 사거리 안에서 공격한다. | `CharacterActor`가 붙은 적 오브젝트에 붙인다. 첫 패스는 `Auto Find Player`를 켜 둔다. | 적이 너무 수동적이거나, 추적/공격이 클릭 조작을 읽기 어렵게 만드는지 |
| `PlayerController` | `Assets/02.Scripts/Character/Controllers/PlayerController.cs` | 플레이어 클릭 이동, 클릭 공격, Shift 정지 공격, UI 위 중복 월드 클릭 차단, 외부 카메라 Ray 입력 처리를 맡는다. | 영웅 오브젝트에 붙인다. 월드 직접 클릭은 `Input Camera`를 쓰고, UI `RawImage` 던전 패널은 `DungeonViewportInputRouter`가 `HandlePrimaryClickRay(...)`로 전달한다. `Ignore Clicks Over UI`는 켜 둔다. Shift+적 클릭은 제자리에서 적을 바라보고, 사거리 밖이면 명령을 유지하다가 적이 사거리 안에 들어오면 타격한다. | 패널 클릭이 올바른 카메라 기준으로 이동/공격되는지, 자기 자신/아군 클릭이 이상한 이동이나 공격으로 이어지지 않는지, Shift 클릭이 정지 공격으로 읽히고 실제 HP를 깎는지 |
| `EnemySpawner` | `Assets/02.Scripts/Dungeon/EnemySpawner.cs` | 방 시작 전 melee prefab의 전투 계약을 검증하고, 모든 spawn point를 NavMesh에 해석한 뒤 enemy prefab을 생성해 `Health`를 `CombatRoom`에 등록한다. | `DungeonRoot` 또는 room object에 붙이고 `CombatRoom`, `Enemy Prefab`, `Spawn Points`를 연결한다. `Snap Spawn Points To Nav Mesh`를 켜고 시작 반경은 `2`를 사용한다. `Gameplay`는 현재 `PF_DungeonEnemy_Melee`와 spawn point 1개가 연결되어 있다. | `LastSpawnMessage`가 `on NavMesh`를 포함하고 HUD가 `Path tracked enemies`인지 확인한다. prefab 계약 누락이나 NavMesh 배치 실패는 prototype simulation으로 숨지 않고 setup blocked로 남아야 한다. |
| `LootDropper` | `Assets/02.Scripts/Items/LootDropper.cs` | 던전 클리어 보상을 `SimpleInventory`에 넣는다. 정의 에셋이 없으면 프로토타입 런타임 아이템을 만든다. | `DungeonRoot`에 붙이고 `Inventory`에는 `GameSystems`의 `SimpleInventory`를 연결한다. 정상 보상은 `Reward Table`을 우선 사용하고, `LastRewardSource`로 authored weighted table / legacy list / prototype fallback을 구분한다. Current `Gameplay` also enables first-Rare access and a 6 non-Rare pity threshold for the authored table. | Normal/Magic/Rare 지급 속도가 너무 후하거나 짠지, prototype fallback이 실제 밸런스처럼 오해되지 않는지. HUD `Loot`가 authored table인지 확인 |
| `GameSaveData` | `Assets/02.Scripts/Shared/GameSaveData.cs` | 저장 파일의 루트 데이터와 지상 방어, 던전, 영웅, 인벤토리 저장 데이터를 정의한다. | 직접 붙이지 않는다. `DefenseSaveManager`가 JSON으로 읽고 쓴다. | 저장해야 할 값이 빠졌는지 |
| `DefenseSaveManager` | `Assets/02.Scripts/GroundDefense/Runtime/DefenseSaveManager.cs` | Gold/Scrap/Frontline Level/강화/성벽/던전 런/인벤토리 상태를 로컬 JSON으로 저장한다. 수동 `Load`는 정확한 스냅샷 복원이고, 시작 시 자동 로드만 최대 8시간 오프라인 진행을 덧붙인다. | `GameSystems` 오브젝트에 붙인다. `DefenseDirector`, `ExpeditionDirector`, `SimpleInventory`는 비워도 자동 탐색한다. | 오프라인 보상이 너무 후하거나, 자동 저장이 수동 테스트 스냅샷을 덮어쓰는 타이밍이 혼동을 주는지 |
| `ItemSlot` | `Assets/02.Scripts/Items/ItemSlot.cs` | Weapon, Armor, Ring 같은 MVP 장비 부위를 정의한다. | 직접 붙이지 않는다. `ItemDefinition`과 `EquipmentSlots`가 사용한다. | MVP 부위가 너무 많거나 적은지 |
| `ItemRarity` | `Assets/02.Scripts/Items/ItemRarity.cs` | Normal, Magic, Rare 등급만 우선 정의한다. | 직접 붙이지 않는다. `ItemDefinition`이 사용한다. | 초반 등급 구분이 충분한지 |
| `ItemDefinition` | `Assets/02.Scripts/Items/ItemDefinition.cs` | 장비 에셋의 ID, 이름, 슬롯, 등급, 요구 레벨, 파워 범위, 스탯 보정을 정의한다. | Project 창에서 `Create > Incremental Diablo > Items > Item Definition`으로 만든 뒤 스탯 보정을 입력한다. | 장비 한 개가 주는 스탯 체감이 과하거나 약한지 |
| `ItemEconomyModel` | `Assets/02.Scripts/Items/ItemEconomyModel.cs` | 장비 부위/등급/티어에 따라 분해 보상을 계산한다. | 직접 붙이지 않는다. `ItemDefinition.SalvageRewards`와 `ItemSalvageService`가 사용한다. | Scrap/Essence 회수량이 너무 후하거나 짠지 |
| `ItemSalvageService` | `Assets/02.Scripts/Items/ItemSalvageService.cs` | 선택한 장비 정의를 분해해 `CurrencyWallet`에 보상을 더한다. | `GameSystems` 같은 오브젝트에 붙이고 `CurrencyWallet`을 연결한다. 인벤토리 구현 전에는 테스트 버튼/임시 호출에서 사용한다. | 중복 장비가 재료 순환으로 충분히 의미가 생기는지 |
| `ItemInstance` | `Assets/02.Scripts/Items/ItemInstance.cs` | Holds one rolled runtime item with instance id, definition id, rarity, level, power, durability, and affix placeholders. Prototype Rare rerolls avoid repeating the current affix when another slot-valid candidate exists. | Created by `SimpleInventory.TryAdd(ItemDefinition, out ItemInstance)` or loaded from `InventorySaveData`. | Whether saved item ids and rolled power remain stable after save/load, and whether a paid reroll visibly changes the affix line. |
| `SimpleInventory` | `Assets/02.Scripts/Items/SimpleInventory.cs` | Stores item instances, assigns stable ids, resolves known item definitions, equips instances, and exports/imports the inventory save slice. | Add it to `GameSystems` beside `CurrencyWallet`, `DefenseSaveManager`, and `ItemSalvageService` for prototype testing. Add authored item assets to known definitions or let `LootDropper` register its reward definitions. | Capacity, duplicate-id handling, save/load definition reconnects, and whether salvage removes the item before paying materials. |
| `StatMod` | `Assets/02.Scripts/Character/Stats/StatMod.cs` | 특정 스탯에 Flat, PercentAdd, PercentMult 보정을 준다. Percent 값은 10 = 10%로 입력한다. | `ItemDefinition`의 Modifiers 배열에서 사용한다. | 퍼센트 입력 방식이 이해되는지 |
| `EquipmentSlots` | `Assets/02.Scripts/Character/Core/EquipmentSlots.cs` | Weapon/Armor/Ring에 장비 정의 또는 live `ItemInstance`를 장착하고 `CharacterStats`로 보정을 전달한다. | 영웅 오브젝트의 `CharacterActor`와 함께 붙어 있다. 슬롯에 `ItemDefinition` 에셋을 직접 넣거나 `SimpleInventory.TryEquip(...)`으로 인스턴스를 장착하면 스탯이 바뀐다. | 장비 장착 후 공격력/체력/이동 속도 체감이 맞는지, 저장/로드 후 장착이 복원되는지 |
| `DefenseHud` | `Assets/02.Scripts/GroundDefense/UI/DefenseHud.cs` | TMP 텍스트와 버튼을 연결해서 현재 상태와 강화 버튼을 보여준다 | Canvas 안의 HUD 오브젝트에 붙이고 Text/Button 슬롯을 연결한다. | 화면에 보이는 문구가 충분히 직관적인지 |
| `GroundDefenseCombatPresenter` | `Assets/02.Scripts/GroundDefense/UI/GroundDefenseCombatPresenter.cs` | `DefenseDirector.Runtime`을 읽어 압박 적, 벽 피격 flash, 타워/수비대 공격 pulse를 scene-authored 오브젝트로 보여주고, 최근 압박/방어/벽 피해율을 `LastCombatMessage`로 노출한다. | `Gameplay > DefenseRoot` 또는 `GroundDefenseLane` 오브젝트에 붙인다. `EnemySpawnAnchor`, `WallAnchor`, `AttackOrigin`, `Pressure Actors`, `Wall Contact Object`, `Attack Pulses`를 연결한다. 첫 패스는 `Auto Find Defense`를 켜 둔다. `Gameplay > DefenseRoot`에는 현재 이 경로가 이미 연결되어 있다. | 적 압박이 정말 성벽으로 몰리는지, 벽 피해가 보이는지, 공격 pulse가 타워/수비대 대응처럼 읽히는지, HUD의 `pressure +/-/s`와 `wall /s`가 전투 감각과 맞는지 |
| `GroundDefenseActorRuntime` | `Assets/02.Scripts/GroundDefense/Runtime/GroundDefenseActorRuntime.cs` | 연속 전선 수치를 개별 압박 적 슬롯의 체력, 이동, 피격, 처치, 벽 접촉 이벤트로 변환하는 P0-C 행동 검증 브리지다. | Current `Gameplay > DefenseRoot`에 이미 붙어 있다. `Defense`를 `DefenseDirector`에 연결하고, 첫 패스 값은 3 slots / 12 HP / 8 pressure per spawn / 3 damage per hit를 사용한다. | P0-C 수용 완료. 회귀 시에만 hits/defeats/contacts와 actor/flash 동기화를 확인한다. 추가 placeholder 폴리시는 하지 않는다. |
| `PlayableScreenFocus` | `Assets/02.Scripts/UI/PlayableScreenFocus.cs` | DefenseFocus, DungeonFocus, InventoryOverlay, CraftingOverlay, RewardOverlay screen states. | Not attached directly. `PlayableScreenLayoutController` uses it. | Whether the state names match the visible screen shape the player expects. |
| `PlayableScreenLayoutController` | `Assets/02.Scripts/UI/PlayableScreenLayoutController.cs` | Switches authored UI RectTransforms between the MVP defense-focused and dungeon-focused layouts, and opens/closes inventory/crafting/reward overlay GameObjects. It refuses missing-overlay focus changes and can apply a chosen gameplay focus before opening an overlay. | Attach to a UI controller object on the main gameplay Canvas. Wire `Panel_DefenseSide`, `Panel_DungeonViewport`, and optional overlay objects. Use `OpenInventoryOverlay`, `OpenCraftingOverlay`, `OpenRewardOverlay`, and `CloseOverlay` from buttons or through `PlayableLoopHud`. Runtime flows can use `TryOpenOverlayAfterGameplayFocus` when an overlay should close back to a specific focus. `PlayableLoopHud` syncs this controller to `DungeonFocus` when a saved or newly changed expedition is `Running`. | Dungeon start or loaded-running restore should make dungeon dominant while defense remains visible; room clear/fail should return to DefenseFocus; auto-opened reward overlays should close back to DefenseFocus; overlays should not open invisibly when unwired. |
| `PanelCameraRenderTarget` | `Assets/02.Scripts/UI/PanelCameraRenderTarget.cs` | Renders a scene camera into a UI `RawImage` through an explicit or runtime-created `RenderTexture`, and exposes bind/readiness state for HUD/QA diagnostics. | Attach to `RawImage_DungeonViewport` or a helper object. Assign `Source Camera` and `Target Image`, keep `Create Runtime Texture` and `Match Image Rect` enabled for the first pass. | The panel should show the camera image at the correct size, the Dungeon HUD line should report a ready/bound render target, and disabling the object should release/restore runtime targets without leaving stale textures. |
| `DungeonViewportInputRouter` | `Assets/02.Scripts/UI/DungeonViewportInputRouter.cs` | Routes left-clicks inside a dungeon `RawImage` to `PlayerController` using the camera that rendered the image. If it sits beside `PanelCameraRenderTarget`, it can auto-resolve the viewport camera from that render target. | Attach to `RawImage_DungeonViewport`. Assign `Viewport Image`, `Viewport Camera`, `Player`, and `Screen Layout`; keep `Require Dungeon Focus` enabled. | Floor clicks should move the hero, enemy clicks should attack, Shift-click should stationary attack, overlay/DefenseFocus clicks should not steal dungeon input, and the Dungeon HUD line should report ready/routed input rather than missing references. |
| `InventoryOverlayPresenter` | `Assets/02.Scripts/UI/InventoryOverlayPresenter.cs` | Fills an authored inventory overlay with item rows, selected-item details, wallet/materials, salvage preview, equip selected, salvage selected, latest selection, close-overlay behavior, and live event refresh after auto-found references change. | Attach to `Panel_InventoryOverlay` or a child controller. Wire TMP labels for header/list/selected item/materials/message and buttons for Previous/Next/Latest/Equip/Salvage/Close. Keep `Auto Find References` on for the first pass or explicitly assign `SimpleInventory`, `ItemSalvageService`, `EquipmentSlots`, `CurrencyWallet`, and `PlayableScreenLayoutController`. | Confirm a dungeon reward appears in the list, selected item details are readable, equip changes hero stats, salvage changes wallet/materials and removes the item, visible text updates immediately, and close returns to the previous focus. |
| `RewardOverlayPresenter` | `Assets/02.Scripts/UI/RewardOverlayPresenter.cs` | Fills an authored reward overlay with pending reward state, loot source, latest reward item details, material preview, claim/open-inventory/equip/salvage/close actions, and live event refresh after auto-found references change. | Attach to `Panel_RewardOverlay` or a child controller. Wire TMP labels for header/reward/item/materials/message and buttons for Claim Reward, Open Inventory, Equip Reward, Salvage Reward, and Close. Keep `Auto Find References` on for the first pass or explicitly assign `ExpeditionDirector`, `LootDropper`, `SimpleInventory`, `ItemSalvageService`, `EquipmentSlots`, `CurrencyWallet`, and `PlayableScreenLayoutController`. | Confirm a clear reward is visible outside the debug HUD, the loot source reads authored table, equip/salvage affects stats/materials, visible text updates immediately, Open Inventory moves to the inventory overlay, and Close returns to the previous focus. |
| `CraftingOverlayPresenter` | `Assets/02.Scripts/UI/CraftingOverlayPresenter.cs` | Fills an authored crafting overlay with item rows, selected item details, current affixes, wallet/materials, salvage preview, Rare reroll cost, reroll selected, salvage selected, reroll-candidate auto-selection, material guidance, last-reroll result feedback, close-overlay behavior, and live event refresh after auto-found references change. | Current `Gameplay` already attaches it to `Panel_CraftingOverlay` and wires first-pass TMP labels/buttons. When rebuilding, attach it to `Panel_CraftingOverlay` or a child controller, wire TMP labels for header/list/selected item/materials/result/message and buttons for Previous/Next/Latest/Reroll Affix/Salvage/Close, keep `Prefer Reroll Candidate On Enable` enabled, and keep `Auto Find References` on for the first pass or explicitly assign `SimpleInventory`, `ItemSalvageService`, `EquipmentSlots`, `CurrencyWallet`, and `PlayableScreenLayoutController`. | Confirm a Rare item is auto-selected when the overlay opens, the reroll status explains missing materials or readiness, reroll spends `Gold + Essence + AlterStone`, the affix list changes, the Result panel records spent cost plus before/after affix summary, equipped hero stats refresh if the item is equipped, visible text updates immediately, salvage still pays materials, and close returns to the previous focus. |
| `PlayableLoopHud` | `Assets/02.Scripts/UI/PlayableLoopHud.cs` | 최소 플레이어 HUD. 지상/던전 깊이/아이템/저장/화면 포커스 상태를 한 패널에서 보여주고 핵심 버튼을 실행한다. 방 클리어 후 보상 overlay를 자동으로 열 수 있고, P0-B 확인 중에는 Dungeon line에 `Viewport: render ... / input ...` 진단을 보여줄 수 있다. | Canvas 안의 오브젝트에 붙이고 TMP 텍스트 6-7개와 기본 Button 14개를 먼저 연결한다. 던전 쪽에는 `Previous Dungeon Depth`, `Next Dungeon Depth`, `Start Dungeon`, `Claim Reward`를 연결한다. Overlay 버튼을 만들 때는 `Open Inventory Overlay`, `Open Crafting Overlay`, `Open Reward Overlay`, `Close Overlay` 슬롯을 추가로 연결한다. 정상 보상 확인 경로에서는 `Open Reward Overlay On Dungeon Clear`를 켜 둔다. 첫 패스는 `Auto Find References`와 `Show Dungeon Viewport Diagnostics`를 켜 둔다. | 선택/해금 깊이가 읽히고 실행 중 깊이 변경이 막히는지, 다음 행동과 전투 결과가 버튼 상태/현재 HP/메시지/화면 포커스로 충분히 드러나는지, 던전 클리어가 수동 디버그 단계 없이 보상 확인으로 이어지는지, 던전 RawImage 패널의 렌더/입력 상태가 P0-B 수동 검증에 충분히 드러나는지 |

아이템 경제 테스트 시 `ItemDefinition.SalvageRewards`는 분해 보상 미리보기이고, `ItemDefinition.AffixRerollCost`는 Rare 장비 옵션 변형 비용이다. `CraftingOverlayPresenter`가 연결된 패널에서는 이 비용을 실제로 소비해 선택된 Rare `ItemInstance`의 프로토타입 affix 1개를 교체한다. 현재 프로토타입 후보 안에서는 가능한 경우 같은 affix 반복을 피하므로, 비용 소비 후 affix 텍스트가 바뀌어야 한다. Normal/Magic은 변형 비용을 반환하지 않는다. Rare는 낮은 `baseTier`에서도 최소 `AlterStone: 1` 분해 보상을 주므로, 초반 장비가 너무 빨리 재굴림 루프로 들어가지 않는지 Play Mode에서 확인해야 한다.

## 3. 가장 빠른 테스트 세팅

1. 씬에 빈 오브젝트를 만들고 이름을 `GameSystems`로 둔다.
2. `GameSystems`에 `CurrencyWallet`, `DefenseUpgradeModel`, `DefenseDirector`, `DefenseSaveManager`, `SimpleInventory`, `ItemSalvageService`를 붙인다. 던전 테스트에서는 `DungeonRoot`에 `ExpeditionDirector`, `CombatRoom`, `LootDropper`를 붙인다.
3. `CurrencyWallet > Starting Amounts`에 다음을 넣는다.

```text
Gold 100
Scrap 25
```

4. `DefenseDirector`에서 `Start On Play`를 켠다.
5. Play를 누르면 Frontline 전투가 계속 진행된다.
6. Gold/Scrap이 시간 단위로 늘어나는지 본다.
7. `Mode`를 Hold와 Push로 바꿔서 압박과 진행도 차이를 본다.
8. `DefenseUpgradeModel`의 Wall/Tower/Defender 레벨과 비용을 바꿔보며 체감이 맞는지 본다.

저장/로드까지 확인하려면:

1. Play 모드에서 Gold/Scrap을 조금 벌거나 강화 버튼을 눌러 상태를 바꾼다.
2. Play 모드를 끄면 `DefenseSaveManager`가 저장한다.
3. 다시 Play를 누른다.
4. Gold/Scrap, Frontline Level, Wall/Tower/Defender Level, 성벽 체력, Hold/Push 모드가 유지되는지 확인한다.
5. `ExpeditionDirector`가 있다면 저장 JSON의 `dungeon.state`, `dungeon.dungeonId`, `dungeon.roomsCompleted`, `dungeon.rewardPending`이 런 호출 결과와 맞는지 확인한다.
6. `LootDropper`와 `SimpleInventory`가 있다면 던전 클리어 후 저장 JSON의 `inventory.itemInstances`가 1개 이상 늘어났는지 확인한다.
7. 저장 후 몇 분 뒤 다시 실행하면 최대 8시간 한도 안에서 오프라인 보상과 손상이 계산된다.

저장 파일은 Unity의 `Application.persistentDataPath` 아래 `incremental_diablo_save.json`으로 만들어진다. 지상전, 던전 런 상태, 인벤토리 인스턴스는 저장 루트에 들어갔다. `CombatRoom`은 런 상태를 클리어/실패로 바꾸고, `ExpeditionDirector`는 클리어 보상을 `LootDropper`를 통해 인벤토리에 넣을 수 있다. `EnemyAIController`가 붙은 적이 있으면 첫 방은 계산형 시뮬레이션 대신 실제 클릭 전투 경로를 탈 수 있다. 전투 결과 세부 로그, 실제 장비 정의 lookup, 완성형 인벤토리 UI는 아직 별도 작업이다.

HUD까지 보고 싶다면:

1. Canvas를 만든다.
2. TextMeshPro Text를 5~7개 만든다.
3. Button을 Start, Repair, Mode Toggle, Wall Upgrade, Tower Upgrade, Defender Upgrade 용도로 만든다.
4. Canvas 안의 빈 오브젝트에 `DefenseHud`를 붙인다.
5. `DefenseHud` 슬롯에 위 Text/Button을 연결한다.

Phase B의 전체 루프 HUD를 보고 싶다면:

1. Canvas 안에 `Panel_PlayableLoopHud` 오브젝트를 만든다.
2. `PlayableLoopHud`를 붙인다.
3. TMP Text 6-7개를 `Summary`, `Resources`, `Dungeon`, `Latest Item`, `Hero Stats`, `Message`, 선택 사항인 `Action Hint` 슬롯에 연결한다.
4. Ground Button 6개를 `Start Defense`, `Repair Wall`, `Toggle Hold/Push`, `Upgrade Wall`, `Upgrade Tower`, `Upgrade Defenders` 슬롯에 연결한다.
5. Dungeon/Item/Save Button 8개를 `Previous Dungeon Depth`, `Next Dungeon Depth`, `Start Dungeon`, `Claim Reward`, `Equip Latest`, `Salvage Latest`, `Save`, `Load` 슬롯에 연결한다.
6. Overlay 버튼을 추가할 때는 `Open Inventory Overlay`, `Open Crafting Overlay`, `Open Reward Overlay`, `Close Overlay` 슬롯에 연결한다. 이 버튼들은 `PlayableScreenLayoutController`의 해당 overlay GameObject가 연결되어야 활성화된다.
7. `Panel_InventoryOverlay`를 만들 때는 `InventoryOverlayPresenter`를 붙이고 TMP labels (`Header`, `Item List`, `Selected Item`, `Materials`, `Message`) plus buttons (`Previous`, `Next`, `Latest`, `Equip`, `Salvage`, `Close`)를 연결한다.
8. `Panel_RewardOverlay`를 만들 때는 `RewardOverlayPresenter`를 붙이고 TMP labels (`Header`, `Reward`, `Item Detail`, `Materials`, `Message`) plus buttons (`Claim Reward`, `Open Inventory`, `Equip Reward`, `Salvage Reward`, `Close`)를 연결한다.
9. `Panel_CraftingOverlay`를 만들 때는 `CraftingOverlayPresenter`를 붙이고 TMP labels (`Header`, `Item List`, `Selected Item`, `Materials`, `Result`, `Message`) plus buttons (`Previous`, `Next`, `Latest`, `Reroll Affix`, `Salvage`, `Close`)를 연결한다.
10. 첫 연결에서는 `Auto Find References`를 켜 둔다. 나중에 여러 영웅/인벤토리가 생기면 참조를 직접 연결한다.

## 4. 현재 규칙의 의미

숫자 프로토타입은 실제 적을 만들지 않고 `Enemy Pressure`라는 숫자를 적 무리가 성벽으로 밀어붙이는 압박으로 쓴다.

```text
Enemy Pressure는 계속 생성된다.
Defense Power가 Enemy Pressure를 깎는다.
Enemy Pressure가 남으면 Wall Health가 깎인다.
Push 상태에서 Enemy Pressure를 0으로 유지하면 Frontline Progress가 오른다.
Frontline Progress가 가득 차면 Frontline Level이 오른다.
```

즉, 지금 단계에서 중요한 질문은 그래픽이 아니라 이것이다.

```text
전투가 끊기지 않고 계속 흐르는 느낌이 드는가?
Hold는 안정 파밍처럼 느껴지는가?
Push는 위험하지만 다음 단계로 미는 선택처럼 느껴지는가?
강화했을 때 압박을 더 잘 버티는 느낌이 드는가?
```

## 5. 피드백하기 쉬운 체크리스트

마음에 안 들면 아래처럼 말해주면 바로 고치기 쉽다.

| 보고 싶은 것 | 피드백 예시 |
| --- | --- |
| 전투 흐름 | "전투가 아직 너무 스테이지처럼 느껴져. Progress보다 압박 숫자가 더 계속 움직였으면 해." |
| Hold/Push | "Hold는 너무 심심해. 낮은 확률로 작은 위기가 있었으면 해." |
| 단계 상승 | "Push를 켜도 레벨업이 너무 느려. 첫 단계는 30초 안에 올라갔으면 해." |
| 실패 방식 | "돌파되면 그냥 멈추기보다 성벽 손상이 먼저 누적됐으면 해." |
| 강화 체감 | "포탑 강화가 너무 약해. 한 번 올렸을 때 Pressure가 확 줄었으면 해." |
| 재화 종류 | "Scrap까지는 좋은데 Essence/AlterStone은 아직 안 보였으면 해." |
| HUD 문구 | "Pressure라는 말이 직관적이지 않아. 적 압박 같은 표현이 나아." |

## 6. 다음 구현 후보

Phase A의 디버그 품질 루프는 확인됐다. 다음 순서는 Phase B를 기준으로 잡는다.

1. `PlayableLoopHud`를 `SampleScene` Canvas에 배치해서 정상 테스트 흐름을 OnGUI 디버그 패널 밖으로 옮긴다.
2. 첫 10-20분 플레이 패스를 잡아 지상 강화, 던전 재도전, 장착/분해가 각각 의미 있는 선택인지 확인한다.
3. 실패/막힘 메시지를 정리해 플레이어가 "왜 실패했는지"와 "무엇을 누르면 되는지"를 바로 알게 한다.

아직 하지 말 것은 그대로 유지한다. 수십 개의 수동 아이템 테이블, 고급 드래그 인벤토리, 복잡한 제작, 실제 장기 밸런스는 Phase B의 최소 흐름이 보인 뒤에 확장한다.
