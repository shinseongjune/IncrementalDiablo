# Ground Defense System Spec

## 2026-06-15 Actual NavMesh Combat Foundation

- `GroundDefenseNavMeshBattlefield` creates a visible ground cube spanning the existing enemy-spawn and wall anchors, builds a runtime `NavMeshSurface`, and spawns two defenders plus three enemies.
- Spawned units are real gameplay actors composed from `CharacterStats`, `Health`, `NavMeshAgent`, `CharacterMotor`, `CombatDriver`, `EquipmentSlots`, `CharacterActor`, collider, and `GroundDefenseNavMeshUnit`.
- Defenders hold a home position, acquire the nearest living enemy inside their leash, move into attack range, attack through `CombatDriver`, and return home when no target is available.
- Enemies acquire living defenders first. When no defender survives, they move to the wall approach point and apply attack damage through `DefenseDirector.ApplyBattlefieldWallDamage`.
- Death disables movement/collision, shows bounded feedback, removes the actor, and respawns the same side after a delay.
- `DefenseRuntimeState` remains authoritative for wall health, breach, rewards, Hold/Push, progression, and save/load. The visible actors do not add manual waves, unit commands, or a second economy.
- The legacy lane presenter, actor projection, enemy pool, static battlefield view, and combat presenter are disabled in `Gameplay`. Their zone/card/billboard presentation is not an acceptance target.

### Accepted foundation and next gate

The user confirmed the generated NavMesh, enemy/defender movement and engagement, death/reinforcement, enemy wall approach/attack, and wall-health loss in Play Mode on 2026-06-15. The next gate is recognizable friendly/enemy models and readable attack ownership in both panel states. Ranged/tower combat and formula-driven density remain subsequent gates.

## 2026-06-15 E0-A1 Sprite Rendering Repair

- The failed generated UV/material role quad path is replaced by runtime `Sprite.Create` cells rendered with `SpriteRenderer`.
- The role sheet remains the source texture, but each enemy/defender/tower/wall cell now uses Unity's sprite alpha path instead of a custom role mesh/material.
- The static defender is flipped to face the approaching enemy. Static zone alpha is reduced and the wall health bar is omitted so bars and colored rectangles cannot dominate the noun proof.
- `StaticGrammar` still shows exactly one enemy, one defender, one tower, and one wall. Runtime attacks, casualties, reinforcements, and pooled density remain hidden.
- E0-A1 is `Needs Unity Play Mode / Sprite rendering repair`. Acceptance requires paused screenshots in both panel states; E0-A2 remains blocked.

## 2026-06-14 E0-A1 Static Grammar Implementation

- `GroundDefenseBattlefieldView` now has an explicit `StaticGrammar` presentation stage. `DefenseRuntimeState` and the pooled actor projection keep running, but the normal panel suppresses runtime enemy motion, attacks, casualties, and reinforcements until E0-A1 is accepted.
- The generated battlefield contains `Zone_EnemyStaging`, `Zone_Approach`, `Line_Contact`, and `Zone_FriendlyDefense`, plus one enemy footprint, one defender footprint, and fixed tower/wall foundations.
- The visible proof contains exactly one enemy, one defender, one tower, and one wall. Enemy/defender art retains opposing facing from the role sheet, structures remain larger and fixed, and the defender is now vertically offset so its feet sit on the ground instead of the billboard center intersecting the ground plane.
- User validation of this initial quad-based path failed: the zone bands and line appeared, but enemy/defender/tower/wall did not read as those concepts. The actual panel showed flat colored regions, capsule-like geometry, bars, and small blocks.
- The 2026-06-15 section above supersedes that rendering path. E0-A2 combat events and E0-A3 pooled density remain hidden until the repaired sprite proof passes.

## 2026-06-13 Play Mode Rejection And RTS Concept Contract

### Observed failure

- Enemies appeared rapidly and mostly moved from the top of the defense panel toward the bottom.
- The player could not tell what kind of units they were, where the actual frontline was, whether they had entered combat, or what action they were performing.
- A projectile-like object was visible near the wall, but the player could not identify the attacker, target, impact, or gameplay meaning.
- Therefore the 2026-06-13 implementation does not satisfy E0-A. Event wiring, pooling, and health changes are not sufficient evidence of RTS readability.

### RTS nouns must be distinct before combat motion

| Concept | Required visual identity | Rejection condition |
| --- | --- | --- |
| Enemy unit | Ground-anchored body, hostile faction treatment, facing toward the defended side, readable weapon/role, stable spawn-side origin | Appears as a floating card, particle, icon, or anonymous object moving vertically |
| Friendly unit | Ground-anchored body, friendly faction treatment, authored defensive line, facing toward enemies, visible melee/ranged role | Indistinguishable from enemies or buildings; attack happens without visible body action |
| Tower | Persistent structure larger than units, fixed foundation, visible weapon/muzzle, clear firing direction | Looks like a unit/card; projectile appears without a visible muzzle |
| Wall/citadel | Largest protected structure, fixed protected-side position, persistent health/damage state | Reads as another sprite/card or receives damage away from the visible structure |
| Projectile | Owned by one visible attacker and one visible target; launch, travel, and impact form one traceable event | Appears near the wall, loops continuously, or has no identifiable source/target |
| Melee attack | Two opposing units meet at contact; attacker stops, winds up, strikes, target reacts, both recover or die | Units pass through each other, keep sliding, or damage occurs with no contact |

### Battlefield zones

The camera must expose four stable authored zones:

```text
[Enemy staging] -> [Approach space] -> [Contact line] -> [Friendly structures / Wall]
```

- Movement may be diagonal or across screen width/depth, but it must read as travel between these zones.
- Screen-vertical movement is allowed only if perspective, ground plane, facing, and destination make the battlefield relationship obvious.
- Fast top-to-bottom spawning that reads like falling objects or a conveyor is prohibited.
- The contact line must remain visible long enough for approach, stop, attack, hit, and death states to be recognized.

### Attack readability contract

- Every attack follows `Unit -> action -> target`.
- Tower/ranged timing: idle/reload -> aim or windup -> muzzle launch -> projectile travel -> target impact -> recovery.
- Melee timing: approach -> stop at range -> windup -> strike -> target reaction -> recovery.
- The projectile, impact, and damage number/health change support the attack; they cannot replace the visible attacker action.
- Use one deterministic exchange at low cadence before enabling formula-driven density. Diagnostic text cannot be used as proof.

### Required implementation order

1. Static silhouette proof with one enemy, one defender, one tower, and one wall.
2. One deterministic melee exchange.
3. One deterministic tower projectile exchange.
4. Enemy death and defender/wall damage.
5. Reinforcement and pooling.
6. Formula-driven density only after steps 1-5 remain readable in both full and compressed defense views.

## 2026-06-13 E0-A Battlefield Implementation

- `GroundDefenseActorRuntime` remains a transient projection of authoritative pressure/clear/wall-damage telemetry and now emits actor spawn, hit, defeat, and wall-contact events for presentation.
- `GroundDefenseBattlefieldView` maps pooled enemies into formation lanes that converge on a fixed contact line before the wall. Three reusable defender visuals hold that line.
- Actual actor-hit events alternate between a visible defender melee lunge and a projectile launched from the crossbow tower toward that actor. These attacks do not create a second damage simulation.
- Enemy defeat still comes from the actor runtime health projection and recycles through the existing pool. When wall damage occurs, a defender can visibly fall and re-enter from the wall side after a bounded reinforcement delay.
- Wall health loss, hit emphasis, and breach color are attached to the visible wall and its health bar. The normal path disables moving pressure markers, legacy repeating attack pulses, and the unattached wall flash.
- This implementation failed its first Unity Play Mode readability check. Preserve useful runtime events and pooling, but do not tune speed/count alone or call this presentation accepted. The next pass must follow the ordered RTS concept contract above.

## 2026-06-12 Approved Production Combat Contract

### Target experience

- Ground defense must look like the defense panel in `GameDesign/References/2026-05-17_FinalGameplayScreenConcept.png`: an isometric dark-fantasy battlefield with a protected citadel edge, fixed defense structures, friendly squads, approaching enemy formations, and a readable contact line.
- `Assets/06.Art/Sprites/GroundDefense/GroundDefense_ReadabilitySheet.png` is the current silhouette reference. Grunt, shield enemy, runner, defender, tower, and wall should appear as battlefield actors/structures, not isolated UI portraits.
- Required visible sequence: enemy formation advances -> defender squad intercepts -> melee/ranged attacks occur -> health and hit reaction change -> units die -> reinforcements enter -> surviving enemies attack the wall.
- Tower and ranged attacks use real projectile presentation tied to a visible attacker and target. Melee attacks require visible contact and attack timing. Wall damage must appear on the wall through damage state, impact, health loss, or breach animation.
- Generic attack pulses, unattached wall flashes, moving pressure markers, and normal-HUD combat diagnostics are not production combat feedback.

### Control boundary

- This is an RTS-readable automatic defense, not a directly controlled RTS.
- Allowed player decisions: Hold/Push, repair, wall/tower/squad/trap upgrades, unlocks, formation or composition policy, and target-priority policy when later supported.
- Excluded player actions: individual unit selection, movement orders, focus-fire clicking, worker/resource control, production queues, and free tower placement.
- Tower and squad positions are authored battlefield roles. Progression changes their count, tier, equipment, appearance, and effectiveness rather than asking the player to solve a placement puzzle.

### Simulation boundary

- `DefenseRuntimeState` remains authoritative for pressure, rewards, progression, breach, save/load, and offline resolution.
- `GroundDefenseBalanceModel` remains the formula source for 900+ hour scaling.
- Visible squads are a pooled projection of the continuous simulation. Density, role mix, reinforcement timing, and visual losses should be formula-driven and reusable rather than stored as hand-authored wave lists.
- Existing archetype, pooling, health, travel, defeat, and wall-contact code can be retained where useful. Billboard-only battlefield props, attack pulse/bolt presentation, and diagnostic player-facing copy are implementation debt to replace.

### First production slice

- One fixed battlefield composition based on the reference image.
- Enemy roles: grunt, shield, runner.
- Friendly roles: melee defender squad plus one ranged/tower source.
- Visible melee contact, one real projectile type, death/recycle, reinforcement entry, and wall attack/damage.
- No manual placement, selection, or wave authoring.

## 2026-06-11 Phase D Ground Progression Profile

- `GroundDefenseBalanceModel` is now the single source of truth for long-horizon ground scaling.
- Ten-level bands scale incoming pressure, defense upgrade output efficiency, pressure capacity, progress requirements, and continuous Gold/Scrap income through bounded formulas.
- Entering Frontline Levels 11, 21, 31, and later band starts grants a formula-driven Gold/Scrap milestone cache. The wallet already persists the granted resources, so no separate claimed-milestone save list is required.
- `DefenseDirector` uses the same profile for live and offline simulation. `DefenseHud` and `PlayableLoopHud` expose the active band, multipliers, next band level, and latest milestone message.
- `Breached` is included in live and offline reward ticking, so the documented 25% recovery income remains available while pressure/progress stay stopped.
- `Tools/Automation/Export-GroundDefenseBalance.ps1` validates monotonic Frontline Levels 1-1000 and exports `GameDesign/Balance/GroundDefenseBalance.csv`.
- This does not add authored wave rows, alter camera/layout composition, or promote the fixed three-slot actor bridge. Phase E E0-A owns the pooled prefab/archetype replacement.

2026-05-23 Phase C visual bridge note:

- `GroundDefenseLanePresenter` now auto-resolves renderers from assigned pressure/progress marker transforms and colors those markers by runtime state.
- Optional scene-authored `Enemy Flow Markers` move from `EnemySpawnAnchor` to `WallAnchor`; active marker count rises with pressure and all assigned markers stay visible on breach.
- This is a visual bridge only. Enemy art, marker count, lane length, camera framing, and final composition remain manual Unity authoring decisions.

2026-05-24 Phase C ground-combat feedback note:

- `GroundDefenseCombatPresenter` is the first code bridge from marker-only lane presentation toward readable ground combat.
- It does not invent waves or hand-authored encounters. It reads the existing continuous `DefenseDirector.Runtime` values and drives scene-authored pressure actors, wall-contact feedback, and tower/defender attack pulses.
- Scene placement, silhouette, marker art, pulse scale, camera framing, and final composition remain manual Unity authoring decisions.

2026-05-25 Phase C runtime-combat telemetry note:

- `DefenseRuntimeState` exposes last-tick incoming pressure, pressure cleared by defense, wall damage, and push progress as per-second feedback values.
- `GroundDefenseCombatPresenter` uses those values to color pressure actors, scale visible attack-pulse count, and report `pressure +/-/s` plus `wall /s` in `LastCombatMessage`.
- This improves Play Mode validation of the existing continuous frontline simulation without adding manual waves, hand-authored enemy lists, or final art assumptions.

2026-06-05 Phase C discrete actor runtime note:

- `GroundDefenseActorRuntime` adds three reusable pressure-actor slots with individual health, travel progress, defense-hit events, defeat events, and wall-contact events.
- It consumes the authoritative `DefenseRuntimeState` pressure/clear/wall-damage rates instead of creating a separate wave list or economy simulation. `GroundDefenseCombatPresenter` maps those runtime slots onto the existing scene-authored pressure actors.
- The actor slots are a rebuildable combat projection and are not saved. Save/load continues to preserve the formula-driven frontline state, then the actor runtime reconstructs visible combat after load.
- The user accepted this behavior bridge for P0-C on 2026-06-05. The current fixed three-slot objects are now frozen: do not spend more production runs tuning their count, color, speed, silhouette, or layout.
- Final enemy archetypes, colliders, animation, pooled prefabs, targeting rules, death handling, and actor-authored stats remain later production replacement work. Preserve the event/telemetry contract where useful; do not promote the blockout composition itself.

작성일: 2026-05-03
문서 목적: 지상 디펜스를 끊임없는 전선 전투와 자동 단계 상승 구조로 정의한다.

## 1. 시스템 목적

지상 디펜스는 플레이어가 접속하지 않아도 계속 싸움이 이어지는 장기 성장 기반이다. 여기서 중요한 감각은 "스테이지를 하나씩 시작하고 끝낸다"가 아니라 "성채 앞 전선에서 적이 계속 밀려오고, 방어선이 버티며 돈과 기본 재료를 번다"이다.

최종 한 줄:

```text
성채 앞 전선에서 끝없이 몰려오는 적 압박을 버티고, 방어선을 강화해서 더 높은 Frontline Level까지 밀어붙인다.
```

## 2. 핵심 방향

이 시스템은 수동 작성 웨이브 목록을 사용하지 않는다.

사용하지 않는 것:

- `WaveDefinition`을 1, 2, 3, 4... 식으로 계속 만드는 방식
- 매 전투가 시작/종료로 끊기는 스테이지 클리어 구조
- 플레이어가 전투 중 병력을 직접 이동시키는 RTS식 조작

사용하는 것:

- 전투는 계속 흐른다.
- 적 압박은 공식으로 계속 생성된다.
- 단계는 `Frontline Level`로 존재한다.
- `Hold`는 현재 단계에서 안정 파밍한다.
- `Push`는 위험을 높이고 다음 단계 진행도를 채운다.

## 3. 화면 구성

MVP 화면은 한 줄 레인이다.

```text
적 스폰 지점                                      성채
    ↓                                             ↓
오른쪽 ------------------------------------------- 왼쪽

[Enemy Spawn] ---> continuous enemies ---> [Defenders / Wall / Towers] | [Citadel]
```

카메라는 고정이다. 플레이어는 전투 중 유닛을 움직이지 않는다. 플레이어가 하는 일은 수리, 강화, Hold/Push 전환, 던전 파밍으로 방어선을 간접 강화하는 것이다.

## 4. 핵심 오브젝트

| 오브젝트 | Unity 이름 예시 | 역할 |
| --- | --- | --- |
| DefenseDirector | `DefenseDirector` | 지속 전선 시뮬레이션, 단계 상승, 보상, 돌파 판정 |
| DefenseRuntimeState | `DefenseRuntimeState` | 현재 전선 상태, 단계, 압박, 진행도, 성벽 체력, 최근 압박/방어/벽 피해 피드백 |
| DefenseUpgradeModel | `DefenseUpgradeModel` | 성벽/포탑/병력 레벨과 방어 수치 계산 |
| CurrencyWallet | `CurrencyWallet` | Gold/Scrap/Essence/AlterStone 보관과 소비 |
| DefenseHud | `DefenseHud` | 단계, 압박, 진행도, 재화, 수리/강화 버튼 표시 |
| DefenseEnemy | `DefenseEnemy` | 시각 프로토타입 단계에서 실제 적 개체 |
| TowerBattery | `TowerBattery` | 시각 프로토타입 단계에서 자동 포탑 공격 |
| DefenseWall | `DefenseWall` | 시각 프로토타입 단계에서 성벽 피격 표현 |
| GroundDefenseActorRuntime | `GroundDefenseActorRuntime` | 아키타입 기반 개별 압박 적 체력, 이동, 피격, 처치, 벽 접촉 상태 |
| GroundDefenseEnemyPool | `GroundDefenseEnemyPool` | 적 프리팹 사전 생성과 재사용 |
| GroundDefenseCombatPresenter | `GroundDefenseCombatPresenter` | `DefenseDirector.Runtime` 기반 풀링 적, 벽 피격, 공격 펄스 피드백 |

## 5. 상태 구조

```text
Idle
→ Holding
→ Pushing

Holding/Pushing
→ Breached
→ WaitingForRepairOrUpgrade
→ Holding 또는 Pushing
```

| 상태 | 설명 |
| --- | --- |
| Idle | 아직 전선 시뮬레이션이 시작되지 않음 |
| Holding | 현재 Frontline Level에서 안정적으로 파밍 |
| Pushing | 압박이 더 강하지만 다음 Frontline Level 진행도가 오름 |
| Breached | 적 압박이 한계에 도달했거나 성벽 체력이 0 |
| WaitingForRepairOrUpgrade | 수리 또는 강화 후 다시 시작해야 하는 상태 |

## 6. 주요 수치

| 수치 | 의미 |
| --- | --- |
| FrontlineLevel | 현재 전선 단계. 손으로 만들지 않고 공식으로 계속 증가한다. |
| EnemyPressure | 현재 방어선에 쌓인 적 압박. 방어력이 부족하면 증가한다. |
| EnemyPressureCapacity | 버틸 수 있는 압박 한계. 이 값에 닿으면 돌파된다. |
| DefensePower | 포탑과 병력의 초당 방어력 합산. |
| WallHealth | 성벽 체력. 압박이 쌓이면 깎인다. |
| FrontlineProgress | Push 중 다음 단계로 넘어가기 위한 진행도. |

## 7. 진행 공식

현재 구현은 기준값에 `GroundDefenseBalanceModel`의 Frontline Level 프로필을 곱한다. 레벨별 수동 행이나 웨이브 목록은 만들지 않는다.

```text
profile = GroundDefenseBalanceModel.Evaluate(frontlineLevel)
incomingPressure = basePressure * profile.incomingPressureMultiplier
if mode == Push:
    incomingPressure *= pushPressureMultiplier

defensePower = rawUpgradeDefensePower * profile.defenseOutputMultiplier
enemyPressure += incomingPressure * deltaTime
enemyPressure -= defensePower * deltaTime
enemyPressure = clamp(enemyPressure, 0, pressureCapacity)
```

압박이 남아 있으면 성벽이 피해를 받는다.

```text
wallDamage = enemyPressure * wallDamagePerPressureSecond * deltaTime
```

Push 상태에서 압박을 0으로 유지할 수 있으면 전선 진행도가 오른다.

```text
surplusDefense = max(0, defensePower - incomingPressure)
progressPerSecond = basePushProgress + surplusDefense * surplusProgressMultiplier
```

진행도가 요구치를 채우면 단계가 오른다.

```text
if frontlineProgress >= progressRequired:
    frontlineLevel += 1
    frontlineProgress = 0
```

## 8. Hold / Push 규칙

| 모드 | 목적 | 위험 | 보상 | 단계 진행 |
| --- | --- | --- | --- | --- |
| Hold | 현재 단계 안정 파밍 | 낮음 | 기본 | 없음 |
| Push | 다음 단계로 밀기 | 높음 | 약간 높음 | 있음 |

Hold는 방치형 안정감을 준다. Push는 "이제 다음 구간을 넘을 수 있나?"라는 목표를 만든다.

## 9. 실패 규칙

실패는 숨겨진 효율 감소가 아니라 화면에서 바로 이해되는 손상으로 처리한다.

조건:

```text
WallHealth <= 0
또는 EnemyPressure >= EnemyPressureCapacity
```

결과:

- 전선 상태가 `Breached`가 된다.
- 지속 보상은 크게 줄지만 0이 되지는 않는다. 수리비가 없는 상태에서도 복구를 기다릴 수 있어야 한다.
- 성벽 수리가 필요하다.
- Frontline Level은 유지한다.
- 플레이어는 수리/강화 후 다시 Hold 또는 Push를 선택한다.

## 10. 보상 규칙

보상은 전투가 계속 흐르는 만큼 시간 단위로 누적된다.

```text
profile = GroundDefenseBalanceModel.Evaluate(frontlineLevel)
goldPerSecond = baseGoldPerMinute / 60 * profile.rewardMultiplier
scrapPerSecond = baseScrapPerMinute / 60 * profile.rewardMultiplier
```

Push 중에는 위험을 감수하므로 보상 배율을 조금 높일 수 있다.

```text
if mode == Push:
    reward *= pushRewardMultiplier
```

돌파 후에는 진행은 멈추지만 최소 복구 수입은 남긴다.

```text
if state == Breached:
    reward *= breachedRewardMultiplier
```

`breachedRewardMultiplier`는 초기 프로토타입에서 25%를 사용한다. 실패를 의미 있게 만들되, Gold를 모두 쓴 플레이어가 수리비를 다시 벌 방법까지 잃어버리는 소프트락은 허용하지 않는다.

새 10레벨 밴드에 진입하면 별도의 수동 보상표 없이 공식 기반 Gold/Scrap milestone cache를 한 번 지급한다. 밴드 진입은 Frontline Level 상승 시점에만 판정되므로 저장을 다시 불러오는 것만으로 보상이 중복 지급되지 않는다.

## 11. 오프라인 진행

오프라인 계산도 같은 공식을 사용한다.

1. 저장된 `lastPlayedAt`과 현재 시간을 비교한다.
2. 오프라인 시간을 적당한 상한으로 자른다.
3. 현재 Frontline Level, Hold/Push 모드, 방어력, 성벽 체력으로 압박과 보상을 계산한다.
4. 돌파가 예상되면 그 시점에서 계산을 멈춘다.
5. 획득 Gold/Scrap, 손상 여부, 현재 Level을 요약해서 보여준다.

예시:

```text
오프라인 2시간 13분
Frontline Lv. 12 유지
획득 Gold: 1,240
획득 Scrap: 88
성벽 손상: 37%
추천: 성벽 수리 또는 포탑 강화
```

## 12. Unity 구현 순서

1. `DefenseDirector`, `DefenseRuntimeState`, `DefenseUpgradeModel`, `CurrencyWallet`를 빈 `GameSystems` 오브젝트에 붙인다.
2. 숫자 HUD로 Frontline Level, Mode, WallHealth, EnemyPressure, Progress, Gold/Scrap을 표시한다.
3. Hold/Push 전환 버튼을 붙인다.
4. 성벽/포탑/병력 강화 버튼을 붙인다.
5. 숫자 시뮬레이션에서 단계 상승과 돌파가 이해되는지 확인한다.
6. `GroundDefenseLanePresenter`를 붙여 숫자 루프가 실제 배치된 앵커/마커/성벽 표시와 함께 움직이는지 확인한다. 이 단계는 시각 전투의 임시 브리지이며, 레인 크기와 카메라 구도는 Unity에서 수동 저작한다.
7. `GroundDefenseCombatPresenter`를 붙여 scene-authored 압박 적, 벽 피격 flash, 타워/수비대 공격 pulse가 같은 `DefenseDirector.Runtime` 값을 읽는지 검증한다. HUD의 `pressure +/-/s`와 `wall /s` 값이 보이는 전투 피드백과 맞는지도 함께 본다.
8. `GroundDefenseActorRuntime`으로 기존 압박 적 슬롯에 개별 체력, 이동률, 자동 방어 피격, 처치, 벽 접촉 상태를 부여한다. 이 상태는 저장하지 않고 권위 있는 연속 전선 상태에서 다시 구성한다. 이 P0-C 검증 브리지는 수용 완료되었으며 추가 블록아웃 폴리시는 하지 않는다.
9. 이후 지상 전투를 다시 다룰 때는 `DefenseEnemy`, `TowerBattery`, `DefenseWall`을 실제 스탯/타겟팅/처치 규칙이 있는 풀링 프리팹 컴포넌트로 교체한다. 고정 슬롯/기존 placeholder 오브젝트를 최종 구조로 확장하지 않는다.

## 13. 완료 기준

MVP 완료 기준:

- 전투가 끊기지 않고 계속 진행된다.
- 적 압박이 방어력보다 강하면 성벽이 손상된다.
- Hold 상태에서는 현재 단계에서 안정 파밍할 수 있다.
- Push 상태에서는 위험이 커지지만 Frontline Level이 오른다.
- 수리/강화 후 이전에 막힌 단계를 다시 버틸 수 있다.
- Gold/Scrap이 제작 또는 던전 준비에 쓰일 수 있다.
