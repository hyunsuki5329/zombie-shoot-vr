# P06 시험장 자산 출처

작성일: 2026-09-23. 아래 표는 초기 기본 도형 시험장 기록이다. **2026-10-06부터 두 적 외형·동작은 사용자 선택 Mixamo 자산으로 교체하는 작업을 진행한다.** 최신 출처와 복원은 [자산 출처](../asset-provenance.md)와 [Mixamo 적용](../mixamo-visual-restore.md)을 따른다. 최종 양안 표시·성능 검증은 미완료다.

| 자산 | 출처·제작 방법 | 위치·사용 조건 |
|---|---|---|
| 근접/원거리 좀비 임시 외형 | 이 프로젝트의 EnemyLabSetup이 Unity 기본 도형으로 직접 구성한 모델. 외부 다운로드 모델 없음 | `Assets/VRFPS/Prefabs/Enemies/MeleeZombie.prefab`, `RangedZombie.prefab`. 외부 모델 원본이나 텍스처를 포함하지 않음 |
| Idle/Walk/Attack/Dead 클립과 Animator | EnemyLabSetup에서 Transform 곡선을 직접 작성. 이동은 NavMeshAgent가 담당하고 루트 모션은 끔 | `Assets/VRFPS/Animators/`. 외부 모션 캡처/애니메이션 없음 |
| 산성 투사체 | 기본 구체와 직접 지정한 URP Unlit 색상 | `Assets/VRFPS/Prefabs/Enemies/AcidProjectile.prefab`, `Assets/VRFPS/Materials/Acid.mat` |
| NavMesh 데이터 | 시험장 WorldSolid 콜라이더에서 생성 | `Assets/VRFPS/Navigation/InteractionLab.asset`. 실제 스캔 공간의 경로 데이터가 아님 |
| AI Navigation 2.0.14 | Unity Technologies, Unity Package Registry에서 2026-09-23 설치. 로컬 package.json과 LICENSE.md 확인 | Unity Companion License for Unity-dependent projects. Package manifest/lock으로 버전 복원하며 PackageCache를 Git에 복사하지 않음. 배포 시 해당 라이선스 원문 확인 경로: 패키지의 LICENSE.md |

도구: `Assets/VRFPS/Editor/EnemyLabSetup.cs`. 최종 좀비 외형과 리깅 품질을 승인한 기록으로 사용하지 않는다.
