# Unity Setup Guide

## 원칙

코드가 권한과 재사용 규칙을 소유하고, Unity Editor는 모델·애니메이션·맵·카메라의 시각 판단을 완성한다. 자동 작업은 위치, 크기, 방 형태, 카메라 구도를 임의로 결정하지 않는다.

## E3-B: 완료된 모델·Animator 연결

- Hero `HeroDefault`와 적 `OrcPADefault`는 기존 `Player`/`PF_DungeonEnemy_Melee` 루트의 `Model` 자식이다. 전투·이동·Collider 권한은 루트에만 둔다.
- 공통 상태기 `Assets/06.Art/Animations/Combat.controller`가 `MoveSpeed`(Float), `Attack`·`Hit`·`Death`(Trigger)와 다섯 상태를 소유한다. Hero/Orc는 `Assets/03.Characters/*/*_Combat.overrideController`로 클립만 바꾼다.
- 시각 Animator를 각 루트의 `CombatAnimationDriver`에 명시 지정하고 Root Motion은 끈다. 캡슐은 `MeshRenderer`만 숨기며 Collider는 보존한다.
- Play Mode에서 Hero와 Orc의 Idle, Move, Attack, Hit, Death를 확인했다. 이 연결은 회귀 전용이다.

## 다음 P0: E3-C 적 행동·텔레그래프 체크리스트

1. `EnemyAIController`의 즉시 기본 공격을 선행 상태와 실행 상태로 분리하되, 피해 권한은 계속 `CombatDriver`와 `Health`에 둔다.
2. 선행 중 적은 목표를 향하고, 플레이어가 읽을 수 있는 하나의 시각 신호를 보인다. 신호의 위치·크기·색은 Unity Editor에서 결정한다.
3. 실행 순간에만 사거리 재확인 후 피해를 한 번 적용한다. 플레이어가 범위를 벗어나면 피해 없이 취소한다.
4. Hero와 Orc의 실제 Animator가 선행/실행을 혼동하지 않는지 확인하고, 보상·저장·계약·드롭 수치는 건드리지 않는다.
5. Play Mode에서 `신호 확인 → 이탈해 회피 → 남아 피격 → 적 처치 후 기존 보상`을 확인한다.

## 확인 기준

- 외부 씬/프리팹 변경 뒤 Unity에서 다시 열어 누락 스크립트를 확인한다.
- 시각 또는 조작 변화는 Play Mode에서 확인한다.
- 문서만 바꾼 날에는 Unity 설정 확인을 반복하지 않는다.
