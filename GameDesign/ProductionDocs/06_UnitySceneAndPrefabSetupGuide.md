# Unity Setup Guide

## 원칙

코드가 권한과 재사용 규칙을 소유하고, Unity Editor는 모델·애니메이션·맵·카메라의 시각 판단을 완성한다. 자동 작업은 위치, 크기, 방 형태, 카메라 구도를 임의로 결정하지 않는다.

## E3-B: 완료된 모델·Animator 연결

- Hero `HeroDefault`와 적 `OrcPADefault`는 기존 `Player`/`PF_DungeonEnemy_Melee` 루트의 `Model` 자식이다. 전투·이동·Collider 권한은 루트에만 둔다.
- 공통 상태기 `Assets/06.Art/Animations/Combat.controller`가 `MoveSpeed`(Float), `Attack`·`Hit`·`Death`(Trigger)와 다섯 상태를 소유한다. Hero/Orc는 `Assets/03.Characters/*/*_Combat.overrideController`로 클립만 바꾼다.
- 시각 Animator를 각 루트의 `CombatAnimationDriver`에 명시 지정하고 Root Motion은 끈다. 캡슐은 `MeshRenderer`만 숨기며 Collider는 보존한다.
- Play Mode에서 Hero와 Orc의 Idle, Move, Attack, Hit, Death를 확인했다. 이 연결은 회귀 전용이다.

## E3-C: 다음 Unity Play Mode 체크리스트

- [ ] 스크립트/프리팹을 다시 불러온 뒤 `PF_DungeonEnemy_Melee`의 `EnemyAttackTelegraph`가 주황색 펄스 링을 표시하는지 확인한다.
- [ ] 링이 지면과 겹치고 읽히는지만 본다. 필요하면 Inspector의 `ringHeight`, `ringWidth`, `ringColor`만 조정한다.
- [ ] `신호 → 이탈해 회피 → 남아 한 번 피격 → 적 처치 후 기존 보상`을 한 번의 Play Mode에서 확인한다.
- [ ] 보상·저장·계약·드롭 수치와 Animator Controller는 수정하지 않는다.

## 확인 기준

- 외부 씬/프리팹 변경 뒤 Unity에서 다시 열어 누락 스크립트를 확인한다.
- 시각 또는 조작 변화는 Play Mode에서 확인한다.
- 문서만 바꾼 날에는 Unity 설정 확인을 반복하지 않는다.
