using System;
using System.Collections.Generic;
namespace VRFPS
{
    public sealed class WaveProgress
    {
        readonly HashSet<int> alive = new HashSet<int>();
        readonly HashSet<int> spawned = new HashSet<int>();
        bool clearAwarded;
        public int ReachedWave { get; private set; }
        public int Pending { get; private set; }
        public int Alive => alive.Count;
        public int Kills { get; private set; }
        public void Begin(int wave, int quota)
        {
            if (wave <= ReachedWave) throw new ArgumentOutOfRangeException(nameof(wave));
            if (quota <= 0) throw new ArgumentOutOfRangeException(nameof(quota));
            if (ReachedWave > 0 && !clearAwarded) throw new InvalidOperationException("Previous wave has not cleared.");
            ReachedWave = wave; Pending = quota;
            alive.Clear(); spawned.Clear(); clearAwarded = false;
        }
        public bool RegisterSpawn(int enemyId)
        {
            if (Pending <= 0 || !spawned.Add(enemyId)) return false;
            Pending--; alive.Add(enemyId); return true;
        }
        public bool ResolveEnemy(int enemyId, bool playerKill)
        {
            if (!alive.Remove(enemyId)) return false;
            if (playerKill && Kills < int.MaxValue) Kills++;
            return true;
        }
        public bool TryClear()
        {
            if (ReachedWave == 0 || clearAwarded || Pending != 0 || Alive != 0) return false;
            clearAwarded = true; return true;
        }
    }
}
