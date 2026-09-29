# 현재 자산 출처와 제출 동봉 자료

확인일: 2026-09-29. 기준 소스 `7fb378c`. 이 문서는 실제 저장소/설치 패키지의 조사 결과다. [기존 후보 목록](vr-fps-project-docs/07-assets-licenses.md)의 다운로드 후보를 채택한 것으로 표시하지 않는다. 최종 모델·스캔과 실제 빌드 포함 파일의 감사는 아직 남아 있다.

## 게임에서 직접 만든 자산

| 자산·경로(Assets/VRFPS 아래) | 제작 방법·수정 | 현재 검증과 제출 상태 |
|---|---|---|
| `Prefabs/Weapons/K2.prefab`, 탄창·재질 | WeaponLabSetup/ReloadSetup의 기본 도형, 손·총구·삽입 앵커와 물리 구성 | 자체 임시 외형. 외부 K2 모델 없음. 최종 K2 실루엣은 아직 완료하지 않음 |
| `Prefabs/Enemies/`, `Animators/` | EnemyLabSetup의 기본 도형과 직접 작성한 Transform 애니메이션 곡선 | 자체 임시 적. 최종 외형/리깅/양안 시각 품질 미완료. [P06 상세](evidence/p06-assets.md) |
| `Audio/` WAV·AudioMixer | FeedbackSetup/WorldFeedbackSetup에서 결정적 합성 신호 생성, 믹서 구성 | 외부 녹음·모션캡처 없음. 실제 총기/좀비 녹음으로 주장하지 않음. 음량·햅틱 실기 대기 |
| 시험장·NavMesh·충돌·UI 프리팹 | 저장된 Unity 씬, 기본 도형·uGUI, 시험장 콜라이더에서 NavMesh 생성 | 실제 촬영 공간이 아님. UI의 Unity 기본 폰트/템플릿 의존성은 아래 외부 자산과 구분 |
| `Runtime/`, `Editor/`, `Tests/` | 프로젝트 기능·생성기·시험 코드. XRI/Unity API와 복제 리그를 사용 | 자체 작성 코드와 외부 패키지/샘플의 소유권을 혼합하지 않음. 저장소 전체에 별도 오픈소스 라이선스를 부여하지 않음 |

위 항목의 작성 시점·커밋·시험은 [진행 기록](progress.md)과 단위별 증빙에 있다. 시험 통과는 최종 외형·실기 승인과 다르다. `PlayerRig.prefab`과 입력 구성은 설치된 XRI Starter Assets에서 출발해 프로젝트 제어기로 수정했으므로 전부 독자 원본인 것으로 표기하지 않는다.

## 포함된 외부 템플릿·샘플·폰트

| 항목 | 확인한 원본/버전 | 사용·수정과 표기 |
|---|---|---|
| Unity VR Template | `com.unity.template.vr` 10.0.0, 로컬 Unity Hub TGZ의 package.json/라이선스 | `VRTemplateAssets`, 초기 설정·씬·리그 기반. Unity Companion License 표기를 [원문](licenses/Unity-VR-Template-10.0.0-LICENSE.md)에 보존 |
| XRI Samples/Starter Assets | `com.unity.xr.interaction.toolkit` 3.5.1 | `Assets/Samples/XR Interaction Toolkit/3.5.1`, 리그/입력/햅틱 기반. Unity 라이선스와 포함된 Apache 제3자 고지를 [패키지 사본](licenses/packages/com.unity.xr.interaction.toolkit-3.5.1/LICENSE.md)에 연결. 별도 Third Party Notices.md도 같은 폴더에 동봉 |
| TextMesh Pro/uGUI | `com.unity.ugui` 2.0.0 및 VR 템플릿 포함 TMP 자산 | `Assets/TextMesh Pro`, UI 셰이더·자원. [uGUI 라이선스](licenses/packages/com.unity.ugui-2.0.0/LICENSE.md), 아래 Liberation 폰트 고지는 별도 |
| Inter Regular | 폰트 내부 `Version 3.010;git-aca7606f2`, Rasmus Andersson/Inter project authors | `Assets/VRTemplateAssets/Fonts/Inter`. TTF 바이트가 로컬 VR10.0.0 템플릿과 일치. [해당 Inter 커밋의 OFL 원문](licenses/Inter-3.010-OFL.txt) 동봉. SDF/재질은 템플릿 제공본 |
| Liberation Sans | 폰트 내부 `Version 2.00.1`, Google2010/Red Hat2012, Steve Matteson | `Assets/TextMesh Pro/Fonts/LiberationSans.ttf`. TTF 바이트가 VR10.0.0 템플릿과 일치. 내부 OFL1.1 선언과 저작권을 확인하고 [공식 원문](licenses/Liberation-OFL.txt) 동봉. 정확한2.00.1 배포 아카이브를 새로 확보한 것은 아님 |
| Unity 패키지 의존성 | manifest/lock과 설치 package.json 버전을 대조 | 내비게이션·입력·XR·URP 등 루트 라이선스/고지49개를 변경 없이 보존. 전체 목록·원본 경로·SHA256은 [조사 JSON](evidence/p12-assets.json) |

폰트 취득일은 과거 템플릿 프로젝트 생성 시점이며 이번 조사일을 폰트 최초 취득일로 꾸미지 않는다. 2026-09-29는 원문 확보·대조일이다. Inter 원문은 [폰트가 식별한 공식 커밋](https://github.com/rsms/inter/blob/aca7606f250a74d4b42ee4c4038dcef28395d000/LICENSE.txt), Liberation 원문은 [공식 라이선스의 고정 커밋](https://github.com/liberationfonts/liberation-fonts/blob/380277b57ffb0b49ca655962c850ff0224a61cae/LICENSE)에서 확보했다. 폰트를 단독 판매하거나 폰트 이름을 바꾼 적은 없다.

## 동봉 범위와 남은 확인

`docs/licenses/`에는 Unity 템플릿 표기1개, 폰트 원문2개, 잠금 버전과 일치하는 설치 패키지 루트 라이선스/고지49개를 보존한다. 원문 내용은 요약문으로 대체하지 않는다. 현재70개 패키지 항목을 대조했으며 루트 고지가 없는 개발 기능 메타 패키지와 엔진 내장 모듈37개도 JSON에 남겼다. 고지 없음이 권리 제한 없음이라는 뜻은 아니다.

최종 Source/Documents와 빌드 배포 묶음에 적용되는 고지를 동봉한다. 앞선 P12-a의 `source-8a28aee.zip`은 Unity 복원 리허설용이며 이번 고지를 포함하지 않는다. 그대로 최종 제출물로 사용하지 말고 확정 커밋에서 새로 내보내고 `docs/licenses/`도 함께 넣는다. Unity 엔진의 최종 배포 고지, 패키지 내부 중첩 제3자 파일과 실제 빌드 의존 자산은 최종 빌드 기준으로 추가 확인한다.

현재 목록은 저장소·패키지 전체 조사로, 모든 샘플이 실행 빌드에 들어간다는 뜻이 아니다. 불필요한 데모·샘플 제거는 GUID/참조와 실제 빌드 의존성을 확인한 뒤 별도 수행한다. 패키지는 잠금으로 복원하고 PackageCache 전체를 소스에 복사하지 않는다.

아직 채택하지 않은 GAMGO K2, Mixamo, Quaternius, War FX는 실제 사용 출처표에서 제외한다. 실제 방/강의실 스캔도 아직 없다. 앞으로 채택하는 모델·동작·스캔은 제작자·원본 URL·취득일·버전·수정·사용/소스 전달 조건과 실제 기기 결과를 추가한다. **현재 T18은 부분 준비 상태이며 최종 출처 검증 완료가 아니다.**
