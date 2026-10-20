# P13 GS 비교 도입부 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. 추가 에이전트는 사용자 또는 적용 지침의 명시적 허용이 있을 때만 사용한다. 체크박스는 실제 수행 후 갱신한다.

**Goal:** 내 방 GS를 감상하다 큰 바이러스를 사격하면 암전 후 기존 폐가가 나타나고 전투가 시작되는 첫 비교 기능을 구현한다.

**Architecture:** 하나의 XR 리그를 유지하며 GS 관찰 씬과 테마 맵을 순차 로드한다. 도입부 상태와 기존 전투 상태를 분리하고 무기 입력의 소유권을 한곳에서 결정한다. 완전히 밝아진 뒤에만 기존 전투를 시작한다.

**Tech Stack:** Unity6000.3.23f1, XRI3.5.1, URP17.3.0, OpenXR1.17.1, Windows PC VR/Quest Pro/Virtual Desktop. UnityGaussianSplatting은 검증 후보이며 아직 설치하지 않았다.

**Spec:** [11 상세 설계](11-gs-comparison-intro.md), [02 PRD R21~R28](02-requirements.md), [08 T21~T28](08-test-plan.md).

상태: **2026-10-20 문서 계획만 작성. 모든 구현 단계 미실행.** 방을 먼저 인수하고 Classroom GS는 자료 확보 후 같은 파이프라인의 후속 적용으로 분리한다.

## Global Constraints

- 기존 WallFix 실행본·사용자가 저장한 Room/Classroom 씬 배치·무관한 XR 설정 변경을 보존한다. 초기 맵 생성기를 다시 실행하지 않는다.
- 1번 방식만 수행한다. GS 전투·임의 공간 업로드·자동 가구 인식·학습 알고리즘 자체 개발은 제외한다.
- 자료 생성과 렌더러는 기존 도구를 사용하며 직접 구현 범위를 정확히 기록한다.
- 도입부에서도 양손 사격·총구 막힘·수동 재장전 유지. 도입부 시계·피격은 웨이브/기록과 분리한다.
- 시점1.15배 유지, 도입부 스틱 이동/A점프 차단·연속 회전 허용, 전투 점프1.35m 유지.
- 페이드1.2초/검정 최소0.25초/밝아짐1.2초, 단계별 로드 제한30초는 설계 초기값이다. 준비8초는 밝아진 뒤 시작한다.
- 앱 전체 API를 시험본에서 검증한다. DX11 실행 중에 DX12로 전환하는 설계를 하지 않는다.
- 성능 수집 보류·발표자료 사용자 담당을 유지한다. 계획 작성으로 새 실기 통과나 Goal 완료를 표시하지 않는다.

## Review Focus

1. 미리보기 사격을 위해 RunPhase.Active를 켜서 적·기록까지 시작하는 오류 → P13-c/d에서 도전 시작 횟수와 기록 불변 시험.
2. 이전 맵 비동기 로드가 취소 후 완료되어 새 화면을 덮는 오류 → P13-e 지연 완료·요청 세대 시험.
3. 한쪽 눈에만 적용되는 페이드, 총·UI가 검정 위로 비치는 오류 → P13-a/f 실제 양안 시험.
4. 전투 초기화가 페이드 후 다시 리그를 이동시키거나 누른 트리거가 연속 발사되는 오류 → P13-d 위치/이벤트 및 트리거 해제 시험.
5. GS를 숨기기만 해 GPU 메모리·씬·이벤트가 누적되는 오류 → P13-b/e/f 수명/10회 반복 시험.

## 파일과 제안 인터페이스

Runtime/Scenes/Tests는 `Zombie Shoot VR/Assets/VRFPS/` 기준이다. 새 어댑터와 시험용 대역 이외의 현재 파일명은 코드에서 확인했다. 아래 계약은 **구현할 API**이며 이미 존재한다고 가정하지 않는다.

| 단위 | 제안 인터페이스와 역할 |
|---|---|
| `RealityIntroProfile` | ScriptableObject. `string mapId`, `string realityScenePath`, `string realityAnchorKey`, `string themeAnchorKey`, `float fadeOutSeconds=1.2f`, `float minBlackSeconds=.25f`, `float fadeInSeconds=1.2f`, `float loadTimeoutSeconds=30f`; `void Validate()` |
| `IRealityPresentation` | `IEnumerator Load(RealityIntroProfile profile)`, `bool IsReady`, `string LastError`, `Transform ObservationAnchor`, `IEnumerator Unload()`; 소유한 GS·관찰 씬만 해제 |
| `IntroVirusTarget` | `void Configure(int playerGunId, Func<bool> canTrigger)`, `event Action Triggered`, `void ResetTarget()`, 기존 `IDamageable` 구현 |
| `VrScreenFade` | `float Alpha`, `IEnumerator FadeTo(float alpha,float seconds,Func<bool> isPaused,Func<bool> isCancelled)`; `void SetImmediate(float alpha)` |
| `ComparisonIntroController` | `IntroPhase Phase`, `bool IsBusy`, `string LastError`, `IEnumerator Begin(RealityIntroProfile profile,MapDefinition map)`, `void SetPaused(bool value)`, `void Cancel()`, `IEnumerator Shutdown()` |
| `WeaponContextRouter` | `WeaponContext Context { get; }`, `double InputTime { get; }`, `void SetIntroState(bool preview,bool blocked,double introTime)`, `void ReleaseIntro()`; RunPhase와 메뉴로부터 최종 입력/시계 계산 |
| 기존 무기/리그 확장 | `K2Controller.SetContext(WeaponContext value)`와 `ContextChanged` 이벤트; `PlayerRigController.SetLocomotionPermissions(bool translate,bool turn,bool jump)`; 기존 호출은 전투 의미의 래퍼로 보존 |
| `CombatSession` 확장 | `IEnumerator PrepareMap(MapDefinition map)`, `void PlacePreparedPlayer(Transform anchor,float relativeYaw)`, `bool BeginPreparedRun()`, `bool HasPreparedMap`; 기존 `StartMap(map)`은 준비+시작 래퍼로 보존 |

`HasPreparedMap`은 로드·NavMesh·시작점 검사가 끝났음을 뜻한다. `BeginPreparedRun()`은 준비되지 않거나 이미 시작했다면 false, 처음이면 기존 Run.StartRun을1회 실행하고 true다. 현재 StartRun 이벤트에 붙어 있는 리그 배치는 Prepare/Place 단계로 분리하고 재도전 배치는 별도 명시적으로 유지한다. 무기·HP 초기화는 StartRun에 남긴다. K2HandInput은 router.InputTime을 소비하고 CombatFeedback은 ContextChanged를 구독하여 도입부 발사/빈 총/장전 소리가 Dead 상태 때문에 차단되지 않게 한다. 이전 UseRunClock/SetPhase 호출자는 호환 래퍼로 유지하되 공유 세션의 실제 입력 소유자는 router 한 개다.

## P13-a 자료·렌더러 호환성 확인

**Files:** 기존 `Packages/manifest.json`, `packages-lock.json`, 렌더 파이프라인 설정은 격리된 시험본에서만 수정. 새 `Scenes/Tests/GsCompatibilityLab.unity`, `docs/evidence/gs-compatibility.json`(실제 결과가 생긴 뒤), GS 제외 경로는 루트 `.gitignore`.

**Consumes:** 사용자 실제 방 GS 또는 출처 명확한 소형 GS 시험 자산. **Produces:** 고정 renderer revision/API/URP/XR 조합과 채택/실패 근거.

- [ ] 기존 씬·패키지·XR 설정·빌드 GUID와 Git 변경을 기록하고 격리된 작업본을 만든다. 원본 프로젝트 전체 Library 복사는 하지 않는다.
- [ ] 실제 방 파일이 GS인지 헤더·임포트로 확인한다. 없으면 필요한 PLY/SPZ와 원본 처리 가능 여부를 요청한다. FBX 대체를 GS로 보고하지 않는다.
- [ ] 고정 후보 revision과 라이선스를 기록하고 시험본에 패키지를 설치한다. 작은 데이터로 DX12·현재 URP·현재 XR 방식부터 시험한다.
- [ ] 양안 표시, HMD·양손·기존 총, 불투명 물체와 GS 가림, 화면 전체 검정, GS 해제를 실제 Quest Pro에서 확인한다. 실패하면 원인을 기록하고 한 가지 조건씩 바꾼다.
- [ ] 통과 전에는 기본 배포본을 변경하지 않는다. 검증된 패키지·설정·증빙만 `chore(P13): validate GS renderer for PC VR` 단위로 커밋한다. 외부 GS는 제외한다.

**Done:** T27의 호환성 근거가 있고 채택할 설정을 재현할 수 있다. 사용자 실제 방 GS가 없으면 렌더러 시험만 완료로 구분한다.

## P13-b 방 GS·바이러스·앵커 자산 준비

**Files:** 새 `Runtime/Intro/RealityIntroProfile.cs`, `RealitySceneRoot.cs`, `IRealityPresentation.cs`, `Rendering/` 어댑터/asmdef, `Scenes/Reality/RoomReality.unity`, `Editor/RealityIntroSetup.cs`, `Tests/EditMode/RealityIntroAssetTests.cs`. 외부 원본은 `External/RealityGS/`, 바이러스는 `External/IntroVirus/` 로컬 제외 폴더에 분리.

**Consumes:** a의 검증된 렌더러, GS 및 취득한 바이러스 ZIP. **Produces:** 유효 프로필·관찰 씬·피격용 모델과 자원 해제 계약.

- [ ] 임포트 전 외부 폴더/meta의 Git 제외를 확인한다. ZIP/GS 원본 해시·파일 목록·출처를 기록한다.
- [ ] 프로필 누락·mapId 불일치·앵커 없음·비정상 스케일을 거부하는 EditMode 시험을 먼저 만든다. 아직 구현 없는 상태의 실패를 확인한다.
- [ ] 방 바닥·기준 길이2개를 보정하고 특징3개/관찰 앵커를 기록한다. 새 방 GS를 테마 가구에 맞춰 비균일 왜곡하지 않는다.
- [ ] OBJ 재질을 연결해 원본 외형을 대조한다. 바이러스 직경0.8m·거리2m를 초기값으로 빈 공간에 둔다. 프로필/씬 검증과 Import/Unload의 재진입 해제를 구현한다.
- [ ] 자산 시험 통과, 모델 재질·관찰 구도의 평면 검토를 하고 실제 양안 결과와 구분한다. GS 해제 후 소유 Renderer/버퍼 수0을 확인한다.
- [ ] 원본 없는 새 소스의 명시적 미준비 처리와 로컬 자료 복원 경로를 기록하고 `feat(P13): prepare room reality assets`로 커밋한다.

**Done:** T21/T25/T28의 자산 준비 부분 충족. 실제 재현 데이터 누락이면 단계 미완료를 유지한다.

## P13-c 도입부 무기 컨텍스트·한 번의 바이러스 명중

**Files:** 새 `Runtime/Intro/IntroVirusTarget.cs`, `Runtime/Weapons/WeaponContext.cs`, `WeaponContextRouter.cs`; 변경 `K2Controller.cs`, `K2HandInput.cs`, `K2FireControl.cs`, `FireGate.cs`, `Runtime/Progression/RunCombatBindings.cs`; `Runtime/Feedback/CombatFeedback.cs`와 왼손/보급 소비자는 동일 컨텍스트로 연결한다. 시험 `Tests/EditMode/IntroWeaponRulesTests.cs`, `Tests/PlayMode/IntroVirusTests.cs`.

**Consumes:** b의 관찰 씬, 기존 IDamageable/총 이벤트. **Produces:** 도입부 사격/시계의 독립 허용과 Triggered1회.

- [ ] 한 손/빈 탄창/벽 막힘/정지/잘못된 sourceId/중복 피해/일시정지 복귀 입력을 시험으로 고정한다. 유효 명중2회에도 Triggered1, 새 StartRun0, 기존 기록 불변을 확인할 실패 시험을 만든다.
- [ ] 단일 router로 입력 컨텍스트를 분리하고 기존 전투 규칙을 유지한다. Intro→Combat 시 새 발사 시계·트리거 해제를 적용한다.
- [ ] 실제 총 Raycast가 비-trigger IntroTarget Collider를 맞히고 Enemy 풀을 거치지 않도록 연결한다. 도입부 재장전과 빈 총 소리를 유지한다.
- [ ] 새 시험과 기존 WeaponRules/WeaponFeedback/재장전 관련 시험을 실행해 통과를 확인한다.
- [ ] `feat(P13): trigger comparison from virus hit`으로 커밋한다.

**Done:** T22의 자동 검증과 기존 발사·탄창 회귀 통과. 실제 GS에서 보이는 판정 범위는 f에서 인수한다.

## P13-d 페이드·테마 준비·게임 시작 연결

**Files:** 새 `Runtime/Intro/ComparisonIntroController.cs`, `IntroPhase.cs`, `VrScreenFade.cs`, 필요한 XR 페이드 셰이더/재질; 변경 `CombatSession.cs`, `CombatSceneEntry.cs`, `MenuController.cs`, `RunCombatBindings.cs`, `Runtime/Player/PlayerRigController.cs`, `Scenes/Combat.unity`; 시험 `Tests/PlayMode/ComparisonIntroTests.cs`, 기존 `CombatSessionTests.cs`.

**Consumes:** b의 Load/Unload와 c의 Triggered/컨텍스트. **Produces:** Begin→검정→준비→밝아짐→BeginPreparedRun의 단일 경로.

- [ ] 명중 전/검정/밝아짐 중 StartRun0, 알파0 후 StartRun1, 첫 준비8초, HP100·30발·도달0·처치0 시험을 먼저 만든다. 현재 즉시 StartMap은 이 조건을 충족하지 않음을 확인한다.
- [ ] CombatSession을 준비·위치 배치·시작으로 분리하고 기존 StartMap 및 재도전 시험을 유지한다. 화면이 밝은 뒤 리그가 재배치되지 않는 시험을 추가한다.
- [ ] 세계·총·손을 가리는 양안 fade를 구현한다. unscaled 시간과 정지/취소 조건을 적용하고 알파1에서만 공간 교환한다.
- [ ] 도입부는 연속 회전만 허용하고 스틱 이동/A점프를 차단하는 리그 권한을 연결한다. 두 스틱 동시 입력과 A를 넣어 이동/점프0·회전 정상, 전투 복귀 후 기존 조작 정상임을 시험한다.
- [ ] 메뉴의 현실 진입/바로 전투를 연결한다. GS 미준비 메시지와 기존 Classroom 직접 전투를 유지한다.
- [ ] 가짜 로더로 순서를 검사하고 실제 씬으로 준비·위치·기록 회귀를 실행한다. `feat(P13): transition reality into themed combat`으로 커밋한다.

**Done:** T23/T24의 자동 검증 통과. 이 단계만으로 VR 페이드 인수를 주장하지 않는다.

## P13-e 취소·실패·정지·반복 수명

**Files:** d의 controller와 menu, b의 렌더 어댑터, 새 `Tests/PlayMode/ComparisonIntroLifecycleTests.cs`.

**Consumes:** d의 단일 전환 흐름. **Produces:** 어느 상태에서도 메뉴로 복구되는 취소/오류 계약.

- [ ] 30초 시간 초과, 취소 후 늦은 로드 완료, 중복 명중, 빠른 맵 클릭, 페이드 정지/재개, 중간 종료, 누른 트리거를 각각 실패 시험으로 만든다.
- [ ] 요청 세대 ID와 소유 자원 정리를 구현한다. 취소된 작업은 늦게 완료되어도 StartRun0이며, 정리 중 새 진입만 잠그고 메뉴/종료를 유지한다.
- [ ] 메뉴 복귀에서 검정 해제·오류 가독성, 재도전은 도입부 건너뛰기, 다시 현실 진입은 바이러스 재설정을 검사한다.
- [ ] 로드/취소/전환10회 뒤 씬·리그·AudioListener·구독·버퍼가 기준으로 돌아오는 자동/실기 항목을 구분하여 확인한다.
- [ ] `fix(P13): recover interrupted comparison sessions`로 커밋한다.

**Done:** T26와 T27 수명 항목 통과. 보류된 실기 항목은 미실행으로 남긴다.

## P13-f 별도 빌드·실기 인수·복원

**Files:** 새 진단/복원 증빙, `docs/hardware-validation.md`, `docs/asset-provenance.md`, `docs/source-reproduction.md`, 실행 안내. 예정 산출물 `Builds/Windows/GSComparison/AbandonedSchool.exe`는 아직 없다.

- [ ] 변경된 규칙의 EditMode/PlayMode와 기존 두 맵 회귀를 실행한다. BuildPipeline에 관찰 씬을 추가하되 개인 원본 FBX와 불필요 원본 이미지가 포함되지 않았는지 확인한다.
- [ ] 검증된 API로 별도 Windows 빌드를 만들고 GUID·패키지 revision·파일 해시·크레딧을 기록한다. WallFix는 유지한다.
- [ ] 사용자에게 T21~T27 실기 순서를 제공한다. GS 고개 이동, 총/바이러스 가림, 양안 검정, 전투 시작 시점과 재도전을 확인한다. 성능 로그 보류는 해제 요청 없이 유지하고 미측정을 명시한다.
- [ ] 로컬 복원 묶음을 만들어 새 작업본에서 자산 연결·빌드 재현을 확인한다. 소스/로컬 자산 포함 범위를 manifest에 기록한다.
- [ ] 독립 변경 검토에서 지적된 항목을 수정·재검증한 뒤 `docs(P13): record comparison acceptance and reproduction` 단위로 커밋·push한다. PR에는 실제 통과/미실행을 분리한다.

**Done:** Room의 R21~R28에 필요한 실제 증빙이 연결되어야 기능 완료다. 전체 Goal 종료는 기존 남은 P11/P12 조건도 별도로 충족해야 한다.

## 실행·Git 기준

실행 시 현재 PR21의 기준 HEAD와 원격 상태를 먼저 대조하고 GS 작업은 `codex/gs-comparison-intro` 같은 별도 브랜치/격리 작업본에서 진행한다. 기존 PR을 통과하지 않은 최종 프로젝트로 병합하지 않는다. 의존 PR이 열려 있으면 실제 기반 브랜치를 PR 설명에 명시한다.

Unity 시험은 설치 경로 `D:/Unity editor/6000.3.23f1/Editor/Unity.exe`와 실제 격리 프로젝트의 절대경로로 실행한다. `-batchmode -runTests -testPlatform EditMode|PlayMode -testResults <절대 XML> -logFile <절대 로그>`를 사용하고 시험에 `-quit`를 붙이지 않는다. 생성한 테스트 필터는 각 작업의 클래스명을 사용한다. 종료 코드만 믿지 않고 XML의 실패0과 시험 실행 건수를 확인한다. 빌드는 현재 저장소의 검증된 빌드 진입점을 확인해 별도 출력 폴더를 지정한다.

수행하지 않은 단계의 체크박스를 문서 작성만으로 채우지 않는다. 단계별 검증 후 관련 소스·meta·문서만 커밋하고 push/PR 리뷰를 이어 간다. 외부 자원·사용자 무관 변경을 일괄 stage하지 않는다.
