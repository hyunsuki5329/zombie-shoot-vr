# 사용자 배치 동기화와 시점 높이 보정 — 2026-10-16

사용자가 다시 저장한 Room/Classroom을 기준으로 처리했다. 가구 위치·회전·크기의 전후 스냅샷이 일치하며, 이번 작업에서는 이전 배율 도구를 실행하지 않았다. 새 외형에 맞춰 MeshCollider와 NavMesh를 동기화했고 RoomHousePreview도 맞췄다.

폐교 교탁 앞 빈 바닥의 원인은 MapRoot 바로 아래 남아 있던 보이지 않는 예전 충돌체였다. Teacher desk (1), Teacher chair (1), Board (1)/(2)/(3)의 옛 충돌과 NavMeshModifier를 제거했다. 현재 외형과 이름이 일치하고 렌더러가 없는 최상위 중복 객체에 한정하며 다른 컴포넌트·Transform은 남긴다. 실제로 보이는 가구의 충돌은 CollisionRoot에서 다시 생성한다. 과거40cm 자동 이동 단계는 제거하여 이후 수동 배치를 다시 밀지 않는다.

수정 전 Classroom (0,0,2.4)에서 NavMesh가 없어 실패했다. 수정 후 앞쪽5지점(-8,-4,0,4,8; z2.4)의 표면·플레이어 연결, 두 맵 활성 WorldSolid 충돌의 CollisionRoot 소속, Room7/7·Classroom8/8 생성점 연결을 확인했다. 실제 가구가 차지하는 공간은 계속 막는다.

시점은 키 기준1.15배다. PlayerRig의 Camera Offset 아래 View Height Offset을 넣어 원시 HMD 높이와 런타임 바닥 오프셋의15%만 위로 더한다. 손·총·팔UI도 같은 만큼 올라가며 머리·손의 로컬 추적값, 리그 크기, 수평 이동량, 점프 높이는 바꾸지 않는다. 바닥 기준1.6m 시점은1.84m가 된다. 원시 높이를 읽기 때문에 반복 프레임/점프가 누적 확대되지 않고 숙일 때도 비례한다. 실제 헤드셋의 높이 체감은 사용자 인수 대기다.

검증: 높이 보정 부재·교탁 앞 경로 실패를 먼저 재현했다. 최종 EditMode106통과/개인 스캔1제외, PlayMode131/131. 프리팹 연결·참조, 숙이기·바닥 오프셋·점프 비누적과 기존 정지/사망/재도전 시험 포함. 로그 `tmp/layout-height-20261016/`. 독립 소스 검토에서 차단 결함은 발견되지 않았다.

Windows 빌드 GUID `2b1adfac2c9e49e6bb796bd133039e2f`, 기존 `Builds/Windows/AbandonedSchool/AbandonedSchool.exe` 경로에350파일 복사·해시 일치 확인. [검증 증빙](evidence/layout-height-20261016.json).
