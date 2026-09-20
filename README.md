# Zombie Shoot VR

촬영한 방과 강의실을 전투 공간으로 사용하는 Unity PC VR 좀비 FPS 학기 프로젝트다. Quest Pro와 Virtual Desktop을 대상으로 K2 양손 사격, 왼손 수동 재장전, 근접·원거리 좀비와 무한 웨이브를 구현한다.

**현재 상태: 기존 Unity VR 템플릿과 개발 문서의 초기 기준선.** 게임 기능 구현, 실제 공간 맵 제작, Windows 빌드와 Quest Pro 인수 시험은 아직 완료하지 않았다.

## 프로젝트 열기

1. 이 저장소를 clone한다.
2. Unity Hub에서 **`Zombie Shoot VR/` 하위 폴더**를 프로젝트로 추가한다. 저장소 루트는 문서를 함께 관리하는 상위 폴더다.
3. Unity **6000.3.23f1**로 열고 패키지 복원과 에셋 임포트를 기다린다.
4. [구현 계획](docs/vr-fps-project-docs/04-implementation-plan.md)의 P01부터 Windows PC VR 환경과 입력을 검증한다.

현재 패키지 기준은 XRI 3.5.1, URP 17.3.0, OpenXR 1.17.1, Input System 1.20.0이다. 정확한 의존성과 잠금 상태는 `Zombie Shoot VR/Packages/`에 보관한다.

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

초기 Git 설정에서는 추적 파일, 제외 대상, `.meta` 대응, 패키지 JSON과 문서 연결을 점검한다. 이는 Unity 실행·빌드·실제 기기 시험의 통과를 의미하지 않는다.
