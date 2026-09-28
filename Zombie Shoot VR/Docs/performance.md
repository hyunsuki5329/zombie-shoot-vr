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

## 풀 등록과 투사체 수명

`PoolRegistry.Register`는 풀 ID·프리팹·상한·타입별 초기화 콜백을 등록한다. `TryRent`는 상한에 도달하면 false를 반환하며, 대여 객체는 비활성 상태로 제공한다. 소유자가 초기화 후 활성화한다. `Return`은 외부 객체·다른 풀 ID·중복 반환을 무시한다. 반환 중 객체는 재대여할 수 없으며 초기화 실패 객체는 폐기한다. 파괴된 객체는 슬롯과 현재 대여 수에서 제외한다.

`GetCounts`의 Current는 현재 대여 수, Peak는 등록 후 최대 동시 대여 수, Created는 현재 보유한 객체 수(누적 생성 횟수가 아님), Capacity는 상한이다. 재도전 시 Current는 0으로 돌아가고 Peak는 풀 수명 동안 유지한다. 레지스트리 소유 객체는 다른 부모로 이동했어도 레지스트리 파괴 시 정리한다.

투사체 풀은 `projectiles` ID와 상한12로 연결했다. 수명 종료·충돌·새 도전에서 반환하며 방향·남은 수명·피해량·공격자·충돌 마스크를 지운다. 잘못된 발사 인수로 예외가 나도 대여 슬롯을 반환한다. 각 타입의 속도·이벤트·타격 이력·애니메이터 초기화는 등록된 콜백의 책임이다. 투사체에는 Rigidbody·Animator·구독 사건이 없다.

현재 레지스트리 연결 대상은 투사체·탄창·적/사체다. 효과 상한과 통계 수집을 다음 단계에서 연결한다. 이 연결 및 합성 반복 시험을 실제 맵30분 안정성 증빙으로 취급하지 않는다.

## 탄창 소유권과 통계

`AmmoPouch`는 `magazines` 풀에 총10개(삽입1+손1+바닥8)를 허용한다. 기존 씬의 장착 탄창은 `Adopt`로 편입하고 위치·활성 상태·부모를 보존한다. 같은 레지스트리 내 중복 편입 및 다른 풀로의 중복 편입은 거절한다.

바닥의 소유자 없는 탄창만 12초 또는 바닥8개 초과 시 반환한다. 반환은 물리 속도·보류된 운동 상태·손/소켓 소유권을 초기화하고 비활성 보관한다. 손과 소켓 탄창은 일반 회수 대상이 아니며 명시적 재도전에서는 전체 반환 뒤 하나를 대여해 장착한다. 일시정지에서는 기존 운동과 수명 정지 규칙을 유지한다.

`AmmoPouch.Counts`로 전체 대여/최대/보유/상한을, `LooseCount`로 현재 바닥 수를 읽는다. `PeakLooseCount`는 수명·상한 정리가 끝난 Tick에서 관측한 최대 바닥 수이며 물리 프레임 사이 모든 순간의 연속 측정은 아니다. 100회 공급·해제 후에도 총10개 상한과 소유 탄창 보호, 만료 후 대여 수 감소, 재도전 후 장착1개를 합성 시험으로 확인했다.

## 적과 사체의 구분

SpawnDirector의 생존 수 제한은 맵 maxAlive(4/6), 원거리 maxRangedAlive(1/2)이며 사체는 최대4개·5초다. 사체는 죽었던 몸체를 그대로 표시하므로 해당 객체의 풀 대여는 표시가 끝날 때까지 유지한다. `AliveCount`/`CorpseCount` 및 `PeakAliveCount`/`PeakCorpseCount`는 이 두 집단을 구분한다. 최대값은 생성·사망 상한 처리 후 갱신하고 재도전 간 유지한다.

레지스트리의 `melee`와 `ranged` 풀은 각각 maxAlive+4, maxRangedAlive+4개의 몸체 슬롯을 허용하며 전체 생성 몸체는 기존16개 상한을 유지한다. 풀의 Current는 생존과 표시 중 사체를 모두 포함하고 Created는 비활성 보관분도 포함하므로 생존 수로 읽지 않는다. 타입별 여유 슬롯은 프리팹 종류가 다른 몸체를 재사용하기 위한 것이다.

사체 만료·초과 사체·재도전에서 Return을 호출한다. 반환 시 공격/대상/사건 구독을 비우고 이동 갱신과 에이전트를 중단하며 애니메이터를 초기화한다. 재대여 시 기존 Initialize가 새 ID·체력·공격 대상·이동·피드백 구독을 설정한다. 반환은 추가 처치나 생성 성공으로 계산하지 않는다.

## 장시간 객체 기록 실행

워크스페이스에서 `pwsh -File scripts/Start-VrDiagnostics.ps1 -Build MenuLab`로 실행한다. `-CheckOnly`를 덧붙이면 실행 없이 경로만 확인한다. 실제 맵을 연결한 뒤에는 `-Build BootstrapPreview`를 사용한다. 직접 실행할 때는 실행 파일에 `-vrfps-diagnostics` 인수를 추가한다. 옵션이 없으면 수집기를 만들지 않는다.

기본 출력은 `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Zombie Shoot VR/Diagnostics/*.jsonl`이다. 정확한 경로는 Player.log의 `VRFPS_DIAGNOSTICS`에도 남긴다. UTC 시각과 GUID로 새 파일을 만들고 기존 파일을 덮어쓰지 않는다. 맵 세션마다 새 파일, 같은 맵 재도전은 같은 파일의 attempt 증가로 구분한다. MenuLab은 `wave-lab`, 공통 맵 세션은 `map-session`으로 기록하며 후자가 실제 스캔이라는 증빙은 아니다.

1초마다 상태·맵·웨이브·처치·경과 실시간·전투 시간·시도 번호, 각 객체의 현재/최대/상한, 관리 힙 바이트를 기록한다. 매 표본을 flush하고 메모리에 표본 목록을 쌓지 않는다. 50ms 트레이서 같은 짧은 효과는 표본 사이에 발생해 관측되지 않을 수 있다. 현재 시각 효과는 고정 트레이서1개와 생성 예고1개를 재사용하므로 생성 객체는2개이며 임시 효과 상한16 이내다. 오디오 소스는 이 시각 효과 수에 포함하지 않는다.

`boundsSatisfied`는 해당 표본의 수량 상한 판정일 뿐 전체 실행의 성능/안정성 합격이 아니다. 헤더의 `cpuGpuTimingCaptured:false`를 확인한다. 관리 힙은 전체 RAM/VRAM이 아니며 이 기록은 CPU/GPU p95·p99를 제공하지 않는다. Unity Profiler/VR 런타임 측정과 함께 같은 구간을 비교해야 한다. JSON 직렬화와 파일 쓰기는 관측 비용이 있으므로 켠/끈 조건도 구분한다.

정상 종료/맵 언로드/수집기 비활성화 시 footer를 기록한다. 강제 종료 시 footer가 없을 수 있으므로 미완료 기록으로 분류한다. 쓰기 실패는 수집을 중단하고 Player.log에 `VRFPS_DIAGNOSTICS_FAILED`를 남긴다. 실제 30분 시험 전 새 파일이 증가하는지 확인하고, 종료 후 footer와 표본 간격·누락·attempt를 확인한다. 자동 시험의 짧은 기록을 30분 실기 결과로 취급하지 않는다.

## 기록 요약

Python 3.11 표준 라이브러리로 검증한 분석기는 워크스페이스 `scripts/analyze_vr_diagnostics.py`다. `python -B scripts/analyze_vr_diagnostics.py "기록.jsonl" --output "새-요약.json"`로 실행한다. 출력 옵션을 생략하면 화면에 JSON을 출력한다. 원본과 기존 요약 파일을 덮어쓰지 않는다.

한 줄씩 읽으며 원본 SHA256, 표본 개수·시간 범위·최대 간격, 관측된 전투 시간, 재도전 번호, 객체 최대 수, 관리 힙 증감을 요약한다. 헤더/스키마·표본 순서·시간 역행·맵 변경·종료 개수·숫자 형식·중복 JSON 키를 검사한다. 잘린 JSON이나 잘못된 스키마는 행 번호와 오류로 거절한다. 정상 형식에서 footer만 없으면 `recording_complete:false`로 표시한다.

`boundsSatisfied`를 신뢰하지 않고 현재 기준(방4/1, 강의실6/2, 시험장4/1 및 나머지 고정 상한)으로 다시 계산한다. 비활성 투사체/탄창 보유 수와 생성 효과 수까지 비교한다. 기록된 상한이 기준과 다르면 `cap_mismatch_samples`에 나타난다. 모르는 맵은 `known_map_policy:false`이며 최대6/2와 비교한 참고값만 제공한다. 밸런스 기준 변경 시 분석기 정책도 함께 갱신해야 한다.

`thirty_minute_sample_span`은 첫/마지막 표본 사이1800초 여부로, 일시정지·메뉴 시간을 포함한다. `observed_active_seconds`는 같은 attempt에서 관측한 전투 시계 차이의 합이며 재도전 경계의 미관측 시간은 포함하지 않는다. 간격2.5초 초과는 누락 검토용 표시이지 성능 합격 기준이 아니다. 종료 코드0은 분석 성공일 뿐 인수 합격이 아니며 `performance_acceptance`는 항상 `unverified`다. CPU/GPU·VRAM·실제 두 맵·기기 출처와30분 플레이를 별도 확인해야 한다.

## 원시 프레임 시간 수집

`pwsh -File scripts/Start-VrDiagnostics.ps1 -Build MenuLab -FrameTimings`는 객체 JSONL과 원시 프레임 `.vrft`를 함께 켠다. 직접 실행 시 `-vrfps-frame-timings` 옵션이다. JSONL과 함께 켜면 같은 파일 이름의 확장자만 다르고, 바이너리 헤더에 연결된 객체 로그 경로가 남는다. JSONL 헤더의 `cpuGpuTimingCaptured:false`는 그 파일 자체에 시간 표본이 없다는 뜻이며 별도의 `.vrft`를 확인해야 한다.

Windows 빌드 함수는 빌드 동안 `PlayerSettings.enableFrameTimingStats`를 켜고 종료/실패 후 원래 설정을 복원한다. 파일 기록 옵션이 꺼져도 이 빌드의 엔진 통계 지원 설정은 켜져 있으므로 계측 비용을 비교할 때 이 조건도 남긴다. 실제 지원 여부는 실행 시 `IsFeatureEnabled`와 수신값으로 확인하며 빌드 설정만으로 GPU 측정 성공을 선언하지 않는다.

설치된 Unity6000.3.23f1 API 문서에서 시간 단위는 ms다. CPU 전체(cpuFrameTime), 메인 스레드, 렌더 스레드, 메인 Present 대기, GPU를 각각 원시값으로 보존한다. CPU 전체에는 대기와 오버헤드가 포함되므로 이를 CPU 작업12.5ms 기준으로 곧바로 판정하지 않는다. CPU 작업 지표 선택과 Unity Profiler/VR 런타임 대조는 별도 검증 사항이며 기준을 임의로 낮추지 않는다.

매 LateUpdate에서 최신1개를 요청하며 frameStartTimestamp가0이면 제외하고 중복/이전 타임스탬프는 다시 세지 않는다. 원시0·NaN·Infinity는 유효성 비트와 함께 보존하며 정상0ms로 평균/백분위에 넣으면 안 된다. CPU/GPU 양수 유한값, Present 대기는0 이상 유한값만 해당 비트를 켠다. 기능 비활성/표본 없음/타임스탬프 없음/중복 수를 footer에 남긴다.

Unity는 지연된 프레임 결과를 반환하므로 phase/attempt는 **측정 프레임의 상태가 아닌 수신 시점의 상태**다. 최신1개 요청이 모든 프레임을 빠짐없이 수집한다고 보장하지 않는다. 시작/종료·상태 전환 경계와 실제 GPU 지원·계측 비용을 검토해야 한다. 현재는 수집 경로와 바이너리 형식 검증이며 실제 기기 시간이나 p95/p99 통과 결과가 아니다.

### VRFT0001 형식

모든 수치는 little-endian이다. 파일은 ASCII8바이트 `VRFT0001`, int32 UTF-8 메타데이터 길이, 해당 JSON 바이트로 시작한다. 메타데이터에는 단위·Unity/빌드/그래픽API·맵/시나리오·시각·기능 상태·phase 이름 배열을 포함한다.

| 레코드 | 순서 | 크기 |
|---|---|---:|
| 프레임(type1) | byte1, uint64타임스탬프, double수신 경과초, int32수신 Unity프레임, int32시도, int32phase, uint32유효성, double CPU전체/메인/렌더/Present대기/GPU | 73바이트 |
| 종료(type2) | byte2, int64 수신프레임/중복/타임스탬프0/기능비활성/표본없음/유효CPU전체/유효GPU | 57바이트 |

유효성 비트는1=CPU전체,2=메인,4=렌더,8=Present대기,16=GPU다. 프레임 문자열을 만들지 않고64KiB 스트림 버퍼에 고정 크기로 기록하고1초마다 flush한다. 종료 레코드가 없거나 중간 레코드가 잘렸으면 미완료로 취급한다.

API 근거: [Unity FrameTiming](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/FrameTiming.html), [FrameTimingManager 지원·지연 조건](https://docs.unity3d.com/6000.0/Documentation/Manual/frame-timing-manager.html). 설치본 `Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.xml`의 필드 설명과 실제 컴파일도 대조했다.

### 원시 기록 분석과 CSV

워크스페이스에서 다음을 실행한다. Python 3.11 표준 라이브러리만 사용한다.

```powershell
python -B scripts/analyze_vr_frame_timings.py "기록.vrft" --output "새-프레임요약.json" --csv "새-프레임.csv"
```

`--after 30 --before 1830 --attempt 1 --phase Active`를 추가하면 해당 수신 경과초(양 끝 포함), 시도, 수신 상태만 선택한다. 상태 전환 직후의 지연 측정값까지 실제 Active 프레임이라고 단정하지 않는다. 필터를 생략하면 전체 완전한 프레임을 사용한다. `frames`는 전체, `selected_frames`는 선택 개수다. 필터 밖 레코드도 파일 무결성 검사를 받는다.

각 지표는 유효값만 정렬한 nearest-rank(`ceil(p×N)`, 1부터 시작) p50/p95/p99와 유효/무효 개수를 제공한다. 유효값이 없으면 JSON null이며 CSV 무효 셀은 비운다. GPU0을 빠른 프레임으로 세지 않는다. CPU전체/메인/렌더/Present대기/GPU를 각각 표시하며 `cpu_active_work_p95_ms`는 null, `performance_acceptance`는 항상 unverified다. CPU 작업 지표와 실제 인수 조건은 별도 확인한다.

헤더·단위·상태 배열·시간/시도 역행·타임스탬프 중복·유효성 비트·footer 개수를 검증한다. 알 수 없는 레코드, 잘못된 footer, footer 뒤 데이터는 오류다. 종료 레코드 부재나 마지막 레코드의 일부 손실은 완전한 앞부분만 분석하고 `recording_complete:false`로 표시한다. 잘린 헤더는 분석하지 않는다. SHA256은 읽은 원본 바이트 전체에 대한 값이다.

기본 최대2,000,000프레임을 초과하면 중단한다. 필요하면 `--max-frames`로 명시적으로 늘리며 백분위 계산 메모리도 함께 늘어난다. 기존 입력/JSON/CSV를 덮어쓰지 않고 파일 검증에 실패하면 CSV를 게시하지 않는다. 종료 코드0은 분석 성공이며 성능 통과가 아니다. 정상 분석된 미완료 기록도 코드0이므로 반드시 recording_complete와 유효 표본 수를 함께 확인한다.
