# Goal 실행 현황

갱신: 2026-09-27. 전체 목표는 미완료이며 진행 중이다.

## 현재 작업

- 현재 기준 커밋: `1344f9a` (공통 전투 세션). 이후 Bootstrap 통합 단위의 최종 SHA는 PR #8 기록에 연결한다.
- 브랜치: `codex/p08-records`. P07 기반 의존 작업이다. 기록·HUD·메뉴·이동/회전 설정과 Bootstrap/Combat 통합을 구현했다. 최신 자동 검증은 PlayMode 96/96, EditMode 50/50이며 실제 두 맵·실기·독립 리뷰는 남아 있다. PR #8은 초안·미병합 상태다.
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
- P05-b 구현·자동 검증: 왼손 소유권, 새 그립 입력당 허리 탄창 1개, 전체 토큰 10개·바닥 8개 상한, 12초 회수와 손 소유 토큰 보호, 프리팹 연결. EditMode 31/31, PlayMode 25/25와 Windows Mono 빌드 성공. 실제 중력 낙하·바닥 정지 시험을 포함한다. [증빙](evidence/p05-manual-reload.json), [실기 절차](evidence/p05-reload-hardware-check.md). P05-a 커밋 `9dde1bb`, [초안 PR #5](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/5).
- P05 미완료 범위: Quest Pro T06/T07 각 10회와 조작 편안함, 독립 리뷰. 리뷰 에이전트는 사용량 제한으로 종료되어 승인으로 취급하지 않는다. P07 전체 정지/재개 통합도 남아 있다. 현재 시험장은 재시작 없이 수동 탄창 교체가 가능하다.
- P06-a 구현·자동 검증: `codex/p06-enemies`에서 근접 준비 후 거리/가림 재검사, 원거리 0.8초 예고와 발사 순간 조준, 고정 방향 투사체의 구간 충돌·최초 피해·4초 수명, 사망 1회 알림을 구현했다. 신규 9개 시험 실패 후 전체 PlayMode 34/34 통과 후 독립 리뷰의 검증 공백 6개를 보완하여 최종 40/40 통과. 리뷰에서 코어 범위의 중대한 결함은 발견되지 않았다. [증빙](evidence/p06-attacks.json). 아직 씬에서 구동하지 않는 공격 코어이며 P06 완료가 아니다.
- P06-a 커밋 `f067136`, [초안 PR #6](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/6). P05의 기기 검증 대기 브랜치를 기반으로 하며 병합하지 않았다.
- P06-b 구현·검증 완료(실기 대기): NavMesh 우회·정지·5초 정체 복구, 12개 투사체 풀, 사망 즉시 피격 해제·5초 사체 회수와 객체 재사용, 두 적 프리팹·직접 만든 애니메이션·시험장 연결. 최종 PlayMode **48/48**, EditMode **31/31**, Windows Mono 빌드 성공. [증빙](evidence/p06-navigation.json), [실기 절차](evidence/p06-enemy-hardware-check.md), [자산 출처](evidence/p06-assets.md).
- P06-b 결함 수정: 최초 씬 시험의 적 0개는 prefab 참조 3개 누락이었다. 씬을 연 뒤 자산을 다시 로드해 해결했다. 리뷰에서 제기한 HMD 근처 책상 상단 NavMesh 선택은 PathPartial 실패로 재현했고, 이동·복구는 플레이어 캡슐 발밑을 사용하도록 수정했다. 공격 조준은 HMD를 유지한다. 실제 가구 우회 이동, 공격 중 정지, 무효 복구점과 씬 시작·재시작·정리 시험이 포함된다.
- P06-b 독립 재검토: 수정된 코드·씬 참조와 결과 XML을 읽기 전용으로 확인했고 범위 내 남은 중요 결함을 발견하지 못했다. Quest Pro T10/T11과 외형·양안 표시·회피 편안함은 아직 대기이므로 PR #6은 초안으로 유지한다.
- P06-b 격리 검증: 사용자 에디터가 SampleScene을 열고 있어 `tmp/p06-verification` detached worktree에서 실행했다. 모든 검증/빌드 프로세스는 정상 종료했다. 원본 생성 자산이 snapshot과 같음을 확인한 뒤 수정된 씬 참조와 NavMesh를 반영했고, 원본·검증 복사본의 VRFPS 파일 해시 전체가 일치함을 확인했다. 사용자 OpenXR·튜토리얼 설정 변경은 보존하고 커밋 범위에서 제외한다. 비교 기록은 `tmp/p06-navigation/workspace-source-snapshot.json`이다.
- 새 빌드: `Zombie Shoot VR/Builds/Windows/EnemyLab/InteractionLab.exe`. 기존 P05 시험장 빌드는 유지한다. X/F6으로 적 시작·정지, Y/F7로 적 초기화. 이 시험장의 피격 횟수는 최종 HP·웨이브·사망 시스템을 대신하지 않는다.
- 다음 실행: P08 설정·Bootstrap·맵 선택/기록 통합을 진행하며 T06/T07·T10/T11 및 메뉴·HUD 실기 결과를 반영한다. P09 실제 맵과 P10~P12의 필수 결과물은 아직 완료되지 않았다.

## 확인한 환경과 증빙

- Unity 실행 파일: `D:/Unity editor/6000.3.23f1/Editor/Unity.exe`.
- 기존 프로젝트의 batchmode 임포트·컴파일은 2026-09-22 정상 종료했다. 로컬 로그: `tmp/p01/baseline.log`, 끝의 반환 코드 0 확인.
- Test Framework 1.6.0과 AI Navigation 2.0.14가 잠금 파일에 존재한다.
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

## P07 진행

- P07-a: VitalState와 WaveProgress 순수 규칙. HP 상한·0.5초 보호 경계·사망 후 회복 차단·웨이브별 한 번 회복·생성 대기/생존 적의 클리어 차단·중복 ID·누적 처치와 실제 시작 웨이브를 검증했다.
- 2026-09-23 신규 EditMode 6개 실패 후 전체 37/37 통과. 2026-09-26 결과 XML과 검증 복사본/원본 소스 해시 일치를 재확인했다. [증빙](evidence/p07-rules.json). 새 시험 실행으로 표시하지 않는다.
- 자체 검토 완료, 독립 리뷰는 사용량 제한으로 미실행이다. 승인이나 병합 조건 충족으로 취급하지 않는다.
- 다음: RunController의 피해/사망 우선 처리와 준비 8초, SpawnDirector 실제 생성 성공 등록, 전체 정지/재개, PlayMode 수명 시험과 T08/T09/T12/T13 실기. 씬·빌드는 아직 P06 동작이다.

### P07-b 실행 흐름 및 플레이어 정지 연결

- P07-a 커밋 `0bb1c6e`, [초안 PR #7](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/7), base `codex/p06-enemies`. 미병합.
- RunController: 준비 8초 종료 시 실제 웨이브 시작, 피해·사망 즉시 확정 후 프레임 후반 클리어/회복, 사망 이벤트 1회, 정지 시 전투·준비 시계 동결, 재도전 시 체력·집계 초기화. MapDefinition 유효성 및 GameBalance 할당 상한 추가.
- RunCombatBindings: 총 발사와 가상 이동 정지, 추적 Transform 갱신 유지, 탄창 Rigidbody 속도 보존/복원과 수명 동결. 기존 수동 재장전 시험도 재실행했다.
- 2026-09-26 신규 수명 시험 7개 RED, 정지 연결 시험 2개 RED 후 전체 PlayMode **57/57**, EditMode **37/37** GREEN. 실제 LateUpdate 순서의 동시 마지막 처치/치명타와 독립 물리 씬의 낙하 동결을 포함한다. [증빙](evidence/p07-lifecycle.json).
- 자체 검토 완료. 독립 리뷰 미완료. Headless OpenXR 기기 부재 로그가 있으며 자동 시험을 실기 추적 증빙으로 사용하지 않는다. 새 빌드는 아직 만들지 않았다.
- 다음 작업: SpawnDirector의 실제 NavMesh 생성·상한·실패 재시도와 적/투사체 정지, 탄약·객체 재도전 초기화, 시험장 씬 연결 및 Windows 빌드, Quest Pro T08/T09/T12/T13.
- Ruling: P07의 클리어 판정은 실행 순서 10000의 LateUpdate에서 처리한다. 기존 무기 LateUpdate와 적/투사체 Update의 피해가 먼저 확정되도록 하기 위함이다. 새로운 피해 생산자를 이 순서 뒤에 추가하면 동시 사망 시험과 처리 순서를 함께 갱신해야 한다.

### P07-c 실제 적 생성과 웨이브 상한

- SpawnDirector가 실제 NavMesh 완전 경로·플레이어 발밑 거리·생성 공간·동시 적/원거리 수를 확인하고 1.5초 예고 후 다시 검사한다. 실패는 Pending과 Kills를 바꾸지 않는다. 원거리 상한으로 기다리는 동안 남은 근접 할당을 먼저 사용한다.
- 1~3웨이브 정확한 유형 수와 6웨이브 원거리 동시 1명/전체 4명, 할당 보존을 실제 프리팹으로 확인했다. 체력·피해 배율은 정수 백분율로 올림하고 원거리 수는 내림한다. 원거리 수용 0 설정의 정체 가능성을 RED로 재현하고 시작 전 거부하도록 수정했다.
- 정지 시 적 NavMeshAgent·Animator·투사체·예고 시간이 멈춘다. 재도전 시 기존 적과 투사체/예고를 정리하고 같은 적 객체를 재사용한다. 생성 ID는 재사용하지 않는다. 보이는 사체는 최대 4개이며 방 객체 풀은 최대 13, 강의실 최대 16개다. 생존 상한은 별도다.
- 2026-09-26 최종 PlayMode **67/67**, EditMode **37/37** 통과. 신규 6개 RED, 보강 9개 중 설정 검증 1개 RED 후 수정했으며 마지막 회귀에는 사체 상한 시험도 포함했다. [증빙](evidence/p07-spawning.json).
- 생성 예고 원기둥은 직접 구성한 Unity primitive이며 기존 자체 제작 LabTarget 재질을 공유한다. 별도 외부 에셋을 추가하지 않았다.
- 자체 검토 완료, 독립 리뷰/실기 미완료. 기존 InteractionLab과 실행 빌드는 아직 P06 driver를 사용한다. 다음은 탄약·탄창 재도전 초기화, P07 시험장 씬 연결·Windows 빌드와 실기 절차 준비다.
- Ruling: 생성 시야 판별 없이 모든 선택 지점에 1.5초 예고를 적용한다. XR 화면의 가시성을 자동 시험으로 추정하지 않으면서 보이는 생성점의 예고를 보장한다. 비용은 첫 생성 및 재선택 시 비가시 지점에도 예고 대기가 적용되는 것이다. 실제 생성 사이 간격은 최소 2초다.

### P07-d 재도전·WaveLab 통합과 빌드

- 2026-09-26 신규 재도전/씬 시험 2개 RED 후 구현. 재도전 시 탄창 풀을 재사용하여 장착 1개·30발로 복구하고 손 소유권·사격 간격을 초기화한다. 누른 채 재시작한 그립은 새 잡기로 처리하지 않는다.
- 별도 `Scenes/Tests/WaveLab.unity`를 P06 시험장에서 파생했다. 기존 InteractionLab은 보존한다. 실제 씬은 준비 8초→웨이브→피해/사망과 X/F6 정지·Y/F7 재도전을 연결한다. 총 입력은 RunController의 전투 시계를 공유한다.
- 최종 **PlayMode 70/70**, **EditMode 37/37** 통과. 실제 씬에서 적 생성·처치 집계와 10회 재도전, 씬 해제 후 적·탄창 정리, 재시작 후 사격 시계 0의 발사를 검증했다. 중간 실패는 씬 Load 완료와 Start 시점, Headless XR 비활성 손 계층을 잘못 가정한 시험 코드였다. 해당 가정을 수정하고 전체 회귀를 다시 실행했다.
- Windows Mono 개발 빌드 성공(exit 0). `Zombie Shoot VR/Builds/Windows/WaveLab/WaveLab.exe` 및 동반 파일 337개의 복사 해시가 모두 일치한다. 소스 VRFPS 파일 181개도 검증 복사본과 일치했다. [시험·빌드 증빙](evidence/p07-wave-lab.json), [실기 절차](evidence/p07-wave-hardware-check.md).
- 생성기·시험 실행 전 한 차례 자동 승인 검토 사용량 오류로 실행되지 않았다. 안내된 재시도 시각 경과를 현재 시각으로 확인한 후 같은 승인 절차로 실행했고 성공했다. 우회 실행하지 않았다.
- 실기 결과를 요청했다. T08/T09/T12/T13의 실제 Quest Pro 결과와 독립 리뷰는 아직 대기다. 미완료 항목 때문에 PR #7은 초안으로 유지한다.
- 다음: P08 맵별 최고 기록 저장·오른팔 UI·메뉴. 실제 방/강의실 촬영본·거리 2개, 제출 공지, P05~P07 잔여 실기 결과는 별도 대기다.
- Ruling: P07 시험장을 별도 WaveLab 씬/빌드로 제공하여 기존 실기 증빙의 P06 실행 경로를 보존한다. 두 시험장은 공통 프리팹과 전투 코드를 사용하므로 공통 변경은 두 씬 회귀 시험으로 확인한다. 비용은 시험장 씬 두 개를 유지해야 한다는 것이다.

## P08 기록·UI·메뉴

- P07-d 커밋 `12bcddc`는 PR #7에 push돼 있으며 초안·미병합 상태다. WaveLab 실기 결과는 아직 대기다.
- P08-a: RecordStore와 BestRecord 구현. 맵별 최고 도전의 웨이브/처치를 함께 저장하며 같은 웨이브에서만 처치로 비교한다. schema 1의 필수 필드·맵 ID·음수·중복 맵을 검사한다.
- 임시 파일을 닫고 읽어 검증한 후 본 파일로 교체한다. 기존 정상 파일만 `.bak`으로 남기며, 손상된 본 파일을 복구할 때 정상 백업을 덮어쓰지 않는다. 쓰기 실패는 false/LastError로 보고하고 메모리의 최고 기록을 유지하여 재시도할 수 있다.
- 2026-09-26 신규 11개 RED 후 구현. 중간 48개 중 2개 실패로 InvalidDataException 처리 누락을 확인해 수정했다. 추가 백업/문서 형식 시험을 포함한 최종 **EditMode 50/50**, 기존 **PlayMode 70/70** 통과. [증빙](evidence/p08-records.json).
- 실제 사용자 기록은 사용하지 않았으며 시험별 새 GUID 임시 폴더에서 파일 잠금·쓰기 불가·본 파일/백업 손상을 확인했다. 자체 검토 완료, 독립 리뷰는 남아 있다.
- 다음: RunEnded 저장 1회 연결과 오류 표시, 오른팔 HUD, 맵 선택·정지·결과·재도전 메뉴, Windows 빌드 검증. 현재 WaveLab 빌드에는 아직 P08 저장 기능이 연결되지 않았다.
- Ruling: .NET DataContractJsonSerializer의 필수 필드 검증을 사용해 누락된 kills를 0으로 오인하지 않게 한다. 추가 패키지 없이 계획의 schema 1 JSON 형식을 유지한다. 비용은 향후 IL2CPP로 전환할 경우 직렬화와 코드 보존을 해당 빌드에서 다시 검증해야 한다는 것이다.

### P08-b 종료 결과와 저장 연결

- P08-a 커밋 `5416507`, [초안 PR #8](https://github.com/hyunsuki5329/zombie-shoot-vr/pull/8), base `codex/p07-progression`. 미병합.
- RunRecordBinding이 실제 RunEnded에서 맵 ID·ReachedWave·Kills를 고정해 저장한다. 중복 종료나 성공 후 저장 재시도는 파일을 다시 쓰지 않는다. 실패해도 사망과 결과 상태를 유지하며 저장만 다시 시도할 수 있다.
- 새 도전 시 현재 결과 표시 상태를 초기화하고, 다른 RunController에 재연결하면 이전 구독을 해제한다. 같은 RecordStore의 맵별 최고 기록은 유지한다.
- 2026-09-27 신규 PlayMode 5개 RED 후 구현. 최종 **PlayMode 75/75**, **EditMode 50/50** 통과. 5 클리어 후 준비에서 종료하면 5, 6 시작 후 사망하면 6, 같은 도전의 50처치가 저장되는 것을 실제 디스크 재로드로 확인했다. [증빙](evidence/p08-results.json).
- 자체 검토 완료, 독립 리뷰 대기. 아직 UI 씬에 바인딩하지 않았으므로 기존 실행 빌드는 P07이다. 다음은 오른팔 HUD·결과/오류 패널·메뉴 입력 연결이다. 메뉴를 닫은 입력이 같은 프레임의 사격으로 이어지지 않도록 트리거 해제 조건을 함께 검증해야 한다.

- P08-c (2026-09-27): 정지·사망 상태 전환 뒤 트리거 해제를 요구하여 메뉴 입력이 사격으로 이어지는 문제를 차단했다. 같은 프레임에 정지·재개해도 재현되던 실패 1개를 확인한 후 PlayMode 76/76 통과. [증빙](evidence/p08-menu-input.json). 팔 UI·메뉴 씬 연결·새 빌드·실기는 대기이며 PR #8은 초안 유지.

- P08-d (2026-09-27): 오른팔 HP·실제 도달 웨이브·처치 수·상태 HUD와 저체력 색/느낌표를 구현하고 WaveLab에 프리팹을 연결했다. 표시 시험 2개 RED 후 전체 PlayMode 78/78 통과. 첫 실행의 1개 실패는 첫 웨이브 시험 데이터 8→6 수정. [증빙](evidence/p08-hud.json). 패널은 12×7cm, 컨트롤러 뒤 12.5cm 초기 후보이며 가독성·가림·실기와 새 빌드·메뉴는 미검증/미완료다.

- P08-e (2026-09-27): 메뉴 상태 제어 구현. 설정 진입 시 정지, 설정 닫기 후 명시적 재개, 사망 결과·같은 맵 재도전·맵 선택 시 현재 도전 종료를 검증했다. 3개 RED 후 전체 PlayMode 81/81 통과. [증빙](evidence/p08-menu-state.json). 실제 버튼/씬·설정 값·맵 로딩·결과 화면 연결은 다음 작업이다.

- P08-f (2026-09-27): uGUI 메뉴 프리팹·XR raycaster/EventSystem·재개/재도전/결과 표시를 WaveLab에 연결했다. 저장 실패 재시도 버튼은 독립 기록 시험에서 검증했다. 2개 RED 후 전체 PlayMode 83/83 및 Windows 빌드 성공, MenuLab 복사 337개 파일 해시 일치. [증빙](evidence/p08-menu-view.json), [실기 절차](evidence/p08-menu-hardware-check.md). 연습장 점수는 저장하지 않으며 설정 조절/실제 맵 선택 버튼은 비활성이다. Bootstrap·실제 맵·영구 기록 씬 통합·실기·독립 리뷰는 남아 있다.

- P08-g (2026-09-27): 이동 1.0~2.0m/s·회전 30~90deg/s 설정과 PlayerPrefs 저장, 메뉴 증감 버튼을 연결했다. 독립 키에서 실제 이동·회전·재로드·범위/비정상 값·버튼 동작을 검증했다. 2개 RED 후 전체 PlayMode 86/86, Windows 빌드 성공, MenuLab 337개 파일 해시 일치. [증빙](evidence/p08-settings.json). 실제 스캔 맵 선택과 실기·독립 리뷰는 대기다.

- P08/P09 맵 로딩 선행 작업 (2026-09-27): MapRoot가 외형/충돌/시작점·WorldSolid·NavMesh를 검사하고 연결되지 않은 적 생성점을 제외한다. 유효 후보가 없으면 시작을 거부한다. 2개 RED 후 전체 PlayMode 88/88, 분리된 NavMesh 섬 사례 추가 후 맵 시험 3/3 통과. [증빙](evidence/p09-map-root.json). 실제 촬영 맵이나 Bootstrap에는 아직 연결하지 않았으며 합성 시험장을 실제 스캔 검증으로 취급하지 않는다.

- P08/P09 맵 로더 (2026-09-27): 저장된 합성 Unity 씬을 실제 추가 로드/해제하고 MapRoot 검증 후에만 공개한다. 잘못된 씬 정리 후 재시도와 없는 씬 선택 시 기존 맵 유지 검증. 3개 RED 후 최초 90/92에서 시험 씬 사전 등록 방식으로 수정하여 전체 92/92 통과. [증빙·재현 명령](evidence/p09-map-loader.json). 격리 프로젝트는 시험 전에 MapLoaderFixtureSetup.EnableForTests로 씬 목록을 등록해야 한다. 실제 사용자 프로젝트의 빌드 씬 목록은 바꾸지 않았다. Bootstrap/Combat 연결과 실제 맵은 다음 작업이다.

- P08/P09 공통 전투 세션 (2026-09-27): 검증한 맵 로딩→이전 런타임 정리→머리 로컬 추적 자세를 보존한 시작점 배치→탄창/도전 초기화→생성/기록 연결을 구현했다. 2개 RED 후 전체 PlayMode 94/94, 추적 오프셋 사례 보강 후 세션 시험 2/2 통과. 합성 맵 10회 재로딩의 씬/런타임 수 유지 및 실제 적 생성·사망 기록 저장을 확인했다. [증빙](evidence/p09-combat-session.json). Bootstrap/Combat 씬에 아직 배치하지 않았으며 코디네이터는 Combat 해제 전 Shutdown을 기다려야 한다. 실제 두 맵 전환·실기는 별도 대기다.

### P08/P09 Bootstrap·Combat 통합 (2026-09-27)

- Bootstrap에서 공통 Combat 씬을 로드하고 맵별 최고 기록·선택·설정 화면을 연결했다. 검증한 맵 시작, 사망 결과 저장, 맵 메뉴 복귀, 로딩 도중 종료의 정리 순서를 합성 씬으로 시험했다. 관련 P08/P09, R02/R14/R15, T02/T14/T15의 기반 구현이며 실제 공간·기기 시험 완료를 의미하지 않는다.
- 초기 93/95 실행의 프레임 수 기반 대기와 씬 누수, 91/96 실행의 시험 씬 미등록을 수정했다. 최종 **PlayMode 96/96, EditMode 50/50**. 시험 클래스의 사전/사후 훅이 합성 씬을 자동 등록하고 원래 빌드 목록을 복원한다. 위 과거 맵 로더 항목의 수동 EnableForTests 절차는 이제 필요하지 않다.
- Windows x64 Mono 개발 빌드 성공, 원본 프로젝트로 복사한 340개 파일의 SHA-256 일치. [증빙](evidence/p09-bootstrap.json). 재현: Unity batchmode에서 `-runTests -testPlatform PlayMode` 또는 `EditMode`; 빌드는 `-executeMethod VRFPS.Editor.BootstrapSetup.Build -quit`이며 격리 프로젝트를 사용한다.
- 실행: `Zombie Shoot VR/Builds/Windows/BootstrapPreview/BootstrapPreview.exe`. **실제 Room/Classroom 씬이 없어 선택 버튼이 비활성인 메뉴 미리보기**다. 전투 조작 시험은 기존 `Builds/Windows/MenuLab/WaveLab.exe`를 사용한다. 합성 시험 씬은 이 빌드에 포함하지 않는다.
- 배포 씬 목록은 Bootstrap→Combat으로 변경했다. 기존 XR 설정 객체 참조는 유지했다. 독립 리뷰와 Quest Pro 메뉴 조작·팔 HUD 가독성·가림 검증은 대기이며 PR #8은 초안으로 유지한다.
- 다음: P10 첫 조작 안내·소리/진동/피드백과 P11 측정 준비를 진행한다. 실제 촬영본·기준 길이 2개 확보 후 P02 평가 및 두 맵 통합, 실기와 성능 시험을 수행한다. 현재 자동 시험 결과를 실제 두 맵의 완료 증빙으로 확대하지 않는다.
