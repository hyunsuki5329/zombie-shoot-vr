# Goal 실행 현황

갱신: 2026-09-23. 전체 목표는 미완료이며 진행 중이다.

## 현재 작업

- 기준 커밋: `a400ecb194c40fd082ef549ea798ca512c4cd411`.
- 브랜치: `codex/p05-manual-reload`. 원격 P04 병합 커밋 `ec79139`로 fast-forward했으며 파일 내용은 `800b562`와 동일함을 확인했다.
- P01-a 구현 커밋: `5a8dc1d844bf5aacd53a522a894bca3eee7c91d3`. [PR #1](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/1)은 원격에서 main에 병합됨(`e0e3be2`). GitHub CI는 없으며 로컬 Unity 검증 결과를 사용한다.
- P01-a 구현·자동 검증 완료: Unity 환경 확인, EditMode 시험 어셈블리와 InteractionLab 구성, Windows Mono 개발 빌드. 증빙: [시험·빌드·소스 해시](evidence/p01-foundation.json), [환경 및 실행 절차](../Zombie%20Shoot%20VR/Docs/environment.md).
- P01 실기 확인: 사용자가 VR 실행과 머리·양손 추적 정상 동작을 확인했다(2026-09-22). [VD 설정·실기 보고](evidence/2026-09-22-vd-hardware-check.md). 전용 입력 구성과 양안 가림 등 후속 검증까지 완료한 것으로 확대하지 않는다.
- P02 실제 촬영본과 기준 길이 2개: 대기. 촬영 경로를 확인하고 나면 작은 공간 검증을 진행한다.
- P01-b: `47ceb4c`에서 프로젝트 전용 VRFPS.inputactions와 PlayerRig를 생성하고 Combat·InteractionLab에 연결했다. 입력 시험 3개가 연결 전 실패했고 연결 후 기존 시험을 포함한 EditMode 6/6이 통과했다. 아래 P03 빌드에도 포함되며 새 입력의 실기는 대기 중이다.
- P03-a 구현·자동 검증: HMD 평면 방향 이동(1.6m/s), 머리 위치 중심 연속 회전(60도/s), CharacterController 높이·중심 갱신, 가상 이동 정지 인터페이스. 런타임·PlayMode 어셈블리 구성. EditMode 12/12, PlayMode 4/4 통과, Windows Mono 시험장 빌드 성공. [증빙](evidence/p03-locomotion.json).
- P03-a 커밋: `e959ef4`, [PR #2](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/2)는 원격에서 codex/p01-environment에 병합됨(`9106a2d`). 해당 이동 변경을 main에 올리는 [PR #4](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/4)는 OPEN이다.
- P03-b 진행: 실제 머리의 수평 이동을 캡슐로 sweep하고 막힌 이동량만 XR 원점에서 보정한다. HMD의 local pose는 변경하지 않는다. 시험장의 열린 두 면에 벽을 추가했다. 경계 누락과 얇은 벽을 통과하는 머리 이동의 재현 시험이 각각 실패함을 확인한 뒤 수정했다.
- P03 실기: 2026-09-23 사용자가 왼쪽 이동·오른쪽 연속 회전·두 스틱 동시·위아래 보며 이동을 확인했다. [실기 기록](evidence/2026-09-23-vr-movement-fire-check.md). 벽 밀착·복귀·재중심의 편안함과 독립 리뷰는 아직 남았다. 이전 리뷰의 사용량 제한 실패를 승인으로 취급하지 않으며 원격 병합 상태와 검증 상태를 구분한다.
- P03-b 검증: EditMode 12/12, PlayMode 5/5 통과와 Windows Mono 재빌드 성공. 최초 수정의 정지 중 2cm 상승 회귀를 수평 보정으로 수정했다. [최신 시험·빌드 해시](evidence/p03-boundaries.json). 빌드 경로는 이전 P03-a와 같고 최신 파일로 갱신됐다.
- P03-b 커밋: `7b39d48`, PR #2에 포함됨. 위 실기 기록을 함께 따른다.
- P04-a: `e13e10e`, FireGate·RunPhase·K2FireControl과 P05의 순수 MagazineState 구현. 최종 EditMode 29/29 통과. [검증 증빙](evidence/p04-firing-rules.json). [PR #3](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/3)은 원격에서 codex/p03-locomotion에 병합됨(`ec79139`). P04는 아직 main 및 PR #4의 head에 포함되지 않는다.
- P04-b: 오른손 고정 임시 총·왼손 앞부분 근접 잡기·오른손 트리거·피해 Raycast·총구 차단 연결. WorldSolid/Enemy/Weapon/PlayerBody 레이어 구성. 시험장에 명중 표적·잔탄 표시·재사용 트레이서 배치. EditMode 30/30, PlayMode 13/13 통과와 Windows Mono 빌드 성공. [최신 증빙](evidence/p04-weapon-lab.json). 실제 기기 T04/T05/T11과 독립 리뷰는 대기다.
- P04-b 실기: 2026-09-23 사용자가 양손 사격과 표적 명중을 확인했다. 위 P04-b 항목의 기기 대기는 당시 상태이며, 현재 남은 범위는 오른손 해제 시 고정·한 손 차단·앞부분 해제 차단·벽 차단과 독립 리뷰다.
- P05-a: MagazineToken과 MagazineSocket 기반 구현. 왼손 소유 Fresh 토큰만 거리 12cm·각도 35도 조건으로 삽입하며, 탈거 시 잔탄 폐기와 Discarded 전환을 함께 처리한다. 초기 만탄 누락이라는 독립 리뷰 지적을 실패 재현 후 수정했다. 씬에는 아직 연결하지 않아 기존 시험장은 30발 소진 후 재시작해야 한다.
- P05 다음 실행: 왼손 전체 소유권·허리 보급·객체 수명과 프리팹 연결. P06~P12는 미착수다. Token의 손별 소유 검증이 전체 왼손의 여러 물체 동시 소유까지 막는다고 해석하지 않는다.

## 확인한 환경과 증빙

- Unity 실행 파일: `D:/Unity editor/6000.3.23f1/Editor/Unity.exe`.
- 기존 프로젝트의 batchmode 임포트·컴파일은 2026-09-22 정상 종료했다. 로컬 로그: `tmp/p01/baseline.log`, 끝의 반환 코드 0 확인.
- Test Framework 1.6.0은 잠금 파일에 존재한다. AI Navigation은 아직 설치되지 않았다.
- Windows OpenXR ActiveRuntime: Virtual Desktop의 `virtualdesktop-openxr.json`.
- Virtual Desktop Streamer 1.34.22.0, 헤드셋 앱 1.34.22 - 207. 사용자 화면: High, 표시 해상도 2496×2688, 90Hz, 270Mbps, SSW Always enabled, SGSR 켜짐. 코덱 미확인. 현재 프로필과 계획의 72Hz·보간 끔 성능 시험을 구분한다.
- 지정된 `.github/agents/*_agent.prompt.md`와 `main_instruction.prompt.md`는 현재 저장소에 없다. 해당 파일의 규칙을 추측하지 않는다.

## 실행 판단 기록

- Ruling: 기존 Unity 경로 유지와 필요한 경우에만 worktree를 사용하는 Goal 지침에 따라 현재 깨끗한 체크아웃의 작업 브랜치를 사용한다. 병렬 에디터는 실행하지 않는다. 비용: 같은 경로에서 사용자가 동시에 수정하면 충돌을 확인해야 한다.
- Ruling: 실행 스킬의 task-start는 `Task 1` 헤더를 요구하지만 승인된 계획은 `P01` 헤더를 사용한다. 계획 원문을 바꾸지 않고 P 번호로 작업과 증빙을 기록한다. 비용: 스킬 자동 장부 대신 진행 문서를 직접 유지한다.
- Ruling: Windows IL2CPP 모듈이 없어 시험장 빌드에 설치된 Mono를 사용한다. 빌드 메서드는 원래 백엔드를 복원한다. 비용: 최종 게임의 백엔드가 달라지면 해당 빌드로 성능·동작을 다시 검증해야 한다.
- P01-a RED→GREEN: 전용 씬 부재로 환경 시험 3개 실패 후, 씬 생성으로 3개 통과. 리뷰 후 최종 EditMode 재실행도 3/3 통과. Windows Mono 개발 빌드 성공과 실행 파일을 확인했다. 이후 사용자 회신으로 VR 실행·머리·양손 추적 정상 결과를 확보했다. 최초 자동 검증 JSON의 hardware 대기는 당시 상태이며 새 실기 증빙을 함께 읽는다.
- 엔진 생성 변경: 빌드·시험의 URP 사전 계산·OpenXR 참조 갱신·UnityConnect 변경은 원래 파일을 백업 후 복원했다. 빌드 실패 중 일시 이동된 XR simulation 파일 4개도 원본 내용 일치를 확인하고 복원했다. 로컬 증빙은 `tmp/p01/`에 유지한다.
- 리뷰: 중대한 지적 없음. 모든 열린 씬의 미저장 상태를 확인하도록 시험 보호를 보완했다. Minor deferred: `SharedSceneCanBeLoaded` 이름과 달리 해당 시험은 씬 자산의 존재를 확인한다. 실제 로드·XR 구성 검사는 별도 InteractionLab 시험이 담당한다.
- 리뷰 범위 밖 판정: 실기·VD·편안함은 실제 기기 결과로 판정하고, 전용 입력·연속 회전·런타임·PlayMode 어셈블리·Bootstrap 동작은 후속 구현 대상으로 남긴다. 비용: P01 전체 완료나 최종 플레이 가능 상태로 해석해서는 안 된다. Mono 빌드와 엔진 생성 변경은 실행자가 최종 로그·diff로 확인한다.
- P01→P03/P04: XR Origin·좌우 입력은 공통 PlayerRig의 기반이다. 오른손 총 고정과 왼손 소유권은 후속 단계에서 일반 잡기와 충돌하지 않게 적용한다.
- Ruling: P03은 승인된 CharacterController 기반 제어기로 이동을 통합하고 기존 XRI 이동·회전·중력 공급자와 샘플의 이동 액션 전환기를 비활성화한다. 이유: 중복 이동과 액션 비활성화 경쟁을 피하고 RunController의 정지 계약을 제공한다. 비용: XR 실기에서 이동·상호작용 동시 동작을 다시 확인해야 한다.
- Ruling: 실제 머리 수평 이동도 이전 캡슐 위치에서 충돌 검사하고 차단량으로 XR 원점을 보정한다. 이유: 캡슐 중심을 곧바로 이동하면 얇은 벽을 통과한다. 비용: 벽에 밀착한 실제 움직임에서 가상 공간 보정의 편안함과 재중심 동작을 반드시 실기로 검증해야 한다. 물리 공간에서 사람의 이동을 막는 기능은 아니다.
- Ruling: P01 기기 수락을 기다리는 동안 P01 브랜치 기반의 P03 의존 PR로 구현을 분리한다. 이유: 미완료 P01을 병합하거나 이동 변경을 같은 PR에 누적하지 않는다. 비용: P01 변경 시 P03 재검증과 기본 브랜치 갱신이 필요하다.
- P01-b 증빙: [입력 시험 결과](evidence/p01-project-input.json). 설정 재실행 후 입력·리그·두 씬의 해시가 동일했다. 리뷰에서 현재 씬의 외부 리그 참조와 부모 관계가 없음을 확인했다. 도구는 임의의 편집된 씬 관계를 보존하는 일반 마이그레이터가 아니므로 초기 기반 씬으로 사용 범위를 제한했다.
- P02→P09: 실제 외형·충돌·가림 검증을 MapRoot로 연결한다. 임시 시험장을 최종 맵으로 판정하지 않는다.
- P04→P05: FireGate는 탄창 삽입과 왼손 앞부분 잡기를 별도로 요구한다. 재장전 완료만으로 발사하지 않는다.
- Ruling: P04-a에서 P05의 승인된 순수 MagazineState 계약을 먼저 구현한다. 이유: 발사와 재장전이 하나의 탄약 상태를 공유하여 임시 중복 탄약 코드를 만들지 않는다. 비용: Fresh 물체·왼손 소유·삽입 거리 검증은 P05에서 별도로 완성해야 하며 순수 규칙 통과를 수동 재장전 완료로 확대할 수 없다.
- Ruling: K2FireControl은 0.1초 간격에서 요청당 최대 한 발만 처리하고 지연된 발사를 누적하지 않는다. 이유: 긴 프레임 후 갑작스러운 여러 발과 손 해제 뒤 예약 사격을 방지한다. 비용: 프레임이 심하게 느리면 실제 발사율이 낮아지므로 성능 시험에서 확인한다.
- P05-a 리뷰 반영: 초기 Fresh 토큰에 부분 소모된 MagazineState를 연결할 수 있던 누락을 `Capacity` 읽기 속성과 만탄 검사로 수정했다. `InitialFreshTokenRejectsPartiallyConsumedAmmoWithoutMutatingToken`은 수정 전 실패했다. 새로운 실행의 초기 장착에만 적용하며 정상 부분 재장전은 Eject/InsertFresh 흐름을 사용한다.
- P05-a 리뷰 범위 밖: 전체 손 소유권·pouch/pool·씬 연결·기기 조작·시간 경과 후 낙하 충돌은 후속 구현/검증 대상이다. 현재 물리 시험은 Rigidbody 상태 전환을 검사하며 실제 낙하 완료를 증명하지 않는다. 비용: 통합 전까지 P05 완료나 실제 수동 재장전 가능으로 판정할 수 없다.
- P06→P07: 실제 생성 성공 후 적 ID를 등록한다. 중복 사망·시스템 회수는 처치를 늘리지 않는다.
- P07→P08: 기록은 ReachedWave와 같은 도전의 Kills를 저장한다. 사망 확정이 클리어 회복보다 먼저다.
- P08/P09→P11/P12: 씬 교체 시 공통 전투 수명·객체 상한·기록 격리를 시험하고 최종 빌드로 재검증한다.

## 필요한 사용자 입력

사용자 확인(2026-09-22): Quest Pro·Virtual Desktop 연결 가능, 방 스캔은 아직 촬영 전, 12월 중순 마무리 예상. 실제 스캔 파일과 실측 길이 두 개는 촬영 후 필요하다. 12월 중순은 사용자 목표 일정이며 공식 제출일로 단정하지 않는다. 제출 형식·스캔 앱 허용 조건은 미확인.
