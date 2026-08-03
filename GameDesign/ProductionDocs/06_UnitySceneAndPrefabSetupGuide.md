# Unity Setup Guide

## 현재 경계

E3-D의 최종 형태는 `Gameplay`에 고정된 두 방을 배치하는 것이 아니다. 중립적인 `crypt_a`/`crypt_b` additive 방 Scene, 각 방 전용 NavMesh, `Gameplay/DungeonRoot`의 `DungeonRoomLoader` 카탈로그와 Build Settings 연결은 준비되었다. 기존 고정 경로, `EnterRoom`, `ReturnToEntrance`를 새로 배치하지 않는다.

## 방 템플릿 제작 체크리스트

다음은 이후 템플릿을 추가하거나 Play Mode를 확인할 때 Unity Editor에서 수행한다.

1. `DungeonRoom_Crypt_A`와 `DungeonRoom_Crypt_B`를 열어 루트 `DungeonRoomTemplate`의 `crypt_a`/`crypt_b` ID, 입구·귀환 포탈·심층 출구·적 앵커를 확인한다. 새 방도 같은 계약으로 additive Scene으로 만든다.
2. `Return Portal` 앵커 위치에 Trigger Collider와 `ReturnPortal`을, `Deeper Exit` 앵커 위치에 Trigger Collider와 `DeeperExit`을 추가한 뒤 각 컴포넌트를 템플릿의 동명 필드에 연결한다. 포탈 Mesh/VFX는 각각 `Active Visuals`에 연결해 방을 정리하기 전에는 보이지 않게 한다.
3. 적·오브젝트·장애물 앵커를 템플릿 안에 둔다. 입구와 두 출구의 이동 경로를 침범하는 앵커는 만들지 않고, 방 전용 NavMesh를 굽는다.
4. `Gameplay`의 `DungeonRoot > DungeonRoomLoader`에서 `ExpeditionDirector`, Player, 영구 거점의 `Return To Hub Point`, `crypt_a`/`crypt_b` Scene Path를 확인한다. 새 additive Scene도 카탈로그와 Build Settings에 함께 넣는다.
5. 같은 `DungeonRoomLoader`를 `CombatRoom > Additive Room Gate`와 `EnemySpawner > Additive Room Spawn Setup`에 연결하고 두 Require 옵션을 켠다. 최종 additive 원정을 시험할 때는 전환용 `DungeonTraversalController`를 비활성화해 자동 전투 시작을 막지 않게 한다. 로더가 연결된 동안 기존 `Spawn Points`는 사용되지 않으며, 생성 적은 템플릿 루트에 속한다.
6. 서로 다른 템플릿 둘 이상을 카탈로그에 넣고, 시작·저장·불러오기에서 같은 템플릿 ID·입구 위치·적 앵커가 복원되는지 확인한다. 방을 정리한 뒤 ReturnPortal은 보상을 확정하고 거점으로 돌려보내며, DeeperExit은 같은 미확정 보상으로 다음 방을 한 번만 로드하는지 확인한다.
7. `Camera_DungeonPanel`의 16:9 Game View에서 입구와 북쪽의 ReturnPortal/DeeperExit 영역이 동시에 보이는지 확인한다. 북쪽 출구를 HUD 바깥이나 카메라 밖에 두지 않는다.

## 확인 기준

- 템플릿의 미적 배치·조명·카메라 판단은 Unity Editor에서 한다. 코드가 임의의 방 구조나 장식 위치를 결정하지 않는다.
- 런타임 방 계획, 저장, 보상, 전투 권한은 코드 계약이 소유한다.
- 시각 또는 조작 변경은 Play Mode에서 확인한다.
