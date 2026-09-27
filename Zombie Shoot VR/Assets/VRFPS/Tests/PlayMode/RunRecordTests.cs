using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRFPS.Tests
{
    public class RunRecordTests
    {
        GameObject root;
        RunController run;
        RunRecordBinding binding;
        MapDefinition map;
        string directory;
        [SetUp] public void Setup()
        {
            directory = Path.GetFullPath(Path.Combine("Temp", "VRFPS-RunRecordTests", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory);
            root = new GameObject("Run record fixture");
            run = root.AddComponent<RunController>(); run.enabled = false;
            binding = root.AddComponent<RunRecordBinding>();
            map = ScriptableObject.CreateInstance<MapDefinition>();
            map.mapId = "room"; map.sceneName = "Room";
            map.maxAlive = 4; map.maxRangedAlive = 1; map.minSpawnDistance = 2;
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(root); Object.DestroyImmediate(map);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        void ClearFirstFiveWaves()
        {
            run.StartRun(map); run.Tick(8);
            var id = 0;
            for (var wave = 1; wave <= 5; wave++)
            {
                var quota = 4 + wave * 2;
                for (var i = 0; i < quota; i++)
                { run.RegisterSpawn(++id); run.ResolveEnemy(id, true); }
                run.Tick(0);
                if (wave < 5) run.Tick(8);
            }
        }

        [TestCase(false, 5)] [TestCase(true, 6)]
        public void ResultUsesActuallyStartedWaveAndPersistsExactlyOnce(bool startSix, int expectedWave)
        {
            binding.Configure(run, new RecordStore(directory));
            var changed = 0; binding.ResultChanged += () => changed++;
            ClearFirstFiveWaves();
            if (startSix) { run.Tick(8); run.ApplyDamage(100, 99, run.ActiveTime); }
            else run.EndRun();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Dead));
            Assert.That(binding.HasResult, Is.True);
            Assert.That(binding.Result.reachedWave, Is.EqualTo(expectedWave));
            Assert.That(binding.Result.kills, Is.EqualTo(50));
            Assert.That(binding.SaveSucceeded, Is.True);
            var saved = new RecordStore(directory).Read("room");
            Assert.That(saved.reachedWave, Is.EqualTo(expectedWave));
            Assert.That(saved.kills, Is.EqualTo(50));
            run.EndRun(); binding.RetrySave();
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(directory, "records-v1.json.bak")), Is.False, "Repeated end/retry must not write again after success.");
        }

        [Test] public void FailedSaveStillPublishesDeathResultAndCanRetryWithoutEndingRunAgain()
        {
            var blocked = Path.Combine(directory, "blocked");
            File.WriteAllText(blocked, "fixture");
            binding.Configure(run, new RecordStore(blocked));
            var changed = 0; binding.ResultChanged += () => changed++;
            run.StartRun(map); run.Tick(8); run.ApplyDamage(100, 99, 0);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Dead));
            Assert.That(binding.HasResult, Is.True);
            Assert.That(binding.Result.reachedWave, Is.EqualTo(1));
            Assert.That(binding.SaveSucceeded, Is.False);
            Assert.That(binding.LastError, Is.Not.Null.And.Not.Empty);
            Assert.That(changed, Is.EqualTo(1));
            File.Delete(blocked);
            Assert.That(binding.RetrySave(), Is.True);
            Assert.That(binding.LastError, Is.Null);
            Assert.That(changed, Is.EqualTo(2));
            Assert.That(new RecordStore(blocked).Read("room").reachedWave, Is.EqualTo(1));
        }

        [Test] public void RestartAndMapSwitchKeepSeparateRecordsAndOnlyCurrentResult()
        {
            binding.Configure(run, new RecordStore(directory));
            ClearFirstFiveWaves(); run.Tick(8); run.EndRun();
            var previousResult = binding.Result;
            map.mapId = "classroom"; map.sceneName = "Classroom";
            run.StartRun(map);
            Assert.That(binding.HasResult, Is.False);
            Assert.That(binding.RetrySave(), Is.False);
            run.Tick(8); run.EndRun();
            Assert.That(binding.Result.reachedWave, Is.EqualTo(1));
            Assert.That(binding.Result.kills, Is.Zero);
            Assert.That(previousResult.reachedWave, Is.EqualTo(6));
            var store = new RecordStore(directory);
            Assert.That(store.Read("room").reachedWave, Is.EqualTo(6));
            Assert.That(store.Read("classroom").reachedWave, Is.EqualTo(1));
        }

        [Test] public void ReconfigureDoesNotLeaveAnOldRunSubscription()
        {
            var old = root.AddComponent<RunController>(); old.enabled = false;
            var store = new RecordStore(directory);
            binding.Configure(old, store); binding.Configure(run, store);
            old.StartRun(map); old.Tick(8); old.EndRun();
            Assert.That(binding.HasResult, Is.False);
            Assert.That(File.Exists(Path.Combine(directory, "records-v1.json")), Is.False);
            run.StartRun(map); run.Tick(8); run.EndRun();
            Assert.That(binding.HasResult, Is.True);
        }
    }
}
