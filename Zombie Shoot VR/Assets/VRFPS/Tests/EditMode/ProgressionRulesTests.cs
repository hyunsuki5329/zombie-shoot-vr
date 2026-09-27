using System;
using NUnit.Framework;
namespace VRFPS.Tests
{
    public class ProgressionRulesTests
    {
        [Test] public void DamageProtectionAndOneHealPerWave()
        {
            var hp = new VitalState(100, 20, 0.5);
            Assert.That(hp.Health, Is.EqualTo(100));
            Assert.That(hp.Damage(30, 1), Is.True);
            Assert.That(hp.Damage(30, 1.1), Is.False);
            Assert.That(hp.HealForClear(5), Is.True);
            Assert.That(hp.HealForClear(5), Is.False);
            Assert.That(hp.HealForClear(4), Is.False);
            Assert.That(hp.Health, Is.EqualTo(90));
            Assert.That(hp.Damage(100, 2), Is.True);
            Assert.That(hp.HealForClear(6), Is.False);
            Assert.That(hp.Damage(1, 3), Is.False);
            Assert.That(hp.Health, Is.Zero);
        }
        [Test] public void ProtectionExpiresAtExactBoundaryAndHealingCannotExceedMaximum()
        {
            var hp = new VitalState(100, int.MaxValue, 0.5);
            hp.Damage(10, 0);
            Assert.That(hp.Damage(10, 0.4999), Is.False);
            Assert.That(hp.Damage(10, 0.5), Is.True);
            Assert.That(hp.HealForClear(1), Is.True);
            Assert.That(hp.Health, Is.EqualTo(100));
            Assert.That(hp.Damage(0, 1), Is.False);
        }
        [Test] public void InvalidHealthConfigurationAndNonfiniteTimeAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VitalState(0, 20, 0.5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VitalState(100, -1, 0.5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VitalState(100, 20, double.NaN));
            var hp = new VitalState(100, 20, 0.5);
            Assert.Throws<ArgumentOutOfRangeException>(() => hp.Damage(1, double.NaN));
            Assert.That(hp.Health, Is.EqualTo(100));
        }
        [Test] public void PendingQuotaPreventsClearAndOnlyStartedWaveIsRecorded()
        {
            var wave = new WaveProgress();
            Assert.That(wave.ReachedWave, Is.Zero); Assert.That(wave.TryClear(), Is.False);
            wave.Begin(5, 2);
            Assert.That(wave.RegisterSpawn(10), Is.True);
            Assert.That(wave.ResolveEnemy(10, true), Is.True);
            Assert.That(wave.TryClear(), Is.False);
            Assert.That(wave.RegisterSpawn(11), Is.True);
            Assert.That(wave.ResolveEnemy(11, true), Is.True);
            Assert.That(wave.ResolveEnemy(11, true), Is.False);
            Assert.That(wave.TryClear(), Is.True);
            Assert.That(wave.TryClear(), Is.False);
            Assert.That(wave.ReachedWave, Is.EqualTo(5));
            wave.Begin(6, 1);
            Assert.That(wave.ReachedWave, Is.EqualTo(6));
            Assert.That(wave.Kills, Is.EqualTo(2));
        }
        [Test] public void DuplicateSpawnsAndUnknownDeathsDoNotConsumeQuotaOrAwardKills()
        {
            var wave = new WaveProgress(); wave.Begin(1, 2);
            Assert.That(wave.ResolveEnemy(999, true), Is.False);
            wave.RegisterSpawn(7);
            Assert.That(wave.RegisterSpawn(7), Is.False);
            Assert.That(wave.Pending, Is.EqualTo(1)); Assert.That(wave.Alive, Is.EqualTo(1));
            wave.ResolveEnemy(7, false);
            Assert.That(wave.RegisterSpawn(7), Is.False);
            Assert.That(wave.Kills, Is.Zero);
            wave.RegisterSpawn(8);
            Assert.That(wave.RegisterSpawn(9), Is.False);
            Assert.That(wave.TryClear(), Is.False);
            wave.ResolveEnemy(8, true);
            Assert.That(wave.TryClear(), Is.True); Assert.That(wave.Kills, Is.EqualTo(1));
        }
        [Test] public void BeginCannotDiscardLiveOrPendingEnemiesOrRepeatWave()
        {
            var wave = new WaveProgress();
            Assert.Throws<ArgumentOutOfRangeException>(() => wave.Begin(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => wave.Begin(1, 0));
            wave.Begin(1, 1);
            Assert.Throws<InvalidOperationException>(() => wave.Begin(2, 1));
            wave.RegisterSpawn(1);
            Assert.Throws<InvalidOperationException>(() => wave.Begin(2, 1));
            wave.ResolveEnemy(1, true);
            Assert.Throws<InvalidOperationException>(() => wave.Begin(2, 1));
            wave.TryClear();
            Assert.Throws<ArgumentOutOfRangeException>(() => wave.Begin(1, 1));
            wave.Begin(2, 1);
            Assert.That(wave.Pending, Is.EqualTo(1));
        }
    }
}
