using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VRFPS.Tests
{
    // Represents the real weapon's default-order LateUpdate damage callback.
    public sealed class LastEnemyAndPlayerDeath : MonoBehaviour
    {
        public RunController Run;
        void LateUpdate()
        {
            Run.ResolveEnemy(6, true);
            Run.ApplyDamage(100, 99, 999999);
            enabled = false;
        }
    }

    public class RunLifecycleTests
    {
        GameObject root;
        MapDefinition map;
        RunController run;

        [SetUp] public void Setup()
        {
            root = new GameObject("Run lifecycle fixture");
            run = root.AddComponent<RunController>();
            run.enabled = false;
            map = ScriptableObject.CreateInstance<MapDefinition>();
            map.mapId = "room"; map.sceneName = "Room";
            map.maxAlive = 4; map.maxRangedAlive = 1; map.minSpawnDistance = 2;
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(map);
        }
        void BeginFirstWave()
        {
            run.StartRun(map); run.Tick(8);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Active));
        }
        void ResolveFirstWaveExceptLast()
        {
            for (var id = 1; id <= 6; id++)
            {
                Assert.That(run.RegisterSpawn(id), Is.True);
                if (id < 6) Assert.That(run.ResolveEnemy(id, true), Is.True);
            }
        }

        [Test] public void PreparationDoesNotRecordNextWaveAndPausePreservesRemainingTime()
        {
            run.StartRun(map); run.Tick(3);
            Assert.That(run.ReachedWave, Is.Zero);
            Assert.That(run.PreparationRemaining, Is.EqualTo(5).Within(0.0001));
            Assert.That(run.RegisterSpawn(1), Is.False);
            Assert.That(run.ApplyDamage(10, 9, 0), Is.False);
            run.SetPaused(true); run.Tick(100);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Paused));
            Assert.That(run.PreparationRemaining, Is.EqualTo(5).Within(0.0001));
            run.SetPaused(false); run.Tick(4.9f);
            Assert.That(run.ReachedWave, Is.Zero);
            run.Tick(0.11f);
            Assert.That(run.ReachedWave, Is.EqualTo(1));
            Assert.That(run.Pending, Is.EqualTo(6));
            Assert.That(run.ActiveTime, Is.Zero);
        }

        [Test] public void ClearHealsOnceThenWaitsEightSecondsBeforeRecordingNextWave()
        {
            BeginFirstWave();
            run.ApplyDamage(30, 9, 0);
            ResolveFirstWaveExceptLast();
            run.Tick(1);
            Assert.That(run.Health, Is.EqualTo(70));
            run.ResolveEnemy(6, true);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Active), "Death callbacks must not award clear immediately.");
            run.Tick(0);
            Assert.That(run.Health, Is.EqualTo(90));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Preparing));
            Assert.That(run.ReachedWave, Is.EqualTo(1));
            Assert.That(run.Kills, Is.EqualTo(6));
            run.Tick(7.9f);
            Assert.That(run.Health, Is.EqualTo(90));
            Assert.That(run.ReachedWave, Is.EqualTo(1));
            run.Tick(0.11f);
            Assert.That(run.ReachedWave, Is.EqualTo(2));
            Assert.That(run.Pending, Is.EqualTo(8));
        }

        [UnityTest] public IEnumerator LateFrameLethalHitWinsOverLastKillAndEndsExactlyOnce()
        {
            BeginFirstWave(); ResolveFirstWaveExceptLast();
            var ended = 0; run.RunEnded += () => ended++;
            root.AddComponent<LastEnemyAndPlayerDeath>().Run = run;
            run.enabled = true;
            yield return null;
            yield return null;
            Assert.That(run.Health, Is.Zero);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Dead));
            Assert.That(run.ReachedWave, Is.EqualTo(1));
            Assert.That(run.Kills, Is.EqualTo(6));
            run.EndRun(); run.SetPaused(false); run.Tick(20);
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(run.RegisterSpawn(100), Is.False);
        }

        [Test] public void CombatPauseRejectsDamageAndDoesNotExpireProtection()
        {
            BeginFirstWave();
            Assert.That(run.ApplyDamage(10, 9, 1000), Is.True);
            run.Tick(0.25f); run.SetPaused(true); run.Tick(60);
            Assert.That(run.ApplyDamage(10, 9, 10000), Is.False);
            Assert.That(run.ActiveTime, Is.EqualTo(0.25));
            run.SetPaused(false);
            Assert.That(run.ApplyDamage(10, 9, 20000), Is.False);
            run.Tick(0.25f);
            Assert.That(run.ApplyDamage(10, 9, 0), Is.True);
            Assert.That(run.Health, Is.EqualTo(80));
        }

        [Test] public void RestartResetsHealthCountersClockAndPauseState()
        {
            BeginFirstWave();
            run.RegisterSpawn(10); run.ResolveEnemy(10, true);
            run.ApplyDamage(60, 9, 0); run.Tick(2); run.SetPaused(true);
            var starts = 0; run.RunStarted += () => starts++;
            run.StartRun(map);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(run.Health, Is.EqualTo(100));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Preparing));
            Assert.That(run.ActiveTime, Is.Zero);
            Assert.That(run.ReachedWave, Is.Zero);
            Assert.That(run.Kills, Is.Zero);
            Assert.That(run.Alive, Is.Zero);
            Assert.That(run.Pending, Is.Zero);
            Assert.That(run.PreparationRemaining, Is.EqualTo(8));
            run.SetPaused(false);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Preparing));
        }

        [Test] public void InvalidMapAndDeltaDoNotDestroyAnExistingRun()
        {
            BeginFirstWave(); run.ApplyDamage(10, 9, 0);
            map.maxRangedAlive = 5;
            Assert.Throws<System.ArgumentException>(() => run.StartRun(map));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => run.Tick(float.NaN));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => run.Tick(-1));
            Assert.That(run.Health, Is.EqualTo(90));
            Assert.That(run.ReachedWave, Is.EqualTo(1));
        }

        [Test] public void LongRunQuotaCapsWithoutIntegerOverflow()
        {
            Assert.That(GameBalance.QuotaForWave(1), Is.EqualTo(6));
            Assert.That(GameBalance.QuotaForWave(2), Is.EqualTo(8));
            Assert.That(GameBalance.QuotaForWave(28), Is.EqualTo(60));
            Assert.That(GameBalance.QuotaForWave(int.MaxValue), Is.EqualTo(60));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => GameBalance.QuotaForWave(0));
        }
    }
}
