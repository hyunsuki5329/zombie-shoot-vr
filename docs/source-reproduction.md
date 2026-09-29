# 소스 복원·시험·Windows 빌드

이 안내는 현재 개발 브랜치의 재현 절차다. 실제 두 맵과 최종 제출 인수는 아직 완료하지 않았다. 실행·조작 확인은 [실기 안내](hardware-validation.md), 단계별 상태는 [진행 기록](progress.md)을 따른다.

## 필요한 환경과 파일

- Unity6000.3.23f1 및 Windows x64 빌드 지원. 현재 빌드 함수는 Mono 개발 빌드를 사용한다. 최종 IL2CPP 또는 Release 빌드는 아직 검증하지 않았다.
- Unity 실행 권한/라이선스와 Unity Package Manager 패키지를 복원할 수 있는 환경. `Packages/manifest.json`과 `packages-lock.json`을 함께 보존한다.
- 주요 잠금 버전: XRI3.5.1, URP17.3.0, OpenXR1.17.1, Input System1.20.0, AI Navigation2.0.14, Test Framework1.6.0.
- 소스: `Zombie Shoot VR/Assets/` 전체와 `.meta`, `Packages/`, `ProjectSettings/`. 폴더 이름이 같은 저장소 루트와 Unity 프로젝트를 혼동하지 않는다.
- Python 분석기를 실행할 경우 Python3.11과 `scripts/` 전체. `analyze_vr_frame_timings.py`는 같은 폴더의 `analyze_vr_diagnostics.py`를 가져온다.

Git clone 또는 아래 소스 내보내기에서 같은 커밋을 고정한다. `Library`, `Temp`, `Logs`, 빌드, 개인 촬영 원본, `.git`, 자격 증명은 복원에 넣지 않는다. 개인 OpenXR/튜토리얼 미커밋 설정을 내보내기에 섞지 않는다. 새 PC에는 그 PC에 맞는 검증된 VR 런타임/연결 환경이 별도로 필요하다.

최종 소스/문서 묶음에는 [자산 출처표](asset-provenance.md)와 `docs/licenses/`도 동봉한다. 아래8a28aee 리허설 ZIP은 후속 추가된 고지를 포함하지 않으며 최종 제출 ZIP이 아니다.

```powershell
# 저장소 루트에서, 새 파일/폴더 이름을 사용한다.
git archive --format=zip --output=source-8a28aee.zip 8a28aee -- 'Zombie Shoot VR/Assets' 'Zombie Shoot VR/Packages' 'Zombie Shoot VR/ProjectSettings'
Expand-Archive -LiteralPath source-8a28aee.zip -DestinationPath Source-8a28aee
```

예시 커밋은 P11 런타임과 분석/실기 안내가 있는 `8a28aee`다. 이 Unity 소스 전용 ZIP에는 보고서·발표·영상·분석 스크립트가 포함되지 않는다. 제출 전체 묶음과 구분한다. 실제 최종 소스는 검증을 마친 최종 커밋으로 새로 내보내고 해당 커밋을 기록해야 한다. 소스의 외부 템플릿/샘플 사용 조건과 제출 범위는 별도 확인하며, 이 로컬 검증이 공개 재배포 허가를 뜻하지 않는다.

Unity Hub에서 내보낸 `Source-8a28aee/Zombie Shoot VR`을 연다. 에디터 업그레이드나 패키지 갱신을 먼저 하지 않는다. 초기 import가 끝난 뒤 Console 오류를 확인한다. 이미 저장된 씬·프리팹·NavMesh가 있으므로 `*Setup.Configure`를 일괄 실행하지 않는다. 해당 도구는 자산을 변경할 수 있으며 일반 복원에 필요하지 않다.

## 씬과 빌드 진입점

| 목적 | 경로/메서드 | 현재 범위 |
|---|---|---|
| 게임 진입 미리보기 | `Assets/VRFPS/Scenes/Bootstrap.unity` | 공통 Combat을 로드하고 안내·맵 선택 표시. 실제 맵 선택 대기 |
| 전투 시험장 | `Assets/VRFPS/Scenes/Tests/WaveLab.unity` | 이동·재장전·웨이브·HUD·피드백, 영구 점수 저장 없음 |
| 공통 전투 | `Assets/VRFPS/Scenes/Combat.unity` | Bootstrap에서 로드하는 공유 구성 |
| 두 개발 빌드 | `VRFPS.Editor.InstructionSetup.Build` | BootstrapPreview와 WaveLab을 순서대로 생성 |

맵 로딩 자동 시험은 합성 시험 씬을 일시 등록하고 종료 시 빌드 씬 목록을 복원한다. 실제 방/강의실이 생긴 것으로 해석하지 않는다.

## 명령행 검증

동일 프로젝트를 다른 에디터가 열고 있으면 먼저 정상 종료한다. 명령마다 프로세스 종료와 결과를 확인한 다음 다음 명령을 실행한다. 서로 다른 시험/빌드라도 같은 프로젝트에서 동시에 실행하지 않는다.

```powershell
$unityEditor = 'D:/Unity editor/6000.3.23f1/Editor/Unity.exe'
$unityProject = 'D:/Unity Project/Zombie Shoot VR/tmp/p12-reproduction/Source/Zombie Shoot VR'
$verificationOutput = 'D:/Unity Project/Zombie Shoot VR/tmp/p12-reproduction'

# EditMode. PlayMode 검증은 testPlatform과 출력 이름을 PlayMode/playmode로 바꿔 실행한다.
$unityArguments = '-batchmode -nographics -projectPath "{0}" -runTests -testPlatform EditMode -testResults "{1}/editmode.xml" -logFile "{1}/editmode.log"' -f $unityProject, $verificationOutput
$unityProcess = Start-Process -FilePath $unityEditor -ArgumentList $unityArguments -WindowStyle Hidden -PassThru
$unityProcess.WaitForExit()
$unityProcess.ExitCode
[xml]$testResult = Get-Content -LiteralPath (Join-Path $verificationOutput 'editmode.xml')
$testResult.'test-run' | Select-Object result,total,passed,failed
```

경로는 복원한 위치에 맞춘다. 출력 폴더는 미리 만들고 이전 결과와 혼동하지 않게 새 이름을 쓴다. 시험 실행에는 `-quit`를 추가하지 않는다. 시작 성공만으로 시험 통과를 판정하지 않으며 XML의 실패/누락과 로그를 함께 확인한다. 관찰 대기 시간이 길어져도 진행 중인 Unity를 중복 실행하지 않는다.

```powershell
$unityArguments = '-batchmode -quit -buildTarget Win64 -projectPath "{0}" -executeMethod VRFPS.Editor.InstructionSetup.Build -logFile "{1}/build.log"' -f $unityProject, $verificationOutput
$unityProcess = Start-Process -FilePath $unityEditor -ArgumentList $unityArguments -WindowStyle Hidden -PassThru
$unityProcess.WaitForExit()
$unityProcess.ExitCode
```

빌드 로그의 `VRFPS_BOOTSTRAP_BUILD_SUCCEEDED`와 `VRFPS_WAVE_LAB_BUILD_SUCCEEDED`, 아래 파일과 함께 생성된 Data·DLL을 확인한다. 두 로그가 모두 있어야 두 빌드 성공이다.

```text
<Unity 프로젝트>/Builds/Windows/BootstrapPreview/BootstrapPreview.exe
<Unity 프로젝트>/Builds/Windows/WaveLab/WaveLab.exe
```

빌드 함수는 실행 중 Mono와 프레임 통계를 켜고 `finally`에서 기존 프로젝트 설정을 복원한다. 복원 소스에서 빌드했다고 원래 프로젝트의 최신 기기 시험용 `MenuLab`을 자동 교체하지 않는다. 실행 파일만 복사하지 말고 빌드 폴더 전체를 보존한다. 빌드 산출물은 GUID/타임스탬프 등에 따라 달라질 수 있으므로 이전 빌드와 바이트가 같아야 성공하는 것은 아니다.

## 저장·로그와 문제 해결

기본 Windows 데이터 위치는 `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Zombie Shoot VR/`이다. 실제 맵 기록은 `records-v1.json`과 `.bak`, 진단은 `Diagnostics/`, 실행 로그는 `Player.log`다. MenuLab은 연습 점수를 저장하지 않는다. 설정은 Unity PlayerPrefs에 저장된다. 오류 재현을 위해 실제 사용자 기록을 지우지 말고 백업과 별도 시험 환경을 사용한다.

- 컴파일 오류: 에디터 버전과 package lock이 유지됐는지, 로그의 첫 컴파일 오류부터 확인한다.
- 패키지 복원 오류: 라이선스·네트워크·UPM 오류를 확인한다. 임의의 최신 버전 설치로 잠금을 바꾸지 않는다.
- 화면/입력 오류: 성공한 Quest Pro/VD 연결과 활성 OpenXR 런타임, Player.log를 대조한다. headless 자동 시험은 실제 두 눈/추적 시험이 아니다.
- 맵 버튼 비활성: 현재 실제 Room/Classroom 자산이 없기 때문에 정상인 미완료 상태다.
- 진단 GPU 표본0: 측정 지원/환경을 확인하고0ms 합격으로 처리하지 않는다. [분석 지침](../Zombie%20Shoot%20VR/Docs/performance.md)을 따른다.

## 현재 복원 검증 상태

2026-09-29 확인: 8a28aee의 추적 소스만 ZIP으로 내보내고 기존 Library가 없는 새 폴더에서 **EditMode50/50, PlayMode116/116**, BootstrapPreview/WaveLab Windows 개발 빌드를 검증했다. [P12 소스 복원 증빙](evidence/p12-source-reproduction.json)에 소스 ZIP·시험 XML·빌드 manifest 해시와 임포트 변경 범위를 기록했다.

Unity의 PC 전역 패키지 캐시/라이선스는 공유할 수 있으므로 완전히 새 PC나 오프라인 복원을 검증한 것으로 확대하지 않는다. 기존 샘플 텍스처 메타데이터 등 에디터 생성 변경은 복원본에서 관찰하고 원래 사용자 프로젝트에 복사하지 않았다. 실제 두 맵·최종 외형·독립 리뷰·제출 형식·최종 기기 인수가 남아 T20 전체 완료가 아니다.


## GAMGO 외형 추가 이후의 필수 복원 단계

P04 외형 브랜치부터 K2/탄창 프리팹은 Git 제외 자원을 참조한다. 기존8a28aee 복원 리허설 결과를 이 변경 이후의 완전 복원 증빙으로 확대하지 않는다. Unity 시험/빌드 전에 [GAMGO 원본 확보 및 복원·적용](gamgo-visual-restore.md)을 수행해야 한다. 원본 ZIP의 라이선스별 전달 조건을 지키고 해당 계정으로 다시 확보할 수 있는 링크·해시를 제공한다. 파일이 없으면 임시 도형으로 최종 빌드를 성공 처리하지 않는다. 새 체크아웃의 전체 복원 리허설은 이 외형 작업 이후 다시 필요하다.
