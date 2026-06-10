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
- Unity Editor 수동 배치가 필요한 경우 연결 순서뿐 아니라, 의도한 시작 수치/범위, 형태, 배치 이유, 고정값과 조정 가능값도 함께 남긴다.
- Git commit/push는 하지 않는다. 변경이 검증되어 올릴 만하면 보고서에서 "사용자 확인 필요: 커밋/푸시 요청"으로 요청한다.
- 한 phase의 최소 검증이 끝났으면 디버그 HUD, smoke test, 진단 helper만 더 늘리는 작업을 기본 선택지로 삼지 않는다. 다음 phase의 **플레이어 가시 작업**으로 바로 넘어간다.

### 2.1 Headless Unity 검증 안전 규칙

2026-05-13 자동화 중 Unity 6000.4.4f1 batchmode 검증이 라이선싱/ILPP 단계에서 멈추며 `Unity.ILPP.Trigger.exe` 시스템 오류 팝업을 반복 생성했다. 재발 방지를 위해, 원인이 별도로 해결되기 전까지 daily automation은 무인 Unity batchmode 컴파일을 실행하지 않는다.

기본 검증 순서는 다음으로 제한한다.

- `.\Tools\Automation\Invoke-IncrementalDiabloChecks.ps1`
- The harness runs the reliable local baseline: `dotnet build .\IncrementalDiablo.sln -v:minimal`, `git diff --check`, `Gameplay.unity` scene-contract checks, automation-plan freshness checks, and local automation TOML health checks.
- Warnings from the harness, especially optional overlay wiring or stale next-unlock text, should be triaged in the same run when they are small/fix-now issues.
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
| Phase C - First Real Game Slice | Completed 2026-06-06 from accumulated accepted evidence across P0-A through P0-D. The normal path now has visible defense behavior, direct-control prefab combat, authored rewards, overlays, crafting, and no silent calculation-combat clear. | One player-facing runtime slice contains a visible ground-defense lane, one direct-control dungeon room with at least one real enemy prefab, and authored item assets/definitions feeding the reward loop without relying on debug-only surfaces for the normal path. | Closed unless a regression appears. Residual overlay polish belongs to Phase E, not another Phase C acceptance loop. |
| Phase D - Long-Horizon Systems Foundation | Current phase. The saved depth ladder, formula-driven threat/reward/material bands, production item registry/save migration path, and dominated-duplicate conversion sink are implemented. | Formula-driven dungeon tiers, ground scaling, item rarity/material sinks, and save migration hooks exist without hand-authored content ladders. | Add D1-A formula-driven ground pressure/output/reward/milestone scaling without manual wave lists. |
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

### Prototype Debt Sweep Rule

Daily automation must treat prototype, debug, fallback, and temporary-MVP code as tracked production debt, not as invisible legacy to route around.

- At the start of each run, execute `Tools/Automation/Get-PrototypeDebtInventory.ps1 -SummaryOnly`.
- Read and update `GameDesign/ProductionDocs/12_PrototypeDebtRegister.md` whenever the scan reveals a meaningful new marker or when a registered item changes state.
- Classify each marker as `Keep at edge`, `Promote`, `Replace`, `Delete`, or `Needs decision`.
- Do not choose debt cleanup ahead of the active production task unless the cleanup directly unblocks normal gameplay, save migration, or build verification.
- At Phase D entry, fold relevant `Alpha blocker` debt into the production feature that replaces it. Do not spend a standalone run cleaning debt that does not unlock player-visible progression.
- If a new prototype/debug/fallback path touches central systems such as combat, loot generation, save/load, or player input, add the register row in the same run and name the retirement trigger.

### 2026-05-31 Automation Authoring Authorization

The user explicitly approved future automation runs to directly adjust layout placement, UI anchoring, scene-safe presentation values, tuning constants, and similar numeric/positioning values when the change is reasonably inferable from existing specs, current scene structure, or a small production goal.

Automation should still ask for confirmation when a change depends on subjective final art direction, camera feel, room scale judgment, combat feel tradeoffs, irreversible scene composition choices, large economy pacing decisions, or any edit that cannot be validated by the local harness or a short Play Mode path.

Default operating rule: prefer making a conservative first-pass value change and documenting the exact Play Mode validation path over leaving visible gameplay blocked only because the value is visual or numeric.

## No-Stagnation Rules

Each automation run must satisfy at least one of these:

- change at least one completion criterion toward `Done`;
- implement or verify a missing system link in the playable loop;
- fix a blocker that prevents Play Mode or build verification;
- add a missing verification path that allows the next run to implement safely.

Docs-only work is allowed only when it directly unblocks code work, records required Unity scene setup, captures a major design decision, updates prototype debt automation, or updates this plan after real implementation. Do not repeat the same category of minor cleanup in two consecutive runs. A prototype debt update counts only when it adds a new untracked marker, changes a registered decision/state, or unblocks a concrete cleanup. Every report must state what moved closer to visible gameplay or what verification path now prevents prototype debt from silently hardening.

Accepted runtime evidence is cumulative. Do not require another full-loop Play Mode pass when every changed critical link has already been accepted after its latest behavior change and the harness guards the unchanged links. Re-run a previously accepted path only for a regression, a changed contract, or a phase-closing risk that is not already covered. Two consecutive runs may not use manual acceptance as their primary output unless the second run responds to a defect found by the first.

## Progress Tracker

| Field | Current value |
| --- | --- |
| Current phase | Phase D - Long-Horizon Systems Foundation |
| Last meaningful movement | 2026-06-10: D0-D now auto-converts a newly rolled authored reward when an owned copy of the same definition has both equal-or-higher level and equal-or-higher rolled power. The depth-scaled salvage payout is visible in the reward overlay and dungeon result, while upgrade candidates remain stored. |
| Next unlock | D1-A is `Next`: apply reusable formula bands to frontline pressure, defense output, ground rewards, and milestone unlocks without creating manual wave lists. |
| Loop coverage | Phase A debug loop, Phase B normal HUD loop, and Phase C first real game slice are complete. Phase D now has a persistent depth ladder, formula-driven dungeon risk/reward/material profiles, durable authored item identity, and an inventory-bloat guard that turns dominated duplicates into saved materials. |
| Known blockers | No current compile, scene-wiring, depth-scaling, reward-scaling, item-registry, save-migration, or duplicate-conversion blocker remains. The next product risk is that the ground layer still lacks a shared long-horizon scaling model comparable to the dungeon depth bands. |

## 3.1 Phase C MVP Completion Task List

This is the canonical MVP-completion checklist for Phase C. Daily automation must update this section every run before the final report: status, latest evidence, blocker, and next validation must be current. If a run makes no checklist change, add a dated `No change needed` note with the reason in `Run Update Notes`.

Work selection rule: choose the first safe `Next` or `P0` item unless a compile/build blocker prevents it. Support or acceptance-detail work can be selected only when it directly unblocks one of the P0 gates or fixes a regression found while validating them.

Status key: `Next` means first default target, `In Progress` means implemented but not fully accepted, `Needs Unity Play Mode` means code/docs are ready but runtime judgment is still required, `Blocked` means an upstream gate or user/editor decision is needed, and `Done` means the completion criteria have recorded evidence.

| ID | Priority | Track | Current status | Completion criteria | Next update required |
| --- | --- | --- | --- | --- | --- |
| P0-A | P0 | Crafting validation | Done | Rare acquisition, salvage for `AlterStone`, Rare affix reroll spend, changed affix text, Result before/after summary, equipped stat refresh, spare-item salvage, overlay close, and previous-focus return are all confirmed in Play Mode. | No further action unless a regression appears. |
| P0-B | P0 | Camera and screen readability | Done | `DefenseFocus` camera, `DungeonFocus` camera, the 70/30 split, defense side-panel crop, overlay occlusion, saved-running dungeon focus restore, routed dungeon-panel click movement/attack, Shift-click target hits, and alert readability are acceptable for MVP. | No further action unless a regression appears. Keep static harness checks; viewport QA copy is tracked in TD-06 and must not become final HUD text. |
| P0-C | P0 | Ground defense readable combat | Done / Behavior accepted / Temporary presentation frozen | Pressure actors, wall contact, defender/tower attacks, wall damage, and frontline pressure read as visible automatic combat rather than only abstract counters. | No further blockout polish. Reopen only for regression or a production replacement using pooled prefabs, archetype data, real targeting/death, and reusable feedback. |
| P0-D | P0 | Dungeon prefab combat feel | Done | `PF_DungeonEnemy_Melee` spawns on NavMesh, activates on `Running`, chases/attacks, takes routed player attacks, shows HP/death/result feedback, clears into the authored reward path, and repeats on a second run without prototype simulation. | No further action unless a regression appears. Keep the prefab/NavMesh/static harness contracts. |
| P0-E | P0 | Reward, inventory, crafting normal path | Closed / Residual UX moved to Phase E | Reward auto-open, equip/salvage actions, inventory changes, paid reroll, result proof, close, and focus return already have accepted evidence. Requiring another pass only to find optional label/layout polish would not change the Phase C exit result. | Reopen only for a concrete regression. Track presentation density, duplicate actions, and final labels as Phase E usability work. |
| P0-F | P0 | Full loop acceptance test | Consciously waived / Covered by cumulative evidence | Phase A and B accepted the connected loop; P0-A through P0-D then accepted every behavior changed for the first real slice. A further 10-20 minute pass would repeat unchanged links rather than answer an uncovered risk. | No further Phase C run. Regression tests remain available at the edge; continue with Phase D production. |
| P1-A | P1 | MVP presentation polish | Moved to Phase E | Top/bottom bars, buttons, labels, overlay density, text fit, and non-debug visual hierarchy are acceptable for the first sellable-slice direction. | Re-enter when long-horizon systems support a 2-4 hour slice, or earlier only for a blocking usability defect. |
| P1-B | P1 | Authored item minimum breadth | Phase C evidence complete / Phase D expansion pending | Existing tier-1 authored items create readable Normal/Magic/Rare choices without expanding content prematurely. | Expand through Phase D reward bands, item registry, affix data, and sink tasks instead of another tier-1 validation pass. |
| P1-C | P1 | Prototype/debug/fallback debt retirement | Tracked / Integrate with replacement features | Registered prototype/debug/fallback paths each have a keep/promote/replace/delete decision, retirement trigger, and alpha-blocker flag. | Run the inventory every automation run, but retire debt only inside the Phase D feature that replaces it or when it blocks production. |
| SUP-01 | Support | Defense alert readability | Done | During `DungeonFocus`, wall/pressure/breach danger can be noticed through the HUD summary/action hint without becoming a new polish track. | No further action unless a regression appears. |

Still deferred beyond the first Phase D progression foundation: Legendary/Unique/Set breadth, large affix pools, multiple dungeon themes, Steam/platform work, cloud/network features, and release marketing surfaces. Depth progression, formula-driven reward pacing, item registry/migration, duplicate sinks, and long-tail scaling are no longer deferred; they belong to the Phase D queue below.

Run Update Notes:

- 2026-06-03: Created this checklist and made it mandatory for daily automation updates. Camera/screen readability is now a first-class P0 gate. `Defense alert` is explicitly tracked as support acceptance detail, not as a primary MVP completion track.
- 2026-06-04: Fixed a small overlay validation risk before the P0-A Play Mode pass: `RewardOverlayPresenter`, `InventoryOverlayPresenter`, and `CraftingOverlayPresenter` now track the exact references they subscribed to and resubscribe when auto-found references appear or change. Current phase remains Phase C; P0-A remains `Next / Needs Unity Play Mode`, with P0-E normal-path validation helped by more reliable live overlay updates.
- 2026-06-04: User confirmed P0-A passes perfectly in Unity Play Mode. Accepted evidence covers Rare acquisition, Rare salvage for `AlterStone`, next Rare affix reroll, material spend, changed affix text, Result before/after proof, equipped-stat refresh, spare-item salvage, overlay close, and previous-focus return. P0-B is now the default `Next` gate; P0-E remains `In Progress` until reward/inventory/crafting normal-path confusion and duplicate-action readability are explicitly judged.
- 2026-06-04: Advanced P0-B from pure manual judgment to code-ready scene wiring. Added a reusable camera-to-`RawImage` render-target binder, a dungeon `RawImage` click router that emits rays from the panel camera, and safer `PlayerController` ray handling for UI-covered clicks plus self/friendly collider hits. P0-B remains open until `Gameplay` wires the `RawImage`, dungeon camera, and router, then passes Play Mode readability/click validation.
- 2026-06-04: Added the Prototype Debt Sweep Rule, created `12_PrototypeDebtRegister.md`, and added `Tools/Automation/Get-PrototypeDebtInventory.ps1` to the verification harness. Automation must now scan and classify prototype/debug/fallback markers every run, but cleanup stays behind P0-B through P0-F unless it directly unblocks a P0 gate.
- 2026-06-04: User found two P0-B validation defects: a saved `Running` dungeon spawned enemies after Play restart without showing the dungeon panel, and Shift-click stationary attacks appeared to swing without hitting. `PlayableLoopHud` now syncs screen focus from expedition state changes/load, and `PlayerController` keeps a stationary target command alive while waiting for range instead of clearing it after one out-of-range swing. Revalidate by stopping Play during a running dungeon, restarting, and checking that the dungeon panel appears, then testing Shift-click against an enemy at/near attack range.
- 2026-06-05: Static scene inspection shows `Gameplay` already has `RawImage_DungeonViewport`, `Camera_DungeonPanel`, `PanelCameraRenderTarget`, and `DungeonViewportInputRouter` wired. Added HUD-facing dungeon viewport diagnostics, input-router camera auto-resolution from the same-object render target, and harness checks for the camera-panel bridge. P0-B remains open for Unity Play Mode readability and routed-click judgment, not for another static wiring pass.
- 2026-06-05: Corrected the duplicate-validation mistake after the user confirmed the prior P0-B work was already completed. The 2026-06-04 Play Mode confirmation and pushed `23cadd3` checkpoint are accepted as P0-B/SUP-01 completion evidence. P0-C ground-defense readable combat is now the default `Next` gate.
- 2026-06-05: Added `GroundDefenseActorRuntime` to `Gameplay > DefenseRoot` and wired it into `GroundDefenseCombatPresenter`. The three existing pressure actors now receive individual health, travel, automatic defense-hit, defeat, and wall-contact states derived from authoritative continuous-frontline telemetry. P0-C remains open only for a focused Unity Play Mode readability check; final actor prefabs, animation, pooling, and archetype stats are later work.
- 2026-06-05: User confirmed that the P0-C behavior appears to work and challenged further time spent on meaningless temporary design. P0-C is accepted as `Done` for behavior readability. The current fixed three-slot/blockout presentation is frozen, registered as replacement debt, and must not receive additional color/count/speed/layout polish. P0-D production-persistent dungeon prefab combat is now the default next task.
- 2026-06-06: Advanced P0-D with a production-persistent spawn fix. `EnemySpawner` now prevalidates the melee prefab's `Health`, enemy `CharacterActor`, `EnemyAIController`, enabled `NavMeshAgent`, and click collider; it resolves all intended spawn positions through `NavMesh.SamplePosition` before instantiation and reports a setup blocker when placement fails. `Gameplay` explicitly enables the NavMesh snap with a `2` unit sample radius, and the repo harness now checks the scene NavMesh surface, prefab contract, and spawn-validation code. P0-D remains open for one focused Play Mode combat/reward/retry acceptance pass.
- 2026-06-06: User confirmed the P0-D path works in Play Mode. P0-D is now `Done`, and normal `Gameplay` disables `CombatRoom` prototype simulation so the accepted prefab encounter is the only normal combat path. P0-E reward/inventory/crafting normal-path review is now the default next task.
- 2026-06-06: Stagnation audit found that the project had already completed two broad loop acceptances (Phase A on 2026-05-14 and Phase B on 2026-05-17) plus four focused Play Mode gates from 2026-06-04 through 2026-06-06. P0-E and P0-F would have added two more passes over largely unchanged links. They are closed/waived by cumulative evidence, Phase C is complete, and Phase D is promoted.
- 2026-06-07: Implemented D0-A persistent dungeon depth progression. `DungeonSaveData` now stores selected/highest-unlocked depth under schema v2 with v1 migration; `ExpeditionDirector` starts the selected depth, unlocks one next depth only after clearing the current highest, and leaves failure non-advancing; `PlayableLoopHud` plus `Gameplay` expose wired `Depth -` / `Depth +` controls and visible active/selected/unlocked status. The harness statically guards the save, unlock, and scene-wiring contracts. This implementation note's focused Play Mode requirement was satisfied by the following acceptance entry.
- 2026-06-07: User confirmed the D0-A Play Mode path. Accepted evidence covers Depth 1 clear -> Depth 2 unlock, selecting and starting Depth 2, failure without unlocking Depth 3, and save/load restoration of selected/highest depth at 2/2. D0-A is `Done`; D0-B formula-driven depth threat/reward bands are now `Next`.
- 2026-06-08: Implemented D0-B formula-driven depth bands. `DungeonDepthBalanceModel` owns ten-depth enemy-health, enemy-damage, reward-power, and material-yield curves; `EnemySpawner`, `LootDropper`, `ItemSalvageService`, and `PlayableLoopHud` consume the same profile. `Export-DungeonDepthBalance.ps1` reads the C# constants, validates monotonic depth 1-100 growth, and exports `GameDesign/Balance/DungeonDepthBalance.csv`. At this implementation checkpoint D0-B awaited the focused Play Mode evidence recorded in the following acceptance entry.
- 2026-06-08: User confirmed the focused D0-B Play Mode comparison was completed successfully. Accepted evidence covers the Depth 1 versus Depth 2 enemy HP/damage increase and the Depth 2 authored reward level/power path. D0-B is `Done`; D0-C item registry and save migration is now `Next`.
- 2026-06-09: Completed D0-C. `ItemDefinitionRegistry.asset` now owns the six authored tier-1 identities and optional legacy-id remaps; schema v3 runs item-id migration before validation/load; unresolved ids remain preserved but visibly quarantined instead of silently restoring snapshot gameplay power; equip/salvage actions disable for those items; and normal `Gameplay` disables runtime loot fallback. The harness guards the registry asset, scene reference, fallback setting, migration source contracts, and D0-C/D0-D routing. D0-D is now `Next`.
- 2026-06-10: Completed D0-D. `LootDropper` rolls a candidate before inventory insertion and auto-converts it only when an owned resolved item with the same canonical definition has both equal-or-higher level and equal-or-higher rolled power. `ItemSalvageService` pays the normal depth-scaled materials without inventory insertion, reward/dungeon UI reports the conversion, and missing conversion dependencies fall back to normal grant instead of deleting the reward. The harness guards the scene reference, enabled policy, comparison constraints, payout path, and D0-D/D1-A routing. D1-A is now `Next`.

## 3.2 Phase D Production Task List

This is the canonical work selector after Phase C. A validation-only run is not a valid default choice. Each P0 task must add persistent progression, scalable data, a sink, or a replacement production contract.

| ID | Priority | Track | Current status | Completion criteria | Next update required |
| --- | --- | --- | --- | --- | --- |
| D0-A | P0 | Save-backed dungeon depth progression | Done | Normal UI can choose a depth from `1..highestUnlockedDepth`; clearing the highest unlocked depth unlocks exactly one next depth; failure does not advance; selected and highest-unlocked depth survive save/load; starting an expedition uses the selected depth. | No further action unless a regression appears. Keep schema-v2 migration, diagnostics, depth-button wiring, and harness contracts. |
| D0-B | P0 | Formula-driven depth threat and reward bands | Done | `DungeonDepthBalanceModel` defines bounded ten-depth bands; spawned enemy health/damage, item level/rolled power, and salvage material yield use the active/source depth; the HUD exposes the profile; the depth 1-100 export/check is deterministic; the user confirmed the Depth 1/2 runtime comparison. | No further action unless a regression appears. Keep runtime scaling, HUD feedback, CSV export, and harness contracts. |
| D0-C | P0 | Item registry and save migration | Done | Saved authored items resolve through a production item-definition registry; unknown ids produce an explicit migration/diagnostic result instead of silently depending on runtime prototype definitions. | No further action unless a registry id changes or a migration regression appears. Keep schema-v3, quarantine, scene registry, and fallback-disable harness contracts. |
| D0-D | P0 | Duplicate-item sink and conversion | Done | A newly rolled authored reward is converted into its normal depth-scaled salvage materials when an owned item with the same definition dominates both level and rolled power; upgrade candidates remain stored; conversion feedback is player-visible; and conversion failure falls back to normal grant. | No further action unless rewards disappear, stronger candidates are converted, or the UI presents an older item as the new reward. Cross-definition scoring, collection, and defender gear remain later product decisions. |
| D1-A | P1 | Formula-driven ground scaling | Next | Frontline pressure, defense output, rewards, and milestone unlocks scale through reusable formulas and bands without manual wave lists. | Define one shared ground progression profile and connect it to the existing continuous-frontline runtime before adding new ground content. |

## 3.3 Progress Assessment Against Game-Form Plan

Current assessment: progress is on track for the staged goal of making the project take recognizable game form, but it is not close to a finished Steam 1.0 product. The project is between the 30-60 minute core-loop MVP target and the 5-10 hour vertical-slice target: the loop systems are connected enough to verify, while the player-facing action layer is still thin.

Speed assessment:

- Good: save/load, real dungeon combat, authored reward continuity, normal overlays, visible ground behavior, and the first persistent depth ladder now support progression beyond a fixed one-room proof.
- Good: the first formula band has been observed against the real spawned prefab and authored reward path in Play Mode, so the depth ladder is now a meaningful risk/reward choice rather than only saved structure.
- Required correction: add D1-A's shared ground progression profile so the automatic defense half can scale beside the dungeon depth ladder. Do not reopen D0-B through D0-D unless their contracts regress.

Direction assessment:

- Correct direction: the project is still preserving the intended PC incremental action RPG shape: automatic ground defense, direct-control dungeon combat, loot, equipment, crafting/salvage, save/load, and long-term progression.
- Current risk: the verified ground lane and fixed three-slot actor projection can be mistaken for final defense architecture. They are accepted behavior bridges, not final content or prefab structure.
- Operating rule: unless a regression or build blocker appears, do not spend another run polishing the current defense blockout, compact depth buttons, or registry diagnostics. The next meaningful production increment is the duplicate-item sink/conversion loop.

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
- 2026-05-18에는 `DungeonRoomPresenter`를 추가해 첫 방 표현을 위한 코드 기반 훅을 준비했다. 이 컴포넌트의 room shell과 tint는 prototype/fallback 용도이며, 최종 방 상태 피드백은 authored 문/스폰/보상/VFX/UI로 대체한다. 방 크기/배치/공간감은 수동 Unity 저작이 더 적합하고, 실제 적 프리팹/스포너 경로도 아직 남아 있으므로 이 항목은 `Next` 상태를 유지한다.

2026-05-19 code progress note:

- `EnemySpawner` now exists for Phase C room setup. It listens to `CombatRoom` state changes, instantiates a configured melee enemy prefab at assigned spawn points, keeps spawned enemies inactive during the start countdown, then registers their `Health` components with `CombatRoom`.
- `CombatRoom.RegisterTrackedEnemies(...)` lets prefab-spawned enemies replace loose scene enemy references without changing the existing reward/save/HUD flow.
- Follow-up scene work has since added `PF_DungeonEnemy_Melee` and one spawn point to `Gameplay`. This still does not complete the Phase C P0 by itself; Play Mode must confirm activation timing, NavMesh movement, click-to-attack feel, clear/fail messaging, and save/reward continuity.

2026-05-21 code progress note:

- `EnemySpawner` now keeps a `LastSpawnMessage` and reports missing prefab / missing `Health` setup blockers into `CombatRoom`.
- `CombatRoom` now stores `TrackedEnemySetupBlocker` and blocks prototype simulation while an enemy-spawn setup blocker is active, so the first real room cannot silently fake-clear through hidden simulation when the prefab path is broken.
- `PlayableLoopHud` now shows the current combat path (`tracked enemies`, `prototype simulation`, `setup blocked`, or `waiting for enemies`) in the dungeon line. If setup is blocked, the action hint names the blocker.
- `Gameplay` already points `EnemySpawner` at `Assets/04.Prefabs/Dungeon/PF_DungeonEnemy_Melee.prefab` and one spawn point. The remaining P0 gate is user Play Mode validation, not more prefab creation by automation.

### P0. 지상 전선 시각 프로토타입

상태: Code telemetry added (Play Mode real-combat feel validation pending)

목표:
숫자 압박 모델만 보이던 지상 전선을, 적이 성벽으로 밀려오고 방어가 자동으로 대응하는 장면으로 바꾼다.

완료 기준:

- 적이 화면에서 지속적으로 전진한다.
- 포탑 또는 병력이 자동으로 적을 공격한다.
- 압박 증가와 성벽 손상이 숫자뿐 아니라 장면에서도 읽힌다.
- 기존 `DefenseDirector` 수치 루프와 시각 오브젝트가 분리되지 않고 함께 움직인다.

2026-05-22 progress note:

- `GroundDefenseLanePresenter` and the `Gameplay` scene wiring proved that scene-authored lane markers, wall fill, pressure fill, state objects, and TMP labels can follow the live `DefenseDirector.Runtime` without hardcoding layout or camera composition.
- This completes the presentation bridge, not the real defense game. The P0 remains open until enemy/pressure actors visibly move toward the wall, wall contact produces readable damage, and tower/defender attacks are represented as player-facing action.

2026-05-23 code progress note:

- `GroundDefenseLanePresenter` now auto-resolves `Renderer` components from the assigned pressure and Push progress marker transforms, so the existing `Gameplay > DefenseRoot` marker objects can recolor by runtime state without extra Inspector wiring.
- Optional `Enemy Flow Markers` can now be assigned as scene-authored transforms. When the frontline is running, the presenter activates a pressure-scaled count of those markers and moves them from `EnemySpawnAnchor` toward `WallAnchor`; on breach, all assigned flow markers remain active.
- The script exposes `LastPresentationMessage` and `ActiveEnemyFlowMarkerCount` for Inspector/HUD diagnostics, but it still leaves marker art, marker count, lane length, camera framing, and final composition to manual Unity authoring.
- This advances the visible ground-lane P0 but does not turn the bridge into real ground combat. The next production work should add visible pressure/enemy actors, wall-contact damage, and tower/defender feedback rather than more helper-only lane diagnostics.

2026-05-24 code progress note:

- `GroundDefenseCombatPresenter` now exists as the next Phase C bridge after the marker-only lane. It reads `DefenseDirector.Runtime` and drives scene-authored `Pressure Actors`, `Wall Contact` feedback, and `Attack Pulses`.
- Pressure actors move from `EnemySpawnAnchor` to `WallAnchor`; active count scales with `EnemyPressure / EnemyPressureCapacity`, and all assigned actors stay at the wall while breached.
- Wall contact feedback flashes when `WallHealth` drops or the frontline becomes breached. Attack pulses scale with `DefenseUpgradeModel.TotalDefensePower` and move from `Attack Origin` toward the leading active pressure actor.
- `PlayableLoopHud` now auto-finds `GroundDefenseCombatPresenter` and shows `LastCombatMessage` in the frontline summary when the component is present, so Play Mode can tell whether this path is wired.
- This is still not final authored defense combat. The remaining gate is manual Unity placement plus Play Mode feel validation of silhouettes, pulse readability, wall-hit timing, and whether the scene reads as defenders fighting enemies rather than as abstract markers.

2026-05-25 code progress note:

- `DefenseRuntimeState` now records the most recent frontline tick as rates: incoming pressure per second, pressure cleared per second, wall damage per second, and push progress per second. These are runtime feedback values only; they do not change save data or long-term scaling rules.
- `GroundDefenseCombatPresenter` now colors pressure actors by combat state and uses the measured cleared-pressure rate, not only upgrade level, when deciding how many attack pulses should be visible.
- `LastCombatMessage` now includes `pressure +/-/s` and `wall /s` values so `PlayableLoopHud` can verify whether the visible pressure actors, defender/tower pulses, and wall contact are following the real simulation.
- Static scene inspection confirms `Gameplay > DefenseRoot` already has `GroundDefenseCombatPresenter`, pressure actors, wall-contact flash, and attack pulses wired. The remaining gate is Play Mode feel validation, not more scene-wiring code.

### P1. Playable screen focus and overlay bridge

Status: Code bridge plus inventory overlay presenter added (Unity panel wiring and Play Mode validation pending)

Goal:
Move the first combined-loop screen away from a single static HUD into the MVP presentation shape described in `11_PlayableScreenPresentationSpec.md`: ground defense fills the default view, starting a dungeon makes dungeon play dominant while defense remains visible, and inventory/crafting/reward panels can open as overlays without reloading scenes.

Completion criteria:

- A reusable controller can switch between `DefenseFocus` and `DungeonFocus` without stopping `DefenseDirector` or `ExpeditionDirector`.
- The controller exposes overlay states for inventory, crafting, and reward presentation.
- The normal `PlayableLoopHud` can request DungeonFocus on dungeon start and DefenseFocus on room clear/fail without requiring debug HUDs.
- The exact RectTransform composition, camera crop, panel art, and overlay contents remain Unity-authored values.

2026-05-26 code progress note:

- Added `PlayableScreenFocus` and `PlayableScreenLayoutController` under `Assets/02.Scripts/UI`.
- The layout controller drives authored RectTransforms by normalized anchors: default `DefenseFocus` uses the full main area; `DungeonFocus` uses a prototype 70/30 split with defense on the right; inventory, crafting, and reward are GameObject overlays over the previous gameplay focus.
- `PlayableLoopHud` now auto-finds the layout controller. If `Sync Screen Focus With Dungeon` is enabled, `StartDungeon()` requests `DungeonFocus`, and room clear/fail requests `DefenseFocus`.
- This advances screen-state behavior only. It does not decide final camera framing, ornate UI art, overlay item list content, or exact panel placement.

2026-05-27 code progress note:

- `PlayableScreenLayoutController` now exposes overlay availability and refuses to switch into `InventoryOverlay`, `CraftingOverlay`, or `RewardOverlay` when the corresponding GameObject is not wired, preventing invisible "opened" overlay states.
- `PlayableLoopHud` now has optional button slots and methods for inventory/crafting/reward overlay open plus close, subscribes to screen-focus changes, and shows current screen focus in the summary line.
- Static scene checks now find the current `Gameplay` overlay GameObjects wired into `PlayableScreenLayoutController`; the remaining work is authored content, button hookup, and Play Mode validation before the normal player can use them visually.

2026-05-28 code progress note:

- `InventoryOverlayPresenter` now exists under `Assets/02.Scripts/UI` as the first player-facing inventory overlay content bridge.
- A wired inventory overlay can show inventory count, item rows, selected item details, wallet/material preview, salvage preview, Rare affix-reroll cost preview when a definition supports it, and action messages.
- The presenter exposes button-safe `SelectPrevious`, `SelectNext`, `SelectLatest`, `EquipSelected`, `SalvageSelected`, and `CloseOverlay` methods. It auto-finds `SimpleInventory`, `ItemSalvageService`, `EquipmentSlots`, `CurrencyWallet`, and `PlayableScreenLayoutController` for the first pass, but exact TMP/Button layout and overlay density remain Unity-authored.
- This advances the normal inventory path beyond HUD-only latest-item actions. It does not wire the scene panel, craft/reroll behavior, reward reveal visuals, final item-list art, or long-term registry/drop-table tooling.

2026-05-29 code progress note:

- `RewardOverlayPresenter` now exists under `Assets/02.Scripts/UI` as the first player-facing reward overlay content bridge.
- A wired reward overlay can show pending/claimed dungeon reward state, loot source, latest reward item details, wallet/material preview, salvage preview, and Rare reroll-cost preview when available.
- The presenter exposes button-safe `ClaimPendingReward`, `OpenInventoryOverlay`, `EquipReward`, `SalvageReward`, and `CloseOverlay` methods. It auto-finds `ExpeditionDirector`, `LootDropper`, `SimpleInventory`, `ItemSalvageService`, `EquipmentSlots`, `CurrencyWallet`, and `PlayableScreenLayoutController` for the first pass, but exact TMP/Button layout, reveal animation, icon art, and overlay density remain Unity-authored.
- This advances dungeon clear feedback beyond HUD/latest-item text. It does not wire the scene panel, author reward reveal visuals, implement crafting behavior, or replace final item registry/drop-table tooling.

2026-05-30 code + scene progress note:

- `CraftingOverlayPresenter` now exists under `Assets/02.Scripts/UI` as the first player-facing crafting overlay content bridge.
- A wired crafting overlay can show inventory rows, selected item details, wallet/material preview, current affixes, salvage preview, and Rare affix-reroll cost.
- `ItemInstance.TryApplyPrototypeAffixReroll(...)` now replaces the selected Rare item's prototype affix roll with one saved `ItemAffixRoll`; the presenter spends `ItemDefinition.AffixRerollCost` through `CurrencyWallet`, not a free preview.
- `ItemEconomyModel` now lets every Rare salvage return at least one `AlterStone`, so the first reroll sink can be fed by the current tier-1 guaranteed reward loop instead of waiting for later tiers.
- `SimpleInventory.NotifyItemsChanged()` and `EquipmentSlots.RefreshEquippedModifiers()` let the reroll update inventory UI and equipped-stat subscribers without changing save schema.
- This advances crafting from a placeholder overlay to a real material sink with first-pass `Gameplay` scene controls. It does not create final affix pools, affix locking, item-level upgrade crafting, icon/scroll polish, or drop-balance export/import tooling.
- Follow-up pacing fix: `LootDropper` now guarantees a Rare authored reward when the inventory has no Rare and can also force a Rare after 6 weighted non-Rare rewards. This keeps the 78/20/2 weights as baseline pacing but removes the Play Mode validation blocker where the first Rare could take dozens of clears.

2026-05-31 code progress note:

- `PlayableLoopHud` now has `openRewardOverlayOnDungeonClear` enabled by default. When a room resolves as cleared and the reward overlay is wired, the normal HUD opens `RewardOverlay` automatically instead of leaving reward review as a manual-only bottom-bar action.
- `PlayableScreenLayoutController.TryOpenOverlayAfterGameplayFocus(...)` applies a target gameplay focus before opening an overlay. The room-clear path uses this to open the reward overlay over `DefenseFocus`, so closing the overlay returns to the post-run defense screen without preserving a partial dungeon transition.
- Manual reward claiming through `PlayableLoopHud.ClaimPendingReward()` also opens the reward overlay after a successful claim when possible.
- This moves the authored reward loop closer to visible gameplay because dungeon clear now leads directly into the reward/equip/salvage decision path. It still requires Play Mode validation of readability, button wiring, close-focus behavior, and whether the reward reveal treatment feels strong enough.
- User follow-up confirmed this reward-overlay flow, so future work should treat it as validated unless a regression appears.

2026-05-31 crafting follow-up note:

- `CraftingOverlayPresenter` now prefers the newest rerollable Rare item when opened, rather than blindly selecting the newest inventory item.
- The crafting header now shows how many Rare items exist and how many are ready for reroll. Materials text now gives a next-step hint, including the current first-pass loop of salvaging one spare Rare for `AlterStone` before rerolling the next Rare.
- The affix/result area now includes a reroll status line, so Play Mode validation can tell whether the button is disabled due to missing materials, a missing wallet, an unresolved definition, or a non-Rare selection.
- This improves the first crafting validation path without adding final affix pools, affix locking, item-level upgrades, icon polish, or long-term drop balance.

2026-06-01 crafting validation feedback note:

- `CraftingOverlayPresenter` now stores a per-item last reroll summary after a successful Rare affix reroll.
- The Result panel shows the spent material cost plus a `before -> after` affix summary for the selected item, so Play Mode validation can confirm material spend and affix mutation without comparing multiple overlay regions by memory.
- This is still first-pass validation feedback. It does not add final affix pools, affix locking, animation, icon treatment, or long-term crafting balance.

2026-06-02 crafting validation anti-repeat note:

- `ItemInstance.TryApplyPrototypeAffixReroll(...)` now checks the selected Rare item's currently saved prototype affix before accepting a new roll.
- When another prototype candidate exists for the item's slot, the reroll avoids returning the same affix id/stat/type/value. This makes the first material-spend validation path reliably show a changed affix line.
- This does not add authored affix pools, affix tags/weights, affix locking, or long-term crafting balance. It only stabilizes the current two-candidate prototype reroll path for Play Mode validation.

2026-06-03 defense alert HUD note:

- `PlayableLoopHud` now builds a `Defense alert` line from breach, low wall health, wall damage per second, high pressure, or damaged-wall state.
- The HUD summary can show the alert without a new TMP field, and the action hint prioritizes severe alerts plus high pressure while `DungeonFocus` or an active dungeon run could distract the player.
- Default first-pass thresholds are low wall at `35%` health and high pressure at `75%` capacity. This is player-facing feedback only; it does not change defense balance, scene composition, camera crop, or final alert art.

2026-06-04 overlay event reliability note:

- `RewardOverlayPresenter`, `InventoryOverlayPresenter`, and `CraftingOverlayPresenter` now resynchronize their event subscriptions after auto-finding references during refresh, not only on enable.
- This keeps reward grant, inventory removal/addition, wallet material spend, salvage payout, and equipped-stat refresh visible in the normal overlay path even if the reference was assigned or discovered after the first enable.
- This does not change economy costs, reward odds, item mutation rules, overlay layout, or final UI density. It reduces the P0-A/P0-E Play Mode validation risk of stale overlay text.

2026-05-26 verification harness note:

- `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` now runs the daily safe verification baseline without Unity batchmode: solution build, `git diff --check`, required `Gameplay.unity` scene-contract tokens, missing-script scan, optional overlay wiring warning, automation-plan freshness, and local automation TOML health.
- The harness intentionally reports unwired inventory/crafting/reward overlays as warnings rather than failures because those panels are authored UI work, not a code blocker. As of 2026-05-28 the current `Gameplay` scene passes this optional overlay check.
- Future automation runs should run this harness before the final report, fix small harness failures immediately, and explain any remaining warnings in the Korean handoff.

### P1. 첫 authored item 세트

상태: Code Done (Play Mode validation pending)

목표:
런타임 임시 아이템 fallback에만 기대지 않고, 실제 `ItemDefinition` 에셋 몇 개가 던전 보상으로 떨어지기 시작한다.

완료 기준:

- 무기/방어구/장신구에서 최소 1개씩 실제 아이템 정의 에셋이 있다.
- 일반 플레이 경로에서 보상은 authored definition을 우선 사용한다.
- save/load 후에도 definition 재연결 경로가 분명하다.
- prototype fallback은 비상 경로로만 남고, 일반 플레이 설명은 authored item 기준으로 바뀐다.

2026-05-20 code progress note:

- `LootDropper` now has a weighted `RewardEntry` table before the old uniform `rewardDefinitions` fallback. This keeps authored rewards sparse enough for Diablo-like item pacing without duplicating asset references in the scene.
- Six tier-1 `ItemDefinition` assets now exist under `Assets/05.ScriptableObjects/Items`: three Normal items, two Magic items, and one Rare ring.
- `Gameplay` wires those assets into `LootDropper` at a prototype per-clear weight split of 78% Normal, 20% Magic, and 2% Rare. This is intentionally conservative because every room clear currently grants one item; it is not a final long-term drop-rate target.
- Prototype runtime rewards remain available only as a fallback if the authored table is empty or invalid.

2026-05-21 diagnostics note:

- `LootDropper` now records `LastRewardSource` (`WeightedRewardTable`, `RewardDefinitions`, or `PrototypeFallback`) and includes the source in `LastDropMessage`.
- `PlayableLoopHud` now shows whether the dungeon reward path is using the authored weighted table, a legacy list, or prototype fallback. No item weights or rarity pacing numbers changed in this run.

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

- 2026-05-24 presentation decision note: the saved final gameplay screen reference should be treated as the target dungeon-dominant layout, while the user's proposed defense-window compression plus dungeon slide-in is the preferred transition into that layout. Temporary MVP values are now stored in `11_PlayableScreenPresentationSpec.md`: `DefenseFocus` starts full-width, `DungeonFocus` uses a right-side defense panel at `30%` width with the dungeon at `70%`, top and bottom bars stay fixed, entry duration starts at `0.38s`, and exit duration starts at `0.32s`. These are not final art/camera values; they exist to guide implementation without blocking on visual tuning.
- 2026-05-15 첫 10분 플레이 패스에서 던전 재도전 버그를 발견했다. 1회 클리어 뒤 다시 `Start Dungeon`을 누르면 `ExpeditionDirector`는 새 런을 `Running`으로 시작했지만, `CombatRoom`은 이전 0번 방의 `Cleared` 상태를 보고 같은 방 재시작을 막아서 경과 시간만 증가했다. 이 재시도 차단 로직은 제거했고, 후속 Play Mode 확인에서는 두 번째 던전도 즉시 `Starting -> Running -> Cleared`로 다시 흘러야 한다.
- 2026-05-16 QA 게이트에서 전선 돌파 후 보상이 0%라면 Gold를 모두 쓴 플레이어가 수리비를 다시 벌지 못해 진행이 잠길 수 있는 소프트락을 발견했다. 초기 프로토타입은 돌파 중 보상을 25%로 낮추되 0으로 만들지 않도록 수정했고, 후속 10-20분 패스에서는 돌파 후 수리 회복이 화면에서 명확히 읽히는지 함께 확인해야 한다.
- 2026-05-17 QA 게이트에서 `Gameplay` 씬 안에 `Player`와 `Enemy`가 이미 있었지만 `CombatRoom`이 둘을 추적하지 않아 일반 플레이 경로가 계속 숨은 계산 전투로만 흘러가는 간극을 발견했다. 첫 수정은 적을 붙이는 데는 성공했지만, 사용자의 피드백대로 적이 그냥 따라오다 멈추고 방/클리어 감각이 전혀 없었다. 후속 수정에서 적을 room lifecycle에 묶고, 현재 HP와 명시적 clear/fail 메시지를 노출했다.
