using UnityEngine;
using VRFPS.Player;
namespace VRFPS
{
    public sealed class RunCombatBindings : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] K2Controller gun;
        [SerializeField] PlayerRigController movement;
        [SerializeField] AmmoPouch pouch;
        RunPhase previousPhase = RunPhase.Dead;

        public void Configure(RunController run, K2Controller gun, PlayerRigController movement, AmmoPouch pouch)
        {
            Unsubscribe();
            this.run = run; this.gun = gun; this.movement = movement; this.pouch = pouch;
            if (isActiveAndEnabled) Subscribe();
        }

        void OnEnable() => Subscribe();
        void Subscribe()
        {
            if (run == null) return;
            run.PhaseChanged += ApplyPhase;
            ApplyPhase(run.Phase);
        }
        void Unsubscribe() { if (run != null) run.PhaseChanged -= ApplyPhase; }
        void OnDisable()
        {
            Unsubscribe();
            ApplyPhase(RunPhase.Paused);
        }
        void ApplyPhase(RunPhase phase)
        {
            var simulationEnabled = phase == RunPhase.Active || phase == RunPhase.Preparing;
            if (gun != null)
            {
                if (phase == RunPhase.Paused || phase == RunPhase.Dead ||
                    previousPhase == RunPhase.Paused || previousPhase == RunPhase.Dead)
                    gun.GetComponent<K2HandInput>()?.RequireTriggerRelease();
                gun.SetPhase(phase);
            }
            previousPhase = phase;
            if (movement != null) movement.SetLocomotionEnabled(simulationEnabled);
            if (pouch != null) pouch.SetSimulationEnabled(simulationEnabled);
        }
    }
}
