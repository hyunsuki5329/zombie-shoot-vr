using System;
using NUnit.Framework;
using UnityEngine;

namespace VRFPS.Tests
{
    public class IntroVirusTests
    {
        GameObject root;
        IntroVirusTarget target;
        bool preview;
        int hits;
        [SetUp] public void Setup()
        {
            root=new GameObject("Test virus");target=root.AddComponent<IntroVirusTarget>();
            preview=true;hits=0;target.Configure(42,()=>preview);target.Triggered+=()=>hits++;
        }
        [TearDown] public void Cleanup()=>UnityEngine.Object.DestroyImmediate(root);
        [Test] public void FirstValidHitIsConsumedBeforeReentrantEvent()
        {
            target.Triggered+=()=>Assert.That(target.ApplyDamage(25,42,0),Is.False);
            Assert.That(target.ApplyDamage(25,42,0),Is.True);
            Assert.That(target.ApplyDamage(25,42,0),Is.False);Assert.That(hits,Is.EqualTo(1));
        }
        [Test] public void WrongSourceNonpositiveDamageAndPausedPreviewDoNotConsume()
        {
            Assert.That(target.ApplyDamage(25,99,0),Is.False);
            Assert.That(target.ApplyDamage(0,42,0),Is.False);
            Assert.That(target.ApplyDamage(-1,42,0),Is.False);
            preview=false;Assert.That(target.ApplyDamage(25,42,0),Is.False);
            preview=true;Assert.That(target.ApplyDamage(25,42,0),Is.True);
            Assert.That(hits,Is.EqualTo(1));
        }
        [Test] public void InvalidClockAndDisabledTargetDoNotConsume()
        {
            Assert.That(target.ApplyDamage(25,42,double.NaN),Is.False);
            Assert.That(target.ApplyDamage(25,42,-1),Is.False);
            target.enabled=false;Assert.That(target.ApplyDamage(25,42,0),Is.False);
        }
        [Test] public void NewVisitResetsConsumptionWithoutStartingRun()
        {
            var run=root.AddComponent<RunController>();var starts=0;run.RunStarted+=()=>starts++;
            target.ApplyDamage(25,42,0);target.ResetTarget();
            Assert.That(target.ApplyDamage(25,42,0),Is.True);
            Assert.That(hits,Is.EqualTo(2));Assert.That(starts,Is.Zero);
            Assert.That(run.Kills,Is.Zero);Assert.That(run.ReachedWave,Is.Zero);
        }
        [Test] public void MissingPredicateIsRejected()=>Assert.Throws<ArgumentNullException>(()=>target.Configure(42,null));
    }
}
