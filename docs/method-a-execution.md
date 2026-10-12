# Method A 실행 기록

2026-10-13 · 기준 f496b84 · codex/p09-scanned-room

설계: [고정 맵 테마](VRShoot_Method_A_Codex_Guide.reviewed.md).

- A00: 기존 사진형 Room과 자동 시험을 기준선으로 보존한다.
- A01: 수동 구조 자산, 맵별 고정 테마, 유효성 검사와 기존 충돌/앵커 이전.
- A02: 편집기에서 안전하게 외형 생성; 입력 오류·반복 생성·사용자 오브젝트 보존 시험.
- A03: 내 방 폐가의 벽·바닥·가구 외형과 경로/전투 대응, 미리보기 및 Windows 빌드.
- A04: 실제 강의실 자료 확보 후 폐교 제작. 현재 촬영 전이므로 완료 불가.
- A05/A06: 원본 의존성 제거·복원·자동 회귀·실기 인수.

Ruling: 기존 기능 브랜치와 검증용 격리 프로젝트 tmp/current-source-restoration/Source/Zombie Shoot VR를 재사용한다. 열려 있는 사용자 에디터를 배치 실행으로 덮어쓰지 않는다. 기존 두 사용자 설정 변경은 보존한다.
Pre-flight: 구조→외형은 읽기 전용, 구조→NavMesh는 별도 재생성, 두 맵은 구조 공유 금지. 현재 사진형 검사는 로컬 참조 시험으로 분리해야 한다.
Ruling: 원본 사진형 Room은 백업하고 새로운 미리보기 씬에서 먼저 검증한다. 알려진 74/74·127/127은 이전 빌드의 기록이며 새 코드 검증을 대신하지 않는다.

현재: A01 테스트 준비. 전체 Goal은 미완료.

## 작업별 계약

1. Runtime/Maps/RoomStructureDefinition.cs: 순수 직렬화 구조, origin/collision/navigation 역할, 유한 좌표·회전·양수 크기·중복 ID 검사, room/classroom 고정 themeId 검증. Tests/EditMode/RoomStructureTests.cs에서 독립 자산 존재와 잘못된 입력 거부를 RED→GREEN으로 확인한다.
2. Editor/RoomStructureSetup.cs: 기존 Room CollisionRoot의 로컬 박스와 앵커를 수동 분류하여 Configuration/Maps/RoomStructure.asset에 저장한다. 누락·비단위 스케일·미지원 콜라이더는 오류 처리한다. 기존 Root와 구조 데이터 비교 시험을 실행한다.
3. Editor/RoomThemeSetup.cs: 별도 RoomHousePreview.unity에 구조 생성·NavMesh 베이크·폐가 시각 루트 생성. 원본 스캔 참조 없음. 생성 소유 표식과 staging 교체를 사용한다. 반복/오류/잘못된 테마 시험, 전체 회귀, Windows 빌드로 확인한다.
4. 미리보기 시각 확인 후 Room의 기존 메뉴 연결을 전환한다. 사진형 기준 씬은 로컬 백업한다. 기존 스캔 표현 요구 시험은 참조와 최종 구조로 구분한다.

Expected: RED는 새 타입/생성기 미존재 또는 계약 위반으로 실패; GREEN은 해당 필터와 전체 suite 실패0. XML·로그를 tmp/method-a에 보존하고 해시·수를 docs/evidence에 기록한다.

A01 데이터: 계약 시험 RED 7/7 실패(구조 타입 미존재) → GREEN 전체 EditMode81/81 통과. 충돌/앵커 이전 시험 준비.

A01 구조 이전: RED 생성기 미존재 → GREEN 전체 EditMode82/82. 테마 렌더러와 실제 VR 인수는 아직 대기.
