# Zombie Shoot VR

촬영한 방과 강의실을 전투 공간으로 사용하는 Unity PC VR 좀비 FPS 학기 프로젝트다. Quest Pro와 Virtual Desktop을 대상으로 K2 양손 사격, 왼손 수동 재장전, 근접·원거리 좀비와 무한 웨이브를 구현한다.

**현재 상태: 폐가·폐교 두 맵에서 공통 전투를 실행할 수 있는 개발 빌드가 준비되어 있다.** 사용자 편집 가구 배치, 양손 사격·수동 재장전, 보스·크롤러, 맵별 기록, 시점 높이1.15배와1.35m 점프를 포함한다. 두 맵의 시점·점프·이동·좀비 접근 및 폐가 추가 벽 충돌은 사용자 확인을 받았다. 체감 성능은 충분하다는 평가이며, 두 맵의 사격·재장전·웨이브·사망·기록 표시·재도전도 사용자 확인을 받았다. 정량 성능과 맵별 약5분 시험 기록, 나머지 최종 제출 인수는 미완료다. [진행 기록](docs/progress.md)에서 검증 범위를 확인한다.

## 프로젝트 열기

1. 이 저장소를 clone한다.
2. Unity Hub에서 **`Zombie Shoot VR/` 하위 폴더**를 프로젝트로 추가한다. 저장소 루트는 문서를 함께 관리하는 상위 폴더다.
3. Unity **6000.3.23f1**로 열고 패키지 복원과 에셋 임포트를 기다린다.
4. [소스 복원·시험·빌드 안내](docs/source-reproduction.md)에 따라 저장된 씬을 사용한다. P01부터 초기 구성기를 전부 다시 실행할 필요는 없다.

현재 패키지 기준은 XRI 3.5.1, URP 17.3.0, OpenXR 1.17.1, Input System 1.20.0이다. 정확한 의존성과 잠금 상태는 `Zombie Shoot VR/Packages/`에 보관한다.

## 최신 빌드 실행

방·강의실 GS에서 바이러스를 사격하면 암전을 거쳐 해당 테마 전투로 진입한다. 기존 직접 전투도 유지한다. GS는 DX12 실행 환경을 사용한다.

- 사용자 VR 인수 빌드: `C:/Users/User/.codex/worktrees/gs-comparison-intro/Zombie Shoot VR/Zombie Shoot VR/Builds/Windows/GSComparison-Classroom-Hybrid/AbandonedSchool.exe`. 구현 소스 `3c7e479`, GUID `aacd2a9f59f545fbbae798302d047920`.
- 최신 한국어 메뉴 인수 빌드: `D:/VRFPS-Reproduction/classroom-hybrid-26f31cb/Zombie Shoot VR/Builds/Windows/GSComparison-Korean-20261113/AbandonedSchool.exe`. 소스 `bb3c40a`, GUID `f6acd745513f4cc09961b2968a8747e2`. 자동 PlayMode206/206 및 빌드 검증 완료, 2026-11-14 사용자가 VR 메뉴와 실제 플레이에서 문제 없음을 확인했다. [변경 및 증빙](docs/evidence/korean-menu.json).

Quest Pro·Virtual Desktop의 기존 연결 환경에서 실행한다. 한국어 빌드는 **조작 방법 → 뒤로 → 방/강의실 → 현실 공간에서 시작**을 선택한다. **바로 전투 시작**은 GS 도입부를 생략한다. 영어 인수 빌드의 동일 메뉴는 Back / Room·Classroom / Start in Reality·Direct Combat이다. 실행 파일만 분리하지 말고 Data 폴더·DLL·출처 고지를 포함한 빌드 폴더 전체를 보관한다.

기존 `Builds/Windows/WallFix`는 보존된 이전 실행본이며 최신 GS 빌드가 아니다. 성능 로그와 정식 반복 시험 보류, 발표·영상 사용자 담당은 유지한다.

| 조작 | 동작 |
|---|---|
| 왼쪽 스틱 / 오른쪽 스틱 | 연속 이동 / 연속 회전 |
| 오른손 A | 접지 상태에서 점프(설정1.35m) |
| 왼손으로 총 앞부분 잡기 + 오른손 트리거 | 양손 사격 |
| 앞부분 해제 → 탄창 탈거 → 버리기 → 허리 새 탄창 → 삽입 | 수동 재장전, 다시 앞부분을 잡아 발사 |
| 오른팔 UI | 체력·웨이브·처치 수 확인 |

점프는 약1.2m 가구 착지로 검사했다. 약1.7m인 폐가 책상에 올라갈 수 있다고 보장하지 않는다. `MenuLab`, `BootstrapPreview` 등은 이전 단계 비교용이며 최신 시험에 사용하지 않는다.

진단 로그를 남길 때는 저장소 루트에서 다음을 실행한다. 실행 환경만 점검하려면 `-CheckOnly`를 추가한다.

```powershell
powershell -File scripts/Start-VrDiagnostics.ps1 -FrameTimings
```

[최신 실기 절차](docs/hardware-validation.md)와 [결과 양식](docs/templates/hardware-session-result.md)에 따라 기록한다. 성능 판정은 고정72Hz·보간 끔 조건의 실제 측정이 필요하다.

## 씬 편집과 복원

- 폐가: `Zombie Shoot VR/Assets/VRFPS/Scenes/Maps/Room.unity`
- 폐교: `Zombie Shoot VR/Assets/VRFPS/Scenes/Maps/Classroom.unity`
- 시작 씬: `Zombie Shoot VR/Assets/VRFPS/Scenes/Bootstrap.unity`

저장된 씬이 현재 가구 배치의 기준이다. 초기 `ClassroomSetup.Configure`, `RoomHouseSetup.ConfigurePreview`, `GrannyHouseSetup.Configure`를 다시 실행하면 수동 배치를 덮어쓸 수 있다. 가구를 수정한 뒤에는 현재 외형 기준으로 충돌·NavMesh를 동기화하고 경로를 검증해야 한다. 격리 배치 에디터에서 사용하는 도구는 `UserLayoutSetup.Configure`다.

빌드와 일부 외부 자산은 Git에 포함되지 않는다. Git clone만으로 모든 외형이 복원되는 것은 아니다. 로컬 소스 ZIP에는 외부 자산/meta가 포함되며, [소스 복원 안내](docs/source-reproduction.md)에서 기준 커밋·검증 범위를 확인한다. 최신 로컬 보관본은 `cb1b0e7-wall-fix`이며 벽 충돌 수정까지 포함한다. 파일 무결성과 검증 작업본 일치는 확인했으며, 이 ZIP의 새 Library 자동 검사와 Windows 빌드까지 확인했다(동일 PC).

## 개발 문서

- [전체 문서와 촬영 가이드](docs/README.md)
- [요구사항 R01~R20](docs/vr-fps-project-docs/02-requirements.md)
- [구현 계획 P01~P12](docs/vr-fps-project-docs/04-implementation-plan.md)
- [시험과 완료 판정 T01~T20](docs/vr-fps-project-docs/08-test-plan.md)
- [Goal 실행 프롬프트](docs/prompts/vr-zombie-fps-goal.md)

## Git 작업 방식

초기 기준선은 `main`에 저장한다. 이후 개발은 작업 단위별 브랜치에서 구현·검증·커밋·push 후 PR로 검토하고, 해당 작업의 필수 시험과 저장소 조건을 충족하면 병합한다. PR 양식에 실행한 검증과 아직 필요한 실제 기기 시험을 구분해 기록한다.

`Assets/`와 `.meta`, `Packages/`, `ProjectSettings/` 및 현재 개발 문서는 추적한다. `Library/`, 캐시, 로컬 설정, 빌드 결과, 임시 파일은 제외한다. 첫 기준선의 바이너리는 일반 Git으로 관리하며, 향후 큰 스캔 에셋을 추가할 때 Git LFS 적용과 공유 범위를 먼저 정한다.

이전 공유 대화 원본·강의 자료는 로컬의 `docs/shared-chat-2026-09-18/`에만 보관하며 저장소에 포함하지 않는다. 사적인 촬영 원본은 `RawCaptures/` 또는 `RawScans/` 등 제외된 경로에 보관한다. 이 자료가 없어도 현재 Unity 템플릿과 개발 문서는 열 수 있다.

## 에셋과 검증 상태

현재 포함된 Unity 템플릿·패키지 샘플·폰트 등 외부 에셋에는 각각의 사용 조건이 적용된다. 저장소 전체에 임의의 오픈 소스 라이선스를 부여하지 않는다. 실제 게임용 에셋을 채택할 때 [출처 관리 문서](docs/vr-fps-project-docs/07-assets-licenses.md)를 갱신한다.

[현재 자산 출처표](docs/asset-provenance.md)와 [보존한 라이선스/제3자 고지](docs/licenses/README.md)를 제출 준비에 사용한다. 미채택 모델 후보와 실제 포함 자산을 구분하며 최종 외형·스캔 출처는 추가 검증이 필요하다.

단위별 자동 시험·빌드와 사용자 관찰은 [진행 기록](docs/progress.md) 및 `docs/evidence/`에 구분해 남긴다. 전체 완료나 성능 합격은 실제 두 맵과 최종 빌드의 인수 결과로 판정한다. 캐시 없는 소스 내보내기 검증은 [복원 안내](docs/source-reproduction.md)에 기록한다.

## 설계 중인 확장: 현실 공간과 테마 맵 비교

2026-10-20 사용자 요청에 따라 방 GS를 감상하다 큰 바이러스를 쏘면 암전 후 폐가로 전환되고 전투가 시작되는 P13을 문서화했다. [PRD](docs/vr-fps-project-docs/02-requirements.md), [상세 설계](docs/vr-fps-project-docs/11-gs-comparison-intro.md), [구현 계획](docs/vr-fps-project-docs/12-gs-comparison-plan.md)을 참고한다. **아직 구현되지 않았으며 위 WallFix 빌드에는 포함돼 있지 않다.**
