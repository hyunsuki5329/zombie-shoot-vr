# P09 폐교 자산 분리 계획

사용자 지정 kiyosueshi의 abandoned school 자산을 실제 강의실 스캔 배치에 사용한다. 원본 건물 배치를 게임 맵으로 대체하지 않는다. 세 스캔 돌출부는 사용자 지시에 따라 제외하고 실제 길이는 확인 대기다.

1. SchoolAssetsTests: 필요한 6개 시각 프리팹과 UV 범위, URP 재질, 원본 FBX 비의존 검증. 없는 Desk로 실패 확인.
2. SchoolAssetsSetup: 책상·의자·교탁·칠판·벽·바닥의 지정 계층만 추출. 삼각형의 UDIM 타일에 따라 메시/재질을 분리하고 UV를 0~1로 옮긴다. 원본 비율을 보존하고 피벗을 바닥 중앙으로 정리한다. 라이브러리의 임시 기준 크기와 실측 배치를 구분한다.
3. 격리 Unity에서 생성·시험·렌더 검토. 원본/추출 자산 로컬 보관, 출처와 복원 경로 기록. 코드/검증/문서만 커밋·push하고 기존 초안 PR에 연결한다.
4. 실측 치수 확정 후 별도 작업으로 Classroom 구조·충돌·NavMesh·스폰·프리팹 배치, 공통 전투·빌드 연결, Quest Pro 검증. 이 라이브러리 작업을 맵 완성으로 판정하지 않는다.

부드러움은 초기 상수0.12, 금속도0이다. BaseColor/Normal을 연결한다. Roughness/Metalness/Height 정밀 변환과 최종 시각 판정은 미완료로 공개하며 성능 측정 후 조정한다.

## 로컬 재생성

Unity6000.3.23f1에서 사용한다. 사용자 ZIP의 `source/`와 `textures/`를 `Assets/VRFPS/External/AbandonedSchool/` 아래에 복사하고 격리 batch editor에서 `VRFPS.Editor.SchoolAssetsSetup.Configure`를 실행한다. ZIP의 SHA256은 [출처](licenses/AbandonedSchool-Attribution.md)와 대조한다. `Generated/`에 독립 메시·재질·6개 프리팹이 생긴다. 이 단계에는 아직 씬에서 참조하는 GUID가 없으며 최종 Classroom 연결 시 복원 manifest를 추가해야 한다.

현재 표준 높이는 책상0.75m, 의자0.9m, 교탁0.8m, 칠판1.2m, 벽3m이며 바닥의 긴 변은10m다. 이는 라이브러리 기준 크기로 실측 치수가 아니다. 각 모델은 균일 배율로 처리하고 가구 비율은 보존한다. 별도 충돌/게임 플레이 컴포넌트는 넣지 않는다.

칠판 원본의 `whitebord` 그룹은 떨어진 부품을 포함하므로 완성된 배치로 사용하지 않는다. `whitebord/frount_whitebord1` 판면만 선택한다. 임시 렌더 검사로 그룹 전체 및 프레임 단독 선택을 배제했다.
