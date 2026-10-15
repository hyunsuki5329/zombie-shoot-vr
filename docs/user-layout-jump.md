# 2026-10-15 사용자 편집 배치와 점프

사용자가 저장한 Room.unity와 Classroom.unity를 기준으로 작업했다. 수정 전 두 씬은 `tmp/user-layout-20261015/`에 보존했다. 현재 씬이 가구 배치의 기준이며, 이전 RoomHouseSetup/GrannyHouseSetup/ClassroomSetup.Configure로 새 씬을 만들면 사용자 배치를 잃는다. 기존 구조 자산은 최초 구조 자료로 남긴다.

- 가구의 실제 MeshFilter 위치·회전·크기에 맞춰 별도 CollisionRoot MeshCollider를 동기화했다. 이동한 의자의 예전 위치에 남아 있던 충돌을 제거했고, 추가된 교탁·칠판도 충돌에 포함한다.
- 폐교의 학생 책상·의자 묶음은 사용자가 저장한 방향·상대 배치를 유지한 채 Z음수 방향으로40cm 이동했다. 새 교탁 앞 통로가 기본 적 NavMesh 반경0.5m에서도 연결되도록 확보한 간격이다. 의자를 밀어 넣은 변경도 반영했다.
- NavMesh를 다시 구워 Room7개·Classroom8개 생성점의 시작점 연결을 확인했다. 플레이어 반경0.2m 캡슐로 폐가 예전 의자 자리, 교탁 옆, 학생 가구 사이4개 통로를 검사했다.
- 오른손 A(primaryButton)에 이미 연결된 Jump 액션을 PlayerRigController에서 사용한다. 접지 상태에서 새 누름 때 한 번 도약한다. 점프 높이2.2m는 확대된 침대에 올라갈 수 있도록 설정했다. 공중 재점프와 정지/사망 중 요청은 차단하며 머리의 로컬 추적 자세를 변경하지 않는다.

검증: 추가 점프 시험은 구현 전 실패, 구현 후1.6m 높이 가구 착지·공중 재점프 거절·정지 차단·착지 후 재점프를 통과했다. 최종 EditMode100/100, PlayMode130/130. 실제 헤드셋의 A입력·점프 체감·가구 위 전투는 사용자 확인이 필요하다.

재동기화는 저장된 두 씬과 동일한 외부 자산 GUID를 갖춘 격리 프로젝트에서 `VRFPS.Editor.UserLayoutSetup.Configure`를 실행한다. 간격 보정 마커가 있어40cm 이동은 반복 적용되지 않는다. 미술을 다시 생성하는 도구가 아니며 외형은 현재 씬에서 읽는다.

실행 빌드: `Builds/Windows/AbandonedSchool/AbandonedSchool.exe`, GUID `2a41ebb5739b4386b8b2b55ca4c66eca`. 테스트 로그와 XML은 `tmp/user-layout-20261015/`에 보존했다.
