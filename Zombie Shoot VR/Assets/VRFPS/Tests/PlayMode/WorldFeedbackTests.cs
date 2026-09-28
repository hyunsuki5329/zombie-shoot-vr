using System;
using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
    public class WorldFeedbackTests
    {
        [Test] public void WaveResumeAndRejectedDamageDoNotDuplicateCuesAndLowHealthUsesThresholdCrossing()
        {
            var root=new GameObject("Run feedback");var map=ScriptableObject.CreateInstance<MapDefinition>();
            try
            {
                var run=root.AddComponent<RunController>();run.enabled=false;
                var type=typeof(RunController).Assembly.GetType("VRFPS.RunFeedback");
                Assert.That(type,Is.Not.Null,"Run feedback is missing.");
                var feedback=root.AddComponent(type);type.GetMethod("Configure").Invoke(feedback,new object[]{run});
                Func<string,int> count=name=>(int)type.GetProperty(name).GetValue(feedback);
                map.mapId="room";map.sceneName="Room";map.maxAlive=4;map.maxRangedAlive=1;map.minSpawnDistance=2;
                run.StartRun(map);Assert.That(count("WaveCues"),Is.Zero);
                run.Tick(8);Assert.That(count("WaveCues"),Is.EqualTo(1));
                run.SetPaused(true);run.SetPaused(false);Assert.That(count("WaveCues"),Is.EqualTo(1));
                Assert.That(run.ApplyDamage(75,1,0),Is.True);Assert.That(count("DamageCues"),Is.EqualTo(1));
                Assert.That(count("LowHealthCues"),Is.EqualTo(1));
                Assert.That(run.ApplyDamage(1,2,0),Is.False);Assert.That(count("DamageCues"),Is.EqualTo(1));
                run.Tick(.6f);run.ApplyDamage(1,2,0);Assert.That(count("DamageCues"),Is.EqualTo(2));
                Assert.That(count("LowHealthCues"),Is.EqualTo(1));
                run.StartRun(map);run.Tick(8);run.ApplyDamage(75,1,0);
                Assert.That(count("WaveCues"),Is.EqualTo(2));Assert.That(count("LowHealthCues"),Is.EqualTo(2));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(map);}
        }
        [Test] public void EnemyWindupSoundsOnceAndRebindingDoesNotMultiplyListeners()
        {
            var root=new GameObject("Enemy feedback");var target=new GameObject("Target");
            try
            {
                root.transform.position=new Vector3(9000,0,0);target.transform.position=root.transform.position+Vector3.forward;
                var enemy=root.AddComponent<ZombieController>();
                var type=typeof(ZombieController).Assembly.GetType("VRFPS.EnemyFeedback");
                Assert.That(type,Is.Not.Null,"Enemy warning feedback is missing.");
                var feedback=(Behaviour)root.AddComponent(type);
                var bind=type.GetMethod("Configure");
                enemy.Configure(1,EnemyKind.Melee,target.transform,root.transform,1<<8);
                bind.Invoke(feedback,new object[]{enemy,target.transform});bind.Invoke(feedback,new object[]{enemy,target.transform});
                Func<int> count=()=>(int)type.GetProperty("WarningCues").GetValue(feedback);
                enemy.Tick(0,0);Assert.That(count(),Is.EqualTo(1));
                enemy.Tick(.1f,.1);Assert.That(count(),Is.EqualTo(1));
                feedback.enabled=false;enemy.Configure(2,EnemyKind.Melee,target.transform,root.transform,1<<8);enemy.Tick(0,0);
                Assert.That(count(),Is.EqualTo(1));
                feedback.enabled=true;enemy.Configure(3,EnemyKind.Melee,target.transform,root.transform,1<<8);
                bind.Invoke(feedback,new object[]{enemy,target.transform});enemy.Tick(0,0);Assert.That(count(),Is.EqualTo(2));
                target.transform.position=root.transform.position+Vector3.forward*4;
                enemy.Configure(4,EnemyKind.Melee,target.transform,root.transform,1<<8);bind.Invoke(feedback,new object[]{enemy,target.transform});
                var tick=type.GetMethod("Tick");var enabled=type.GetMethod("SetCombatEnabled");
                Func<int> approaches=()=>(int)type.GetProperty("ApproachCues").GetValue(feedback);
                tick.Invoke(feedback,new object[]{0f});tick.Invoke(feedback,new object[]{1f});Assert.That(approaches(),Is.EqualTo(1));
                tick.Invoke(feedback,new object[]{2f});Assert.That(approaches(),Is.EqualTo(2));
                enabled.Invoke(feedback,new object[]{false});tick.Invoke(feedback,new object[]{30f});Assert.That(approaches(),Is.EqualTo(2));
                enabled.Invoke(feedback,new object[]{true});tick.Invoke(feedback,new object[]{0f});Assert.That(approaches(),Is.EqualTo(2));
                enemy.ApplyDamage(10000,1,0);tick.Invoke(feedback,new object[]{10f});Assert.That(approaches(),Is.EqualTo(2));
                Assert.That(root.GetComponentsInChildren<AudioSource>(true).Length,Is.EqualTo(2));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(target);}
        }
    }
}
