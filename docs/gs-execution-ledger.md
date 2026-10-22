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

## P13-d/e 전환 Controller 통합

- PR28 draft: https://github.com/hyunsuki5329/zombie-shoot-vr/pull/28 (72d5b6f). 후속 codex/p13-comparison-controller.
- IRealityPresentation 경계와 ComparisonIntroController를 구현했다. 현실 준비→첫 유효 명중→암전→현실 해제→테마 준비/배치→밝아짐→새 전투1회 경로다. 실제 CombatSession과 fake 현실 어댑터를 사용하는 시험을 RED→GREEN으로 진행했다.
- 정지 시 도입부 시계/명중 차단, 중복 명중·새 진입 거부, 설정된 단계 제한, 늦은 현실 로드 정리, Controller 파괴 경계를 시험했다. 테스트 timeout은 빠른 주입값 .03초이며 실제 기본30초 기다림을 실행했다고 주장하지 않는다.
- 독립 리뷰 P2 3건: 정리 실패 후 영구 Busy, 페이드 중단을 성공으로 오인, 전체 소유 씬 파괴 시 테마 누수. 각 실패 시험을 확인한 뒤 정리 재시도·알파/활성 검사·독립 테마 해제로 수정했다.
- 추가 자체 검토: 취소된 PrepareMap 완료가 기본 스폰 배치를 실행하던 경계를 RED로 확인했다. 선택적 canCommit predicate가 false면 런타임 생성/리그 배치 전에 소유 맵을 해제한다. 기존 호출은 호환된다.
- Ruling: 정리 실패 시 새 Begin은 잠그고 Shutdown으로 잔여 정리를 재시도한다. 실제 리소스가 해제됐는지 모르는 상태에서 Busy를 무조건 해제하지 않는다. 메뉴는 후속 연결에서 재시도·오류를 표시해야 한다.
- Ruling: 프로필 themeAnchorKey=playerSpawn은 기존 맵 시작점을 사용한다. 다른 키는 현재 MapRoot 하위 이름이 유일해야 하며 기존 PlacePreparedPlayer의 소유/NavMesh/도달성 검증을 통과해야 한다. 사용자 씬 배치는 변경하지 않는다.
- 최종 graphics PlayMode155/155통과. 실제 GS 어댑터/자료, IntroTarget 마스크/충돌 레이어, 메뉴 소유권·오류 표시, 양안·전체10회 수명 실기는 아직 미완료다. fake 현실 시험을 GS 렌더 검증으로 대체하지 않는다.

- 최종 EditMode124통과/개인 스캔1제외. [전환 증빙](evidence/comparison-controller.json).

## P13-d/e 메뉴 소유권·정리 재시도

- PR29 draft: https://github.com/hyunsuki5329/zombie-shoot-vr/pull/29 (1ebf4ac). 후속 codex/p13-intro-menu.
- Intro의 Run 종료가 Result를 열던 경계를 RED로 재현하고 메뉴 상태 소유권으로 수정했다. Intro 정지 중 전투 Retry를 숨기고 정리 실패 시 MapSelection에서 정리 Retry를 제공한다. 정리 완료 전 새 맵/현실 진입은 잠근다.
- SelectReality는 자료/바인딩 누락을 명시하며 정상 진입과 전투 인계, 정지·설정·취소를 시험했다. 독립 리뷰 중요 지적0; 제안된 정상 entry 경로와 실제 Retry 버튼 시험을 추가했다.
- 최종 graphics PlayMode156/156, EditMode124통과/1제외. [증빙](evidence/intro-menu.json). 실제 GS 버튼/씬 바인딩·메뉴 상위 렌더링·VR 인수는 미완료이며 fake 어댑터 시험을 실제 GS 검증으로 확대하지 않는다.

## P13-d 메뉴 상위 렌더링

- PR30 draft: https://github.com/hyunsuki5329/zombie-shoot-vr/pull/30 (3814bd0). 후속 codex/p13-menu-overlay.
- 실제 Menus 프리팹 GPU 시험에서 검정 위 패널0픽셀 실패를 재현했다. 기존 추적 중인 XRI UI/NoZTest 셰이더 + queue5000 재질을 사용하여 페이드4999 이후 그린다. 비활성 설정/안내 UI까지 포함하고 runtime Material을 생성하지 않는다. 재생성 MenuSetup도 같은 자산 참조를 유지한다.
- DX12 전체 PlayMode157/157통과. 이후 시험만 보강해 버튼 이미지를 제외하고 폰트와 패널·외부 검정을 각각 확인, 대상5/5통과. 독립 정적 리뷰 중요 지적0 및 프리팹/재질/셰이더 GUID 연결 확인.
- [증빙](evidence/menu-overlay.json). Player 빌드 포함·XR 양안·실제 GS 합성과 암전 중 컨트롤러 메뉴 조작은 아직 미검증이다. 원본 보존5개 SHA256 재확인.

## P13-b 바이러스 로컬 자산

- PR31 draft: https://github.com/hyunsuki5329/zombie-shoot-vr/pull/31 (5dbbd74). 후속 codex/p13-virus-assets.
- source color.png 색상, color11.png 노멀을 직접 이미지 확인했다. roughness→smoothness alpha 변환, URP Lit·크기0.8m·중앙정렬·비-trigger 반경0.32m의 재생성 utility를 작성했다. 기하 간소화는 아직 수행하지 않았다.
- 자산 미존재 RED→전체 EditMode125통과/1제외. Preview의 unsaved-scene 오류를 확인하고 독립 PreviewScene으로 변경 후 DX12 렌더 이미지를 확인했다. 독립 정적 리뷰 중요 지적0.
- private 생성물/meta 백업 CRC/SHA 확인 및 고지 파일 추가. [증빙](evidence/intro-virus-assets.json). layer/mask/GS 배치·양안·최종 빌드/복원은 미완료다.

## P13-c 전용 레이어·사격 마스크

- PR32 draft: https://github.com/hyunsuki5329/zombie-shoot-vr/pull/32 (54191dc). 후속 codex/p13-intro-hit-layers.
- IntroTarget 미정의, 전용 표적 명중 누락, Intro의 Enemy 피격3건을 RED로 확인했다. layer13을 정의하고 물리 접촉 비활성, Intro 전용 worldSolidMask|IntroTarget을 적용했다. Combat의 직렬화 마스크와 총구 WorldSolid 검사는 보존했다.
- 실제 private 모델 Raycast 시험까지 DX12 PlayMode161/161, EditMode125통과/1제외. 독립 검토에서63개의 layer13 관련 행렬 비트 외 변화0 확인, 중요 지적0. [증빙](evidence/intro-hit-layers.json).
- 재임포트 후 기존 meta SHA 유지 확인. 최신 생성물은 로컬 p13-virus-layer-20261021 ZIP에 CRC/SHA 검증해 보관했다. 실제 GS 충돌 배치·VR·빌드 검증은 별도로 남는다.

## P13-b 관찰 씬 계약

- PR33 draft: https://github.com/hyunsuki5329/zombie-shoot-vr/pull/33 (714801a). 후속 codex/p13-reality-scene-contract.
- RealitySceneRoot는 profile과 map/dataId 일치, 소유활성 참조, 유일 anchor key, root/anchor unit scale와 dataRoot의 profile 보정 scale, 선언한 non-trigger 충돌, 씬 전체의 중복/금지 component를 확인한다. actual GS renderer readiness와 구분하며 아직 로더에 연결하지 않았다.
- 첫 시험은 EditMode에서 PlayMode용 씬 생성 API를 사용한 setup 오류였고, 이를 기능 RED로 세지 않는다. PreviewScene으로 수정한 뒤 valid1통과/invalid8실패를 확인했다.
- 독립 리뷰 P2: 고정 절대 오차가 작은 scale에서 축0·두배·음수를 허용. 세 사례9통과/3실패 RED 후 양수/상대 오차로 수정. 최종 EditMode137통과/1제외. [증빙](evidence/reality-scene-contract.json).
- 실측 배치/GS identity 확인·렌더 준비·로더 연결·XR/빌드는 남아 있다. 이 구성 validator를 실제 GS 인수로 대체하지 않는다.

## P13-b/e 관찰 씬 로드·렌더 수명 경계

- PR34 draft: https://github.com/hyunsuki5329/zombie-shoot-vr/pull/34 (bfed8d4). 후속 codex/p13-reality-loader.
- Downloads 및 하위에서 새 PLY/SPZ/SPLAT 없음 재확인. 사용자 재촬영 회신을 기다리며 질문을 반복하지 않았다.
- SceneRealityPresentation은 경로 예약·소유씬·root.Validate·단일 활성 RealityRenderSource의 dataId/ready/resources 확인을 수행한다. Release는 초기화 전/부분 실패 후에도 idempotent여야 하고 HasResources=false를 확인해야 씬 해제한다. 실제 GS bridge는 아직 없다.
- 파일은 그대로 둔 기존 fixture 씬 shell을 로드 콜백에서만 시험 구성으로 변환하여 실제 SceneManager 경로를 시험했다. 초기4실패→4통과. 구성/renderer identity/readiness 실패, 남은 자원, 외부 씬 보호, caller IEnumerator 폐기 후 실제 작업 지속을 포함한다.
- 독립 리뷰 P2: StartCoroutine의 첫yield 전 Release 콜백 재진입.7통과/1실패로 재현하고 starting guard + current.IsDone 잠금으로 수정했다. wrapper 폐기가 실제 작업 잠금을 해제하지 않는다.
- 최종 DX12 전체 PlayMode169/169. 실제씬10회 반복 후 sceneCount 기준 복귀 확인; fake resource flag를 실제GS GPU/양안/성능 검증으로 확대하지 않는다. [증빙](evidence/reality-scene-loader.json). 관찰씬/패키지bridge/Bootstrap 연결은 후속이다.

## P13-b 실제 GS package bridge

- base9395cd9 → f535106, [PR36 draft](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/36). pinned2c6fed37(1.1.1), 기존 package 버전 변화0. 기존 게임 pipeline 설정은 수정하지 않았다.
- Ruling: 별도 합성 시험 pipeline에서 실제 bridge를 먼저 검증하고 운영 pipeline 선택/복원은 다음 단위에서 연결한다 — 원본 게임 MSAA4/API를 보존하며 자원 수명을 독립 검증하기 위함 — 비용은 다음 연결 단위가 끝날 때까지 메뉴에서 실제 GS를 시작할 수 없다는 점이다.
- RED3건→GREEN3건, 추가 구성/비활성화/외부 소유/가림/암전 시험11통과. 독립 리뷰 P2 sorter invalid cache는11통과/1실패 RED, view buffer 누락은0통과/1실패 RED. Release에서 sorter cache 초기화, Initialize/IsReady에서 필수8buffer+4scratch+5objects+등록/정렬기 유효성 검사를 적용했다.
- 최종 DX12 PlayMode182/182, EditMode137통과/1제외. 18자원손상 변형, 실제합성GS10수명회,opaque depth/fade 복원 포함. [증빙](evidence/gs-renderer-bridge.json). 정적 리뷰는 GitHub 승인이나 실기 인수가 아니다.
- 임포터 종료 Persistent allocation1 경고의 원인은 아직 진단하지 않았다. Vulkan/Player stripping/실제방/XR/최종복원 미검증. 검증대상은 runtime owned GPU fields이며 전체VRAM/성능 측정이 아니다.

## P13-d GS pipeline 수명 연결

- base2000540 →2d98a8c, [PR37 draft](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/37). GsPipelinePresentation은 기존 IRealityPresentation을 감싸고, GS load 전에 quality override/카메라 renderer0을 선택한다. 내부 Unload 성공 뒤 원래 quality slot/index를 복원하며 default pipeline/asset 자체를 수정하지 않는다.
- 최초6실패→6통과. 확장9통과/1실패는 failed Unload 후 stale ready; Unload 시작 시 ready=false로 수정. 최종 DX12 PlayMode192/192, actual synthetic GS pixel→기존 pipeline 렌더 복원 포함. [증빙](evidence/gs-pipeline-scope.json).
- caller Dispose에도 persistent drain/잠금 유지, owner 중복과 동기 재진입 거부, 실패시 cleanup retry, qualitylevel이 바뀌어도 원래 slot만 복원/현재선택 유지, 외부 새 override 보존, 카메라파괴 cleanup 확인. 독립 정적 리뷰 추가중요지적0. 이번 runtime 범위에서 EditMode 재실행 없음.
- 운영 진입/관찰씬/앵커와 실제VR/Player 검증은 다음 단계. 이 scoped adapter 시험을 사용자 전체 전환 인수로 바꾸지 않는다.

## P13-d Bootstrap·메뉴 운영 연결

- base c40389c → efc832c, [PR38 draft](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/38). ComparisonIntroInstaller가 기존 CombatSceneEntry 초기화 이벤트에서 실제 presentation adapter를 연결한다. GS profile은 생성하지 않았으며, 미설정/지원 API 오류는 현실 진입만 차단한다.
- Controls 최초 표시 순서 결함2건을 재현하고 이벤트를 OpenInstructions 앞으로 이동했다. 실제 버튼의 잘못된 profile 차단과 이미 소유된 scene 로드 실패 후 메뉴/렌더 설정/직접 전투 복구 확인. 최종 Play193/193, Edit137/1제외. [증빙](evidence/intro-bootstrap.json).
- 독립 리뷰 추가 중요 지적0. 메뉴 실제 mono 이미지에서 두 행·오류 이유·Settings/Controls를 확인했다. 이는 VR 가독성이나 실제 촬영본의 성공 전환 인수가 아니다. 다음은 실제 GS를 포함한 관찰 씬→기존 맵의 통합 검증이다.

## 2026-10-22 P13 실제 GS 전환 통합 시험

- 소스 `2e81403`, [PR39](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/39) draft, base `codex/p13-intro-bootstrap`. 운영 Bootstrap/adapter/씬 로더/실제 GPU GS/K2 Raycast/fade/기존 Room 준비 인계를10회 반복하고 Classroom 직접 진입도 확인했다. 관찰 자료는 합성256 GS이고 fixture 씬의 시험 중 구성만 바꿨다.
- 최초 단독1/1 통과. 리뷰에서 RunStarted 순간 확인 누락과 실패시 시험 전역 설정 오염 가능성2건을 보강했다. 동기 시작 경계에서 alpha0/관찰씬 해제/GPU 자원 반환을 검사하고 teardown이 원래 quality slot/asset을 독립 복원한다. 최종 전체 DX12 PlayMode194/194. [증빙](evidence/gs-flow-integration.json).
- 원본 해시5/5 일치, 기존 두 맵/저장된 Graphics·Quality 설정 diff 없음. 초기8초·HP100·30발·처치0을 확인하기 위해 run clock을 껐다. 이 시험을 Wave1 전투·컨트롤러 실입력·양안·실제 방 인수로 확대하지 않는다. EditMode는 시험만 추가한 이번 단위에서 재실행하지 않았다.

## 2026-10-23 P13 새 소스 복원 리허설

- 소스6ea502b를 Git archive로 내보내고 로컬 외부 자산687개 파일을 GUID/meta와 함께 별도 ZIP에 보관했다. 2,991개 파일을 `D:/VRFPS-Reproduction/gs-6ea502b/`에 복원하고 CRC/SHA를 대조했다. Library와 실제 촬영 GS는 포함하지 않았다. 합성 GS는 새 복원본에서 재생성했다.
- 새 복원본 EditMode137통과/개인스캔1제외, DX12 PlayMode194/194. 기존 BootstrapSetup.Build의 Windows x64 Mono 개발 빌드 성공: GUID54d64a1919184786b7dc5d4befc087a6, 산출물351파일 SHA 기록. [증빙](evidence/p13-source-restore.json).
- 동일 PC 패키지 캐시/라이선스를 사용했다. Player 실행·실제 API·GS stripping·XR은 미검증이며 최종 GSComparison 배포본이 아니다. 성능 수집은 수행하지 않았다.
- Unity 임포트/빌드 후56개 파일의 바이트 변경, 누락0을 기록했다. 샘플 메타 외 Volume filter/URP prefilter·runtime settings/preloadedAssets 변경을 포함한다. 기존 두 맵/게임 소스/외부 자산은 변경 목록에 없다. 원본 프로젝트에 복사하지 않았고 원본 기준 해시5/5 일치.
- fixture importer 종료의 Persistent allocation1 경고가 재현됐다. 원인은 미진단이며 전체 메모리 무누수 주장은 하지 않는다. 실제 촬영본/앵커·VR·최종 빌드·그 최종본의 복원은 계속 남아 있다.

- 복원 증빙 단위2366ca1, [PR40 draft](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/40), base codex/p13-gs-flow-integration. 독립 리뷰에서2991개 복원 파일과351개 빌드 파일을 재해시해 감사와 일치, P1/P2 없음. 외부 GitHub 승인과 실제 VR 인수는 아니다.

## P13-a/f Windows GS 진단 실행 계획

- Ruling: 기존 승인된 호환성·별도 빌드 범위에서 합성 GS 전용 Player probe를 만든다 — 실제 Player의 GPU 출력과 포함된 셰이더/리플렉션 경로는 Editor 시험만으로 입증되지 않기 때문이다 — 실제 촬영본·양안·최종 GSComparison 인수를 대신하지 않는 제한을 결과에 명시한다.
- 전용 실행 옵션으로만10회 GS 표시/검정/해제와 pipeline 복원을 실행하고 JSON 결과·빌드 GUID·실제 API를 기록한다. 별도 출력 폴더, DX12 빌드 설정은 종료 후 복원, 사용자 기록·성능 로그에는 접근하지 않는다. 먼저 실패 시험을 만들고 실제 native 실행 결과까지 확인한다.

## 2026-10-23 P13 실제 Windows GS 진단

- 소스4c84f7d, [PR41 draft](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/41), base codex/p13-source-restore. 전용 실행 옵션에서만 합성 GS 표시/검정/해제10회와 JSON·exitcode를 기록하는 별도 Player probe를 추가했다. 사용자 기록/성능 수집은 없다.
- 초기2건 RED→GREEN. 독립 리뷰 P2(최종 inventory 관찰 예외가 보고/종료를 건너뜀)를2통과/1실패로 재현하고 전체 실행 오류를 실패 보고로 변환했다. 최종 DX12 PlayMode197/197. 재검토 응답에서 수정 확인·추가 P1/P2 없음; 이후 reviewer final은 사용량 제한으로 종료됐다.
- 실제 Windows x64 Mono 개발 빌드 GUIDa02759d14b874012ab295b60a0454cdc: Direct3D12,10회/검정10프레임/최소 GS픽셀23648, 자원 반환·pipeline 복원 true, exit0. 같은 빌드 -nographics는 성공false/0회/정리true/exit1. 출처 포함343파일 SHA 기록. [증빙](evidence/gs-player-probe.json).
- managed stripping=Disabled이며 이 조건에서 포함된 실제 셰이더·반사 경로를 검증했다. OpenXR는 HMD unavailable이었다. 실제 방·양안·full Player 게임 전환·IL2CPP/상위 stripping·최종 GSComparison 인수는 미완료다. 원본 해시5/5 일치. 진단 추가만인 이번 단위의 EditMode는 재실행하지 않았다.
