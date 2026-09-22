namespace VRFPS
{
    public sealed class K2FireControl
    {
        readonly MagazineState magazine;
        double nextShotTime;
        public const double ShotInterval = 0.1;

        public K2FireControl(MagazineState magazine)
        {
            this.magazine = magazine ?? throw new System.ArgumentNullException(nameof(magazine));
        }

        // activeTime excludes pauses. A new run owns a new controller and magazine.
        // No queued shots: every request rechecks current grip, phase, obstruction and ammo.
        public bool TryFire(bool triggerHeld, bool leftGrip, RunPhase phase, bool muzzleBlocked, double activeTime)
        {
            if (double.IsNaN(activeTime) || double.IsInfinity(activeTime) || activeTime < 0) return false;
            if (!triggerHeld || !FireGate.CanFire(leftGrip, magazine.Inserted, magazine.Rounds, phase, muzzleBlocked)) return false;
            if (activeTime + 1e-9 < nextShotTime) return false;
            if (!magazine.TryConsume()) return false;
            nextShotTime = activeTime + ShotInterval;
            return true;
        }
    }
}
