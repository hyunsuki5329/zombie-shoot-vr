# 현재 자산 출처와 제출 동봉 자료

확인일: 2026-09-29. 기준 소스 `7fb378c`. 이 문서는 실제 저장소/설치 패키지의 조사 결과다. [기존 후보 목록](vr-fps-project-docs/07-assets-licenses.md)의 다운로드 후보를 채택한 것으로 표시하지 않는다. 최종 모델·스캔과 실제 빌드 포함 파일의 감사는 아직 남아 있다.

## 게임에서 직접 만든 자산

| 자산·경로(Assets/VRFPS 아래) | 제작 방법·수정 | 현재 검증과 제출 상태 |
|---|---|---|
| `Prefabs/Weapons/K2.prefab`, 탄창·재질 | WeaponLabSetup/ReloadSetup의 기본 도형, 손·총구·삽입 앵커와 물리 구성 | GAMGO K2·STANAG 외형을 로컬 복원/적용. 실제 기기 인수 대기 |
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

GAMGO K2와 STANAG 탄창 및 아래 Mixamo 모델·동작을 채택했다. Quaternius, War FX는 아직 채택하지 않아 실제 사용 출처표에서 제외한다. 실제 방/강의실 스캔도 아직 없다. 앞으로 채택하는 모델·동작·스캔은 제작자·원본 URL·취득일·버전·수정·사용/소스 전달 조건과 실제 기기 결과를 추가한다. **현재 T18은 부분 준비 상태이며 최종 출처 검증 완료가 아니다.**


## K2·탄창 확보와 로컬 Unity 적용 (2026-09-29)

사용자가 로그인된 Chrome에서 원본 FBX ZIP 두 개를 직접 다운로드했다. Downloads 원본은 보존하고 Git 제외 경로 `tmp/external-assets/gamgo/`에 복사·압축 해제했다. ZIP CRC, 경로 이탈 없음, 파일 크기와 SHA256을 확인했다. [취득·파일별 증빙](evidence/p04-gamgo-acquisition.json)에 기록하며 다운로드 성공을 Unity 적용/실기 통과로 표시하지 않는다.

| 모델 | 제작자·원본 | 다운로드 페이지 표시 | 확보 내용 |
|---|---|---|---|
| DAEWOO - K2 | [GAMGO](https://sketchfab.com/3d-models/daewoo-k2-258d30e1f3e04c13a60abd916425b0c2) | Free Standard, [조건](https://sketchfab.com/licenses) | daewoo-k2.zip 9,710,704바이트, FBX1·PNG13 |
| 30 rounds 5.56mm STANAG MAG | [GAMGO](https://sketchfab.com/3d-models/30-rounds-556mm-stanag-mag-1eadc947c0894aa0bbac0c6226a21bda), 사용자 링크 https://skfb.ly/oxAOu | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 30-rounds-556mm-stanag-mag.zip 14,829,988바이트, FBX1·JPG8 |

탄창 표기: “30 rounds 5.56mm STANAG MAG” by GAMGO, CC BY 4.0, 위 원본 링크. 원본 파일 바이트는 보존했다. Unity에서 크기·방향·위치를 조정하고 URP 재질을 구성했다. K2의 거칠기를 매끄러움 알파로 패킹하고 탄창은 알파 없는 JPEG에 매끄러움0.35를 초기값으로 적용했다. K2는 CC BY로 간주하지 않으며 원본 자산을 Git에 올리지 않았다. 소스 전달 범위에 맞는 확보·복원 절차가 필요하다. 로컬 tmp는 최종 제출 보관소가 아니며 최종 패키징 때 별도 보존해야 한다.

Unity에서 실제 메시/재질 슬롯을 검사하고 기존 Foregrip/Muzzle/MagazineSocket을 보존하여 시각 모델을 정렬했다. 탄창 JPG는 알파 채널이 없으므로 파일명의 Smoothnes만으로 매끄러움 채널이 보존되었다고 가정하지 않는다. [복원·적용 절차와 검증 한계](gamgo-visual-restore.md)를 따른다. 기기 인수는 아직 완료하지 않았다.


## 패키지 내부 고지 보완 (2026-09-30)

잠금 버전과 일치하는 설치 PackageCache70개 내부에서 LICENSE/COPYING/NOTICE/OFL 및 Third Party Notices 이름을 검색했다. 기존 루트49개와 별도로 Mathematics1.3.3의 Noise 하위 LICENSE1개를 발견해 [원문](licenses/packages/com.unity.mathematics-1.3.3/Unity.Mathematics/Noise/LICENSE)을 바이트 그대로 보존했다. Ashima Arts와 Stefan Gustavson의 저작권·허용 조건·면책을 포함하며 루트 Unity 라이선스로 대체하지 않는다. [경로·해시와 조사 범위](evidence/p12-nested-notices.json).

파일명 검색으로 발견한 범위이며 소스 내부 인라인 고지나 다른 이름의 고지까지 모두 확인했다는 뜻은 아니다. 최종 빌드 의존성 및 엔진 배포 고지 확인은 남아 있다. 원본 PackageCache 전체를 Git이나 제출 Source에 넣지 않는다.

## Mixamo 좀비 모델·동작 (2026-10-06)

사용자 Adobe 계정으로 [Mixamo](https://www.mixamo.com/)에서 다운로드한 Yaku J Ignite(근거리), Parasite L Starkie(원거리), Scary Zombie Pack(12개 FBX)을 로컬에 보존했다. 공급자는 Adobe/Mixamo이며 개별 원저작자 크레딧은 다운로드 파일명만으로 추정하지 않는다. 원본 파일의 해시는 [복원 스크립트](../scripts/Restore-MixamoAssets.py)에 기록한다.

실제 사용 동작은 zombie idle/walk/attack/death/scream이다. 사용하지 않는 나머지7개 동작은 로컬 다운로드 보존용이며 프리팹 Animator에 연결하지 않았다. 모델에는 유효한 Humanoid avatar가 있지만 원본 동작은 한 프레임 바인드 포즈여서 동작 팩을 리타게팅했다. 수정 내용은 [복원·적용 문서](mixamo-visual-restore.md)를 따른다.

[Adobe 공식 FAQ](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html)를 2026-10-06 확인했다. 캐릭터·애니메이션을 개인/상업/비영리 프로젝트와 게임에 royalty-free로 사용할 수 있다고 안내한다. 이 안내를 원본 자원의 독립 재배포 허가로 확대하지 않는다. FBX·추출 텍스처·생성 자원은 Git 제외하고 사용자 취득 파일로 복원한다. 최종 Source 전달 범위는 제출 방식과 원본 제공 조건을 확인한 후 확정한다.

## 별도 피격 모션 (2026-10-07)

Mixamo의 `Zombie Reaction Hit` 중 Flinches 동작을 사용자 로그인 세션에서 FBX for Unity / Without Skin / 30fps / none으로 취득했다. 원본명 `Yaku J Ignite@Zombie Reaction Hit.fbx`, 486,960바이트, SHA256 `cdb0ccb2edc0327527829aaa5faa1121e95e05177bf5a66053b2804850ec02a1`이다. 임포트 결과2초/30fps의 Humanoid 동작이며 두 적에 상체 마스크와0.4초 재생·블렌딩을 적용했다. 위 동작 팩에 포함된 파일로 혼동하지 않는다. 사용 조건과 원본 Git 제외 정책은 위 Adobe/Mixamo 항목과 동일하다.

## 사용자 제공 발사·장전·빈 총 음원

기존 합성 무기음 대신 사용자 제공 MP3 세 개를 사용한다. 출처는 [무료 소리 창고](https://pgtd.tistory.com/227), 게시자 〖 -라일락- 〗이며 페이지에서 CC BY 4.0을 연결한다. 사용 구간·페이드·mono 변환·정규화·로컬 복원은 [음원 문서](user-weapon-audio.md)를 따른다. 기존 합성 전투 신호의 출처는 그대로 유지하며 세 무기음을 합성 원본이라고 표시하지 않는다.
# 보스·돌연변이 자원 추가 (2026-10-08)

사용자가 제공한 Mixamo 모델 Skeletonzombie T Avelange와 Zombiegirl W Kurniawan을 각각 5웨이브 주기 보스와 6웨이브 이후 크롤러에 사용한다. 사용자 로그인 세션에서 Zombie Crawl(In Place), Zombie Biting(Victim On The Ground)을 FBX for Unity / Without Skin / 30fps / 키프레임 축소 없음으로 내려받았다. 이전 Adobe/Mixamo 사용 조건 및 원본 Git 제외 원칙을 유지한다. 원본 4개의 SHA256은 [복원 스크립트](../scripts/Restore-EnemyVariants.py)에 기록했다. Humanoid 리타게팅, URP 재질 연결, 보스 크기 조정, 크롤러 접지 보정과 머리 판정을 적용했다. [상세 복원 절차](boss-crawler.md).
