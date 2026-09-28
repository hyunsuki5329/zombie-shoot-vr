using System;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
namespace VRFPS.Tests
{
    public class ProjectileAllocationTests
    {
        [Test] public void SaturatedOverlapBufferCannotHideWallBehindPlayerColliders()
        {
            var root=new GameObject("Dense overlap");var shot=new GameObject("Projectile");
            try
            {
                root.transform.position=new Vector3(12000,100,0);
                var receiver=root.AddComponent<EnemyDamageProbe>();
                for(var i=0;i<16;i++)
                {
                    var body=new GameObject("Player collider");body.transform.SetParent(root.transform,false);body.layer=9;body.AddComponent<BoxCollider>();
                }
                var wall=new GameObject("Wall");wall.transform.SetParent(root.transform,false);wall.layer=8;wall.AddComponent<BoxCollider>();
                Physics.SyncTransforms();var projectile=shot.AddComponent<AcidProjectile>();
                projectile.Launch(root.transform.position,Vector3.forward,1,10,1<<8,1<<9);projectile.Tick(.01f,0);
                Assert.That(projectile.Flying,Is.False);Assert.That(receiver.Hits,Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(shot);}
        }
        static int Allocations(Action action)
        {
            using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"GC.Alloc",4096,
                ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                action();recorder.Stop();
                Assert.That(recorder.Valid,Is.True,"GC allocation recording is unavailable.");
                Assert.That(recorder.Count,Is.LessThan(4096),"Allocation recording capacity exhausted.");
                return recorder.Count;
            }
        }
        [Test] public void AllocationCounterDetectsAKnownManagedAllocation()
        {
            var measured=Allocations(()=>GC.KeepAlive(new byte[8192]));
            TestContext.Out.WriteLine("Known 8192-byte array allocation events: "+measured);
            Assert.That(measured,Is.GreaterThan(0),"Calibrate the recorder before interpreting zero allocation.");
        }
        [Test] public void RepeatedPlayerImpactsDoNotAllocateOverlapArrays()
        {
            var root=new GameObject("Impact allocation fixture");var target=new GameObject("Player target");
            try
            {
                var origin=new Vector3(12000,100,0);target.transform.position=origin;target.layer=9;
                target.AddComponent<BoxCollider>();var receiver=target.AddComponent<EnemyDamageProbe>();Physics.SyncTransforms();
                var projectile=root.AddComponent<AcidProjectile>();
                for(var i=0;i<100;i++){projectile.Launch(origin,Vector3.forward,1,10,1<<8,1<<9);projectile.Tick(.001f,0);}
                var allocated=Allocations(()=>{for(var i=0;i<1000;i++){projectile.Launch(origin,Vector3.forward,1,10,1<<8,1<<9);projectile.Tick(.001f,0);}});
                TestContext.Out.WriteLine("Repeated impact allocation events: "+allocated);
                Assert.That(receiver.Hits,Is.EqualTo(1100));
                Assert.That(allocated,Is.Zero,"Repeated impact/reuse must not allocate overlap arrays.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(target);}
        }
        [Test] public void EmptyFlightDoesNotAllocateCollisionArraysEveryTick()
        {
            var root=new GameObject("Projectile allocation fixture");
            try
            {
                var projectile=root.AddComponent<AcidProjectile>();
                projectile.Launch(new Vector3(12000,100,0),Vector3.forward,1,10,1<<8,1<<9);
                for(var i=0;i<100;i++)projectile.Tick(.0001f,0);
                var allocated=Allocations(()=>{for(var i=0;i<1000;i++)projectile.Tick(.0001f,0);});
                TestContext.Out.WriteLine("Empty flight allocation events: "+allocated);
                Assert.That(projectile.Flying,Is.True);
                Assert.That(allocated,Is.Zero,"Steady flight must not allocate a fresh overlap array each tick.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
