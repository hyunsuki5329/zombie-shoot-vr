# P13 실행 기록

계획: `docs/vr-fps-project-docs/12-gs-comparison-plan.md`.
설계: `docs/vr-fps-project-docs/11-gs-comparison-intro.md`.

## 2026-10-20 시작

- 기반 `c407d83157354d4d92cb307fbf5338550841bd19`: PR22가
  `codex/p06-boss-crawler`에 병합된 최신 게임+설계+인계 문서.
  원본 `33cffb8`과 Git tree가 `bfda3d5a12532d5921a104cab6a02c4cf99f5699`로 같다.
  `origin/main`은 `fc5ff33`으로 별도 최종 통합이 필요하다.
- 작업 브랜치 `codex/gs-comparison-intro`, 앱 관리 격리 worktree.
  원본 씬/사용자 미커밋 설정/WallFix는 수정하지 않는다.
- Desktop/Downloads에서 PLY/SPZ를 찾지 못해 사용자에게 파일 경로와 생성 정보를
  한 번 요청했다. 부재를 PC 전체 검색 결과로 확대하지 않는다.
- 기존 ZIP에서 누락 외부 자산/meta 670개만 복원했다. Library는 복사하지 않았다.
  새 Library 기준 시험 진행 중이며 과거 시험 수를 새 통과로 쓰지 않는다.

## 의존 계약 검토

| 생산 → 소비 | 계약 | 판단 |
|---|---|---|
| a → b/f | 고정 renderer/API/URP/XR, 실제 양안 인수 | a의 실기 완료 전 배포 설정 채택 금지 |
| b → c/d/e | 프로필/앵커 및 Load/Unload 수명 | 미확보 GS 대신 대역으로 논리만 시험 가능 |
| c → d/e | 단일 총기 컨텍스트/입력 시계 | RunPhase.Active로 미리보기 구현 금지 |
| d → e/f | 준비와 시작 분리, 완전 밝아진 후 시작1회 | 재도전의 기존 시작 위치 초기화는 유지 |
| e → f | 늦은 완료 정리 및 재진입 잠금 | coroutine 중단만으로 비동기 취소 완료라 하지 않음 |

Ruling: 이미 승인된 설계/계획을 실행하며 기획 재승인은 생략한다.
사용자의 명시 지시가 근거다. 실제 요구 변경이 필요하면 별도로 알린다.

Ruling: P13-a의 실제 GS/VR 인수 대기 중 데이터 독립적인 b~e 구현을 진행한다.
설계가 허용한 대역 시험만 사용하고 실제 GS 완료로 표시하지 않는다.
잘못 판단하면 실제 렌더러 통합 때 추가 수정이 필요하다.

## 작업 상태

- P13-a: 진행 중. 공식 후보 revision
  `2c6fed37da67a217367261fcfcd3316d34c73e76`, package1.1.1, MIT.
  Unity6 URP RenderGraph 필수, DX11 미지원, MSAA 미지원.
  기존 RenderGraph 호환모드0, 두 URP 자산 MSAA4여서 시험본 변경 필요.
  소스상 OnDisable에서 GPU 버퍼 Dispose를 호출하지만 실제 회수 검증은 아직 없다.
- P13-b: 대기. 실제 방 자료·척도/앵커 대응 미확인.
- P13-c: 데이터 독립 구현/자동 시험 완료, 독립 리뷰 진행 중. 실제 자산 연결은 대기.
- P13-d/e: 미착수.
- P13-f: 미착수. 실제 양안·기존 두 맵 새 API 인수·성능 미측정.

## 자료 수신·기준 시험 후속

- 사용자가 Downloads에 `room.ply`와 `school.ply`를 추가했다.
  [검사 증빙](evidence/gs-input-inspection.json): 각각177,390/414,624개
  XYZ/RGB 정점만 있고 Gaussian scale/rotation/opacity/SH 속성이 없다.
  사용자는 Mesh/Splat이 별개임을 확인하고 새로 스캔해 제공하기로 했다.
- 읽기 전용 PLY 사전 검사기는 Gaussian 헤더 후보 여부만 판정한다.
  실제 렌더러 임포트 성공을 판정하지 않는다. 시험7개 실패→통과.
- 독립 검토가 `--output`을 입력과 같게 지정하면 원본을 덮는 결함을 발견했다.
  재현 시험 실패 후 출력 파일을 독점 생성하도록 수정하여 기존 파일/별칭을 보호한다.
- 검토에서 남긴 제한: 전체 PLY 요소/정점값을 해석하지 않아 뒤쪽 요소가 잘못된
  정점 길이를 가릴 수 있다. 잘못된 element 문법은 일반 파싱 예외가 날 수 있다.
  이 도구는 완전한 파일 유효성 검사기가 아니며 최종 채택에는 렌더러 임포트가 필요하다.
- 새 Library EditMode108통과/개인 스캔1제외. PlayMode130통과/EnemyLab1실패:
  씬 언로드 후 남은 적 예상0/실제2. 같은 시험 단독 재실행은1/1통과.
  원인/수명 경계를 조사 중이며 전체 PlayMode 통과로 기록하지 않는다.
- 격리 GS 시험본의 고정 패키지 임포트/컴파일 및 DX12 초기화 성공.
  실제 렌더링·양안·GPU 회수·새 API의 기존 전투 인수는 아직 수행하지 않았다.

공식 근거:
[renderer](https://github.com/aras-p/UnityGaussianSplatting/tree/2c6fed37da67a217367261fcfcd3316d34c73e76),
[URP 통합](https://github.com/aras-p/UnityGaussianSplatting/blob/2c6fed37da67a217367261fcfcd3316d34c73e76/docs/render-pipeline-integration.md).

## 구현·자동 시험 후속

- PR23 입력 검사 병합 `88ed8c7`, PR24 시험장 소유권 수정 병합 `c0f1c9d`.
  둘 다 `codex/p06-boss-crawler`가 대상이며 원격 CI/승인 리뷰는 없었다.
  별도 에이전트의 정적 검토 결과를 GitHub 승인으로 표현하지 않는다.
- 프로필 검증 및 첫 유효 바이러스 피격 처리: 새 시험13개 중 구현 전12실패,
  구현 후 전체 EditMode121통과/개인 스캔1제외.
- 총기의 Disabled/Preparing/Intro/Combat 컨텍스트를 분리했다. RunCombatBindings는
  단일 router를 사용하고 K2HandInput/피드백도 같은 컨텍스트를 따른다.
  미리보기는 RunPhase.Active를 시작하지 않는다. 컨텍스트 변경은 발사 시계를
  재설정하고 실제 입력에서 트리거 해제를 요구한다.
- 새 무기 규칙3개 실패→통과. 실제 Raycast/벽 가림/정지 후 트리거/기존 피드백 시험
  포함 전체 PlayMode136/136. 최종 EditMode124통과/1제외.
  [증빙](evidence/intro-weapon-context.json).
- 중간 EditMode의 기존 RecordStore 복구 저장 시험이1회 실패했다. LastError를
  실패 메시지에 포함한 재실행은 통과했다. 저장 런타임 변경은 없으며 원인을
  확정하거나 저장 결함을 수정했다고 주장하지 않는다.
- 실제 관찰 씬/바이러스 외형·메뉴·암전·전투 연결과 새 VR 인수는 미완료다.
  관찰 중 회전 권한은 d에서 연결한다. 현재 단위만 게임 완성으로 표시하지 않는다.
- 원본 두 씬·사용자 설정2개·WallFix 실행파일은 시작 시점 SHA256과 모두 같다.

## 무기 컨텍스트 독립 리뷰

- 일반 Preparing→Combat에도 트리거 해제를 요구하던 회귀(P2)를 새 실제 입력 시험으로
  재현했다. 해제 요구를 Disabled/Intro 경계로 제한하여 기존 전투 준비의 입력을 유지한다.
- Ruling: 정지 복귀가 남은 발사 간격을 지우는 의견은 기존 규칙 보존 요구에 해당해
  수정 대상으로 판단했다. 같은 소유자의 정지 복귀0.01초 뒤 재발사 허용을 RED로 확인하고,
  시계 소유자(Intro/Combat)가 바뀌거나 새 도입부를 시작할 때만 간격을 재설정한다.
  잘못 판단하면 기존 연사 간격이 달라지므로 관련 시험으로 고정했다.
- 실제 LeftHandInteractor→소켓→보급→삽입→앞부분 재잡기 경로의 Intro 시험을 추가했다.
  미리보기 재장전에서 RunPhase.Dead를 유지하고 보급/삽입 피드백을 검사한다.
- 경미한 제한: router를 다른 총에 재설정하면 이전 입력의 참조가 남을 수 있다.
  현재 게임 구성은 세션마다 한 번 설정하며, 여러 총으로 교체하는 기능은 없다.
  IntroTarget 전용 레이어/실제 관찰 씬 마스크 연결은 자산 통합 단계에 남아 있다.

- 추가 재장전 시험의 종료에서 총이 먼저 파괴되면 router가 파괴된 참조에 접근하는 실패를 확인했다.
  Unity null 검사로 수명 경계를 보호한 뒤 전체 PlayMode138/138통과.
  실제 VR과 GS 공간의 결과는 여전히 미검증이다.

## P13-d 준비 단계 분리 진행

- PR25 draft: https://github.com/hyunsuki5329/zombie-shoot-vr/pull/25 (64700c2), c 자동 시험과 독립 리뷰 수정 포함. 실제 VR 미수행으로 병합하지 않았다.
- 후속 브랜치는 PR25 위에 쌓은 codex/p13-prepared-combat이다.
- 준비 중 RunStarted0, 첫 시작만1회, 준비8초·HP100·30발·기록0, 첫 시작 위치 유지와 재도전 기본 스폰 시험을 추가했다. 기존 구현 래퍼에서 새 시험2실패를 확인한 뒤 CombatSession을 분리했다. 관련4개 시험 통과; 전체 회귀는 아직 실행 전이다.
- Ruling: PlacePreparedPlayer의 relativeYaw는 관찰 앵커에 대한 리그 yaw 차이로 전달한다. 테마 앵커 yaw에 이를 더하고 HMD 로컬 pose는 보존하여 실제 관찰 방향도 같은 차이로 이어진다. 호출자가 HMD yaw를 중복 적용하면 방향이 어긋나므로 전환 통합 시험에서 별도 확인한다.
- 회전만 허용하는 리그 권한 분리 시험 진행 중. 암전·메뉴·비동기 취소 연결은 아직 미완료다.

- 준비·회전 구현 후 전체 PlayMode141/141통과. 독립 리뷰에서 시작 예외 복구와 새 앵커 유효성 검사 누락(P2)2건을 받아 수정 중이다.
- 시작 이벤트 예외를 주입한 시험은 실패를 재현했다. BeginPreparedRun은 오류/비활성 상태를 반환하고 기존 StartMap 래퍼는 소유 런타임·맵을 Shutdown으로 정리하도록 보완했다.
- Ruling: 테마 앵커는 현재 MapRoot 하위의 활성 Transform이어야 하며 동일 agent의 NavMesh와 충분히 먼 도달 가능한 적 후보를 검사한다. 기본 스폰이 검증됐다고 새 위치도 안전하다고 간주하지 않는다. 프로필은 기존 playerSpawn을 참조할 수 있어 맵 배치를 변경할 필요는 없다.

- 리뷰 보완 후 최종 PlayMode143/143, EditMode124통과/개인 스캔1제외. [증빙](evidence/prepared-combat.json). 원본 맵2개·사용자 설정2개·WallFix 실행파일 SHA256 보존 확인.
- 경미한 시험 여지: 회전 권한은 실제 리그 시험으로 검증했고 router의 연결은 정적 확인했다. 전체 IntroController 상태 시험에서 router를 포함한 통합을 검증할 예정이며 아직 수행했다고 표기하지 않는다.

## P13-d 화면 페이드 단위

- PR26 draft: https://github.com/hyunsuki5329/zombie-shoot-vr/pull/26 (d79f6c0). 후속 codex/p13-screen-fade에서 진행.
- 시간 배율0에서도 unscaled 진행, 일시정지 알파 고정, 취소·SetImmediate의 이전 요청 무효화, 잘못된 값 거부 시험3개 실패→통과.
- 카메라 내부 구체와 ZTest Always/queue4999 셰이더로 월드·가까운 물체를 덮는 시각 구현을 추가했다. stereo instancing 매크로를 사용하지만 실제 양안 결과는 아직 미검증이다.
- 단안 GPU(DX11/RTX4060Ti) 시험에서 배경+가까운 cube→모든 픽셀 검정→복원 및 시각 오브젝트 해제까지4/4통과. 실제 총/손, GS 합성, 메뉴의 상위 순서, XR 인수로 확대 해석하지 않는다.
- Ruling: 페이드 전용 셰이더 queue4999를 예약하고 메뉴는 후속 통합에서 이보다 위에서 그려야 한다. 현재 메뉴가 페이드 위에 보인다고 주장하지 않는다. 카메라 레이어/클리핑 조건을 검증하며 설정 변경 시 재구성이 필요하다.

- 전체 graphics PlayMode147/147통과. 독립 리뷰 중요 결함0, 경미한 FadeTo 간 대체 시험 제안을 반영했다. 이후 DX12 대상 시험4/4통과. [증빙](evidence/screen-fade.json). 전체147 시험은 추가 대체 assertion 전이며 런타임 코드는 동일하다.

## P13-e 비동기 작업 수명 기반

- PR27 draft: https://github.com/hyunsuki5329/zombie-shoot-vr/pull/27 (e5bb4da). 후속 codex/p13-transition-lifecycle.
- IntroAsyncOperation은 요청자와 독립된 임시 DontDestroyOnLoad 호스트에서 중첩 IEnumerator를 끝까지 실행한다. MoveNext/Current/Dispose 오류를 기록하고 열거자 finally와 호스트 반환을 보장한다.
- 요청자 파괴 및 중첩 오류 시험2실패→2통과. 실제 SceneManager 비동기 로드→언로드 중 요청자 파괴 시험 포함 전체 graphics PlayMode150/150통과. [증빙](evidence/intro-operation-drain.json).
- 독립 읽기 검토 중요 결함0. Controller/세대 무효화/timeout/메뉴 정리 연결은 아직 미완료다. 작업 대기를 멈추는 것을 실제 씬 취소로 오인하지 않도록 이 단위를 먼저 구성했다.
- Ruling: 어댑터는 로드 코루틴을 IEnumerator로 반환한다. 별도 StartCoroutine에서 던진 예외는 Unity가 소유하므로 이 drain의 오류 포착 범위로 주장하지 않는다. 실제 GPU 자원 해제 증명은 렌더 어댑터 통합 단계에 남아 있다.
