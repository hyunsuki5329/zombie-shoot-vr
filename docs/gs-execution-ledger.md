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
