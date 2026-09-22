namespace VRFPS
{
    public static class FireGate
    {
        public static bool CanFire(bool leftGrip, bool inserted, int rounds, RunPhase phase, bool muzzleBlocked) =>
            leftGrip && inserted && rounds > 0 && phase == RunPhase.Active && !muzzleBlocked;
    }
}
