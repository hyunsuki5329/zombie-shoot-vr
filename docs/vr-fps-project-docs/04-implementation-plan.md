# 현실 공간 기반 VR 좀비 FPS Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. superpowers:subagent-driven-development is an alternative only when parallel agent work is explicitly authorized. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 방과 강의실에서 K2 양손 사격과 왼손 재장전으로 두 종류 좀비의 무한 웨이브를 플레이하고 맵별 기록을 남기는 PC VR 게임을 완성한다.

**Architecture:** 촬영 공간의 외형과 충돌·NavMesh를 분리하고 공통 Combat 씬을 두 맵에서 사용한다. 순수 규칙인 발사·탄약·체력·웨이브·기록을 XR 입력과 분리해 시험한다. GS 채택은 작은 실제 기기 시험으로 결정하며 텍스처 메시를 대안으로 둔다.

**Tech Stack:** 기존 VR 템플릿 10.0.0, Unity 6000.3.23f1, URP 17.3.0, OpenXR 1.17.1, XRI 3.5.1, Input System 1.20.0, AI Navigation, C#, Unity Test Framework, Quest Pro, Virtual Desktop. 2026-09-20 갱신. AI Navigation·Test Framework의 추가 설정과 기기 실행은 P01에서 확인한다.

**Spec:** [02 요구사항](02-requirements.md), [03 기술 설계](03-architecture.md), [06 초기 밸런스](06-gameplay-balance.md).

## Global Constraints

- Windows PC VR, Quest Pro, Virtual Desktop. PC는 i5-14600KF, RAM 32GB, RTX 4060 Ti 8GB.
- 사전 제작한 내 방·강의실 두 맵, K2 한 종류, 근접·원거리 좀비.
- 오른손 총 영구 부착, 양손 사격만 허용, 왼손 수동 재장전.
- 새 탄창 30발, 무한 예비 탄창, 제거한 잔탄 폐기, 장전 손잡이 조작 없음.
- 왼쪽 스틱 연속 이동과 오른쪽 스틱 연속 회전. 스냅 회전으로 변경하지 않는다.
- 최대 체력 100, 웨이브 클리어 회복, 짧은 피격 보호, 오른팔 전완 UI.
- 5웨이브 클리어 후 6웨이브 사망은 `wave 6`. 준비 시간에 다음 번호를 미리 기록하지 않는다.
- 무료 에셋 우선, 원본·수정본·출처 관리. 실제 취득·검증 결과를 기록한다.
- 현재 문서의 코드는 설계 예시이며 Unity에서 컴파일하거나 시험한 결과가 아니다.

## 1 파일과 작업 경계

파일 책임은 03의 표를 따른다. 아래 경로는 앞으로 정할 Unity 프로젝트 루트 기준이다. `Files`에서 `Assets/VRFPS/`를 생략한 Runtime·Scenes·Prefabs·Tests·Configuration·Input·Art·Animators·UI·Audio 경로는 모두 그 아래를 뜻한다. Docs·Packages·ProjectSettings는 프로젝트 루트 기준이다. 코드 스니펫은 해당 파일의 핵심 메서드 또는 규칙의 설계 예시다. 엔진 어댑터·직렬화 필드는 설치한 패키지 API에 맞춰 구현하고 기기 시험을 완료한다.

작업 의존 순서는 `P01→P02→P03→P04→P05→P06→P07→P08→P09→P10→P11→P12`다. P02의 외형 문제가 남으면 전투는 InteractionLab에서 진행하되 최종 G4 전에 검증된 외형을 넣는다. 한 사람이 수행하므로 병렬 작업 시간을 전제하지 않는다.

| 작업 | 기본 시간 | 완료 결과 |
|---|---:|---|
| P01 | 5시간 | 실제 기기에서 실행되는 빈 프로젝트 |
| P02 | 12시간 | 작은 스캔 공간과 기술 선택 기록 |
| P03 | 5시간 | 연속 이동·회전과 충돌 |
| P04 | 12시간 | 양손 K2 사격 |
| P05 | 14시간 | 왼손 수동 재장전 |
| P06 | 13시간 | 근접·원거리 좀비 |
| P07 | 9시간 | 생존·웨이브·체력 규칙 |
| P08 | 8시간 | 오른팔 UI·기록·메뉴 |
| P09 | 20시간 | 실제 방·강의실 두 맵 |
| P10 | 6시간 | 안내·소리·진동·효과 |
| P11 | 18시간 | 성능과 장시간 검증 |
| P12 | 12시간 | 제출 빌드·보고서·발표·출처 |

기본 합계 134시간과 여유 26시간이다. 각 작업의 체크박스는 실제로 실행한 뒤 표시한다. 큰 작업은 아래 행동 단위로 나누어 저장한다. 시험이 필요한 규칙 코드는 실패하는 시험→최소 구현→통과 순으로 진행한다. Inspector 설정이나 단순 파일 정리에 구현을 그대로 복제하는 시험을 만들지는 않는다.

## 2 공통 검증 방법

순수 규칙 시험은 Test Runner의 EditMode, Unity 객체·씬 시험은 PlayMode, XR 조작은 실제 Quest Pro에서 실행한다. 각 코드 작업에서 신규 시험을 먼저 실행해 기능 부재를 확인하고 구현 후 같은 시험을 다시 실행한다. 해당 작업의 인수 시험을 통과하면 관련 파일과 `.meta`만 커밋하고 작업 ID를 커밋 메시지에 남긴다. 무관한 사용자 변경은 포함하지 않는다.

CLI 검증은 G0에서 기록한 Unity.exe 경로와 선택한 프로젝트 절대 경로를 사용한다. 아래 PowerShell 변수는 실행자가 실제 경로를 입력받도록 하며 예시 경로를 실제 경로라고 가정하지 않는다.

```powershell
$unityEditorPath = Read-Host 'G0에서 검증한 Unity.exe의 절대 경로'
$gameProjectPath = Read-Host 'Unity 프로젝트의 절대 경로'
if (!(Test-Path -LiteralPath $unityEditorPath)) { throw '에디터 경로 없음' }
if (!(Test-Path -LiteralPath (Join-Path $gameProjectPath 'Assets'))) { throw '프로젝트 경로 오류' }
& $unityEditorPath -batchmode -projectPath $gameProjectPath -runTests -testPlatform EditMode -testResults (Join-Path $gameProjectPath 'TestResults-EditMode.xml') -logFile (Join-Path $gameProjectPath 'TestResults-EditMode.log')
```

반환 코드뿐 아니라 결과 XML의 실패·오류 수를 읽는다. PlayMode는 같은 호출의 `-testPlatform`을 `PlayMode`로 변경하고 파일명을 분리한다. 이 명령을 아직 실행한 것은 아니다.

## P01 실행 환경과 시험장

**Files:** Create `Assets/VRFPS/Scenes/Bootstrap.unity`, `Scenes/Combat.unity`, `Scenes/Tests/InteractionLab.unity`, `Runtime/VRFPS.Runtime.asmdef`, `Tests/EditMode/VRFPS.EditModeTests.asmdef`, `Tests/PlayMode/VRFPS.PlayModeTests.asmdef`, `Docs/environment.md`, `.gitignore`. Modify `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/ProjectSettings.asset`.

**Interfaces:** Consumes 실제 PC·Quest Pro·Virtual Desktop. Produces XROrigin, Main Camera, Left Controller, Right Controller, 입력 Action Asset `Assets/VRFPS/Input/VRFPS.inputactions`, Windows 빌드 환경과 정확한 버전 기록.

- [ ] 지정한 기존 프로젝트가 있으면 파일·버전·변경 상태를 먼저 확인한다. 없다면 새 Unity 프로젝트를 생성하고 작업 루트를 기록한다.
- [ ] 현재 설치된 XRI 3.5.1과 위 패키지 조합·잠금 파일을 확인하고 Windows OpenXR 설정을 검증한다. 필요한 추가 패키지만 구성한다. 컨트롤러 프로파일과 현재 PC OpenXR 런타임을 기록한다.
- [ ] 설치한 XRI 버전의 Starter Assets를 참고해 XR Origin과 입력을 구성한다. 다른 버전의 튜토리얼 컴포넌트 이름을 억지로 혼합하지 않는다.
- [ ] 1m 큐브와 바닥, 벽을 놓고 Quest Pro에서 양안 표시·머리·양손 이동을 확인한다.
- [ ] Windows 개발 빌드를 실행하여 편집기 밖에서도 T01을 통과한다.
- [ ] `Docs/environment.md`에 에디터 패치, URP·XRI·OpenXR·Input System·AI Navigation 버전, OpenXR 런타임, Virtual Desktop 버전·72Hz·해상도·코덱을 실제 값으로 기록한다.
- [ ] Library·Temp·Logs·Obj·빌드 캐시를 Git에서 제외하고 Assets의 meta 파일을 유지한다. P01 상태를 커밋한다.

**Done:** 실행 파일에서 바닥과 컨트롤러가 안정적으로 보이며 입력이 연결된다. 연결 문제가 있으면 스캔 임포트 전에 해결한다.

## P02 작은 공간의 표현 방식 결정

**Files:** Create `Assets/VRFPS/Art/Scans/RoomSample/`, `Scenes/Tests/ScanLab.unity`, `Docs/scan-proof.md`. Modify `ProjectSettings/GraphicsSettings.asset`, 사용 중인 URP Renderer 설정.

**Interfaces:** Consumes P01 XR 씬, 작은 촬영 결과와 기준 거리. Produces 실제 스케일의 `VisualRoot`, `CollisionRoot`, GS/메시 결정과 측정 기록. 공통 맵 필드는 P09에서 정식화한다.

- [ ] 05의 촬영 전 점검 후 책상·벽·바닥이 함께 있는 작은 구역을 Scaniverse Classic에서 연속 스캔한다. 앱 내 처리와 파일 내보내기를 먼저 검증한다.
- [ ] 별도 복원에 사용할 원본은 겹치는 일반 사진으로 확보한다. 일반 동영상만 준비한 경우에는 프레임 추출·선별 경로를 따르며 앱 스캔의 입력으로 바로 가져올 수 있다고 가정하지 않는다.
- [ ] 원본·앱 버전·출력 형식·기준 거리 2개·사용 렌즈·촬영 해상도를 저장한다. 05의 체크리스트로 흐림과 누락 면을 확인한다.
- [ ] GS 후보를 고정된 커밋 또는 버전으로 임포트하고 D3D12·URP 조건을 적용한다. 필요한 변경을 기록한다.
- [ ] 큐브 적을 벽 앞뒤에 배치하고 양쪽 눈의 깊이·총구·투사체 가림을 확인한다.
- [ ] 충돌 구조와 실제 길이를 비교하고 72Hz에서 빈 맵·적 포함 프레임과 VRAM을 측정한다.
- [ ] GS가 G1을 통과하지 못하면 텍스처 메시 결과에 같은 시험을 적용한다.
- [ ] `Docs/scan-proof.md`에 비교 결과·선택 이유·남은 보수 구역을 기록하고 P02를 커밋한다.

**Done:** 외형·충돌·가림·기기 성능을 확인한 한 가지 경로가 선택된다. 기준은 T11·T16·T17이다.

## P03 플레이어 이동과 연속 회전

**Files:** Create `Runtime/Player/PlayerRigController.cs`, `Prefabs/PlayerRig.prefab`. Modify `Input/VRFPS.inputactions`, `Scenes/Combat.unity`, `Scenes/Tests/InteractionLab.unity`.

**Interfaces:** Consumes HMD pose, left `Vector2 Move`, right `Vector2 Turn`, bool locomotionEnabled. Produces 충돌 가능한 이동과 오른쪽 연속 회전. 후속 RunController가 이동 활성 상태를 제어한다.

- [ ] 왼쪽 스틱 이동과 오른쪽 스틱 회전 Action을 분리한다.
- [ ] HMD 전방을 바닥에 투영하고 CharacterController를 통해 이동한다. 머리를 숙여도 지면 아래로 이동하지 않게 한다.

```csharp
public static Vector3 GetPlanarMove(Vector3 headForward, Vector2 input) {
    var forward = Vector3.ProjectOnPlane(headForward, Vector3.up).normalized;
    if (forward.sqrMagnitude < 0.001f) return Vector3.zero;
    var right = Vector3.Cross(Vector3.up, forward);
    return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
}
// 이동량 = GetPlanarMove(...) * moveSpeed * deltaTime.
// 회전은 HMD의 바닥 투영 위치를 중심으로 yaw 입력 * turnSpeed * deltaTime.
```

- [ ] 오른쪽 스틱은 연속 회전 공급자 한 개만 사용한다. 스냅 공급자는 비활성화한다.
- [ ] HMD의 실제 수평 이동·숙임에 맞춰 캡슐 중심과 높이를 갱신하고 벽·바닥을 시험한다.
- [ ] 속도 설정을 06의 범위로 연결한다. 의도하지 않은 카메라 흔들림을 넣지 않는다.
- [ ] T03과 벽 밀착 시험을 실제 기기에서 통과하고 P03을 커밋한다.

**Done:** 이동·회전·고개 조작이 분리되고 벽을 통과하거나 바닥에서 떨어지지 않는다.

## P04 K2 양손 사격

**Files:** Create `Runtime/Weapons/FireGate.cs`, `Runtime/Weapons/K2Controller.cs`, `Runtime/Combat/IDamageable.cs`, `Runtime/Progression/RunPhase.cs`, `Tests/EditMode/FireGateTests.cs`, `Prefabs/Weapons/K2.prefab`. Modify `Prefabs/PlayerRig.prefab`.

**Interfaces:** Consumes 오른손 앵커·왼손 앞부분 잡기·오른손 트리거. Produces `FireGate.CanFire(bool,bool,int,RunPhase,bool)`, `K2Controller.SetLeftGrip(bool)`, `SetCombatEnabled(bool)`, `IDamageable.ApplyDamage(int,int,double)`.

- [ ] 다음 시험을 추가하고 EditMode에서 아직 구현이 없음을 확인한다. 같은 이름의 RunPhase는 03의 계약으로 먼저 정의한다.

```csharp
[Test]
public void ForegripReleaseBlocksFire() {
    Assert.That(FireGate.CanFire(false, true, 30, RunPhase.Active, false), Is.False);
    Assert.That(FireGate.CanFire(true, true, 30, RunPhase.Active, false), Is.True);
    Assert.That(FireGate.CanFire(true, true, 30, RunPhase.Paused, false), Is.False);
    Assert.That(FireGate.CanFire(true, true, 30, RunPhase.Active, true), Is.False);
}
```

- [ ] 허용 조건을 다음처럼 한 지점에서 결정한다. 모든 발사 요청이 이 함수를 통과하게 한다.

```csharp
public static bool CanFire(bool leftGrip, bool inserted, int rounds,
    RunPhase phase, bool muzzleBlocked) =>
    leftGrip && inserted && rounds > 0 && phase == RunPhase.Active && !muzzleBlocked;
```

- [ ] 임시 K2 모델에 주 잡기·보조 잡기·총구·탄창 앵커를 배치한다. 오른손에서 총이 떨어지는 XRI 선택 해제 동작은 연결하지 않는다.
- [ ] 오른손 위치를 유지하며 왼손 방향으로 조준하고, 유효 벡터가 거의 0일 때 이전 방향을 유지한다.
- [ ] 0.1초 발사 간격과 총구 Raycast를 연결한다. 오른손 기준점→총구 구간이 WorldSolid에 막히면 발사를 차단한다.
- [ ] 앞부분 해제 이벤트에서 즉시 연사를 끄고 재개도 FireGate로 판단한다.
- [ ] 후보 K2 모델의 조건과 분리 탄창을 검사하여 교체한다. 조건이 불명확하면 임시 모델을 유지하고 P05 구현을 진행한다.
- [ ] EditMode 시험과 T04·T05·T11을 통과하고 P04를 커밋한다.

**Done:** 한 손 발사, 정지·사망 상태 발사, 벽 너머 총구 발사가 모두 차단된다.

## P05 왼손 수동 재장전

**Files:** Create `Runtime/Weapons/MagazineState.cs`, `MagazineToken.cs`, `MagazineSocket.cs`, `AmmoPouch.cs`, `Runtime/Player/LeftHandInteractor.cs`, `Tests/EditMode/MagazineStateTests.cs`, `Prefabs/Weapons/Magazine.prefab`. Modify `K2Controller.cs`, `PlayerRig.prefab`.

**Interfaces:** Consumes 왼손 잡기·해제와 삽입 앵커. Produces MagazineState의 `Rounds`, `Inserted`, `Eject()`, `InsertFresh()`, `TryConsume()`와 Fresh/Inserted/Discarded 토큰. 탄창 제거량은 반환해도 재사용하지 않는다.

- [ ] 다음 잔탄 폐기·중복 삽입 시험을 추가하고 실패를 확인한다.

```csharp
[Test]
public void PartialMagazineCannotPreserveRounds() {
    var mag = new MagazineState(30);
    for (int i = 0; i < 13; i++) Assert.That(mag.TryConsume(), Is.True);
    Assert.That(mag.Eject(), Is.EqualTo(17));
    Assert.That(mag.Rounds, Is.Zero);
    Assert.That(mag.TryConsume(), Is.False);
    Assert.That(mag.InsertFresh(), Is.True);
    Assert.That(mag.Rounds, Is.EqualTo(30));
    Assert.That(mag.InsertFresh(), Is.False);
}
```

- [ ] 최소 탄약 규칙을 구현한다. 실물 토큰 검증은 Socket에서 먼저 수행한다.

```csharp
public sealed class MagazineState {
    readonly int capacity;
    public int Rounds { get; private set; }
    public bool Inserted { get; private set; }
    public MagazineState(int capacity) {
        if (capacity <= 0) throw new System.ArgumentOutOfRangeException(nameof(capacity));
        this.capacity = capacity; Rounds = capacity; Inserted = true;
    }
    public int Eject() {
        int discarded = Rounds; Rounds = 0; Inserted = false; return discarded;
    }
    public bool InsertFresh() {
        if (Inserted) return false;
        Inserted = true; Rounds = capacity; return true;
    }
    public bool TryConsume() {
        if (!Inserted || Rounds <= 0) return false;
        Rounds--; return true;
    }
}
```

- [ ] 왼손 소유 대상을 하나로 제한한다. 앞부분을 놓아야 장착 탄창을 잡을 수 있게 한다.
- [ ] 탈거 시 Eject를 호출하고 해당 실물 토큰을 Discarded로 바꾼다. 놓으면 바닥에 떨어지며 재삽입을 거부한다.
- [ ] 왼쪽 허리 공급에서 새 그립 입력 1회에 Fresh 토큰 1개를 준다. 잡고 있는 새 탄창을 놓쳐도 다시 공급받게 한다.
- [ ] 빈 Socket에서 Fresh·왼손 소유·거리 0.12m·각도 35도 조건을 모두 만족할 때 한 번 결합하고 InsertFresh를 호출한다.
- [ ] 삽입 뒤 왼손 소유권을 비우고 앞부분을 다시 잡기 전까지 발사를 차단한다. 장전 손잡이 단계는 넣지 않는다.
- [ ] T06·T07을 각 10회 반복한다. 잡고 있는 탄창은 수명 회수 대상에서 제외한다.
- [ ] 시험을 통과하고 P05를 커밋한다.

**Done:** 잔탄 교체, 빈 탄창, 새 탄창 분실, 잘못된 방향 삽입, 연속 재장전에서도 진행 불능이 없다.

## P06 근접과 원거리 좀비

**Files:** Create `Runtime/Enemies/ZombieController.cs`, `AcidProjectile.cs`, `Prefabs/Enemies/MeleeZombie.prefab`, `RangedZombie.prefab`, `Animators/Zombie.controller`, `Tests/PlayMode/EnemyAttackTests.cs`. Modify `InteractionLab.unity`.

**Interfaces:** Consumes NavMesh 경로, 플레이어 위치, `IDamageable`. Produces `ZombieController`의 이동·공격·사망 및 적 ID의 단일 사망 알림, `AcidProjectile`의 최초 충돌 피해. 아래 `IsMeleeHitValid`는 ZombieController의 순수 공간 검사 보조 메서드다.

- [ ] 벽 앞뒤에 플레이어 표적을 둔 시험에서 벽 뒤 피해가 발생하지 않는 조건을 먼저 만든다.
- [ ] 같은 기반 프리팹으로 두 적 유형을 만들고 이동 애니메이션은 in-place, 위치는 NavMeshAgent가 결정하게 한다.
- [ ] 근접은 준비 동작 완료 순간 거리와 시야를 다시 확인한다.

```csharp
public static bool IsMeleeHitValid(Vector3 origin, Vector3 target,
    float range, int solidMask) {
    Vector3 delta = target - origin;
    float distance = delta.magnitude;
    return distance <= range &&
        (distance < 0.001f || !Physics.Raycast(origin, delta / distance, distance, solidMask));
}
```

- [ ] 원거리는 유효 거리와 시야를 확보하고 0.8초 예고 후 현재 발사 방향으로 투사체를 쏜다. 발사 이후 자동 추적하지 않는다.
- [ ] 투사체는 이전→다음 위치 구간을 검사하고 최초 충돌만 처리한다. 월드에 부딪히면 플레이어 피해 없이 회수한다.
- [ ] 사망 상태는 공격·추가 피해를 차단하고 사망 알림을 한 번 보낸다. 근접·원거리의 외형 또는 공격 준비 표시를 구분한다.
- [ ] 길이 막힌 적의 5초 진행량을 확인하고 유효 지점으로 재배치한다. ID와 생존 수는 유지한다.
- [ ] T10·T11을 실행하고 필요한 애니메이션 출처를 기록한 뒤 P06을 커밋한다.

**Done:** 두 공격 모두 피하거나 가릴 수 있고 벽 뒤에서 피해가 발생하지 않는다.

## P07 웨이브와 체력

**Files:** Create `Runtime/Combat/VitalState.cs`, `Runtime/Progression/WaveProgress.cs`, `RunController.cs`, `SpawnDirector.cs`, `Runtime/Configuration/GameBalance.cs`, `Tests/EditMode/VitalStateTests.cs`, `WaveProgressTests.cs`, `Tests/PlayMode/RunLifecycleTests.cs`.

**Interfaces:** Consumes 적 생성 성공·사망 ID·피해·초기 수치. Produces 03의 VitalState, WaveProgress, RunController, SpawnDirector 계약. UI는 이 상태를 읽으며 내부 필드를 직접 변경하지 않는다.

- [ ] 다음 두 시험으로 조기 클리어와 회복 오류를 재현할 기준을 만든다.

```csharp
[Test]
public void PendingQuotaPreventsClearAndReachedWaveIsSix() {
    var wave = new WaveProgress();
    wave.Begin(6, 2);
    Assert.That(wave.ReachedWave, Is.EqualTo(6));
    Assert.That(wave.RegisterSpawn(10), Is.True);
    Assert.That(wave.ResolveEnemy(10, true), Is.True);
    Assert.That(wave.TryClear(), Is.False);
    Assert.That(wave.RegisterSpawn(11), Is.True);
    Assert.That(wave.ResolveEnemy(11, true), Is.True);
    Assert.That(wave.ResolveEnemy(11, true), Is.False);
    Assert.That(wave.TryClear(), Is.True);
    Assert.That(wave.TryClear(), Is.False);
    Assert.That(wave.ReachedWave, Is.EqualTo(6));
}
[Test]
public void DamageProtectionAndOneHealPerWave() {
    var hp = new VitalState(100, 20, 0.5);
    Assert.That(hp.Damage(30, 1.0), Is.True);
    Assert.That(hp.Damage(30, 1.1), Is.False);
    Assert.That(hp.HealForClear(5), Is.True);
    Assert.That(hp.HealForClear(5), Is.False);
    Assert.That(hp.Health, Is.EqualTo(90));
    Assert.That(hp.Damage(100, 2.0), Is.True);
    Assert.That(hp.HealForClear(6), Is.False);
    Assert.That(hp.Health, Is.Zero);
}
```

- [ ] 생존 ID는 `HashSet<int>`로 관리하고, 시작 시 quota를 Pending에 설정하여 성공한 생성만 1 감소시킨다. 아래 알고리즘을 해당 메서드에 적용한다.

```csharp
// WaveProgress 내부 필드와 핵심 처리
readonly System.Collections.Generic.HashSet<int> alive = new();
readonly System.Collections.Generic.HashSet<int> spawned = new();
bool clearAwarded;
public bool RegisterSpawn(int enemyId) {
    if (Pending <= 0 || !spawned.Add(enemyId)) return false;
    Pending--; alive.Add(enemyId); return true;
}
public bool ResolveEnemy(int enemyId, bool playerKill) {
    if (!alive.Remove(enemyId)) return false;
    if (playerKill) Kills++;
    return true;
}
public bool TryClear() {
    if (ReachedWave <= 0 || clearAwarded || Pending != 0 || alive.Count != 0) return false;
    clearAwarded = true; return true;
}
// Begin은 양수 wave·quota와 이전 웨이브 종료를 검사한다.
// ReachedWave=wave, Pending=quota, alive·spawned 초기화, clearAwarded=false.
// Kills는 Begin에서 초기화하지 않고 새 도전 생성에서만 0으로 시작한다.
```

- [ ] VitalState는 다음 처리 순서를 구현한다. `lastHealWave`는 0, `nextDamageTime`은 음의 무한대로 시작한다.

```csharp
public bool Damage(int amount, double activeTime) {
    if (amount <= 0 || Health <= 0 || activeTime < nextDamageTime) return false;
    Health = System.Math.Max(0, Health - amount);
    nextDamageTime = activeTime + protectionSeconds;
    return true;
}
public bool HealForClear(int wave) {
    if (Health <= 0 || wave <= lastHealWave) return false;
    Health = System.Math.Min(maxHealth, Health + healPerWave);
    lastHealWave = wave; return true;
}
```

- [ ] RunController는 피해와 사망을 먼저 확정하고 살아 있을 때만 클리어·회복·준비로 넘어가게 한다.
- [ ] SpawnDirector는 06의 총 수·유형 비율·동시 상한·유효 위치를 검사한다. 생성 실패는 Pending을 소비하지 않는다.
- [ ] 준비 종료 시 Begin을 호출한다. 준비 중에는 다음 웨이브 번호를 ReachedWave에 쓰지 않는다.
- [ ] 정지 시 전투 시간·생성·AI·투사체를 멈추고 머리·컨트롤러 추적은 유지한다.
- [ ] T08·T09·T12·T13을 통과하고 P07을 커밋한다.

**Done:** 두 적을 상대로 여러 웨이브를 진행하고 사망하면 회복으로 되살아나지 않는다.

## P08 오른팔 UI와 맵별 기록

**Files:** Create `Runtime/UI/ForearmHUD.cs`, `MenuController.cs`, `Runtime/Persistence/RecordStore.cs`, `BestRecord.cs`, `Tests/EditMode/RecordStoreTests.cs`, `Prefabs/UI/ForearmHUD.prefab`, `Prefabs/UI/Menus.prefab`. Modify `RunController.cs`, `Bootstrap.unity`.

**Interfaces:** Consumes Health, ReachedWave, Kills, MapDefinition.mapId. Produces `RecordStore.Read(string)`, `TryUpdate(string,int,int)`, `Save()`와 결과 메뉴. BestRecord는 같은 도전의 reachedWave·kills를 보존한다.

- [ ] 독립된 시험 임시 폴더에 RecordStore를 구성하고 아래 비교 규칙을 시험한다. 실제 사용자 기록 파일로 시험하지 않는다.

```csharp
public static bool IsBetter(BestRecord old, int reachedWave, int kills) =>
    reachedWave > old.reachedWave ||
    (reachedWave == old.reachedWave && kills > old.kills);
// 사례: 기존 (6,42)에 (5,100)은 거부, (6,43)은 채택, (7,1)은 채택.
// room을 갱신해도 classroom 기록은 그대로여야 한다.
```

- [ ] 03의 schemaVersion 1 형식을 구현하고 임시 파일→본 파일 교체·백업·오류 알림을 연결한다.
- [ ] 사망 시 `TryUpdate(mapId, wave.ReachedWave, wave.Kills)` 후 Save를 한 번 호출한다. 클리어 수로 결과를 만들지 않는다.
- [ ] 오른손 컨트롤러 기준 전완 패널을 배치하고 체력·웨이브·처치 수를 표시한다. ‘준비 중’에는 도달 0을 `wave 1`로 저장하지 않는다.
- [ ] 맵 선택·설정·정지·결과·같은 맵 재도전 버튼을 연결한다. 메뉴 입력과 사격 입력이 동시에 동작하지 않게 한다.
- [ ] 새 도전에서 체력100·탄창30·처치0·도달0·풀·이벤트 구독이 초기화되는지 확인한다.
- [ ] T13·T14·T15와 기록 파일 손상·쓰기 실패 시험을 통과하고 P08을 커밋한다.

**Done:** `wave 6` 규칙과 맵별 최고 기록이 앱 재실행 후에도 유지되고 팔 UI가 읽힌다.

## P09 방과 강의실 완성

**Files:** Create `Runtime/Maps/MapDefinition.cs`, `MapRoot.cs`, `Configuration/Maps/Room.asset`, `Classroom.asset`, `Scenes/Maps/Room.unity`, `Classroom.unity`, `Art/Scans/Room/`, `Classroom/`. Modify `MenuController.cs`, `Combat.unity`.

**Interfaces:** Consumes G1의 외형 경로와 공통 전투. Produces 03의 MapDefinition·MapRoot 필드, `room`·`classroom` 맵 ID. 두 맵은 같은 진행·저장 코드를 사용한다.

- [ ] 05 절차로 두 공간을 촬영하고 실제 거리·원본·가공본을 기록한다.
- [ ] 외형·충돌 구조·NavMesh·PlayerSpawn·EnemySpawns를 각각 배치한다.
- [ ] 각 생성점에 대해 NavMesh 위 여부와 시작점까지 `NavMeshPathStatus.PathComplete`를 검사한다. 실패 지점은 생성 후보에서 제외하고 씬에서 수정한다.
- [ ] 맵별 최대 생존·원거리 수와 최소 생성 거리를 06대로 시작한다. 좁은 방의 실제 회피 가능성을 시험한다.
- [ ] 메뉴에서 Combat과 선택 맵을 로드하고 이전 맵 언로드 시 풀·이벤트·참조를 해제한다.
- [ ] T02·T11·T16을 각 맵에서 실행하고 맵 전환 10회 동안 이전 적과 기록 참조가 섞이지 않는지 확인한다.
- [ ] 두 맵의 기준 이미지와 보수 내역을 저장하고 P09를 커밋한다.

**Done:** 두 맵에서 별도 규칙 코드를 복제하지 않고 전체 생존 흐름을 플레이할 수 있다.

## P10 안내와 전투 피드백

**Files:** Create `Runtime/Feedback/CombatFeedback.cs`, `UI/InstructionPanel.prefab`, `Audio/AudioMixer.mixer`. Modify `K2Controller.cs`, `MagazineSocket.cs`, `ZombieController.cs`, `ForearmHUD.cs`.

**Interfaces:** Consumes 발사·삽입·피격·공격 예고·웨이브 시작 사건. Produces 짧은 소리·진동·효과, 02의 R19 조작 안내. 사건별 중복 재생을 방지한다.

- [ ] 첫 안내 문구를 ‘왼쪽 스틱 이동 / 오른쪽 스틱 회전 / 왼손 앞부분을 잡고 오른손 트리거 발사 / 왼손으로 탄창 교체 / 오른팔에서 체력 확인’으로 구성한다.
- [ ] 총성·빈 총·삽입·피격·웨이브 시작·좀비 접근과 공격 준비 소리를 연결한다.
- [ ] 진동은 설치한 XRI 버전의 햅틱 API로 연결하고 강도를 기기에서 조정한다. 카메라 반동을 넣지 않는다.
- [ ] 체력 부족 신호를 색·아이콘·짧은 소리로 보완한다. 지속적인 붉은 화면으로 시야를 가리지 않는다.
- [ ] 처음 보는 사람이 안내만으로 1회 발사와 재장전을 할 수 있는지 관찰하고 T19에 기록한다.
- [ ] 에셋 출처를 갱신하고 P10을 커밋한다.

**Done:** 핵심 조작과 공격 예고를 알아볼 수 있고 총·탄창·팔 UI를 가리지 않는다.

## P11 성능과 안정성

**Files:** Create `Runtime/Pooling/PoolRegistry.cs`, `Tests/PlayMode/PoolLifecycleTests.cs`, `Docs/performance.md`, `Docs/test-results.md`. Modify 적·탄창·투사체·효과 생성·회수 코드와 맵 설정.

**Interfaces:** Consumes P10까지 완성된 전투. Produces 제한된 풀과 현재·최대 객체 수 통계. `PoolRegistry.TryRent(string poolId,out GameObject instance)`는 빈 슬롯이 없으면 false, `Return(string poolId,GameObject instance)`는 중복 반환을 무시하고 상태를 초기화한다.

- [ ] 반복 생성·반환 후 활성 개수가 상한을 넘지 않고 같은 객체가 이중 반환되지 않는 PlayMode 시험을 만든다.
- [ ] 풀 상한을 적=맵 maxAlive, 원거리 투사체12, 바닥 탄창8, 사체4, 효과16으로 시작한다. 손에 들린 탄창과 삽입 탄창은 바닥 탄창 수명 회수에서 제외한다.
- [ ] Return에서 속도·타이머·타격 이력·이벤트 구독·애니메이터 상태를 초기화한다.
- [ ] 각 맵 30분 동안 일반 전투와 재장전을 반복해 객체 수·VRAM·프레임 변화를 기록한다.
- [ ] 08의 빈 맵·최대 적·효과·탄창 조합을 고정된 해상도·72Hz에서 측정한다.
- [ ] CPU 병목이면 AI 갱신·생성·GC를, GPU 병목이면 스캔·투명 효과·텍스처·그림자를 줄이고 같은 조건을 재측정한다.
- [ ] 치명 결함을 수정한 뒤 관련 규칙 시험과 실패했던 시나리오만 다시 수행한다. 변경 없이 전체 시험을 반복하지 않는다.
- [ ] T17을 포함해 필수 시험 결과를 기록하고 P11을 커밋한다.

**Done:** 끝없이 진행해도 객체 수가 제한되고 시연 환경의 목표 프레임과 조작 기준을 만족한다. 미달 항목을 통과로 기록하지 않는다.

## P12 제출과 재현

**Files:** Create 프로젝트 `README.md`, `Docs/asset-credits.md`, `Docs/build-and-run.md`, `Docs/demo-script.md`, `Docs/final-report.md`, 최종 발표자료와 `Builds/Windows/` 실행 파일. Modify 최신 버전·시험·출처 기록.

**Interfaces:** Consumes P11 통과 빌드, 원본 출처·측정 기록. Produces 09의 제출 묶음과 시연·보고서. 이 작업만으로 파일을 외부에 전송하지 않는다.

- [ ] 최신 수업 공지에 맞춰 파일 형식·용량·제출일을 확정한다.
- [ ] 두 맵을 Build Profile에 포함하고 개발용 무적·웨이브 점프·강제 사망·로그 UI를 최종 빌드에서 끈다.
- [ ] 새 폴더에 빌드를 복사해 실행하고 기록이 없는 첫 실행·재실행을 확인한다.
- [ ] 소스 묶음에서 Library·Temp·캐시를 제외하고 Assets·meta·Packages·ProjectSettings와 재현 안내를 포함한다.
- [ ] 09의 5분 시연을 녹화하고 양손 사격·재장전·원거리 엄폐·팔 UI·두 맵·기록을 보여 준다.
- [ ] 실제 구현한 것과 한계·측정 결과를 보고서에 쓰고, 사용한 에셋·앱·렌더러의 출처를 정리한다.
- [ ] T18·T20을 확인하고 최종 커밋·빌드 식별자를 문서에 기록한다.

**Done:** 다른 사람이 안내에 따라 프로젝트를 열고 실행하며, 발표·보고서의 주장을 실제 빌드와 시험 기록으로 확인할 수 있다.

## 3 구현 인수 체크

- [ ] R01~R20 각각에 실행 작업과 시험 결과가 존재한다.
- [ ] 재장전 손, UI 팔, 회전 방식, 도달 웨이브의 정의가 사용자 결정과 같다.
- [ ] 무료 후보를 취득·임포트·검증 완료로 잘못 표시하지 않았다.
- [ ] GS 기술 선택과 실제 기기 성능 결과가 기록되어 있다.
- [ ] 필수 기능 변경은 사용자 결정으로 남겼고 일정 단축만을 위해 조용히 삭제하지 않았다.
