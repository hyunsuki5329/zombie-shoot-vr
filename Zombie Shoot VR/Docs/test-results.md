# 검증 결과 진입점

최종 인수 상태: 미완료. 실제 두 스캔 맵·Quest Pro 세부 조작·72Hz CPU/GPU·각 맵30분·초보 사용자 관찰·독립 리뷰가 남아 있다.

## 최신 자동 검증

2026-09-28 P11 객체 기록·원시 프레임 시간 수집 연결: 전체 PlayMode **116/116** 통과. 원시 시간 수집 증빙은 `docs/evidence/p11-frame-timings.json`이다. 기록 기능 증빙은 `docs/evidence/p11-diagnostics.json`이다. 적/사체 증빙은 `docs/evidence/p11-enemy-pool.json`이다. 풀 수명 증빙은 `docs/evidence/p11-pool-registry.json`, 탄창 소유권 증빙은 `docs/evidence/p11-magazine-pool.json`이다. 앞선 투사체 충돌 할당 최적화의 전체 PlayMode는 105/105이었다. 증빙 원본 및 소스/빌드 해시는 워크스페이스의 `docs/evidence/p11-projectile-allocation.json`을 참조한다. 앞 단위의 EditMode 50/50은 이번 변경에서 재실행한 결과가 아니다.

| 측정 | 변경 전 | 최종 변경 후 |
|---|---:|---:|
| 알려진 8KB 배열 생성 | Unity GC.Alloc 1회 | Unity GC.Alloc 1회 |
| 빈 비행 Tick 1,000회 | GC.Alloc 1,000회 | GC.Alloc 0회 |
| 충돌·재사용 1,000회 | GC.Alloc 1,000회 | GC.Alloc 0회 |

위 숫자는 현재 스레드의 GC 사건 수다. 바이트나 CPU/GPU 시간, 최종 게임의 성능 통과를 뜻하지 않는다. .NET 할당 카운터가 알려진 배열도 0으로 보고한 초기 결과는 증빙에서 제외했다. [측정 절차와 한계](performance.md)를 따른다.

기존 얇은 벽 차단·충돌 1회 피해·수명·풀 제한 시험과 함께, 플레이어 충돌체 16개와 벽이 동시에 겹친 경우에도 피해를 막는 회귀 사례가 통과했다.

## 진단 분석 도구

2026-09-28 Python 3.11.9 표준 라이브러리 시험11/11 통과. 기록 종료/누락/상한/스키마/시간/재도전/파일 보호를 검증했다. 분석기 단위에서는 Unity 런타임을 변경하지 않았으며 이후 원시 프레임 수집 단위의 최신 Unity 결과는 위 항목을 따른다. 증빙: `docs/evidence/p11-diagnostics-analysis.json`.

## 전체 진행과 실기

각 단위별 전체 기록·실기 대기 항목·PR 상태는 워크스페이스 `docs/progress.md`를 기준으로 한다. 과거 실기 보고를 이후 변경의 자동 승인으로 확대하지 않는다. 실제 결과를 받으면 날짜·빌드·환경·기대·관찰·판정과 증빙을 연결한다.
