# GAMGO 외형 적용 작업본

현재 상태: **로컬 모델·재질과 총/탄창 프리팹 적용, 실제 기기 검증 대기**. 격리 프로젝트에서 최종 EditMode51/51과 외형 적용 후 PlayMode116/116을 확인했다. Windows 빌드 결과는 진행 기록을 따른다.

## 로컬 자원 복원

[출처 및 라이선스](asset-provenance.md)의 모델 두 개를 사용자 계정으로 취득한다. ZIP은 Git에 넣지 않는다. 파일명은 `daewoo-k2.zip`, `30-rounds-556mm-stanag-mag.zip`이며 취득 증빙의 SHA256과 다르면 복원 도구가 중단한다. 원본이 변경된 경우 검토 없이 해시를 바꾸지 않는다.

Unity를 닫은 상태에서 저장소 루트의 PowerShell로 실행한다:

```powershell
python -B scripts/Restore-GamgoAssets.py --archives 'C:/Users/User/Downloads' --project './Zombie Shoot VR'
& 'D:/Unity editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath "$PWD/Zombie Shoot VR" -executeMethod VRFPS.Editor.GamgoVisualSetup.Configure -logFile "$PWD/tmp/gamgo-apply.log"
```

이는 프리팹을 수정하는 명시적 적용 명령이다. 실제 기기 검증과 최종 제출 인수 전 단계다. 안전한 검증에는 기존 격리 프로젝트 경로를 사용한다. 복원 도구는 두 ZIP의 해시/CRC/항목/기존 파일을 모두 확인한 뒤 쓰며 사용자가 수정한 로컬 자산은 덮어쓰지 않는다.

## 적용 범위

- `Assets/VRFPS/External/GAMGO`는 원본 FBX/텍스처와 생성 URP 재질·패킹 텍스처를 담는 Git 제외 폴더다.
- K2는 원본 +X축을 리그 +Z축으로 바꾸고 0.14배, 위치(0,-0.17,0.079)를 초기값으로 사용한다. 탄창도0.14배, 원본 회전을 적용하고 기존 소켓 피벗 중심에 맞춘다. 실제 손/눈 확인 전 확정값이 아니다.
- 기존 Foregrip/Muzzle Transform과 탄창 물리/삽입 규칙을 보존한다. 기본 도형 Renderer를 숨기고 별도 GAMGO Visual을 추가한다.
- K2 metallic의 R과 1-roughness의 A를 URP 맵으로 묶는다. 탄창 JPEG에는 알파가 없어 smoothness0.35를 초기값으로 사용한다. GPU 임포트 상한은2K다.
- 새 WeaponVisualAssetTests는 적용된 외형을 검사하므로 이 복원/적용 절차 후 실행한다. 기존 기본 도형 상태에서는 의도적으로 실패한다.

## 검증과 미해결

격리 프로젝트에서 모델 누락 검사1개 실패를 확인한 뒤 적용, 전체 EditMode51/51 통과. 이 결과는 시각 품질을 증명하지 않는다. ZIP 복원의 잘못된 해시 거부/정상 복원/변경 파일 보존을 직접 검사했다.

첫 Camera.Render 호출에 나타난 부자연스러운 광택은 재질을 전혀 변경하지 않은 연속 렌더에서도 이후 호출에 사라졌다. 따라서 초기 노멀맵 결함 추정은 확정하지 않는다. 노멀 강도0.2 비교에 초기화 효과가 섞였음을 확인하고 원본 강도1을 유지했다. 최종 적용기는 Mikk 접선 계산·green 반전 없음·강도1을 명시하며 첫 미리보기 프레임을 최종 품질 근거로 쓰지 않는다. 초기화 후 기본색·홈·탄창 정렬을 측면에서 확인했지만 실제 Windows 실행의 첫 프레임, 반대쪽·양안 표시, 실제 손동작은 아직 미검증이다.

격리 적용기를 반복 실행한 뒤 EditMode51/51, 외형 적용 상태 PlayMode116/116을 확인했다. 생산 프로젝트에는 같은 로컬 자원과 두 프리팹만 복사했으며 사용자 XR/프로젝트 설정은 보존했다. Git 프리팹은 로컬 자원 GUID를 참조하므로 새 체크아웃에서 복원/적용 명령을 실행하기 전에는 외부 참조가 누락된다. 적용기가 새로 생성된 로컬 GUID로 두 프리팹의 외형 참조를 다시 연결한다. 이 절차 없이 소스 복원 완료를 주장하지 않는다.

진단 이미지와 로그: `tmp/external-assets/gamgo/`, `tmp/p06-verification/Zombie Shoot VR/gamgo-rig-*.png`. 이는 로컬 조사 산출물이며 최종 제출 증빙은 확정 빌드에서 다시 만든다.


## 새 GUID 복원에서 확인한 결함과 수정 (2026-09-30)

3133162의 Git 소스만 새 폴더에 내보내고 원본 ZIP을 복원해 모델 meta/GUID를 새로 생성했다. 기존 도구는 Unity가 `GAMGO Visual (Missing Prefab...)`로 바꾼 자식을 제거하지 못해 옛 모델 참조를 남겼다. 종료0이나 기존51개 시험만으로 이를 발견하지 못했다.

수정 도구는 바로 아래 자식 중 자체 생성 이름 또는 관측된 누락 프리팹 이름만 제거한다. 검사에는 저장된 `m_SourcePrefab` GUID가 실제 자산 경로로 해석되는지 확인을 추가했다. 수정 전1개 실패 후 복원본 전체 EditMode51개 통과, 두 프리팹 모두 옛 GUID 없음·새 GUID 참조1개를 확인했다. [복원 증빙](evidence/p04-gamgo-restoration.json). 새 PC·오프라인·실기 증명은 아니다.
