# GAMGO 외형 적용 작업본

현재 상태: **WIP — 격리 프로젝트만 적용, 최종 시각 검증 미통과**. 기존 실행 빌드와 원본 프로젝트의 총/탄창 프리팹은 아직 교체하지 않았다.

## 로컬 자원 복원

[출처 및 라이선스](asset-provenance.md)의 모델 두 개를 사용자 계정으로 취득한다. ZIP은 Git에 넣지 않는다. 파일명은 `daewoo-k2.zip`, `30-rounds-556mm-stanag-mag.zip`이며 취득 증빙의 SHA256과 다르면 복원 도구가 중단한다. 원본이 변경된 경우 검토 없이 해시를 바꾸지 않는다.

Unity를 닫은 상태에서 저장소 루트의 PowerShell로 실행한다:

```powershell
python -B scripts/Restore-GamgoAssets.py --archives 'C:/Users/User/Downloads' --project './Zombie Shoot VR'
& 'D:/Unity editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath "$PWD/Zombie Shoot VR" -executeMethod VRFPS.Editor.GamgoVisualSetup.Configure -logFile "$PWD/tmp/gamgo-apply.log"
```

이는 프리팹을 수정하는 명시적 적용 명령이다. 현재 시각 결함이 남아 있어 최종 제출에 사용하지 않는다. 안전한 검증에는 기존 격리 프로젝트 경로를 사용한다. 복원 도구는 두 ZIP의 해시/CRC/항목/기존 파일을 모두 확인한 뒤 쓰며 사용자가 수정한 로컬 자산은 덮어쓰지 않는다.

## 적용 범위

- `Assets/VRFPS/External/GAMGO`는 원본 FBX/텍스처와 생성 URP 재질·패킹 텍스처를 담는 Git 제외 폴더다.
- K2는 원본 +X축을 리그 +Z축으로 바꾸고 0.14배, 위치(0,-0.17,0.079)를 초기값으로 사용한다. 탄창도0.14배, 원본 회전을 적용하고 기존 소켓 피벗 중심에 맞춘다. 실제 손/눈 확인 전 확정값이 아니다.
- 기존 Foregrip/Muzzle Transform과 탄창 물리/삽입 규칙을 보존한다. 기본 도형 Renderer를 숨기고 별도 GAMGO Visual을 추가한다.
- K2 metallic의 R과 1-roughness의 A를 URP 맵으로 묶는다. 탄창 JPEG에는 알파가 없어 smoothness0.35를 초기값으로 사용한다. GPU 임포트 상한은2K다.
- 새 WeaponVisualAssetTests는 적용된 외형을 검사하므로 이 복원/적용 절차 후 실행한다. 기존 기본 도형 상태에서는 의도적으로 실패한다.

## 검증과 미해결

격리 프로젝트에서 모델 누락 검사1개 실패를 확인한 뒤 적용, 전체 EditMode51/51 통과. 이 결과는 시각 품질을 증명하지 않는다. ZIP 복원의 잘못된 해시 거부/정상 복원/변경 파일 보존을 직접 검사했다.

URP 측면 렌더에 부자연스러운 삼각형 광택이 있다. 같은 시점의 Unlit 기본색과 노멀 비활성화 비교에서는 해당 문제가 사라졌다. 노멀 green 반전은 해결하지 못했고 FBX 접선 Import 시도는 원본에 접선이 없다는 경고를 반환했다. 진단용 임포터 변경은 격리 프로젝트에만 있으며 원본 프로젝트에는 적용하지 않았다. 이 원인 조사와 재현 가능한 임포터 설정 확정, PlayMode 회귀, 재적용 안정성, Windows 빌드, 실제 기기 확인이 남아 있다. 노멀 제거를 최종 해결로 간주하지 않는다.

진단 이미지와 로그: `tmp/external-assets/gamgo/`, `tmp/p06-verification/Zombie Shoot VR/gamgo-rig-*.png`. 이는 로컬 조사 산출물이며 최종 제출 증빙은 확정 빌드에서 다시 만든다.
