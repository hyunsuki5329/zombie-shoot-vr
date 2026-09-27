using System;
using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
    public class WeaponFeedbackTests
    {
        [Test] public void OnlySuccessfulManualInsertionEmitsCueAndRestartMountIsSilent()
        {
            var root=new GameObject("Insertion feedback");
            try
            {
                var gun=root.AddComponent<K2Controller>();
                var socket=root.AddComponent<MagazineSocket>();
                var hand=new GameObject("Left hand").transform;hand.SetParent(root.transform);
                var initial=new GameObject("Initial").AddComponent<MagazineToken>();initial.transform.SetParent(root.transform);
                var fresh=new GameObject("Fresh").AddComponent<MagazineToken>();fresh.transform.SetParent(root.transform);
                var feedback=root.AddComponent<CombatFeedback>();feedback.Configure(gun,socket);
                socket.Initialize(gun.Magazine,hand,initial);Assert.That(feedback.CueCount,Is.Zero);
                socket.TryEject(hand);Assert.That(fresh.TryGrab(hand),Is.True);
                Assert.That(socket.TryInsert(fresh),Is.True);Assert.That(feedback.CueCount,Is.EqualTo(1));
                Assert.That(socket.TryInsert(fresh),Is.False);Assert.That(feedback.CueCount,Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<AudioSource>(true).Length,Is.EqualTo(3));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void SuccessfulShotsAndEmptyTriggerEdgesProduceOneCueAndDisabledFeedbackUnsubscribes()
        {
            var root=new GameObject("Feedback test");
            var clip=AudioClip.Create("Test cue",441,1,44100,false);
            try
            {
                var right=new GameObject("Right").transform;right.SetParent(root.transform);
                var left=new GameObject("Left").transform;left.SetParent(root.transform);left.localPosition=Vector3.forward*.3f;
                var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(root.transform);muzzle.localPosition=Vector3.forward*.7f;
                var gun=root.AddComponent<K2Controller>();gun.Configure(right,left,muzzle,1<<8,(1<<8)|(1<<9));
                var type=typeof(K2Controller).Assembly.GetType("VRFPS.CombatFeedback");
                Assert.That(type,Is.Not.Null,"Weapon audio and haptic feedback is not implemented.");
                var feedback=(Behaviour)root.AddComponent(type);
                foreach(var name in new[]{"shotClip","emptyClip","insertClip"})type.GetField(name).SetValue(feedback,clip);
                type.GetMethod("Configure").Invoke(feedback,new object[]{gun,null});
                Func<int> count=()=> (int)type.GetProperty("CueCount").GetValue(feedback);
                gun.SetPhase(RunPhase.Active);gun.SetLeftGrip(true);
                Assert.That(gun.Tick(true,0),Is.True);Assert.That(count(),Is.EqualTo(1));
                Assert.That(gun.Tick(true,.001),Is.False);Assert.That(count(),Is.EqualTo(1));
                while(gun.Magazine.TryConsume()){}
                gun.Tick(true,1);Assert.That(count(),Is.EqualTo(1),"Holding trigger must not repeat an empty click.");
                gun.Tick(false,2);gun.Tick(true,3);Assert.That(count(),Is.EqualTo(2));
                gun.Tick(true,4);Assert.That(count(),Is.EqualTo(2));
                gun.SetPhase(RunPhase.Paused);gun.Tick(false,5);gun.Tick(true,6);Assert.That(count(),Is.EqualTo(2));
                gun.SetPhase(RunPhase.Active);gun.SetLeftGrip(true);feedback.enabled=false;
                gun.Tick(false,7);gun.Tick(true,8);Assert.That(count(),Is.EqualTo(2));
                feedback.enabled=true;type.GetMethod("Configure").Invoke(feedback,new object[]{gun,null});
                gun.Tick(false,9);gun.Tick(true,10);Assert.That(count(),Is.EqualTo(3),"Rebinding must not duplicate listeners.");
                Assert.That(root.GetComponentsInChildren<AudioSource>(true).Length,Is.EqualTo(3));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);}
        }
    }
}
