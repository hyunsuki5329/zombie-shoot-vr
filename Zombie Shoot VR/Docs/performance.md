# 성능·장시간 검증

현재 상태: 실제 두 스캔 맵의 성능과 30분 시험은 미실행이다. 개발 빌드 성공이나 자동 시험으로 72Hz 통과를 주장하지 않는다.

## 고정 측정 조건

- Quest Pro + Virtual Desktop, 72Hz, 프레임 보간 끔. 해상도·코덱·비트레이트·PC 유선 연결 여부·무선 환경을 기록한다.
- 전투 구간 CPU와 GPU 각각 p95 ≤ 12.5ms를 목표로 한다. 두 시간을 합산하지 않는다. p99·끊김·RAM/VRAM·객체 상한도 남긴다.
- 빈 맵: 2분 워밍업 후 3분. 일반 전투/재장전 3분. 최대 동시 적·탄창·효과 3분. 실제 각 맵 30분. 재도전과 맵 전환 각각 10회.
- GPU 시간이 제공되지 않는 도구에서 0ms를 기록하지 않는다. 지원되는 Unity Profiler/VR 런타임 측정으로 보완한다.
- 사용자가 제공한 기존 90Hz·SSW Always enabled 설정은 별도 프로필이다. 위 72Hz 기준의 시험으로 혼합하지 않는다.

## 자동 GC 회귀 측정

`ProjectileAllocationTests`는 `Unity.Profiling.ProfilerRecorder`의 `GC.Alloc` 사건을 현재 스레드에서 기록한다. 측정 전 별도의 8KB 배열 생성이 실제로 잡히는지 교정 시험을 통과해야 한다. 레코더의 유효성 및 용량 초과도 확인한다.

현재 Unity 6000.3.23f1 환경에서 `GC.GetAllocatedBytesForCurrentThread()`는 알려진 8KB 할당도 0을 반환했다. 따라서 이 카운터로 얻은 초기 0 값은 무할당 증빙으로 쓰지 않는다. 교정된 Unity 레코더에서는 변경 전 투사체의 빈 비행/충돌 반복 1,000회에 각각 GC.Alloc 1,000회가 재현됐다. 최종 변경 후 결과는 워크스페이스 `docs/evidence/p11-projectile-allocation.json`에 기록한다.

최적화는 투사체별 Collider 버퍼 1개를 재사용한다. 겹침이 있을 때 피해 적용 전에 WorldSolid를 재확인해 버퍼에 가려진 벽도 우선한다. 빈 비행에는 추가 질의를 하지 않으며 단일 플레이어의 충돌 하나만 필요하다는 게임 규칙을 사용한다. 관리 할당 제거와 실제 CPU 시간 개선은 별개이므로 CPU 개선 여부는 실측해야 한다.

이 시험은 GC 사건 수의 회귀 검증이다. 할당 바이트·실제 FPS·CPU/GPU p95·30분 안정성 결과로 확대하지 않는다. 최신 전체 PlayMode 결과와 빌드 식별자를 함께 보관한다.

API 근거: [Unity ProfilerRecorder 현재 스레드 GC.Alloc 예제](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorderOptions.CollectOnlyOnCurrentThread.html).

## 남은 작업

풀 등록/반환과 현재·최대 객체 수 통계, 장시간 기록 수집, 실제 두 맵 측정, 병목별 비교 측정과 최종 판정을 이어 수행한다. 기존 객체 제한(동시 적 4/6, 원거리 1/2, 투사체12, 바닥 탄창8, 사체4, 일시 효과16)은 코드·시험과 실제 관측을 함께 대조한다.
