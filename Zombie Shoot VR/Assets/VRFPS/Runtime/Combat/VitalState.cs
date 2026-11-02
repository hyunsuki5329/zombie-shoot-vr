using System;
namespace VRFPS
{
    public sealed class VitalState
    {
        readonly int maxHealth, healPerWave;
        readonly double protectionSeconds;
        double nextDamageTime = double.NegativeInfinity;
        int lastHealWave;
        public int Health { get; private set; }
        public VitalState(int maxHealth, int healPerWave, double protectionSeconds)
        {
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (healPerWave < 0) throw new ArgumentOutOfRangeException(nameof(healPerWave));
            if (protectionSeconds < 0 || double.IsNaN(protectionSeconds) || double.IsInfinity(protectionSeconds))
                throw new ArgumentOutOfRangeException(nameof(protectionSeconds));
            this.maxHealth = maxHealth; this.healPerWave = healPerWave; this.protectionSeconds = protectionSeconds;
            Health = maxHealth;
        }
        public bool Damage(int amount, double activeTime)
        {
            if (double.IsNaN(activeTime) || double.IsInfinity(activeTime)) throw new ArgumentOutOfRangeException(nameof(activeTime));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0 || Health == 0 || activeTime < nextDamageTime) return false;
            Health = Math.Max(0, Health - amount);
            nextDamageTime = activeTime + protectionSeconds;
            return true;
        }
        public bool HealForClear(int wave)
        {
            if (Health == 0 || wave <= lastHealWave) return false;
            Health = (int)Math.Min(maxHealth, (long)Health + healPerWave);
            lastHealWave = wave;
            return true;
        }
    }
}
