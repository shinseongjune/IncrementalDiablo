# Unity Setup Guide

## 원칙

코드가 권한과 재사용 규칙을 소유하고, Unity Editor는 모델·애니메이션·맵·카메라의 시각 판단을 완성한다. 자동 작업은 위치, 크기, 방 형태, 카메라 구도를 임의로 결정하지 않는다.

## E3-B: 완료된 모델·Animator 연결

- Hero `HeroDefault`와 적 `OrcPADefault`는 기존 `Player`/`PF_DungeonEnemy_Melee` 루트의 `Model` 자식이다. 전투·이동·Collider 권한은 루트에만 둔다.
- 공통 상태기 `Assets/06.Art/Animations/Combat.controller`가 `MoveSpeed`(Float), `Attack`·`Hit`·`Death`(Trigger)와 다섯 상태를 소유한다. Hero/Orc는 `Assets/03.Characters/*/*_Combat.overrideController`로 클립만 바꾼다.
- 시각 Animator를 각 루트의 `CombatAnimationDriver`에 명시 지정하고 Root Motion은 끈다. 캡슐은 `MeshRenderer`만 숨기며 Collider는 보존한다.
- Play Mode에서 Hero와 Orc의 Idle, Move, Attack, Hit, Death를 확인했다. 이 연결은 회귀 전용이다.

## E3-C: 보류된 전투 감각 확인

- 텔레그래프와 반응성 조정은 배포했다. 최종 `신호 → 이탈 회피 → 재교전` 감각 확인은 이후 행동 패키지 작업으로 보류한다.

## E3-D: 첫 물리 던전 설정 체크리스트

- `Gameplay/DungeonRoot`에 `DungeonTraversalController`를 추가하고 `ExpeditionDirector`, `CombatRoom`, `Player`, 입구 귀환 Transform을 지정한다. 기존 자동 한 방 시작은 이 컴포넌트가 점유한다.
- 입구 뒤에 순서가 있는 두 개 이상의 전투 방을 배치한다. 각 방 입구에는 `Is Trigger` Collider와 `DungeonTraversalTrigger(EnterRoom)`를 두고, Controller의 `rooms[0..n]`에 같은 순서로 넣는다. 각 node에는 그 방 안의 적 스폰 Transform을 하나 이상 넣어 `EnemySpawner`가 다른 방의 스폰 지점을 재사용하지 않게 한다. `ExpeditionDirector.totalRooms`는 그 방 수와 같게 설정한다.
- 각 방의 다음 길을 막는 기존 문·벽·오브젝트를 해당 `Exit Blocker`에 지정한다. 마지막 보상/퇴장에는 `DungeonTraversalTrigger(ReturnToEntrance)`를 두고 Controller의 `returnTrigger`에 지정한다.
- NavMesh를 다시 굽고, Play Mode에서 `입구 계약 시작 → 첫 방 진입/전투 → 길 개방 → 다음 방 → 보상/퇴장 → 입구 귀환`을 확인한다. 외부 씬 변경 뒤 `Gameplay`를 다시 연다.

## 확인 기준

- 외부 씬/프리팹 변경 뒤 Unity에서 다시 열어 누락 스크립트를 확인한다.
- 시각 또는 조작 변화는 Play Mode에서 확인한다.
- 문서만 바꾼 날에는 Unity 설정 확인을 반복하지 않는다.
