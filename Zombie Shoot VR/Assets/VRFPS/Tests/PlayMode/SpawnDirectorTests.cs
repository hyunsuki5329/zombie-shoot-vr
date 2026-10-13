#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;

namespace VRFPS.Tests
{
    public class SpawnDirectorTests
    {
        GameObject root;
        NavMeshSurface surface;
        MapDefinition map;
        RunController run;
        SpawnDirector director;
        Transform head, player;
        readonly Vector3 offset = new Vector3(4000, 0, 0);
        const int PlayerSource = 55;
        GameObject Child(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform); return go;
        }
        [TestCase("Walk")][TestCase("Attack")]
        public void CrawlerDeathMovesFromCurrentPosePausesAndResets(string state)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/Enemies/CrawlerZombie.prefab");
            var actor=Object.Instantiate(prefab,offset+Vector3.forward*3,Quaternion.identity,root.transform).GetComponent<EnemyActor>();
            actor.Initialize(999,head,null,new Vector3[0],LayerMask.GetMask("WorldSolid"),6);
            var animator=actor.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.Update(0);animator.SetFloat("Speed",state=="Walk"?1:0);animator.SetBool("Attack",state=="Attack");
            animator.Play("Base Layer."+state,0,.3f);animator.Update(0);
            var ground=actor.GetComponent<GroundedEnemyVisual>();ground.AlignPose();
            var motion=actor.GetComponent<CrawlerDeathMotion>();
            var skull=animator.GetBoneTransform(HumanBodyBones.Head);var before=skull.position;
            var actorPosition=actor.transform.position;
            actor.Controller.ApplyDamage(10000,PlayerSource,0);motion.ApplyPose();ground.AlignPose();
            Assert.That(motion.Playing,Is.True);Assert.That(Vector3.Distance(skull.position,before),Is.LessThan(.001f),"Lethal hit must not snap the pose");
            foreach(var hit in actor.GetComponentsInChildren<Collider>())Assert.That(hit.enabled,Is.False);
            actor.SetCombatEnabled(false);actor.Tick(3,3);motion.ApplyPose();ground.AlignPose();
            Assert.That(motion.Elapsed,Is.Zero);Assert.That(Vector3.Distance(skull.position,before),Is.LessThan(.001f));
            actor.SetCombatEnabled(true);actor.Tick(.5f,3.5);motion.ApplyPose();ground.AlignPose();
            Assert.That(Vector3.Distance(skull.position,before),Is.GreaterThan(.03f),"Death must visibly move");
            actor.Tick(.6f,4.1);motion.ApplyPose();ground.AlignPose();
            Assert.That(motion.Elapsed,Is.EqualTo(CrawlerDeathMotion.Duration));
            Assert.That(skull.position.y,Is.GreaterThan(.06f).And.LessThan(.65f));
            var settled=skull.position;actor.Tick(.5f,4.6);motion.ApplyPose();ground.AlignPose();
            Assert.That(Vector3.Distance(skull.position,settled),Is.LessThan(.001f));
            Assert.That(actor.transform.position,Is.EqualTo(actorPosition));
            actor.Tick(3.5f,8.1);Assert.That(actor.gameObject.activeSelf,Is.False);
            actor.Initialize(1000,head,null,new Vector3[0],LayerMask.GetMask("WorldSolid"),6);
            Assert.That(motion.Playing,Is.False);Assert.That(animator.enabled,Is.True);Assert.That(actor.Controller.IsDead,Is.False);
        }
        [SetUp] public void Setup()
        {
            root = new GameObject("Spawn integration fixture");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform); floor.layer = LayerMask.NameToLayer("WorldSolid");
            floor.transform.position = offset + Vector3.down * 0.1f;
            floor.transform.localScale = new Vector3(20, 0.2f, 20);
            surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.layerMask = LayerMask.GetMask("WorldSolid");
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            player = Child("Player").transform; player.position = offset;
            var body = player.gameObject.AddComponent<CharacterController>();
            body.height = 1.7f; body.radius = 0.2f; body.center = Vector3.up * 0.85f;
            head = Child("Head").transform; head.SetParent(player); head.localPosition = Vector3.up * 1.7f;
            run = player.gameObject.AddComponent<RunController>(); run.enabled = false;
            map = ScriptableObject.CreateInstance<MapDefinition>();
            map.mapId = "room"; map.sceneName = "Room";
            map.maxAlive = 4; map.maxRangedAlive = 1; map.minSpawnDistance = 2;
            director = Child("Director").AddComponent<SpawnDirector>(); director.enabled = false;
            var folder = "Assets/VRFPS/Prefabs/Enemies/";
            director.Configure(run, head,
                AssetDatabase.LoadAssetAtPath<GameObject>(folder + "MeleeZombie.prefab").GetComponent<EnemyActor>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(folder + "RangedZombie.prefab").GetComponent<EnemyActor>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(folder + "AcidProjectile.prefab").GetComponent<AcidProjectile>(),
                new[] { offset + new Vector3(-4, 0, 4), offset + new Vector3(4, 0, 4),
                    offset + new Vector3(-4, 0, -4), offset + new Vector3(4, 0, -4) }, PlayerSource);
            run.StartRun(map); run.Tick(8);
        }
        [TearDown] public void Cleanup()
        {
            var data = surface.navMeshData;
            surface.RemoveData(); Object.DestroyImmediate(root); Object.DestroyImmediate(map);
            if (data != null) Object.DestroyImmediate(data);
        }
        EnemyActor[] Actors() => director.GetComponentsInChildren<EnemyActor>(true);
        void SpawnOne()
        {
            director.Tick(2.01f); // Pick a valid position and start the warning.
            director.Tick(1.51f); // Commit only after warning and position revalidation.
        }

        [Test] public void SpawnWarnsBeforeConsumingQuotaAndRespectsAliveCap()
        {
            Assert.That(director.TrySpawn(EnemyKind.Melee, out _), Is.False);
            Assert.That(director.WarningActive, Is.True);
            Assert.That(run.Pending, Is.EqualTo(6));
            director.Tick(1.49f);
            Assert.That(run.Alive, Is.Zero);
            director.Tick(0.02f);
            Assert.That(run.Pending, Is.EqualTo(5));
            Assert.That(run.Alive, Is.EqualTo(1));
            for (var i = 0; i < 3; i++) SpawnOne();
            Assert.That(run.Alive, Is.EqualTo(4));
            Assert.That(run.Pending, Is.EqualTo(2));
            SpawnOne();
            Assert.That(run.Alive, Is.EqualTo(4));
            Assert.That(Actors().Length, Is.EqualTo(4));
            Actors()[0].Controller.ApplyDamage(1000, PlayerSource, 0);
            Assert.That(run.Kills, Is.EqualTo(1));
            SpawnOne();
            Assert.That(run.Alive, Is.EqualTo(4));
            Assert.That(run.Pending, Is.EqualTo(1));
        }

        [Test] public void ApproachingWarningCancelsSpawnWithoutFakeKillOrQuotaConsumption()
        {
            director.TrySpawn(EnemyKind.Melee, out _);
            Assert.That(director.WarningActive, Is.True);
            player.position = director.WarningPosition;
            Physics.SyncTransforms();
            director.Tick(1.6f);
            Assert.That(run.Alive, Is.Zero);
            Assert.That(run.Pending, Is.EqualTo(6));
            Assert.That(run.Kills, Is.Zero);
            Assert.That(director.WarningActive, Is.False);
        }

        [Test] public void MissingPlayerPathDoesNotConsumeQuota()
        {
            player.position = offset + Vector3.right * 100;
            director.Tick(10);
            Assert.That(run.Pending, Is.EqualTo(6));
            Assert.That(run.Alive, Is.Zero);
            Assert.That(director.WarningActive, Is.False);
            player.position = offset;
            SpawnOne();
            Assert.That(run.Alive, Is.EqualTo(1));
        }

        [Test] public void DisconnectedPlayerNavMeshAndNewSolidObstacleRejectSpawn()
        {
            var island = GameObject.CreatePrimitive(PrimitiveType.Cube);
            island.transform.SetParent(root.transform); island.layer = LayerMask.NameToLayer("WorldSolid");
            island.transform.position = offset + new Vector3(30, -0.1f, 0);
            island.transform.localScale = new Vector3(6, 0.2f, 6);
            var oldData = surface.navMeshData;
            surface.RemoveData(); surface.BuildNavMesh(); Object.DestroyImmediate(oldData);
            player.position = offset + Vector3.right * 30;
            director.Tick(10);
            Assert.That(run.Pending, Is.EqualTo(6)); Assert.That(run.Alive, Is.Zero);
            Assert.That(director.WarningActive, Is.False);
            player.position = offset;
            director.Tick(2.01f);
            Assert.That(director.WarningActive, Is.True);
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.SetParent(root.transform); obstacle.layer = LayerMask.NameToLayer("WorldSolid");
            obstacle.transform.position = director.WarningPosition + Vector3.up;
            obstacle.transform.localScale = Vector3.one * 2;
            Physics.SyncTransforms(); director.Tick(1.6f);
            Assert.That(run.Pending, Is.EqualTo(6)); Assert.That(run.Alive, Is.Zero);
        }

        [Test] public void ZeroRangedCapacityIsRejectedBeforeItCanStallWaveThree()
        {
            map.maxRangedAlive = 0;
            Assert.Throws<System.ArgumentException>(() => run.StartRun(map));
        }

        [Test] public void PauseFreezesWarningAgentsAndProjectileAndRestartClearsThem()
        {
            director.TrySpawn(EnemyKind.Melee, out _);
            run.SetPaused(true); director.Tick(20);
            Assert.That(run.Alive, Is.Zero);
            Assert.That(director.WarningActive, Is.True);
            run.SetPaused(false); director.Tick(1.6f);
            Assert.That(run.Alive, Is.EqualTo(1));
            var actor = Actors()[0];
            var pool = director.GetComponentInChildren<AcidProjectilePool>();
            Assert.That(pool.TryLaunch(offset + Vector3.up * 5, Vector3.forward, 5, 15), Is.True);
            var shot = pool.GetComponentInChildren<AcidProjectile>();
            var position = shot.transform.position;
            run.SetPaused(true); director.Tick(20);
            Assert.That(actor.GetComponent<NavMeshAgent>().isStopped, Is.True);
            Assert.That(actor.GetComponentInChildren<Animator>().speed, Is.Zero);
            Assert.That(shot.transform.position, Is.EqualTo(position));
            Assert.That(shot.Flying, Is.True);
            run.StartRun(map);
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(actor.gameObject.activeSelf, Is.False);
            Assert.That(director.WarningActive, Is.False);
            Assert.That(run.Alive, Is.Zero);
            run.Tick(8); SpawnOne();
            Assert.That(Actors().Length, Is.EqualTo(1), "Retry should reuse the inactive actor.");
            Assert.That(actor.Controller.Health, Is.EqualTo(100));
            actor.Controller.ApplyDamage(100, PlayerSource, 0);
            Assert.That(run.Kills, Is.EqualTo(1));
            Assert.That(run.Alive, Is.Zero);
        }

        [Test] public void FullWavesUseExactRangedQuotaAndSystemDeathsDoNotAwardKills()
        {
            for (var wave = 1; wave <= 3; wave++)
            {
                var quota = wave == 1 ? 6 : wave == 2 ? 8 : 10;
                var ranged = 0;
                for (var i = 0; i < quota; i++)
                {
                    SpawnOne();
                    Assert.That(run.Alive, Is.EqualTo(1));
                    foreach (var actor in Actors())
                    {
                        if (!actor.gameObject.activeSelf || actor.Controller.IsDead) continue;
                        var isRanged = actor.name.StartsWith("Ranged");
                        if (isRanged) ranged++;
                        Assert.That(actor.Controller.Health, Is.EqualTo(wave == 1 ? 100 : wave == 2 ? 105 : isRanged ? 88 : 110));
                        actor.Controller.ApplyDamage(1000, wave == 1 ? 999 : PlayerSource, 0);
                    }
                    run.Tick(0);
                }
                Assert.That(ranged, Is.EqualTo(wave == 3 ? 1 : 0));
                Assert.That(run.Phase, Is.EqualTo(RunPhase.Preparing));
                Assert.That(run.ReachedWave, Is.EqualTo(wave));
                Assert.That(run.Kills, Is.EqualTo(wave == 1 ? 0 : wave == 2 ? 8 : 18));
                run.Tick(8);
            }
        }

        [Test] public void BossAndCrawlerQuotasSurviveTenWavesAndRetry()
        {
            for(int w=1;w<=10;w++) {
                int bosses=0,crawlers=0,spawned=0;
                while(run.Phase==RunPhase.Active && spawned<100) {
                    SpawnOne();Assert.That(run.Alive,Is.EqualTo(1));
                    foreach(var actor in Actors())if(actor.gameObject.activeSelf && !actor.Controller.IsDead) {
                        spawned++;if(actor.Variant==EnemyVariant.Boss)bosses++;
                        if(actor.Variant==EnemyVariant.Crawler){crawlers++;Assert.That(actor.GetComponent<NavMeshAgent>().speed,Is.EqualTo(2.2f));}
                        actor.Controller.ApplyDamage(10000,PlayerSource,run.ActiveTime);
                    }
                    run.Tick(0);
                }
                Assert.That(spawned,Is.EqualTo(GameBalance.QuotaForWave(w)));
                Assert.That(bosses,Is.EqualTo(w%5==0?1:0));Assert.That(crawlers>0,Is.EqualTo(w>5));
                Assert.That(director.CorpseCount,Is.LessThanOrEqualTo(4));
                Assert.That(director.CreatedCount,Is.LessThanOrEqualTo(director.EnemyBodyCapacity));
                run.Tick(8);
            }
            run.StartRun(map);run.Tick(8);SpawnOne();
            foreach(var actor in Actors())if(actor.gameObject.activeSelf&&!actor.Controller.IsDead)Assert.That(actor.Variant,Is.EqualTo(EnemyVariant.Normal));
        }

        [Test] public void RangedCapDefersItsQuotaWhileMeleeUsesFreeSlots()
        {
            for (var w = 1; w <= 5; w++)
            {
                while (run.Phase == RunPhase.Active)
                {
                    SpawnOne();
                    Assert.That(run.Alive, Is.EqualTo(1));
                    foreach (var actor in Actors())
                        if (actor.gameObject.activeSelf && !actor.Controller.IsDead)
                            actor.Controller.ApplyDamage(1000, PlayerSource, 0);
                    run.Tick(0);
                }
                run.Tick(8);
            }
            Assert.That(run.ReachedWave, Is.EqualTo(6));
            for (var i = 0; i < 4; i++) SpawnOne();
            var rangedAlive = 0; EnemyActor firstRanged = null;
            foreach (var actor in Actors())
                if (actor.gameObject.activeSelf && !actor.Controller.IsDead && actor.name.StartsWith("Ranged"))
                { rangedAlive++; firstRanged = actor; }
            Assert.That(rangedAlive, Is.EqualTo(1));
            Assert.That(run.Alive, Is.EqualTo(4));
            Assert.That(run.Pending, Is.EqualTo(12));
            firstRanged.Controller.ApplyDamage(1000, PlayerSource, 0);
            SpawnOne();
            rangedAlive = 0;
            foreach (var actor in Actors())
                if (actor.gameObject.activeSelf && !actor.Controller.IsDead && actor.name.StartsWith("Ranged")) rangedAlive++;
            Assert.That(rangedAlive, Is.EqualTo(1));
            Assert.That(run.Pending, Is.EqualTo(11));
            Assert.That(director.CreatedCount, Is.LessThanOrEqualTo(13));
        }

        [Test] public void WaveScalingUsesFloorForRangedAndCeilingForStats()
        {
            Assert.That(GameBalance.RangedQuotaForWave(1), Is.Zero);
            Assert.That(GameBalance.RangedQuotaForWave(3), Is.EqualTo(1));
            Assert.That(GameBalance.RangedQuotaForWave(6), Is.EqualTo(2));
            Assert.That(GameBalance.RangedQuotaForWave(20), Is.EqualTo(13));
            Assert.That(GameBalance.RangedQuotaForWave(int.MaxValue), Is.EqualTo(18));
            Assert.That(GameBalance.EnemyHealth(EnemyKind.Melee, 2), Is.EqualTo(105));
            Assert.That(GameBalance.EnemyDamage(EnemyKind.Ranged, 2), Is.EqualTo(16));
            Assert.That(GameBalance.EnemyHealth(EnemyKind.Ranged, int.MaxValue), Is.EqualTo(160));
            Assert.That(GameBalance.EnemyDamage(EnemyKind.Melee, int.MaxValue), Is.EqualTo(15));
        }

        [Test] public void FifthRecentCorpseRetiresOldestWithoutAwardingAnotherKill()
        {
            for (var i = 0; i < 4; i++) SpawnOne();
            var first = Actors()[0];
            foreach (var actor in Actors()) actor.Controller.ApplyDamage(1000, PlayerSource, 0);
            Assert.That(run.Kills, Is.EqualTo(4));
            SpawnOne();
            foreach (var actor in Actors())
                if (actor.gameObject.activeSelf && !actor.Controller.IsDead)
                    actor.Controller.ApplyDamage(1000, PlayerSource, 0);
            var corpses = 0;
            foreach (var actor in Actors()) if (actor.gameObject.activeSelf && actor.Controller.IsDead) corpses++;
            Assert.That(corpses, Is.EqualTo(4));
            Assert.That(director.CorpseCount,Is.EqualTo(4));
            Assert.That(director.PeakCorpseCount,Is.EqualTo(4));
            Assert.That(director.PeakAliveCount,Is.EqualTo(map.maxAlive));
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(run.Kills, Is.EqualTo(5));
            Assert.That(run.Pending, Is.EqualTo(1));
        }
        [Test] public void EnemyRegistryRetainsCorpsesThenReturnsAndReusesActorsAcrossRetries()
        {
            SpawnOne();
            var registry=director.GetComponent<PoolRegistry>();Assert.That(registry,Is.Not.Null);
            var first=Actors()[0];first.Controller.ApplyDamage(1000,PlayerSource,0);
            Assert.That(registry.GetCounts("melee").Current,Is.EqualTo(1),"Corpse retains its body lease.");
            run.StartRun(map);
            Assert.That(registry.GetCounts("melee").Current,Is.Zero);
            Assert.That(first.Controller.IsWindingUp,Is.False);
            Assert.That(first.Controller.ApplyDamage(10,PlayerSource,0),Is.False);
            for(var attempt=0;attempt<20;attempt++)
            {
                run.Tick(8);SpawnOne();Assert.That(Actors().Length,Is.EqualTo(1));
                first.Controller.ApplyDamage(1000,PlayerSource,0);
                Assert.That(run.Kills,Is.EqualTo(1),"Reusing body must not duplicate death subscriptions.");
                run.StartRun(map);
                Assert.That(registry.GetCounts("melee").Current,Is.Zero);
            }
            Assert.That(registry.GetCounts("melee").Created,Is.EqualTo(1));
            Assert.That(registry.GetCounts("melee").Peak,Is.EqualTo(1));
            Assert.That(director.AliveCount,Is.Zero);Assert.That(director.CorpseCount,Is.Zero);
            Assert.That(director.PeakAliveCount,Is.EqualTo(1));Assert.That(director.PeakCorpseCount,Is.EqualTo(1));
        }
        [Test] public void CorpseExpiryReturnsLeaseWithoutChangingKillsOrPendingQuota()
        {
            SpawnOne();var actor=Actors()[0];actor.Controller.ApplyDamage(1000,PlayerSource,0);
            var pending=run.Pending;director.Tick(5.1f);
            Assert.That(actor.gameObject.activeSelf,Is.False);
            Assert.That(director.GetComponent<PoolRegistry>().GetCounts("melee").Current,Is.Zero);
            Assert.That(director.CorpseCount,Is.Zero);Assert.That(run.Kills,Is.EqualTo(1));
            Assert.That(run.Pending,Is.EqualTo(pending));
            Assert.That(actor.GetComponent<NavMeshAgent>().enabled,Is.False);
        }
    }
}
#endif
