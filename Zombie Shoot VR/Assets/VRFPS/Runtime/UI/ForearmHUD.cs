using UnityEngine;
namespace VRFPS
{
    [DefaultExecutionOrder(11000)]
    public sealed class ForearmHUD : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] TextMesh health;
        [SerializeField] TextMesh progress;
        [SerializeField] GameObject warning;
        int shownHealth = -1, shownWave = -1, shownKills = -1, shownSeconds = -1;
        RunPhase? shownPhase;

        public void Configure(RunController run, TextMesh health, TextMesh progress, GameObject warning)
        {
            this.run = run; this.health = health; this.progress = progress; this.warning = warning;
            shownHealth = shownWave = shownKills = shownSeconds = -1; shownPhase = null;
            Refresh();
        }
        void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (run == null || health == null || progress == null || warning == null) return;
            if (shownHealth != run.Health)
            {
                shownHealth = run.Health;
                health.text = $"HP {shownHealth} / {GameBalance.MaxHealth}";
                var low = shownHealth <= 25;
                health.color = low ? Color.red : Color.white;
                warning.SetActive(low);
            }
            var seconds = Mathf.CeilToInt((float)run.PreparationRemaining);
            if (shownWave == run.ReachedWave && shownKills == run.Kills &&
                shownPhase == run.Phase && shownSeconds == seconds) return;
            shownWave = run.ReachedWave; shownKills = run.Kills;
            shownPhase = run.Phase; shownSeconds = seconds;
            var status = run.Phase == RunPhase.Preparing ? $"PREPARE {seconds}s" : run.Phase.ToString().ToUpperInvariant();
            progress.text = $"WAVE {shownWave}   KILLS {shownKills}\n{status}";
        }
    }
}
