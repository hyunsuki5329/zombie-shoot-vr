# P01 환경과 시험장

확인일: 2026-09-22. 사용자가 VR 실행·머리·양손 추적 정상 동작을 확인했다. 전체 P01 구성과 후속 인수 검증은 진행 중이다.

| 항목 | 확인값 |
|---|---|
| Editor | Unity 6000.3.23f1 |
| 설치 경로 | `D:/Unity editor/6000.3.23f1/Editor/Unity.exe` |
| XRI | 3.5.1 |
| URP | 17.3.0 |
| OpenXR | 1.17.1 |
| Input System | 1.20.0 |
| Test Framework | 1.6.0, 기존 간접 의존성 |
| AI Navigation | 미설치, 후속 구성 필요 |
| OpenXR ActiveRuntime | `C:/Program Files/Virtual Desktop Streamer/OpenXR/virtualdesktop-openxr.json` |
| Virtual Desktop Streamer | 설치 실행 파일 버전 1.34.22.0 |
| Quest Pro 연결 | 사용자가 VR 실행·머리·양손 추적 정상 확인 |
| VR 스트리밍 | High, 표시 해상도 2496×2688, 90Hz, 270Mbps |
| 보간·업스케일 | SSW Always enabled, SGSR 켜짐 |
| 코덱 | 제공된 사진에서 미확인 |

상세 설정과 확인 범위는 [실기 증빙](../../docs/evidence/2026-09-22-vd-hardware-check.md)을 따른다. 현재 90Hz·SSW 켜짐 프로필을 계획의 72Hz·보간 끔 성능 검증과 혼동하지 않는다.

## 이번 기반 구성

`Assets/VRFPS/Scenes/Tests/InteractionLab.unity`에는 설치된 XRI 3.5.1 Starter Assets의 XR Origin, 좌우 컨트롤러, XR Interaction Manager, 12m 바닥, 두 벽, 1m 큐브와 조명이 있다. 시작 위치 앞 3m에 큐브를 두어 크기·추적 확인에 사용한다.

`Combat.unity`에는 같은 XR 기반만 있으며 `Bootstrap.unity`는 후속 메뉴·씬 로딩을 위한 빈 씬이다. 최종 게임 진입점이 아니다. Combat과 InteractionLab은 프로젝트의 `Prefabs/PlayerRig.prefab`과 `Input/VRFPS.inputactions`를 사용한다. 설치된 Starter Assets를 복제하여 입력 참조를 연결했다. P03-a에서 왼쪽 스틱 이동(1.6m/s)과 오른쪽 스틱 연속 회전(60도/s)을 프로젝트 제어기로 연결했다. 총기 조작은 후속 단계다. 사용자의 머리·양손 추적 확인은 이 입력 변경 이전 빌드에 대한 결과다.

## 재현 명령

아래 명령은 저장소 루트에서 실행한다. 다른 Unity 에디터로 이 프로젝트를 연 상태에서는 실행하지 않는다. `Start-Process`가 반환하는 프로세스 객체로 종료를 확인하고 로그·시험 XML을 함께 읽는다.

```powershell
$editor = 'D:/Unity editor/6000.3.23f1/Editor/Unity.exe'
$project = Join-Path (Get-Location) 'Zombie Shoot VR'
$output = Join-Path (Get-Location) 'tmp/p01'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$args = '-batchmode -nographics -projectPath "{0}" -runTests -testPlatform EditMode -testResults "{1}/editmode.xml" -logFile "{1}/editmode.log"' -f $project, $output
$process = Start-Process $editor -ArgumentList $args -WindowStyle Hidden -PassThru
$process.WaitForExit()
[xml]$results = Get-Content (Join-Path $output 'editmode.xml')
$results.'test-run' | Select-Object result,total,passed,failed
```

초기 씬이 없는 체크아웃에서만 `-batchmode -quit -executeMethod VRFPS.Editor.ProjectSetup.CreateFoundationScenes`로 생성할 수 있다. 기존 씬은 덮어쓰지 않는다. 이미 저장소에 있는 씬은 다시 생성할 필요가 없다.

전용 입력 구성은 `-batchmode -nographics -quit -executeMethod VRFPS.Editor.ProjectInputSetup.Configure`에 같은 `-projectPath`와 별도 `-logFile`을 지정하여 재현한다. 기존 전용 자산은 유지하고, 수정된 샘플 리그의 자동 교체는 거부한다. 정상 입력 참조와 두 씬의 활성 입력 자산은 EditMode 시험으로 확인한다.

이 도구의 최초 리그 교체는 현재의 초기 기반 씬 두 개에만 적용한다. 다른 객체가 리그·카메라·컨트롤러를 참조하거나 리그에 부모를 추가한 씬의 마이그레이션에는 사용하지 않는다. 해당 관계는 자동 보존하지 않는다. 현재 두 씬에는 이 관계가 없음을 리뷰에서 확인했다.

시험장 Windows 개발 빌드는 `-batchmode -quit -buildTarget Win64 -executeMethod VRFPS.Editor.ProjectSetup.BuildInteractionLab`과 같은 `-projectPath`, 별도 `-logFile`을 사용한다. 현재 Windows IL2CPP 모듈이 없어 최초 빌드는 실패했다. 시험장 빌드 메서드는 설치된 Mono 백엔드를 명시적으로 사용하고 종료 시 기존 백엔드 설정을 복원한다. 최종 게임의 백엔드 선택을 확정한 것은 아니다.

결과 위치는 프로젝트의 `Builds/Windows/InteractionLab/InteractionLab.exe`이며 Git에서 제외한다. 빌드 성공은 로그의 `VRFPS_BUILD_SUCCEEDED`와 실제 파일로 확인한다.

## T01 실제 기기 확인 절차

1. Quest Pro를 Virtual Desktop으로 PC에 연결하고 사용한 스트리밍 설정을 기록한다.
2. 시험장 실행 파일을 열고 양쪽 눈에 바닥·벽·앞쪽 큐브가 보이는지 확인한다.
3. 고개를 좌우로 돌리고 숙일 때 추적이 유지되는지 확인한다.
4. 왼손·오른손을 각각 움직여 대응하는 컨트롤러가 따라오는지 확인한다.
5. 두 눈의 가림 오류, 바닥 높이, 크기 이상, 입력 누락이 있으면 재현 동작과 관찰을 기록한다.

보고 형식: 빌드 식별자 / 실행 날짜 / 양안 표시 / 머리 추적 / 왼손 / 오른손 / VD 설정 / 오류·관찰. 이 결과를 받기 전에는 T01 통과로 기록하지 않는다.

## P03-a 빌드와 남은 T03 검증

현재 `Builds/Windows/InteractionLab/InteractionLab.exe`는 P03-a 이동 구현을 포함한다. 런타임 DLL 해시는 [P03 증빙](../../docs/evidence/p03-locomotion.json)에 있다. 기존 XRI 이동·스냅·중력 공급자는 비활성화했으며 프로젝트 제어기 하나가 CharacterController를 움직인다. 입력 구성 재현 후 `VRFPS.Editor.PlayerLocomotionSetup.Configure`를 batchmode에서 실행하면 프리팹 설정을 재현할 수 있다.

기기에서 왼쪽 스틱 단독 이동, 오른쪽 스틱 단독 연속 회전, 두 스틱 동시 입력, 위·아래를 보며 이동, 숙이기, 큐브·기존 벽에 스틱으로 접근하는 순서로 T03을 확인한다. 기대 결과는 수직 이동 오류나 스냅 회전이 없고 스틱으로 벽을 통과하지 않는 것이다. 머리·양손 표시도 회귀 확인한다. 보고에는 각 동작의 결과와 빌드 식별자를 포함한다.

현재 시험장은 벽이 두 면만 있다. 열린 바닥 경계와 실제 머리를 가상 벽 너머로 움직이는 경우의 대응은 미완성이므로 P03 전체 통과를 주장하지 않는다. 일시정지 인터페이스는 자동 시험으로 확인했지만 사용자 메뉴는 아직 없다.
