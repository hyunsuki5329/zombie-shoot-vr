# 보스·기어다니는 돌연변이

사용자 요청(2026-10-08)에 따라 근접 적에 두 변형을 추가한다.

- 5·10·15…웨이브: 첫 근접 슬롯1개를 Skeletonzombie T Avelange 보스로 대체한다. 기본 체력500, 피해25, 속도0.9m/s, 공격 예고1초. 기존 웨이브 체력/피해 배율을 적용하므로 첫 등장인5웨이브는 체력600·피해27이다.
- 6웨이브부터: 근접 슬롯의 약1/3을 Zombiegirl W Kurniawan 돌연변이로 대체한다. 기본 체력70, 피해8, 속도2.2m/s, 예고0.45초. 같은 슬롯에 보스가 예정되면 보스가 우선한다.
- 일반 적 할당량에 더하는 방식이 아니며 총 할당량·동시 생존·원거리 상한은 유지한다. 생성 실패는 슬롯을 소비하지 않는다.
- 돌연변이는 Mixamo Zombie Crawl(In Place)로 이동하고 Zombie Biting(Victim On The Ground)으로 공격한다. 서 있는 피격 동작을 섞지 않고 사망 시 낮은 자세에서 정지한 뒤 기존 시체 회수 규칙을 따른다. 보스는 기존 대기·걷기·공격·사망·피격 동작을 리타게팅한다.
- 돌연변이의 접지 보정은 애니메이션 후 발·발끝·손 높이를 기준으로 시각 모델만 보정한다. NavMesh 루트는 이동하지 않는다. 낮은 수평 몸통 판정과 움직이는 머리 판정을 사용한다.
- 전투·정지·사망·재도전은 기존 상태 규칙을 공유한다. 풀은 일반 근접/원거리/돌연변이/보스로 분리하고 보관 객체 용량 합계를 진단에 기록한다. 동시 전투 객체 상한은 기존 맵 설정을 유지한다.

## 취득과 복원

보스 및 돌연변이 FBX는 사용자가 제공했다. 두 모션은 사용자 로그인 Mixamo Chrome 탭에서 FBX for Unity / Without Skin /30fps/키프레임 축소 없음으로 다운로드했다. Zombie Crawl은 In Place를 켰다. 스킨 없는 두 모션은 다운로드 기준 캐릭터인 Skeleton의 T자 자세 Avatar를 복사해 임포트한다. 모션 첫 프레임에서 Avatar를 새로 만들면 크롤링 몸통이 대각선으로 들리는 문제가 있어 이 경로를 사용하지 않는다. 원본 파일은 변경하지 않으며 Mixamo 사용 조건은 기존 출처 문서를 따른다.

기존 GAMGO·Mixamo·음원 복원 후 아래 추가 원본4개를 같은 폴더에 놓고 실행한다.

- Skeletonzombie T Avelange.fbx
- Zombiegirl W Kurniawan.fbx
- Skeletonzombie T Avelange@Zombie Crawl.fbx
- Skeletonzombie T Avelange@Zombie Biting.fbx

```powershell
python -B scripts/Restore-EnemyVariants.py --source 'C:/Users/User/Downloads' --project './Zombie Shoot VR'
```

그다음 격리된 Unity 프로젝트에서 `VRFPS.Editor.EnemyVariantSetup.Configure`를 실행한다. 새GUID가 생성되는 복원에서는 기본 `MixamoVisualSetup.Configure` 이후 변형 적용기를 실행해야 한다. 원본·생성 재질·Animator는 기존 Mixamo Git 제외 폴더에 보관한다.

실제 VR에서는5웨이브 보스의 크기·공격 가독성,6웨이브부터 돌연변이의 접지·피격·접근 속도를 확인한다. 자동 테스트나 평면 렌더를 실제 기기 인수로 표시하지 않는다.

## 검증과 실행

EditMode68개(크롤러 발 접지·상체 높이 포함), PlayMode121개, Python23개를 검증한다. 최신 결과와 빌드 해시는 [증빙](evidence/boss-crawler.json)을 따른다. Windows 실행 파일은 `Zombie Shoot VR/Builds/Windows/BossCrawlerMenuLab/WaveLab.exe`다. 독립 코드 리뷰에서 중요한 결함은 발견되지 않았으나 실제 VR에서 바닥 접촉·외형과 피격 위치의 일치·접근 속도 확인이 남아 있다.
