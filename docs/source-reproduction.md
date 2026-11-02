# 소스 복원·시험·Windows 빌드

2026-10-18 최신 ZIP 새 Library 복원 완료: cb1b0e7-wall-fix 원본을 빈 폴더에 풀어 EditMode108통과/개인 스캔1제외, PlayMode131/131, Windows 빌드 성공을 확인했다. 씬3개 원본 일치, 원본 스캔 포함0, 출처2개 동봉 확인. 복원 빌드 GUID `845dc8e7a5604288b78f8ed2d0727f49`. 동일 PC 캐시·라이선스를 사용했으며 기존 WallFix 실행본은 교체하지 않았다. [증빙](evidence/wall-fix-clean-restoration.json).

2026-10-18 최신 로컬 소스 묶음: `Zombie Shoot VR/Builds/Reproduction/cb1b0e7-wall-fix/source-with-local-assets.zip`. 벽 충돌 수정과5분 시험 기준 포함. 2,861개 파일 CRC/SHA256 검사 및 빌드 검증 작업본과 코드·씬·프리팹·자산241개 일치를 확인했다(C# 줄바꿈 정규화). 이 ZIP 자체의 새 Library 재개방은 미실행이며 이전 ZIP의 복원 검증과 구분한다. 외부 자산은 로컬 보관만 한다. 사용자 요청으로 로그 작업은 후순위로 두었다. [증빙](evidence/wall-fix-source-backup.json).

2026-10-16 최신 ZIP 복원 검증 완료: `2d3f3e6-low-jump`를 빈 폴더에 풀어 Library 없이 EditMode106통과/실패0/개인 스캔 검사1제외, PlayMode131/131, Windows 빌드 성공을 확인했다. 세 맵 씬은 ZIP과 동일하며 원본 스캔 빌드 포함0, 출처 안내문2개 동봉을 확인했다. 복원 빌드 GUID `eb43eaca52b4478fab7aaa28b29750c2`. 같은 PC의 패키지 캐시·라이선스를 사용했으며 새 PC 또는 실제 VR 검증은 아니다. 기존 실기용 빌드는 유지했다. [증빙](evidence/low-jump-clean-restoration.json).

2026-10-16 최신 소스 보관본: `Zombie Shoot VR/Builds/Reproduction/2d3f3e6-low-jump/source-with-local-assets.zip`. 점프1.35m와 시점1.15배, 최신 두 맵 배치를 포함한다. 2,856파일 CRC/SHA256 및 검증 캐시의 코드·씬·프리팹·자산241개 일치를 확인했다(C# 줄바꿈 정규화). ZIP의 새 Library 복원 검증은 위 최신 결과를 따른다. 외부 자산 포함 로컬 보관용이며 최종 제출 완료가 아니다. [증빙](evidence/low-jump-source-backup.json).

2026-10-16 최신 소스 보관본: `Zombie Shoot VR/Builds/Reproduction/a7c149d-view-height-layout/source-with-local-assets.zip`. 시점1.15배·최신 사용자 배치·옛 충돌 제거 포함. 2,855파일 무결성 검사와 시험한 씬/NavMesh/플레이어 프리팹/높이 코드/의자 메시 일치 확인. ZIP 자체 새 캐시 재개방은 미실행. [보관 증빙](evidence/layout-height-source-backup.json).

2026-10-16 후속: 사용자 재편집 배치를 그대로 보존해 충돌·NavMesh를 갱신하고 폐교의 보이지 않는 옛 충돌체5개를 제거했다. 시점 높이1.15배 보정을 추가했다. [변경·검증](layout-height-20261016.md). 이전519495c 소스 ZIP은 이번 변경 전 보관본이다.

2026-10-16 최신 소스 보관본: `Zombie Shoot VR/Builds/Reproduction/519495c-map-scale/source-with-local-assets.zip`. 소스519495c와 로컬 외부 자산/meta를 포함한다. 2,842파일 CRC·SHA256 검증, 실제 시험한 캐시의 두 맵/미리보기·NavMesh·수정 의자 메시 일치 확인. ZIP 자체의 새 캐시 재개방은 미실행이며 이전83dc690 ZIP의 새 캐시 시험과 구분한다. [보관 증빙](evidence/map-scale-source-backup.json).

2026-10-16 최신 기준: 폐가 공간2배·가구1.5배, 폐교 공간2배·책상/의자1.5배. 아래 이전 크기 설명보다 이 결정이 우선한다. [상세 변경](map-scale-20261016.md)

## 2026-10-15 최신 ZIP 새 캐시 복원 검증

소스83dc690과 외부 자산/meta를 담은 ZIP을 빈 폴더에 풀고 Library 없이 검증했다. 자산 재연결이나 씬 재생성 없이 EditMode99통과/실패0/개인 원본 스캔 검사1제외, PlayMode130/130, Windows 빌드 성공을 확인했다. Room/Classroom 씬은 ZIP 원본과 해시가 같으며 두 출처 안내문도 빌드에 동일하게 동봉됐다. 원본 스캔과 학교 원본 FBX의 빌드 포함은0이다. [복원 증빙](evidence/user-layout-clean-restoration.json).

복원 검증 빌드 GUID는 `a7080199e5c04e3b9d2165f9064b5bc0`, 로그는 `tmp/user-layout-archive-validation/`에 있다. 동일 PC의 패키지 캐시·라이선스를 사용한 검증이며 다른 PC 검증이나 VR 인수가 아니다. 기존 사용자 테스트용 AbandonedSchool.exe(GUID `2a41ebb5739b4386b8b2b55ca4c66eca`)는 교체하지 않았다. 실제 두 맵 인수·성능·장시간 시험과 사용자 담당 발표/시연 자료, 최종 병합은 남아 있다.

## 2026-10-15 수동 편집 씬 보존 우선

현재 기준은 사용자 편집을 반영한83dc690의 Room/Classroom 씬이다. 아래10월14일 이전 재생성 절차는 당시 상태의 이력이며 최신 씬에 그대로 적용하지 않는다. ClassroomSetup.Configure, RoomHouseSetup.ConfigurePreview, GrannyHouseSetup.Configure는 초기 배치를 만들기 때문에 현재 씬을 덮어쓸 수 있다.

최신 복원에서는 씬과 동일한 외부 자산/meta를 함께 확보해야 한다. 가구 배치·방향은 저장된 씬에서 읽고 `UserLayoutSetup.Configure`로 충돌/NavMesh만 동기화한다. 게임 빌드는 `ClassroomSetup.Build`를 사용한다. [배치·점프 변경](user-layout-jump.md).

## 2026-10-14 Classroom 추가

[학교 자산 복원](school-assets-plan.md)에 따라 로컬 학교 라이브러리를 먼저 생성한 뒤 격리 Unity에서 `VRFPS.Editor.ClassroomSetup.Configure`를 실행한다. 이 단계는 생성 프리팹 GUID로 Classroom 씬·구조·NavMesh 참조를 다시 연결한다. 기존 Granny Room도 복원한 상태에서 `VRFPS.Editor.ClassroomSetup.Build`를 실행하면 두 맵 Windows 빌드와 두 출처 안내문을 생성한다. `Assets/VRFPS/Documentation` 폴더에 두 Attribution.txt 파일이 필요하다.

현재 캐시 프로젝트에서 EditMode99/99·PlayMode128/128·빌드는 통과했다. 후속으로 소스eaf2567의 새 Library에서 학교 재생성, EditMode98통과/개인 스캔1제외, PlayMode128통과 및 Windows 빌드까지 확인했다([증빙](evidence/school-clean-restoration.json)). 같은 PC에서 학교 외 자산532개는 기존 메타와 함께 복사했으므로 모든 원본의 신규 재생성이나 다른 PC 검증은 아니다. 검증한 복원본의 로컬 보관 ZIP도 생성했다. 아래 보관본 설명을 따른다. 외부 원본과 생성 라이브러리는 Git 제외이며 소스만 복제해서 즉시 실행 가능하다고 주장하지 않는다.


### 새 캐시 복원에서 확인된 필수 재연결

2026-10-14 소스 `eaf2567`을 Library 없는 별도 폴더로 풀어 검사했다. 기존 외부 자산/meta를 복사해도 Git 소스의 K2 모델과 무기 음원 참조가 다를 수 있다. 실제 첫 EditMode 결과는96통과/2실패/1제외였으며, 아래 두 단계를 실행한 뒤98통과/실패0/개인 스캔 검사1제외를 확인했다.

- `VRFPS.Editor.GamgoVisualSetup.Configure`
- `VRFPS.Editor.UserWeaponAudioSetup.Configure`

학교는 원본 ZIP에서 새 메타로 SchoolAssetsSetup.Configure → ClassroomSetup.Configure를 성공시켰다. 기존 외부 자산 복사만으로 재현 완료를 판단하지 않고 전체 검사와 빌드를 이어서 확인한다. 증빙 로그는 `tmp/school-restore-validation/`에 있다.
## Granny 가구·표면 적용 버전

2026-10-13 추가: 사용자 제공 Granny ZIP의 가구와 재질로 Room 외형을 교체한다. 원본과 추출 결과는 Git 제외이며 이전62c7427 ZIP에는 포함되지 않는다. 현재 브랜치를 복원할 때 [가구 자산 복원 안내](granny-house-assets.md)를 먼저 따른다.

```powershell
python scripts/Restore-GrannyHouse.py --archive 'C:/Users/User/Downloads/dvloper-grannys-house-official-model.zip' --project '<복원 Unity 프로젝트>'
# 기본 RoomHousePreview가 존재하는 상태에서 실행
& 'D:/Unity editor/6000.3.23f1/Editor/Unity.exe' -batchmode -quit -projectPath '<복원 Unity 프로젝트>' -executeMethod VRFPS.Editor.GrannyHouseSetup.Configure -logFile granny-configure.log
```

성공 표식은 `GRANNY_THEME_CREATED`다. 이후 아래 `RoomHouseSetup.PromoteAndBuild`를 실행한다. 기본 `RoomHouseSetup.ConfigurePreview`만 실행하면 이전 자체 제작 외형으로 돌아가므로 GrannyHouseSetup.Configure를 이어 실행한다. 현재 빌드가 외부 모델 권리 관계를 검증했다는 의미는 아니며 공개 배포 전 출처 문서의 미확인 항목을 해결한다.

## 최신 확대 버전 새 캐시 검증

`Zombie Shoot VR/Builds/Reproduction/62c7427-house-scaled/`에 최신 확대 소스 ZIP·외부 자산 ZIP·manifest·README를 보관한다. 같은 새 폴더에 두 ZIP을 풀고 Unity6000.3.23f1로 연다. 2026-10-13 Library가 없는 새 복원 폴더에서 외부188파일 SHA256을 확인하고 EditMode95통과/실패0/개인 스캔 검사1제외, PlayMode127통과 및 Windows 빌드 성공을 확인했다. packed RoomScan 자산은0이다. [증빙](evidence/clean-room-restoration.json).

동일 PC의 새 캐시 복원 시험이며 다른 PC·오프라인·라이선스 신규 설정 검증은 아니다. 외부 ZIP은 개인 로컬 복원용이다. 아래7af6176 묶음의 미실행 설명은 이전 백업에 대한 이력이며 최신 확대 소스의 결과와 구분한다.

## 2026-10-13 폐가 로컬 백업

`Zombie Shoot VR/Builds/Reproduction/7af6176-house/`에 소스7af6176 ZIP(약90MiB), 외부 자산 ZIP(약156MiB), manifest와 README를 보관했다. GAMGO·Mixamo·음원188파일과 meta를 포함하고 RoomScan은 필요하지 않아 제외했다. 두 ZIP CRC 및 전체 외부 파일 SHA256 대조를 통과했다. 이 묶음을 새 Unity Library에서 여는 시험은 미실행이며 최종 제출본으로 표시하지 않는다.

## 2026-10-13 Method A 폐가 경로

현재 Room은 내 방 구조 기반 폐가 시제품이며 게임 공간은2.5배 확대했다(커밋62c7427). 구조 자산·직접 만든 테마 프리팹/재질·독립 NavMesh는 Git으로 관리한다. 최신 크기 검증은 [확대 증빙](evidence/room-gameplay-scale.json), 이전 생성 과정은 [Method A 실행 기록](method-a-execution.md)을 따른다. 아래 사진형 RoomScan 복원은 이전 비교용 경로다.

기존 GAMGO·Mixamo·음원 복원은 필요하지만 폐가 생성에는 원본 방 FBX가 필요하지 않다. `RoomStructure.asset`이 있으면 이를 사용한다. 다음 명령은 각 프로세스 종료·성공 로그를 확인하며 격리 프로젝트에서 순서대로 실행한다.

```powershell
& 'D:/Unity editor/6000.3.23f1/Editor/Unity.exe' -batchmode -quit -projectPath '<복원 프로젝트>' -executeMethod VRFPS.Editor.RoomHouseSetup.ConfigurePreview -logFile house-configure.log
& 'D:/Unity editor/6000.3.23f1/Editor/Unity.exe' -batchmode -quit -projectPath '<복원 프로젝트>' -executeMethod VRFPS.Editor.RoomHouseSetup.PromoteAndBuild -logFile house-build.log
```

성공 표식은 `VRFPS_HOUSE_PREVIEW_CREATED`, `VRFPS_HOUSE_BUILD_SUCCEEDED`다. `Builds/Windows/AbandonedHouse/scan-dependency-check.txt`의 packed RoomScan 자산 0도 확인한다. 사진형 Room을 다시 생성하는 `ScannedRoomSetup.Configure`는 새 폐가를 덮어쓰므로 이 경로에서 실행하지 않는다.


이 안내는 현재 개발 브랜치의 재현 절차다. 실제 두 맵과 최종 제출 인수는 아직 완료하지 않았다. 실행·조작 확인은 [실기 안내](hardware-validation.md), 단계별 상태는 [진행 기록](progress.md)을 따른다.

## 최신 복원 보관본 (2026-10-08)

소스 기준은 `1412b0ad9a6f22b665f3b98f23191110185302bf`이며 사용자 인수가 끝난 무기 음원을 포함한다. 로컬 `Zombie Shoot VR/Builds/Reproduction/1412b0a/`에 다음 파일을 보관한다. 최종 제출 ZIP이 아니라 실제 맵 제작 전 개발 버전의 복원 묶음이다.

- `source-1412b0a.zip`: 해당 커밋의 Unity Assets/meta·Packages·ProjectSettings·프로젝트 Docs·scripts·docs·README. 개인 미커밋 설정과 Library는 제외한다.
- `external-originals-local-only.zip`: 해시를 확인한 GAMGO 원본2개·Mixamo 원본4개·음원3개 및 출처·라이선스 문서. 기존 다운로드 위치가 바뀌어도 로컬 복원할 수 있도록 보관하며 공개 재배포 허가를 뜻하지 않는다.
- `manifest.json`과 `README.md`: 원본/압축 파일 SHA256과 실행 순서. 원본 압축과 소스 압축은 서로 다른 새 폴더에 푼다.

복원 순서는 원본3종 복사 → `GamgoVisualSetup.Configure` → `MixamoVisualSetup.Configure` → `UserWeaponAudioSetup.Configure` → EditMode/PlayMode → `InstructionSetup.Build`다. 각각 Unity 프로세스가 종료하고 성공했는지 확인한 다음 진행한다. 모든 Setup을 무차별 실행하는 절차는 아니다.

이번 검증은 Git에서 내보낸 새 프로젝트에서 시작하고 기존 Library나 외부 자산 meta를 복사하지 않았다. 같은 PC의 전역 패키지 캐시·Unity 라이선스 사용은 허용하므로 새 PC·완전 오프라인 검증과 구분한다. 자세한 실제 결과와 해시는 [최신 복원 증빙](evidence/current-source-restoration.json)에 기록한다. 아래 `8a28aee` 예시는 이전 시점의 이력이다.

## 2026-10-12 실제 방 소스 백업

현재 로컬 묶음은 `Zombie Shoot VR/Builds/Reproduction/c7eadf1-room/`이다. 소스 기준 `c7eadf1`, 런타임 구현 `6aaee2f`이며 최종 제출본이 아니다.

- `source-c7eadf1.zip`: Git의 Unity 소스·잠금 패키지·설정·문서·스크립트. 개인 미커밋 설정은 제외한다.
- `external-assets-private-local-only.zip`: 현재 GAMGO·Mixamo·음원·RoomScan의 원본과 생성 자산206개 및 meta. 사용자 방을 포함하므로 로컬 전용이며 Git/공개 업로드하지 않는다.
- `manifest.json`: 각 외부 자산과 ZIP의 SHA256. ZIP CRC와 외부 자산206개 전체 바이트를 대조했다.

새 폴더 하나에 소스 ZIP과 외부 자산 ZIP을 차례로 풀어 `Zombie Shoot VR` 경로가 합쳐지도록 한다. meta를 유지하고 Unity6000.3.23f1로 연다. 생성 자산 GUID를 보존하는 복원이므로 모든 Setup을 일괄 실행하지 않는다. 새 Library에서의 실제 시험 결과는 [방 증빙](evidence/room-scan.json)을 따른다. 같은 PC의 패키지 캐시/라이선스를 사용한 검증이며 완전 오프라인·다른 PC 보장은 아니다.

원본 FBX에서 방을 다시 생성하려면 [방 스캔 복원](room-scan.md)의 원본 복사 → `ScannedRoomSetup.Configure` → 시험 → `ScannedRoomSetup.Build`를 적용한다. 원본/가공 메시/텍스처/NavMesh는 Git에서 제외하고 생성 코드는 포함한다. 보스/크롤러 원본 복원은 [추가 절차](boss-crawler.md)를 따른다.

## 필요한 환경과 파일

- Unity6000.3.23f1 및 Windows x64 빌드 지원. 현재 빌드 함수는 Mono 개발 빌드를 사용한다. 최종 IL2CPP 또는 Release 빌드는 아직 검증하지 않았다.
- Unity 실행 권한/라이선스와 Unity Package Manager 패키지를 복원할 수 있는 환경. `Packages/manifest.json`과 `packages-lock.json`을 함께 보존한다.
- 주요 잠금 버전: XRI3.5.1, URP17.3.0, OpenXR1.17.1, Input System1.20.0, AI Navigation2.0.14, Test Framework1.6.0.
- 소스: `Zombie Shoot VR/Assets/` 전체와 `.meta`, `Packages/`, `ProjectSettings/`. 폴더 이름이 같은 저장소 루트와 Unity 프로젝트를 혼동하지 않는다.
- Python 분석기를 실행할 경우 Python3.11과 `scripts/` 전체. `analyze_vr_frame_timings.py`는 같은 폴더의 `analyze_vr_diagnostics.py`를 가져온다.
- 현재 외형 브랜치는 Git 제외 외부 모델을 사용한다. 사용자 취득 파일로 [GAMGO K2·탄창](gamgo-visual-restore.md) 및 [Mixamo 좀비·동작](mixamo-visual-restore.md)의 복원과 해당 외형 적용기를 실행해야 한다. 아래 과거8a28aee 리허설은 이 외부 자산 도입 전 기록이다.

사용자 제공 무기 음원3개도 [음원 복원 절차](user-weapon-audio.md)에 따라 복원·적용한 후 최종 자산 시험을 실행한다.

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

보스·돌연변이 변경부터는 기본 Mixamo 복원·적용 이후 [추가 모델과 모션 4개 복원 및 EnemyVariantSetup 적용](boss-crawler.md)을 수행한다. 크롤러 모델은 Zombiegirl W Kurniawan이다. 이 단계를 생략하면 변형 프리팹의 외부 참조가 누락되므로 시험·빌드를 진행하지 않는다. 기존 1412b0a 복원 패키지는 변경 전 이력으로 보존한다.

기본 Windows 데이터 위치는 `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Zombie Shoot VR/`이다. 실제 맵 기록은 `records-v1.json`과 `.bak`, 진단은 `Diagnostics/`, 실행 로그는 `Player.log`다. MenuLab은 연습 점수를 저장하지 않는다. 설정은 Unity PlayerPrefs에 저장된다. 오류 재현을 위해 실제 사용자 기록을 지우지 말고 백업과 별도 시험 환경을 사용한다.

- 컴파일 오류: 에디터 버전과 package lock이 유지됐는지, 로그의 첫 컴파일 오류부터 확인한다.
- 패키지 복원 오류: 라이선스·네트워크·UPM 오류를 확인한다. 임의의 최신 버전 설치로 잠금을 바꾸지 않는다.
- 화면/입력 오류: 성공한 Quest Pro/VD 연결과 활성 OpenXR 런타임, Player.log를 대조한다. headless 자동 시험은 실제 두 눈/추적 시험이 아니다.
- 맵 버튼 비활성: Classroom은 실제 스캔과 폐교 맵이 준비되지 않아 비활성이다. 최신 폐가 빌드의 Room도 비활성이면 정상으로 간주하지 말고 씬 등록·Room 정의·Player.log를 확인한다.
- 진단 GPU 표본0: 측정 지원/환경을 확인하고0ms 합격으로 처리하지 않는다. [분석 지침](../Zombie%20Shoot%20VR/Docs/performance.md)을 따른다.

## 현재 복원 검증 상태

2026-09-29 확인: 8a28aee의 추적 소스만 ZIP으로 내보내고 기존 Library가 없는 새 폴더에서 **EditMode50/50, PlayMode116/116**, BootstrapPreview/WaveLab Windows 개발 빌드를 검증했다. [P12 소스 복원 증빙](evidence/p12-source-reproduction.json)에 소스 ZIP·시험 XML·빌드 manifest 해시와 임포트 변경 범위를 기록했다.

Unity의 PC 전역 패키지 캐시/라이선스는 공유할 수 있으므로 완전히 새 PC나 오프라인 복원을 검증한 것으로 확대하지 않는다. 기존 샘플 텍스처 메타데이터 등 에디터 생성 변경은 복원본에서 관찰하고 원래 사용자 프로젝트에 복사하지 않았다. 실제 두 맵·최종 외형·독립 리뷰·제출 형식·최종 기기 인수가 남아 T20 전체 완료가 아니다.


## GAMGO 외형 추가 이후의 필수 복원 단계

P04 외형 브랜치부터 K2/탄창 프리팹은 Git 제외 자원을 참조한다. 기존8a28aee 복원 리허설 결과를 이 변경 이후의 완전 복원 증빙으로 확대하지 않는다. Unity 시험/빌드 전에 [GAMGO 원본 확보 및 복원·적용](gamgo-visual-restore.md)을 수행해야 한다. 원본 ZIP의 라이선스별 전달 조건을 지키고 해당 계정으로 다시 확보할 수 있는 링크·해시를 제공한다. 파일이 없으면 임시 도형으로 최종 빌드를 성공 처리하지 않는다. 외형 추가 후 Git 소스만 새 폴더로 복원하는 리허설에서 누락된 이전 모델 참조가 남는 결함을 발견해 수정했다. 수정 후 EditMode51/51과 Windows 빌드2개를 확인했다. [상세 증빙](evidence/p04-gamgo-restoration.json). 새 복원본 PlayMode·새 PC·오프라인 시험까지 통과한 것으로 확대하지 않는다.

## 두 맵 로컬 소스 보관본 (2026-10-14)

`Zombie Shoot VR/Builds/Reproduction/eaf2567-two-maps/`의 `restored-source-local-only.zip`, `manifest.json`, `README.md`를 함께 보관한다. 584,271,736바이트, 2,823개 파일의 CRC와 SHA256을 검증했다([증빙](evidence/two-map-source-bundle.json)). 소스 기준eaf2567에 학교 생성 및 K2/음원 참조 재연결을 적용한 스냅샷이다. 순수 Git archive나 최종 제출본과 구분한다. Unity Library·빌드·개인 스캔은 포함하지 않았다.

빈 폴더에 압축을 풀고 `Zombie Shoot VR`을 Unity6000.3.23f1로 연다. 원본 복원본은 자동 검사와 빌드를 통과했고 압축 구성원은 바이트 단위로 검증했지만 이 ZIP을 다시 풀어 Unity를 여는 시험은 별도로 수행하지 않았다. 외부 자산이 포함되어 로컬 보관하며 공개 재배포 허가로 해석하지 않는다.

### 수동 편집 배치 로컬 보관본

`Zombie Shoot VR/Builds/Reproduction/83dc690-user-layout/`에 소스·로컬 자산 ZIP, manifest, README를 보관했다. 2,834개 파일 CRC/SHA256과 현재 두 씬의 바이트 일치를 확인했다. Library·개인 스캔·사용자의 무관한 미커밋 설정은 포함하지 않았다. 이 ZIP을 다시 연 Unity 시험은 미실행이며 최종 제출본으로 표시하지 않는다. [증빙](evidence/user-layout-source-backup.json).

## P13 바이러스 복원

원본 ZIP 경로를 `VRFPS_VIRUS_ARCHIVE`로 지정하고 격리 Unity에서 `-batchmode -nographics -executeMethod VRFPS.Editor.IntroVirusSetup.Import -quit`를 실행한다. 원본 SHA가 다르면 거부한다. 외부 생성물은 `Assets/VRFPS/External/IntroVirus/`이며 Git 제외다. 기존 GUID를 보존하려면 로컬 `Builds/Reproduction/p13-virus-20261021/derived-with-meta.zip`을 해당 폴더로 복원한다. 새 import의 GUID가 달라지면 관찰 씬 참조를 재연결해야 한다. 현재 관찰 씬은 아직 연결하지 않았다. 보관 ZIP의 CRC/SHA 검증만 완료했으며 전체 새 소스 복원 검증으로 간주하지 않는다.

P13 전용 레이어 연결 이후 최신 바이러스 생성물은 `Builds/Reproduction/p13-virus-layer-20261021/derived-with-meta.zip`이다. 앞선 자산 보관본은 그대로 유지하며 새 보관본은 IntroTarget(layer13) Collider 설정을 포함한다. 기존 meta SHA는 유지됐고 새 ZIP CRC/파일 SHA 검증을 마쳤다. 새 프로젝트 실행 복원은 아직 미검증이다.

## P13 GS 렌더러 시험 재현

`Packages/manifest.json`과 lock은 UnityGaussianSplatting의 `2c6fed37da67a217367261fcfcd3316d34c73e76`을 가리킨다. Git/네트워크가 있는 격리 Unity6000.3.23f1 작업본에서 resolve한다. 다른 기존 패키지 버전은 변경하지 않았다. 원본 게임 pipeline/MSAA/API 설정도 보존한다.

1. 격리 프로젝트에 `-batchmode -force-d3d12 -executeMethod GaussianRealityFixtureSetup.Create -quit`를 실행한다. `-projectPath`와 별도 `-logFile`을 지정한다. 합성256 GS와 시험 pipeline은 Git 제외 `Assets/VRFPS/External/RealityGS/SyntheticFixture/`에 생성된다. 사용자 촬영본을 대체하지 않는다.
2. 새 Unity 프로세스에서 `-batchmode -force-d3d12 -runTests -testPlatform PlayMode -testFilter GaussianRealitySourceTests -testResults <절대 XML 경로> -logFile <절대 로그 경로>`로 실행한다. 시험에는 `-quit`를 붙이지 않는다. fixture 누락은 제외가 아니라 실패다.
3. 시험은 runtime에서만 pipeline을 선택하고 종료 후 원래 설정을 복원한다. 실제 GS 픽셀·가림·암전과 GPU 자원 수명을 검증하며 XR, Player stripping, 실제 방 보정은 별도다. 최종 전체 시험은 [증빙](evidence/gs-renderer-bridge.json)을 참조한다.

생성기는 프로젝트 소유의 결정적 분석형 데이터를 만든다. 기존 GUID를 보존해야 하는 운영 GS는 별도 원본/가공본/meta 백업을 사용해야 한다. 생성기 임포트 종료의 Persistent allocation1 경고는 미진단이며, 이 단위를 전체 새 소스/Windows 복원 완료로 간주하지 않는다.

P13 운영 경로 통합 시험은 같은 합성 fixture 생성 후 `-testFilter GaussianComparisonFlowTests`로 재현한다. 전체 PlayMode에 포함되며 기존 MapSceneTestSetup이 관찰용 shell을 시험 중에만 등록한다. 실제 GS 렌더→K2 명중→암전→Room 초기 준비10회와 Classroom 직접 진입을 검사한다. [증빙](evidence/gs-flow-integration.json). 실제 스캔/VR/Player 시험이 아니며, 이 시험에서 run clock은 초기 상태 검사를 위해 비활성화한다.

## P13 현재 소스·로컬 자산 복원 리허설 (2026-10-23)

기준 `6ea502b`의 로컬 보관본은 격리 작업본의 `Zombie Shoot VR/Builds/Reproduction/p13-6ea502b/`에 있다. `tracked-source.zip`, `local-assets-with-meta.zip`, `manifest.json`, `post-unity-audit.json`, `README.md`를 함께 보관한다. 두 ZIP은 같은 새 빈 폴더에 풀고 중첩 `Zombie Shoot VR` 프로젝트를 연다. 외부 자산이 포함되므로 공개 재배포용이 아니다. 실제 촬영 GS·Library·빌드·사용자 무관 미커밋 설정은 제외했다.

2,991파일을 실제로 새 폴더에 복원해 CRC/개별 SHA를 확인하고 합성 GS를 재생성했다. 새 Library에서 EditMode137통과/개인스캔1제외, DX12 PlayMode194/194를 확인했다. Windows BootstrapPreview 개발 빌드도 성공했고351파일 SHA와 GUID54d64a1919184786b7dc5d4befc087a6을 기록했다. [상세 증빙](evidence/p13-source-restore.json).

이 실행 파일은 `D:/VRFPS-Reproduction/gs-6ea502b/Zombie Shoot VR/Builds/Windows/BootstrapPreview/BootstrapPreview.exe`다. 기존 네 씬의 소스 빌드 재현 결과이며, 촬영본이 연결된 최종 GSComparison 빌드는 아니다. Player를 실행하지 않았으므로 실제 Player API/VR/GS stripping 통과를 주장하지 않는다. 동일 PC의 package cache·라이선스 사용도 명시한다.

Unity 임포트/빌드가56개 파일을 다시 직렬화했고 누락은0이다. sample metadata뿐 아니라 Volume filter·URP shader prefilter/runtime settings·preloadedAssets 변경을 포함하므로 바이트가 같은 복원본이라고 표현하지 않는다. 맵과 게임 코드, 외부 자산은 변경 목록에 없다. 원본 프로젝트에는 반영하지 않았다. importer 종료 Persistent allocation1 경고는 미진단 상태로 남겨 두었다.

## P13 Windows GS 진단 재현

기준4c84f7d에서 앞서 설명한 합성 fixture를 생성한 후, 격리 프로젝트에 `-batchmode -force-d3d12 -executeMethod VRFPS.Editor.GaussianPlayerProbeSetup.Build -quit`와 명시적 projectPath/logFile을 지정한다. API automatic/list와 backend는 finally에서 복원된다. 생성한 관찰용 진단 씬은 Git 제외 RealityGS/PlayerProbe 아래에 있으며 기존 게임 씬을 바꾸지 않는다.

출력은 격리 작업본의 `Zombie Shoot VR/Builds/Windows/GsCompatibilityProbe/`다. README와 GaussianSplatting-Attribution.txt를 함께 보관한다. 실행 옵션은 `-batchmode -force-d3d12 -vrfps-gs-probe -vrfps-gs-probe-result <새 절대 JSON 경로> -logFile <새 절대 로그 경로>`다. exit0/JSON success와 buildGuid를 함께 확인한다. 그래픽 없는 실패 경로는 force-d3d12 대신 nographics로 실행하며 exit1/실패JSON을 확인했다.

[현재 증빙](evidence/gs-player-probe.json)은 Mono Development/managed stripping Disabled의 실제 DX12 단안 진단이다. 실제 촬영본·Quest Pro·최종 게임 배포 및 그 복원으로 확대하지 않는다. 이전6ea502b 복원 ZIP에는 이 후속 진단 코드가 없으며, 최종 제출본 복원은 모든 자산·기능 통합 후 다시 검증해야 한다.

## P13 PLY 임포트 수명 수정

고정 revision2c6fed37의 package1.1.1을 `Packages/org.nesnausk.gaussian-splatting`에 embedded로 포함했다. Git archive에 패키지 소스·meta·MIT 라이선스가 함께 들어가며 별도 PackageCache 수정에 의존하지 않는다. runtime/shader 파일은 원본과 동일하다. 변경은 PLY raw 배열과 실패 시 변환 배열 해제이며 `VRFPS-PATCH.md`에 범위를 기록했다.

격리 프로젝트에서 `-batchmode -nographics -executeMethod GaussianImportLifetimeCheck.Run -quit`로 재현한다. 정상/속성 누락/잘린 PLY를 각각10회 읽고 `GS_IMPORT_LIFETIME_CHECK` 마커와 종료 로그를 함께 확인한다. 수정 전에는 exit0이어도 Persistent30개 누수가 발생했으므로 종료 코드만으로 통과를 판단하지 않는다. 수정 후 같은30건에서 누수 경고가 없어졌다. 변환 후처리 예외는 코드 리뷰 범위이며 강제 주입 시험은 수행하지 않았다. SPZ 오류 수명·전체 프로세스 메모리·VR 성능 검증으로 확대하지 않는다.

위의6ea502b 복원본과4c84f7d Player 빌드는 이 수정 이전 산출물이다. 최종 배포·복원은 실제 GS 자료 통합 이후 다시 검증한다.

## 실제 Room GS 복원 묶음 (소스 b221bcb)

`Zombie Shoot VR/Builds/Reproduction/p13-room-b221bcb/`의 tracked-source.zip, local-assets-with-meta.zip, manifest.json을 함께 보존한다. 소스2417개와 로컬 외부 자산706개(기존 모델/오디오 및 Room 원본·가공본·scene/profile/meta)가 포함된다. 합성 fixture/Library는 제외했으며 자동 시험을 위해 합성 fixture만 별도 재생성한다.

빈 폴더에 두 ZIP을 같은 루트로 풀고 manifest SHA를 확인한다. Unity6000.3.23f1에서 명시적 projectPath와 로그를 지정해 RoomRealitySetup.Configure로 관찰씬 등록/프로필 연결을 재현한다. 자동 시험에는 GaussianRealityFixtureSetup.Create 후 EditMode/PlayMode를 실행한다. 실제 게임 빌드는 RoomComparisonBuild.Build이며 합성 fixture는 그 빌드 씬 목록에 없다. 원본 PLY는 변환 재현 자료이며 Player 패킹 목록에서 제외됨을 확인했다.

원본 재변환이 필요할 때만 VRFPS_ROOM_PLY 환경변수를 원본 경로로 설정하고 RoomRealitySetup.Import를 실행한다. 정확한 SHA를 확인하며 Medium 형식과 카메라 JSON 제외를 고정한다. 가공 payload bytes는 재변환 후 일치했지만 upstream m_DataHash 메타데이터는 달라졌으므로 전 자산 바이트 동일 재생성을 주장하지 않는다. 실제 제공된 묶음의 CRC/개별 SHA 복원은 검증했다. 새 Unity 시험/빌드 결과는 room-capture-integration.json과 후속 복원 증빙을 따른다.

2026-11-01 수정 소스0088570의 Git archive는 `Builds/Reproduction/p13-room-walk-0088570/tracked-source.zip`에 보존했다(프로젝트 루트 기준). 기존 p13-room-b221bcb/local-assets-with-meta.zip을 함께 사용한다. 수정 빌드 명령은 RoomComparisonBuild.BuildMovementRevision이며 기존 GSComparison을 덮어쓰지 않는다. 이 수정본의 새 Library 복원 시험은 아직 수행하지 않았고, 기존 b221bcb 복원 결과와 구분한다.

0088570 복원 진행: `D:/VRFPS-Reproduction/room-walk-0088570`에 소스2418파일과 기존 로컬 자산706파일을 새로 풀었다. ZIP 내부와 추출 파일의 SHA 비교3124/3124 일치, 최초 Library 없음 확인 후 Unity 구성을 시작했다. 이 단계는 파일 무결성 증빙이며 자동 시험·빌드 성공을 대신하지 않는다.
복원본 RoomRealitySetup.Configure는 ROOM_REALITY_CONFIGURED 마커와 exit0으로 종료했다. 자동 시험 fixture 생성은 별도 단계로 진행한다.

복원 검증 완료(0088570): 새 Library에서 Configure→fixture→EditMode137통과/1제외→DX12 PlayMode201/201→BuildMovementRevision이 성공했다. 복원 빌드 GUID584af24cd30f4af99d2bd98e86fcdebf,356파일 해시를 기록했다. 최종 archive 대조 변경58/누락0이며 게임 코드·두 테마 맵·GS payload는 유지됐다. 동일 PC 패키지 캐시/라이선스 조건이고 복원 Player는 실행하지 않았다. 사용자 전달GUID298c029c12e347cf95ca51efe9dd5f57은 교체하지 않았다. 위의 복원 미수행/진행 중 기록은 이 결과로 갱신한다.

## Room surface repair (59ca570)

프로젝트의 Builds/Reproduction/p13-room-surface-repair/tracked-source.zip과 local-repair-overlay.zip을 보존한다. 빈 복원 폴더에 새 소스 ZIP, 기존 p13-room-b221bcb/local-assets-with-meta.zip을 풀고 **그 뒤** 보정 overlay7파일(scene/meta/재질)을 덮어쓴다. overlay CRC와 현 작업본7개 SHA 일치를 확인했으며, 이 보정본 새 Library 전체 복원은 아직 재실행하지 않았다. 기존 복원시험과 구분한다.

씬 등록이 필요하면 RoomRealitySetup.Configure를 먼저 실행하고 overlay를 마지막에 적용해야 한다(Configure는 씬을 재생성한다). 원본 씬으로부터 보정을 재작성하려면 overlay 대신 RoomSurfaceRepair.Apply를 batchmode로 실행한다. Apply는 scene/meta를 Builds/Reproduction/RoomSurfaceRepair/<timestamp>에 백업하며, 기존 보정 그룹이 있거나 캘리브레이션이 다르면 거절한다. 빌드는 RoomComparisonBuild.BuildSurfaceRepairRevision이다. 원본PLY/GS payload는 바꾸지 않는다.

59ca570 보정본 복원 검증 갱신: D:/VRFPS-Reproduction/room-repair-59ca570에 새 소스+기존 외부자산을 복원하고 Library 없는 상태에서 Configure를 실행했다. 이후 overlay7파일을 적용해 SHA7/7을 확인했다. 새 Library DX12 집중 시험 RoomSurfaceRepairTests/RoomCaptureFlowTests2/2(보정 토글·렌더·해제,실제GS전환10회,두맵Wave1), Windows 재빌드GUIDf8e6f03fb5814b1baffef373a0923d71 성공. 원본PLY/가공payload6개 해시 일치. 전체202개를 복원본에서 다시 실행한 것은 아니며, 동일 PC 캐시/라이선스 조건이고 복원 Player 실행·VR 인수는 미실행이다. 앞의 새 Library 미수행 기록은 이 범위에서 갱신한다.

옷장 수정289b2b3 최신 묶음: Builds/Reproduction/p13-room-wardrobe-289b2b3/tracked-source.zip + local-repair-overlay.zip. 새소스→기존외부자산→Configure→최신overlay 순서다. 두ZIP CRC 확인, 이번 국소 수정의 새Library 복원은 아직 미실행. 기존 보정 씬을 직접 갱신할 때는 RoomSurfaceRepair.RestoreWardrobeDetail을 batchmode로 실행한다(백업 생성, 기존 벽 보정 유지). 새로 원본씬에서 Apply하면 처음부터 옷장 평면 없는 보정을 생성한다. 빌드는 RoomComparisonBuild.BuildWardrobeRevision. 이전 Wood.mat는 복원 보존용으로 남지만 새Player 패킹에는 포함되지 않는다.

## Room mirror + FBX wardrobe (f4cf87f)

`Builds/Reproduction/p13-room-mirror-fbx-20261102` contains tracked-source.zip, local-room-overlay.zip (14 files), fbx-authoring-reference.zip (6 files), and overlay-manifest.json. Restore tracked source and the baseline p13-room-b221bcb external archive, run Configure only if scene registration is needed, then apply this overlay LAST. Do not run Configure or old repair authoring after the final overlay: those recreate or assume the unmirrored scene. Runtime overlay includes the baked wardrobe texture/mesh/material and does not require FBX at runtime. The reference archive is only required to re-author from the preceding unmirrored three-wall repair scene with RoomMirrorWardrobe.Apply. Source FBX/raw PLY remain unchanged.

Overlay/reference ZIP CRC passed. Latest private scene hash and tests/build are in evidence/room-mirror-wardrobe.json. A new empty-Library restoration was not repeated for this revision. Windows build method: RoomComparisonBuild.BuildMirrorWardrobeRevision. Undo via Builds/Reproduction/RoomMirrorWardrobe/before-application-20261102/RoomReality.unity, preserving its meta. Keep generated assets until restored scene is verified; no raw deletion is necessary.

