# Zombie Shoot VR

촬영한 방과 강의실을 전투 공간으로 사용하는 Unity PC VR 좀비 FPS 학기 프로젝트다. Quest Pro와 Virtual Desktop을 대상으로 K2 양손 사격, 왼손 수동 재장전, 근접·원거리 좀비와 무한 웨이브를 구현한다.

**현재 상태: 임시 시험장에서 전투·메뉴·기록 기반과 성능 기록/분석 구현, 실제 두 맵·최종 기기 인수 대기.** 이동, 양손 사격, 수동 재장전, 두 적 유형, 웨이브·정지·재도전, 오른팔 HUD, 설정과 소리·진동을 구현했다. 실제 촬영 방/강의실과 최종 외형·실기·성능·제출물은 미완료다. 브랜치마다 포함된 작업이 다르므로 [진행 기록](docs/progress.md)의 기준 커밋과 PR을 확인한다.

## 프로젝트 열기

1. 이 저장소를 clone한다.
2. Unity Hub에서 **`Zombie Shoot VR/` 하위 폴더**를 프로젝트로 추가한다. 저장소 루트는 문서를 함께 관리하는 상위 폴더다.
3. Unity **6000.3.23f1**로 열고 패키지 복원과 에셋 임포트를 기다린다.
4. [소스 복원·시험·빌드 안내](docs/source-reproduction.md)에 따라 저장된 씬을 사용한다. P01부터 초기 구성기를 전부 다시 실행할 필요는 없다.

현재 패키지 기준은 XRI 3.5.1, URP 17.3.0, OpenXR 1.17.1, Input System 1.20.0이다. 정확한 의존성과 잠금 상태는 `Zombie Shoot VR/Packages/`에 보관한다.

## 실행할 수 있는 빌드

- `Zombie Shoot VR/Builds/Windows/MenuLab/WaveLab.exe`: 현재 로컬 전투 시험장. 점수는 저장하지 않으며 실제 스캔 맵이 아니다.
- `Zombie Shoot VR/Builds/Windows/BootstrapPreview/BootstrapPreview.exe`: 첫 안내·맵 선택·설정 미리보기. 실제 Room/Classroom 씬이 없어 맵 선택은 비활성이다.

빌드는 Git에 포함되지 않는다. 새 소스에서 빌드하면 전투 시험장은 `Builds/Windows/WaveLab/`에 생성되며, 기존 로컬 `MenuLab/`은 그 빌드를 복사한 경로다. [최신 실기 안내](docs/hardware-validation.md)와 [결과 양식](docs/templates/hardware-session-result.md)을 사용한다. 과거 InteractionLab 실행 결과를 최신 모든 기능의 기기 통과로 확대하지 않는다.

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
