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

A02: RED 생성기 미존재 → GREEN 전체 EditMode83/83. 정적 프리팹을 구조 크기에 맞춰 검증하고 소유 외형만 교체한다.
A03 미술 판단: 외부 에셋 자동 취득 없이 직접 생성한 낡은 목재/회벽/천 재질과 정적 프리팹을 사용한다. 기존 통째로 막힌 가구 충돌을 설명하는 캐비닛형 외형을 사용한다. 최종 미술·VR 인수는 별도이며 현재는 최초 폐가 미리보기다.

A03 시각 확인: 첫 평면 렌더는 Performance URP에서 추가 점광원이 꺼져 있어 어두웠다. 기존 파이프라인을 유지하고 주 방향광으로 가독성을 보완했다. 침대 상부의 겹친 면을 목재 하부/천 상부로 나누어 coplanar 표면을 제거했다. 현재 외형은 직접 만든 첫 시제품이며 최종 미술 인수 아님.

독립 리뷰: 루트 변환 누락과 백업 실패 무시 2건. 루트 이동/회전/스케일/중간 부모 5개 및 불가능한 백업 1개 RED 실패 확인 후 직접 identity 자식 검증·검증된 백업·SaveScene 반환값 검사로 수정. 사진형 자산 의존성이 있는 기존 Room에만 로컬 참조 백업을 요구하므로 스캔 없는 재생성이 가능하다.

2026-10-13 Goal 도구 상태 active 확인. 이전 사용량 한도에 의한 리뷰/명령 미실행은 완료로 계산하지 않았으며 환경 재개 후 다시 실행했다.

Final review fixes: 6 RED failures → EditMode91/91, PlayMode127/127. 원본 파일을 Assets 밖으로 옮긴 상태에서 ConfigurePreview 성공. 사용한 Library/패키지 캐시는 기존 캐시이며 새 PC 복원 검증과 구분한다.

A05: 원본 RoomScan 분리 상태 ConfigurePreview와 PromoteAndBuild 모두 성공. packed RoomScan 자산0, 빌드 GUID313ef653032c4d93bf721f66bf9f08ca. 검증 후 로컬 참조 폴더 복원. 결과물501개 파일을 사용자 프로젝트로 복사하고 SHA256 일치 확인.
A06: 2026-10-13 사용자 응답 아직 실행 전. 실기·성능·최종 미술 및 강의실 맵은 미완료.

작업 상태: A01 구조 데이터/이전 및 A02 안전 생성 구현·자동 검증 완료. A03 첫 폐가 시제품과 Windows 빌드 완료, 최종 미술 미완료. A04 강의실 미촬영. A05 원본 독립 생성·빌드와 복원 안내 완료, 새 Library 백업 인수는 대기. A06 실기 대기. 런타임7af6176, PR #21 초안 유지. 원본 사진형을 로컬로 보존하고 직접 만든 시제품 외형으로 전환한 판단의 비용은 추가 미술·기기 인수가 필요하다는 점이다.
