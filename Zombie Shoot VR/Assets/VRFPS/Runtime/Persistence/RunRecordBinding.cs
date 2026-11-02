using System;
using UnityEngine;
namespace VRFPS
{
    public sealed class RunRecordBinding : MonoBehaviour
    {
        RunController run;
        RecordStore records;
        string resultMapId;
        public bool HasResult { get; private set; }
        public BestRecord Result { get; private set; }
        public bool SaveSucceeded { get; private set; }
        public string LastError { get; private set; }
        public event Action ResultChanged;

        public void Configure(RunController run, RecordStore records)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (records == null) throw new ArgumentNullException(nameof(records));
            Unsubscribe();
            this.run = run; this.records = records;
            ClearResult();
            if (isActiveAndEnabled) Subscribe();
        }
        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();
        void Subscribe()
        {
            if (run == null || records == null) return;
            run.RunStarted += ClearResult; run.RunEnded += CaptureResult;
        }
        void Unsubscribe()
        {
            if (run == null) return;
            run.RunStarted -= ClearResult; run.RunEnded -= CaptureResult;
        }
        void ClearResult()
        {
            HasResult = SaveSucceeded = false;
            Result = default; resultMapId = LastError = null;
        }
        void CaptureResult()
        {
            if (HasResult) return;
            resultMapId = run.Map.mapId;
            Result = new BestRecord(run.ReachedWave, run.Kills);
            HasResult = true;
            RetrySave();
        }
        public bool RetrySave()
        {
            if (!HasResult) return false;
            if (SaveSucceeded) return true;
            try
            {
                records.TryUpdate(resultMapId, Result.reachedWave, Result.kills);
                SaveSucceeded = records.Save();
                LastError = records.LastError;
            }
            catch (ArgumentException error)
            {
                // A misconfigured map must not prevent the death/result transition.
                SaveSucceeded = false;
                LastError = "Record save failed: " + error.Message;
            }
            ResultChanged?.Invoke();
            return SaveSucceeded;
        }
    }
}
