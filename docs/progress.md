# Goal 실행 현황

갱신: 2026-09-22. 전체 목표는 미완료이며 진행 중이다.

## 현재 작업

- 기준 커밋: `a400ecb194c40fd082ef549ea798ca512c4cd411`.
- 브랜치: `codex/p01-environment`.
- P01-a 구현·자동 검증 완료: Unity 환경 확인, EditMode 시험 어셈블리와 InteractionLab 구성, Windows Mono 개발 빌드. 증빙: [시험·빌드·소스 해시](evidence/p01-foundation.json), [환경 및 실행 절차](../Zombie%20Shoot%20VR/Docs/environment.md).
- P01 실기 완료 조건 T01: 대기. 실제 Quest Pro 양안·머리·양손 입력 시험을 아직 수행하지 않았다.
- P02 실제 촬영본과 기준 길이 2개: 대기. 촬영 경로를 확인하고 나면 작은 공간 검증을 진행한다.
- P03~P12: 미착수. 상세 체크리스트는 기존 구현 계획을 따른다.

## 확인한 환경과 증빙

- Unity 실행 파일: `D:/Unity editor/6000.3.23f1/Editor/Unity.exe`.
- 기존 프로젝트의 batchmode 임포트·컴파일은 2026-09-22 정상 종료했다. 로컬 로그: `tmp/p01/baseline.log`, 끝의 반환 코드 0 확인.
- Test Framework 1.6.0은 잠금 파일에 존재한다. AI Navigation은 아직 설치되지 않았다.
- Windows OpenXR ActiveRuntime: Virtual Desktop의 `virtualdesktop-openxr.json`.
- 설치된 Virtual Desktop Streamer 실행 파일 버전: 1.34.22.0. 실제 HMD 설정·연결·72Hz·해상도·코덱·비트레이트는 미확인.
- 지정된 `.github/agents/*_agent.prompt.md`와 `main_instruction.prompt.md`는 현재 저장소에 없다. 해당 파일의 규칙을 추측하지 않는다.

## 실행 판단 기록

- Ruling: 기존 Unity 경로 유지와 필요한 경우에만 worktree를 사용하는 Goal 지침에 따라 현재 깨끗한 체크아웃의 작업 브랜치를 사용한다. 병렬 에디터는 실행하지 않는다. 비용: 같은 경로에서 사용자가 동시에 수정하면 충돌을 확인해야 한다.
- Ruling: 실행 스킬의 task-start는 `Task 1` 헤더를 요구하지만 승인된 계획은 `P01` 헤더를 사용한다. 계획 원문을 바꾸지 않고 P 번호로 작업과 증빙을 기록한다. 비용: 스킬 자동 장부 대신 진행 문서를 직접 유지한다.
- Ruling: Windows IL2CPP 모듈이 없어 시험장 빌드에 설치된 Mono를 사용한다. 빌드 메서드는 원래 백엔드를 복원한다. 비용: 최종 게임의 백엔드가 달라지면 해당 빌드로 성능·동작을 다시 검증해야 한다.
- P01-a RED→GREEN: 전용 씬 부재로 환경 시험 3개 실패 후, 씬 생성으로 3개 통과. 리뷰 후 최종 EditMode 재실행도 3/3 통과. Windows Mono 개발 빌드 성공과 실행 파일을 확인했다. 실제 기기 실행 통과는 아직 주장하지 않는다.
- 엔진 생성 변경: 빌드·시험의 URP 사전 계산·OpenXR 참조 갱신·UnityConnect 변경은 원래 파일을 백업 후 복원했다. 빌드 실패 중 일시 이동된 XR simulation 파일 4개도 원본 내용 일치를 확인하고 복원했다. 로컬 증빙은 `tmp/p01/`에 유지한다.
- 리뷰: 중대한 지적 없음. 모든 열린 씬의 미저장 상태를 확인하도록 시험 보호를 보완했다. Minor deferred: `SharedSceneCanBeLoaded` 이름과 달리 해당 시험은 씬 자산의 존재를 확인한다. 실제 로드·XR 구성 검사는 별도 InteractionLab 시험이 담당한다.
- 리뷰 범위 밖 판정: 실기·VD·편안함은 실제 기기 결과로 판정하고, 전용 입력·연속 회전·런타임·PlayMode 어셈블리·Bootstrap 동작은 후속 구현 대상으로 남긴다. 비용: P01 전체 완료나 최종 플레이 가능 상태로 해석해서는 안 된다. Mono 빌드와 엔진 생성 변경은 실행자가 최종 로그·diff로 확인한다.
- P01→P03/P04: XR Origin·좌우 입력은 공통 PlayerRig의 기반이다. 오른손 총 고정과 왼손 소유권은 후속 단계에서 일반 잡기와 충돌하지 않게 적용한다.
- P02→P09: 실제 외형·충돌·가림 검증을 MapRoot로 연결한다. 임시 시험장을 최종 맵으로 판정하지 않는다.
- P04→P05: FireGate는 탄창 삽입과 왼손 앞부분 잡기를 별도로 요구한다. 재장전 완료만으로 발사하지 않는다.
- P06→P07: 실제 생성 성공 후 적 ID를 등록한다. 중복 사망·시스템 회수는 처치를 늘리지 않는다.
- P07→P08: 기록은 ReachedWave와 같은 도전의 Kills를 저장한다. 사망 확정이 클리어 회복보다 먼저다.
- P08/P09→P11/P12: 씬 교체 시 공통 전투 수명·객체 상한·기록 격리를 시험하고 최종 빌드로 재검증한다.

## 필요한 사용자 입력

사용자 확인(2026-09-22): Quest Pro·Virtual Desktop 연결 가능, 방 스캔은 아직 촬영 전, 12월 중순 마무리 예상. 실제 스캔 파일과 실측 길이 두 개는 촬영 후 필요하다. 12월 중순은 사용자 목표 일정이며 공식 제출일로 단정하지 않는다. 제출 형식·스캔 앱 허용 조건은 미확인.
