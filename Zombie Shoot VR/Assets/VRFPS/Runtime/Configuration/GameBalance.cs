namespace VRFPS
{
    public static class GameBalance
    {
        public const int MaxHealth = 100, HealPerClear = 20;
        public const double ProtectionSeconds = 0.5, PreparationSeconds = 8;

        public static int QuotaForWave(int wave)
        {
            if (wave <= 0) throw new System.ArgumentOutOfRangeException(nameof(wave));
            return (int)System.Math.Min(60, 6L + (wave - 1L) * 2);
        }
    }
}
