# Unity Setup Guide

## 원칙

코드가 권한과 재사용 규칙을 소유하고, Unity Editor는 모델·애니메이션·맵·카메라의 시각 판단을 완성한다. 자동 작업은 위치, 크기, 방 형태, 카메라 구도를 임의로 결정하지 않는다.

## E3-B: 모델과 Animator 연결

1. 현재 루트는 `Gameplay/Player`와 `PF_DungeonEnemy_Melee`다. 두 루트의 `CharacterActor`, `CharacterMotor`, `CombatDriver`, `Health`, `CombatAnimationDriver`는 보존한다.
2. 승인된 Hero/첫 적 리그 모델을 각 루트의 기존 `Model` 자식에 연결하고, 클릭용 Collider는 유지한다. 별도 장식 오브젝트나 두 번째 전투 주체를 만들지 않는다.
3. 각 리그의 `Animator`를 루트 `CombatAnimationDriver`에 지정한다. 자식에서 자동 탐색도 가능하지만, 최종 프리팹에서는 명시 지정한다.
4. Animator Controller의 기본 상태는 `Idle`이며, `MoveSpeed`(float)로 `Move`를 구동한다. `Attack`, `Hit`, `Death` trigger는 각각 해당 상태로 전이한다. 사망 뒤에는 `Health.Refill()` 전까지 Death 상태를 유지한다.
5. `CombatAnimationDriver`의 **Validate Animator Contract**를 실행해 네 파라미터를 확인하고, Play Mode에서 Hero와 첫 적 모두 Idle, Move, Attack, Hit, Death를 실제 전투 중에 확인한다.

현재 저장소에는 Hero/첫 적의 실제 리그·Animator 자산이 없고 캡슐 프록시만 있다. 필요한 입력은 PC용 사용 권한이 확인된 Hero와 근접 적의 Humanoid 또는 동일한 Generic 리그 모델, 다섯 상태를 담은 Animator Controller다. 임포트는 `Assets/03.Characters/Hero/`와 `Assets/03.Characters/Enemies/` 아래에 두며, 임의의 외부 에셋 구매·다운로드·배치는 하지 않는다.

## E3-D: 첫 던전 맵

- 구성: 입구 → 짧은 경로 → 전투장 → 보상/퇴장.
- 필수 연결: 충돌, NavMesh, Hero 입력, 적 스폰, 전투 종료, 보상, 실패 복귀.
- 수동 판단: 방 크기, 조명, 카메라 구도, 오브젝트 위치, 모델 스케일, 실루엣.
- 코드/프리팹 검증: 스폰 지점, NavMesh 배치, 필수 컴포넌트, 저장/복귀 계약.

## 확인 기준

- 외부 씬/프리팹 변경 뒤 Unity에서 다시 열어 누락 스크립트를 확인한다.
- 시각 또는 조작 변화는 Play Mode에서 확인한다.
- 문서만 바꾼 날에는 Unity 설정 확인을 반복하지 않는다.
