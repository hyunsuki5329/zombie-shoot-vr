using System;
using NUnit.Framework;

namespace VRFPS.Tests
{
    public class WeaponRulesTests
    {
        [Test]
        public void ForegripReleaseBlocksFire()
        {
            Assert.That(FireGate.CanFire(true, true, 30, RunPhase.Active, false), Is.True);
            Assert.That(FireGate.CanFire(false, true, 30, RunPhase.Active, false), Is.False);
            Assert.That(FireGate.CanFire(true, true, 30, RunPhase.Active, true), Is.False);
        }

        [TestCase(RunPhase.Preparing)]
        [TestCase(RunPhase.Paused)]
        [TestCase(RunPhase.Dead)]
        public void NonCombatPhaseAlwaysBlocksFire(RunPhase phase)
        {
            Assert.That(FireGate.CanFire(true, true, 30, phase, false), Is.False);
        }

        [TestCase(false, 30)]
        [TestCase(true, 0)]
        [TestCase(true, -1)]
        public void MissingOrEmptyMagazineBlocksFire(bool inserted, int rounds)
        {
            Assert.That(FireGate.CanFire(true, inserted, rounds, RunPhase.Active, false), Is.False);
        }

        [Test]
        public void PartialMagazineDiscardsRemainingRoundsAndRejectsDuplicateInsertion()
        {
            var magazine = new MagazineState(30);
            for (var i = 0; i < 13; i++) Assert.That(magazine.TryConsume(), Is.True);
            Assert.That(magazine.Eject(), Is.EqualTo(17));
            Assert.That(magazine.Rounds, Is.Zero);
            Assert.That(magazine.Inserted, Is.False);
            Assert.That(magazine.TryConsume(), Is.False);
            Assert.That(magazine.Eject(), Is.Zero);
            Assert.That(magazine.InsertFresh(), Is.True);
            Assert.That(magazine.Rounds, Is.EqualTo(30));
            Assert.That(magazine.InsertFresh(), Is.False);
        }

        [Test]
        public void EmptyMagazineStaysInsertedAndNeverGoesNegative()
        {
            var magazine = new MagazineState(2);
            Assert.That(magazine.TryConsume(), Is.True);
            Assert.That(magazine.TryConsume(), Is.True);
            Assert.That(magazine.TryConsume(), Is.False);
            Assert.That(magazine.Inserted, Is.True);
            Assert.That(magazine.Rounds, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidCapacityIsRejected(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MagazineState(capacity));
        }

        [Test]
        public void AutomaticFireHonorsCadenceAndNeverCatchesUpWithBurst()
        {
            var magazine = new MagazineState(30);
            var fire = new K2FireControl(magazine);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 0), Is.True);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 0.099), Is.False);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 0.1), Is.True);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 5), Is.True);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 5), Is.False);
            Assert.That(magazine.Rounds, Is.EqualTo(27));
        }

        [Test]
        public void GripReleasePauseBlockedMuzzleAndTriggerReleaseSpendNoAmmo()
        {
            var magazine = new MagazineState(30);
            var fire = new K2FireControl(magazine);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 0), Is.True);
            Assert.That(fire.TryFire(true, false, RunPhase.Active, false, 1), Is.False);
            Assert.That(fire.TryFire(true, true, RunPhase.Paused, false, 1), Is.False);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, true, 1), Is.False);
            Assert.That(fire.TryFire(false, true, RunPhase.Active, false, 1), Is.False);
            Assert.That(magazine.Rounds, Is.EqualTo(29));
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 1), Is.True);
        }

        [Test]
        public void ReloadUsesSameMagazineAndStillNeedsForegrip()
        {
            var magazine = new MagazineState(1);
            var fire = new K2FireControl(magazine);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 0), Is.True);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 1), Is.False);
            magazine.Eject();
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 1), Is.False);
            magazine.InsertFresh();
            Assert.That(fire.TryFire(true, false, RunPhase.Active, false, 1), Is.False);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 1), Is.True);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-1d)]
        public void InvalidClockCannotSpendAmmo(double time)
        {
            var magazine = new MagazineState(30);
            var fire = new K2FireControl(magazine);
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, time), Is.False);
            Assert.That(magazine.Rounds, Is.EqualTo(30));
            Assert.That(fire.TryFire(true, true, RunPhase.Active, false, 0), Is.True);
        }
    }
}
