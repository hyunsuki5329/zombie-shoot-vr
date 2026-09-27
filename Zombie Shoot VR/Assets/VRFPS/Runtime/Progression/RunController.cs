using System;
using UnityEngine;
namespace VRFPS
{
    // Weapon input resolves in default-order LateUpdate. Clear/heal runs afterwards,
    // once all this frame's damage callbacks have had a chance to end the run.
    [DefaultExecutionOrder(10000)]
    public sealed class RunController : MonoBehaviour, IDamageable
    {
        VitalState vital;
        WaveProgress wave;
        RunPhase resumePhase;
        public RunPhase Phase { get; private set; } = RunPhase.Dead;
        public double ActiveTime { get; private set; }
        public double PreparationRemaining { get; private set; }
        public MapDefinition Map { get; private set; }
        public int Health => vital?.Health ?? 0;
        public int ReachedWave => wave?.ReachedWave ?? 0;
        public int Kills => wave?.Kills ?? 0;
        public int Pending => wave?.Pending ?? 0;
        public int Alive => wave?.Alive ?? 0;
        public event Action RunStarted;
        public event Action RunEnded;
        public event Action<RunPhase> PhaseChanged;
        public event Action<int> WaveStarted;
        public event Action<int,int> DamageTaken;

        public void StartRun(MapDefinition map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.Validate();
            Map = map;
            vital = new VitalState(GameBalance.MaxHealth, GameBalance.HealPerClear, GameBalance.ProtectionSeconds);
            wave = new WaveProgress();
            ActiveTime = 0;
            PreparationRemaining = GameBalance.PreparationSeconds;
            resumePhase = RunPhase.Preparing;
            Phase = RunPhase.Preparing;
            RunStarted?.Invoke();
            PhaseChanged?.Invoke(Phase);
        }

        public void SetPaused(bool paused)
        {
            if (Phase == RunPhase.Dead) return;
            if (paused && Phase != RunPhase.Paused)
            {
                resumePhase = Phase;
                ChangePhase(RunPhase.Paused);
            }
            else if (!paused && Phase == RunPhase.Paused) ChangePhase(resumePhase);
        }

        public void EndRun()
        {
            if (Phase == RunPhase.Dead) return;
            ChangePhase(RunPhase.Dead);
            RunEnded?.Invoke();
        }

        void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (Phase == RunPhase.Paused || Phase == RunPhase.Dead) return;
            if (Health == 0) { EndRun(); return; }
            if (Phase == RunPhase.Preparing)
            {
                PreparationRemaining = Math.Max(0, PreparationRemaining - deltaTime);
                if (PreparationRemaining == 0)
                {
                    var next = checked(ReachedWave + 1);
                    wave.Begin(next, GameBalance.QuotaForWave(next));
                    ChangePhase(RunPhase.Active);
                    WaveStarted?.Invoke(next);
                }
                return;
            }
            ActiveTime += deltaTime;
            if (!wave.TryClear()) return;
            vital.HealForClear(ReachedWave);
            PreparationRemaining = GameBalance.PreparationSeconds;
            ChangePhase(RunPhase.Preparing);
        }

        public bool RegisterSpawn(int enemyId) => Phase == RunPhase.Active && wave.RegisterSpawn(enemyId);
        public bool ResolveEnemy(int enemyId, bool playerKill) => Phase == RunPhase.Active && wave.ResolveEnemy(enemyId, playerKill);

        public bool ApplyDamage(int amount, int sourceId, double time)
        {
            // Callers can have local clocks; invulnerability belongs to this run's
            // paused combat clock, not to a projectile or weapon's timestamp.
            var before=Health;
            if (Phase != RunPhase.Active || !vital.Damage(amount, ActiveTime)) return false;
            DamageTaken?.Invoke(before,Health);
            if (Health == 0) EndRun();
            return true;
        }

        void ChangePhase(RunPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            PhaseChanged?.Invoke(Phase);
        }
    }
}
