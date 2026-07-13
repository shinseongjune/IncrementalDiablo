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

### 다음 순번 — 담당자용 E3-B 에셋·Animator 연결 체크리스트

이 순번의 목표는 두 전투 주체가 실제 전투에서 `Idle`, `Move`, `Attack`, `Hit`, `Death`를 보이게 하는 것이다. 전투 판정·이동·저장 코드는 이미 연결되어 있으므로, 아래 외의 새 전투 주체나 수치 변경은 하지 않는다.

#### 1. 먼저 준비할 승인 자원

- Hero 1종과 근접 적 1종의 **PC 출시 사용 권한이 확인된** 리그 모델. 텍스처/머티리얼과 원본 사용 조건도 함께 보관한다.
- 각 모델에 대응하는 `Idle`, `Move`, `Attack`, `Hit`, `Death` 클립 5개. 한 FBX 안에 있어도 되고, 별도 FBX여도 되지만 같은 스켈레톤/Avatar를 공유해야 한다.
- Hero와 적 각각의 Animator Controller 1개. 권장 파일명은 `Hero_Combat.controller`, `MeleeEnemy_Combat.controller`이다.
- 리그는 Avatar 매핑이 오류 없이 되는 경우에만 `Humanoid`를 사용한다. 그렇지 않으면 모델과 5개 클립 전체를 같은 루트 기준의 `Generic`으로 통일한다. 한 Controller 안에서 Humanoid와 Generic 클립을 섞지 않는다.

#### 2. 프로젝트에 넣고 임포트할 위치와 설정

1. Unity `6000.4.4f1` 프로젝트에서 모델/클립을 다음 위치에 넣는다.
   - Hero: `Assets/03.Characters/Hero/`
   - 첫 근접 적: `Assets/03.Characters/Enemies/`
2. 모델 FBX의 **Rig** 탭에서 Hero/적 각각의 Avatar를 만든다. 분리된 클립 FBX는 해당 모델 Avatar를 참조하게 한다.
3. **Animation** 탭에서 정확히 다섯 클립을 노출한다. `Idle`, `Move`만 `Loop Time`을 켜고 `Attack`, `Hit`, `Death`는 한 번 재생되게 둔다.
4. 루트 이동이 들어간 클립은 제자리 재생이 되도록 정리한다. 실제 위치·회전은 루트의 `NavMeshAgent`와 `CharacterMotor`가 소유하므로 Animator의 **Apply Root Motion은 끈다**.
5. 첫 배치에서는 전투 루트의 스케일·위치·`NavMeshAgent`·`CapsuleCollider` 값을 바꾸지 않는다. 모델의 높이·방향·크기 조정은 가져온 리그 자식에서만 하며, 현재 캡슐 Collider(높이 2, 반지름 0.5)는 클릭/전투 기준으로 유지한다.

#### 3. Hero/적 Controller에 반드시 넣을 계약

두 Controller의 파라미터 이름과 형식은 대소문자를 포함해 아래와 같아야 한다.

| 이름 | 형식 | 코드가 보내는 시점 |
| --- | --- | --- |
| `MoveSpeed` | Float | 매 프레임 `NavMeshAgent.velocity.magnitude` |
| `Attack` | Trigger | 성공한 기본 공격 뒤 |
| `Hit` | Trigger | 피해를 입었지만 생존했을 때 |
| `Death` | Trigger | 체력이 0이 되었을 때 |

- 기본 상태는 `Idle`로 둔다. `Idle` ↔ `Move` 전이는 `MoveSpeed` 조건으로 만든다(초기 기준: `> 0.01` 이동, `<= 0.01` 정지).
- `Attack`, `Hit`은 Trigger로 각 일회성 상태에 들어간 뒤 Exit Time 후 `Idle`로 돌아오게 한다. 다음 프레임의 `MoveSpeed`가 필요하면 `Move`로 다시 전이한다.
- `Death`는 Any State에서 들어가고 나가는 전이를 만들지 않는다. 런타임의 `Health.Refill()`이 Animator를 기본 상태로 되돌린다.
- 피해·보상·발사체용 Animation Event는 이 순번에 추가하지 않는다. 실제 공격/피해 판정은 이미 `CombatDriver`와 `Health`가 소유한다.

#### 4. 씬과 프리팹에 실제로 연결할 곳

1. `Gameplay`의 `Player` 루트 아래 기존 `Model` 자식에 Hero 리그를 자식으로 넣는다. `Player` 루트의 `CharacterActor`, `CharacterMotor`, `CombatDriver`, `Health`, `NavMeshAgent`, `CombatAnimationDriver`는 그대로 둔다.
2. Hero 리그의 Animator에 `Hero_Combat.controller`를 지정하고 **Apply Root Motion을 끈다**. 그 Animator를 `Player` 루트의 `CombatAnimationDriver > Animator` 필드에 명시적으로 드래그한다.
3. `PF_DungeonEnemy_Melee`의 기존 `Model` 자식에 적 리그를 자식으로 넣고, 적 Animator에 `MeleeEnemy_Combat.controller`를 지정한다. 적 루트의 `CombatAnimationDriver > Animator`에도 같은 Animator를 명시적으로 지정한다.
4. 두 기존 `Model`에는 캡슐 `MeshRenderer`와 `CapsuleCollider`가 함께 있다. 새 리그가 정상 표시된 것을 확인한 뒤 캡슐 **MeshRenderer만** 끄고, `CapsuleCollider`와 `Model` 오브젝트는 삭제하지 않는다.
5. 리그 자식에 `CharacterActor`, `Health`, `CombatDriver`, `NavMeshAgent`를 중복 추가하지 않는다. 각 전투 주체의 권한은 기존 루트 하나만 가진다.

#### 5. 저장 전 Play Mode 확인

1. Hero와 적 루트에서 **CombatAnimationDriver → Validate Animator Contract**를 실행한다. Console에 네 파라미터 준비 완료가 각각 표시되어야 한다.
2. `Gameplay` Play Mode에서 Hero와 첫 적 각각에 대해 아래 다섯 상태를 실제로 확인한다.
   - 정지: `Idle`
   - NavMesh 이동: `Move`
   - 성공한 기본 공격: `Attack`
   - 사망 전 피해: `Hit`
   - 체력 0: `Death`가 유지되고 회복/재생성 후 `Idle`로 복귀
3. 모델이 CapsuleCollider와 크게 어긋나지 않는지, 클릭/추적/공격 거리와 NavMesh 이동이 기존처럼 동작하는지 확인한다. 누락 파라미터 경고나 리그/Avatar 오류가 있으면 저장하지 말고 해당 모델 또는 Controller 설정부터 고친다.
4. Hero 씬과 적 프리팹을 저장한 뒤, 사용한 모델/클립/Controller의 실제 경로와 위 다섯 상태 확인 결과를 다음 자동화 실행에 전달한다.

#### 이 순번에서 하지 않을 일

- 외부 에셋 구매·다운로드, 임의의 맵/카메라 배치, 전투 수치·드롭·저장 규칙 변경
- 캡슐 Collider 제거, 새 전투 주체 생성, 기존 전투 루트 컴포넌트의 자식 복제

## E3-D: 첫 던전 맵

- 구성: 입구 → 짧은 경로 → 전투장 → 보상/퇴장.
- 필수 연결: 충돌, NavMesh, Hero 입력, 적 스폰, 전투 종료, 보상, 실패 복귀.
- 수동 판단: 방 크기, 조명, 카메라 구도, 오브젝트 위치, 모델 스케일, 실루엣.
- 코드/프리팹 검증: 스폰 지점, NavMesh 배치, 필수 컴포넌트, 저장/복귀 계약.

## 확인 기준

- 외부 씬/프리팹 변경 뒤 Unity에서 다시 열어 누락 스크립트를 확인한다.
- 시각 또는 조작 변화는 Play Mode에서 확인한다.
- 문서만 바꾼 날에는 Unity 설정 확인을 반복하지 않는다.
