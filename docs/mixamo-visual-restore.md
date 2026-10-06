# Mixamo 좀비 외형 복원

사용자 지정: **Yaku J Ignite = 근거리**, **Parasite L Starkie = 원거리**. 두 모델의 원본은 바인드 포즈만 포함하므로 별도로 확보한 Scary Zombie Pack의 동작을 Humanoid로 리타게팅한다.

## 로컬 복원

사용자가 Mixamo에서 취득한 `Yaku J Ignite.fbx`, `Parasite L Starkie.fbx`, `Scary Zombie Pack.zip`을 준비한다. ZIP 다운로드 설정은 FBX for Unity / No Character / 30fps / none이었다. 파일의 SHA256은 복원 스크립트에 고정되어 있다. 다른 다운로드를 받았다면 해시를 임의 변경하지 말고 내용부터 검토한다.

Unity가 해당 프로젝트를 열고 있지 않은 상태에서 저장소 루트에서 실행한다. 기존 작업을 보존하려면 별도 검증 프로젝트에서 먼저 실행한다.

```powershell
python -B scripts/Restore-MixamoAssets.py --downloads 'C:/Users/User/Downloads' --project './Zombie Shoot VR'
New-Item -ItemType Directory -Force tmp | Out-Null
& 'D:/Unity editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath "$PWD/Zombie Shoot VR" -executeMethod VRFPS.Editor.MixamoVisualSetup.Configure -logFile "$PWD/tmp/mixamo-apply.log"
```

복원기는 세 원본의 해시, ZIP CRC/항목과 기존 파일을 검사한 후 14개 FBX를 복원한다. 변경된 로컬 원본은 덮어쓰지 않는다. Unity 적용기는 Humanoid avatar, 추출 텍스처, URP 재질과 Animator를 만들고 두 적 프리팹의 외형 참조를 다시 연결한다. 생성된 재질/Animator는 재실행하면 설정이 다시 적용된다.

`Assets/VRFPS/External/Mixamo/` 원본·추출물·생성물은 Git 제외다. 새 체크아웃은 복원과 Unity 적용 **둘 다** 필요하다. 총/탄창은 별도의 [GAMGO 복원](gamgo-visual-restore.md)도 필요하다. 이 과정을 하지 않은 소스는 외부 모델 참조가 누락되며 외형 테스트가 실패한다.

## 연결과 제한

- 두 모델 모두 유효한 Humanoid avatar. Yaku 8,144삼각형, Parasite 10,856삼각형. 텍스처 임포트 상한2K.
- Idle/Walk/Dead는 공통 모션, 근거리 Attack은 zombie attack, 원거리는 zombie scream을 발사 준비 동작으로 사용한다. 전용 침 뱉기 모션은 아니다.
- 이동은 기존 NavMeshAgent가 담당한다. 평면 루트 이동은 추출하고 `applyRootMotion=false`로 두어 외형이 콜라이더에서 멀어지지 않게 한다.
- 공격 클립 약40% 지점을 기존 준비시간(근거리0.65초/원거리0.8초)에 맞추고 후속 동작을 이어간다. 실제 피해·투사체 생성은 기존 ZombieController 타이머가 담당하며 애니메이션 이벤트를 추가하지 않는다. 실제 VR에서 접촉감은 추가 확인이 필요하다.
- 원본 diffuse 알파가 있는 재질은 cutout으로 처리한다. 별도 diffuse atlas에 대응하지 않는 normal atlas는 재사용하지 않는다.
- 기존 기본 도형 외형을 끄고 그 안의 이전 Animator를 제거해 새 Animator 하나만 사용한다. 기존 콜라이더·공격 원점·색상 경고·풀링 규칙을 유지한다.
- 이 변경은 외형·동작 통합이며 최종 P06 인수 완료가 아니다. 실제 VR 크기/바닥/공격 방향/양안/프레임 성능, 전용 피격 동작과 최종 맵 검증은 남아 있다.

검증 결과와 빌드 경로는 [진행 기록](progress.md) 및 [증빙](evidence/p06-mixamo-visuals.json)을 따른다.

## 새 소스 복원 리허설 (2026-10-06)

`53daa00`의 Assets/Packages/ProjectSettings를 Git에서 별도 폴더로 내보내고, 기존 Library 및 외부 자원 meta 없이 위 복원을 실행했다. Yaku/Parasite의 모델 GUID가 새로 생성됐으며 각 적 프리팹에 새 원본 참조가 정확히1개 있고 이전 GUID가 남지 않았음을 확인했다. 복원본 EditMode57/57, PlayMode116/116 통과, BootstrapPreview/WaveLab Windows 빌드 모두 성공·종료0이었다. [복원 증빙](evidence/p06-mixamo-restoration.json).

같은 PC의 전역 패키지 캐시와 Unity 라이선스는 재사용할 수 있으므로 새 PC 또는 오프라인 복원 증명은 아니다. 기존 기기용 MixamoMenuLab은 교체하지 않았다. 사용자 답변 기준 새 빌드 VR 검증은 아직 실행 전이다.
