using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
    public class PoolLifecycleTests
    {
        GameObject root,template;
        PoolRegistry registry;
        [SetUp] public void Setup()
        {
            root=new GameObject("Pool test");template=new GameObject("Template");template.SetActive(false);
            registry=root.AddComponent<PoolRegistry>();
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);Object.DestroyImmediate(template);}
        [Test] public void BoundedReuseIgnoresForeignAndDuplicateReturnsAndResetsBeforeReuse()
        {
            var resets=0;
            registry.Register("test",template,2,item=>{resets++;item.name="Reset";});
            Assert.That(registry.TryRent("test",out var first),Is.True);
            Assert.That(registry.TryRent("test",out var second),Is.True);
            Assert.That(registry.TryRent("test",out _),Is.False);
            first.SetActive(true);first.transform.position=Vector3.one*23;
            registry.Return("unknown",first);registry.Return("test",template);
            Assert.That(registry.GetCounts("test").Current,Is.EqualTo(2));
            registry.Return("test",first);registry.Return("test",first);
            Assert.That(resets,Is.EqualTo(1));Assert.That(first.activeSelf,Is.False);
            Assert.That(first.transform.localPosition,Is.EqualTo(Vector3.zero));
            for(var i=0;i<1000;i++)
            {
                Assert.That(registry.TryRent("test",out var reused),Is.True);
                Assert.That(reused,Is.SameAs(first));Assert.That(reused.name,Is.EqualTo("Reset"));
                registry.Return("test",reused);
            }
            registry.Return("test",second);
            var counts=registry.GetCounts("test");
            Assert.That(counts.Current,Is.Zero);Assert.That(counts.Peak,Is.EqualTo(2));
            Assert.That(counts.Created,Is.EqualTo(2));Assert.That(counts.Capacity,Is.EqualTo(2));
        }
        [Test] public void ProjectilePoolRegistersCapacityAndReturnsExpiredAndRestartedShots()
        {
            var projectile=template.AddComponent<AcidProjectile>();var pool=root.AddComponent<AcidProjectilePool>();
            pool.Configure(projectile,1<<8,1<<9);
            for(var cycle=0;cycle<20;cycle++)
            {
                for(var i=0;i<12;i++)Assert.That(pool.TryLaunch(new Vector3(12000,100,0),Vector3.forward,1,15),Is.True);
                Assert.That(pool.TryLaunch(Vector3.zero,Vector3.forward,1,15),Is.False);
                Assert.That(registry.GetCounts("projectiles").Current,Is.EqualTo(12));
                if(cycle%2==0)pool.Tick(4.1f,4.1);else pool.RetireAll();
                Assert.That(pool.ActiveCount,Is.Zero);
                Assert.That(registry.GetCounts("projectiles").Current,Is.Zero);
            }
            Assert.That(registry.GetCounts("projectiles").Created,Is.EqualTo(12));
            Assert.That(registry.GetCounts("projectiles").Peak,Is.EqualTo(12));
        }
        [Test] public void InvalidLaunchReturnsLeaseAndDestroyedShotsDoNotAccumulateInPool()
        {
            var pool=root.AddComponent<AcidProjectilePool>();pool.Configure(template.AddComponent<AcidProjectile>(),1<<8,1<<9);
            Assert.Throws<System.ArgumentException>(()=>pool.TryLaunch(Vector3.zero,Vector3.zero,1,15));
            Assert.That(registry.GetCounts("projectiles").Current,Is.Zero);
            for(var i=0;i<20;i++)
            {
                Assert.That(pool.TryLaunch(Vector3.one*12000,Vector3.forward,1,15),Is.True);
                var shot=root.GetComponentInChildren<AcidProjectile>();Assert.That(shot,Is.Not.Null);
                Object.DestroyImmediate(shot.gameObject);
            }
            Assert.That(pool.TryLaunch(Vector3.one*12000,Vector3.forward,1,15),Is.True);
            Assert.That(pool.CreatedCount,Is.EqualTo(1));Assert.That(pool.ActiveCount,Is.EqualTo(1));
        }
        [Test] public void ReturningObjectCannotBeRentedInsideResetAndFailedResetIsQuarantined()
        {
            var shouldThrow=true;
            registry.Register("test",template,1,item=>
            {
                Assert.That(registry.TryRent("test",out _),Is.False);
                registry.Return("test",item);
                if(shouldThrow)throw new System.InvalidOperationException("Reset failure");
            });
            Assert.That(registry.TryRent("test",out var first),Is.True);
            Assert.Throws<System.InvalidOperationException>(()=>registry.Return("test",first));
            Assert.That(registry.GetCounts("test").Current,Is.Zero);
            shouldThrow=false;
            Assert.That(registry.TryRent("test",out var replacement),Is.True);
            Assert.That(replacement,Is.Not.SameAs(first));registry.Return("test",replacement);
            Assert.That(registry.GetCounts("test").Current,Is.Zero);
        }
        [Test] public void AdoptedSceneObjectKeepsItsOwnerAndConsumesExactlyOneSlot()
        {
            registry.Register("a",template,1,item=>{});registry.Register("b",template,1,item=>{});
            var initial=new GameObject("Scene initial");initial.transform.SetParent(root.transform,false);
            Assert.That(registry.Adopt("a",initial),Is.True);
            Assert.That(initial.activeSelf,Is.True);Assert.That(initial.transform.parent,Is.EqualTo(root.transform));
            Assert.That(registry.Adopt("a",initial),Is.False);Assert.That(registry.Adopt("b",initial),Is.False);
            Assert.That(registry.TryRent("a",out _),Is.False);
            registry.Return("a",initial);
            Assert.That(registry.TryRent("a",out var reused),Is.True);Assert.That(reused,Is.SameAs(initial));
            Assert.That(registry.GetCounts("a").Created,Is.EqualTo(1));
        }
    }
}
