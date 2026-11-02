namespace VRFPS
{
    public enum EnemyVariant { Normal, Crawler, Boss }
    public static class GameBalance
    {
        public const int MaxHealth = 100, HealPerClear = 20;
        public const double ProtectionSeconds = 0.5, PreparationSeconds = 8;
        public static int RangedQuotaForWave(int wave)
        {
            var quota = QuotaForWave(wave);
            if (wave < 3) return 0;
            // Integer percentages avoid floating-point flooring/ceiling at exact boundaries.
            var percent = System.Math.Min(30L, 10L + (wave - 3L) * 2);
            return (int)System.Math.Max(1, quota * percent / 100);
        }
        public static EnemyVariant MeleeVariantForWave(int wave, int index)
        {
            if(wave<=0 || index<0) throw new System.ArgumentOutOfRangeException();
            if(wave%5==0 && index==0)return EnemyVariant.Boss;
            return wave>5 && index%3==0 ? EnemyVariant.Crawler : EnemyVariant.Normal;
        }
        public static float EnemySpeed(EnemyKind kind, EnemyVariant variant) => variant==EnemyVariant.Crawler?2.2f:variant==EnemyVariant.Boss?.9f:kind==EnemyKind.Melee?1.1f:.85f;
        public static float AttackWindup(EnemyKind kind,EnemyVariant variant) => variant==EnemyVariant.Boss?1f:variant==EnemyVariant.Crawler?.45f:kind==EnemyKind.Melee?.65f:.8f;
        public static int EnemyHealth(EnemyKind kind, int wave, EnemyVariant variant=EnemyVariant.Normal) =>
            Scaled(variant==EnemyVariant.Boss?500:variant==EnemyVariant.Crawler?70:kind == EnemyKind.Melee ? 100 : 80, wave, 5, 200);
        public static int EnemyDamage(EnemyKind kind, int wave, EnemyVariant variant=EnemyVariant.Normal) =>
            Scaled(variant==EnemyVariant.Boss?25:variant==EnemyVariant.Crawler?8:kind == EnemyKind.Melee ? 10 : 15, wave, 2, 150);
        static int Scaled(int initial, int wave, int increment, int cap)
        {
            if (wave <= 0) throw new System.ArgumentOutOfRangeException(nameof(wave));
            var percent = System.Math.Min(cap, 100L + (wave - 1L) * increment);
            return (int)((initial * percent + 99) / 100);
        }

        public static int QuotaForWave(int wave)
        {
            if (wave <= 0) throw new System.ArgumentOutOfRangeException(nameof(wave));
            return (int)System.Math.Min(60, 6L + (wave - 1L) * 2);
        }
    }
}
