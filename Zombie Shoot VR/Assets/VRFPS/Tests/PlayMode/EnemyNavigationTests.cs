using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Unity.AI.Navigation;

namespace VRFPS.Tests
{
    public class EnemyNavigationTests
    {
        GameObject root;
        NavMeshSurface surface;
        readonly Vector3 offset = new Vector3(2000, 0, 0);
        GameObject Box(string name, Vector3 position, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(root.transform);
            go.transform.position = offset + position; go.transform.localScale = size;
            return go;
        }
        [SetUp] public void Setup()
        {
            root = new GameObject("Isolated navigation fixture");
            Box("Floor", new Vector3(0, -0.1f, 0), new Vector3(12, 0.2f, 12));
            Box("Furniture", Vector3.up, new Vector3(1, 2, 3));
            surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }
        [TearDown] public void Cleanup()
        {
            var data = surface.navMeshData;
            surface.RemoveData(); Object.DestroyImmediate(root);
            if (data != null) Object.DestroyImmediate(data);
        }
        ZombieMotor Create(out ZombieController enemy, out NavMeshAgent agent)
        {
            var go = new GameObject("Enemy"); go.transform.SetParent(root.transform);
            go.transform.position = offset + new Vector3(0, 0, -3);
            agent = go.AddComponent<NavMeshAgent>();
            enemy = go.AddComponent<ZombieController>();
            var target = new GameObject("Target"); target.transform.SetParent(root.transform);
            target.transform.position = offset + new Vector3(0, 0, 3);
            enemy.Configure(91, EnemyKind.Melee, target.transform, go.transform, 1 << 8);
            var motor = go.AddComponent<ZombieMotor>();
            motor.Configure(target.transform, EnemyKind.Melee, new[] { offset + new Vector3(3, 0, -3) });
            return motor;
        }
        [UnityTest] public IEnumerator AgentRoutesAroundFurnitureAndStopsWhenPausedOrDead()
        {
            var motor = Create(out var enemy, out var agent);
            motor.Tick(0.01f);
            for (var i = 0; i < 10 && (agent.pathPending || !agent.hasPath); i++) yield return null;
            Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(agent.path.corners.Length, Is.GreaterThan(2), "A direct path would cross furniture.");
            motor.SetCombatEnabled(false); Assert.That(agent.isStopped, Is.True);
            motor.SetCombatEnabled(true); motor.Tick(0.01f); Assert.That(agent.isStopped, Is.False);
            enemy.ApplyDamage(100, 5, 1); motor.Tick(0.01f); Assert.That(agent.isStopped, Is.True);
        }
        [Test] public void FiveSecondStallRepositionsWithoutReplacingEnemyOrHealth()
        {
            var motor = Create(out var enemy, out var agent);
            enemy.ApplyDamage(25, 5, 0);
            motor.Tick(5.1f);
            Assert.That(motor.RecoveryCount, Is.EqualTo(1));
            Assert.That(agent.transform.position.x, Is.EqualTo(offset.x + 3).Within(0.1));
            Assert.That(enemy.Health, Is.EqualTo(75));
        }
        [Test] public void ProjectilePoolRejectsThirteenthShotAndReusesRetiredObjects()
        {
            var pool = root.AddComponent<AcidProjectilePool>();
            var template = new GameObject("Acid template"); template.transform.SetParent(root.transform);
            var projectile = template.AddComponent<AcidProjectile>(); template.SetActive(false);
            pool.Configure(projectile, 1 << 8, 1 << 9);
            for (var i = 0; i < 12; i++) Assert.That(pool.TryLaunch(offset + Vector3.up * 5, Vector3.forward, 1, 15), Is.True);
            Assert.That(pool.TryLaunch(offset, Vector3.forward, 1, 15), Is.False);
            Assert.That(pool.CreatedCount, Is.EqualTo(12));
            pool.Tick(4.1f, 4.1); Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.TryLaunch(offset, Vector3.forward, 1, 15), Is.True);
            Assert.That(pool.CreatedCount, Is.EqualTo(12));
        }

        [Test] public void ActorDeathDisablesHitboxImmediatelyAndReusesSameBodyOnReset()
        {
            var motor = Create(out var enemy, out var agent);
            var body = enemy.gameObject.AddComponent<CapsuleCollider>();
            var target = root.transform.Find("Target");
            var actor = enemy.gameObject.AddComponent<EnemyActor>();
            actor.Configure(91, EnemyKind.Melee, target, enemy.transform, null, new Vector3[0], 1 << 8);
            enemy.ApplyDamage(100, 5, 0);
            Assert.That(body.enabled, Is.False);
            actor.Tick(5.1f, 5.1); Assert.That(actor.gameObject.activeSelf, Is.False);
            actor.Configure(92, EnemyKind.Melee, target, enemy.transform, null, new Vector3[0], 1 << 8);
            Assert.That(actor.gameObject.activeSelf, Is.True); Assert.That(body.enabled, Is.True);
            Assert.That(enemy.Health, Is.EqualTo(100));
        }

        [UnityTest] public IEnumerator AgentActuallyWalksAroundFurniture()
        {
            var motor = Create(out var enemy, out var agent);
            var furthestSideways = 0f;
            var deadline = Time.realtimeSinceStartup + 15;
            while (Time.realtimeSinceStartup < deadline && agent.transform.position.z < 1.9f)
            {
                motor.Tick(Time.deltaTime);
                furthestSideways = Mathf.Max(furthestSideways, Mathf.Abs(agent.transform.position.x - offset.x));
                yield return null;
            }
            Assert.That(agent.transform.position.z, Is.GreaterThanOrEqualTo(1.9f));
            Assert.That(furthestSideways, Is.GreaterThan(0.6f), "Agent must move around the furniture's side.");
        }

        [Test] public void WindupAndRecoveryStopAgentAndInvalidRecoveryDoesNotTeleport()
        {
            var motor = Create(out var enemy, out var agent);
            var target = root.transform.Find("Target");
            motor.Configure(target, EnemyKind.Melee, new[] { offset + Vector3.right * 100 });
            var original = agent.transform.position;
            motor.Tick(5.1f); Assert.That(motor.RecoveryCount, Is.Zero);
            Assert.That(agent.transform.position, Is.EqualTo(original));
            target.position = original + Vector3.forward * 0.9f;
            enemy.Tick(0, 0); motor.Tick(0);
            Assert.That(enemy.IsWindingUp, Is.True); Assert.That(agent.isStopped, Is.True);
            enemy.Tick(0.66f, 0.66); motor.Tick(0.01f);
            Assert.That(enemy.IsWindingUp, Is.False); Assert.That(agent.isStopped, Is.True);
        }

        [UnityTest] public IEnumerator NavigationUsesPlayerFeetWhenHeadIsCloserToAnIsolatedTabletop()
        {
            Box("Wide table", new Vector3(3, 0.5f, 3), new Vector3(3, 1, 3));
            surface.BuildNavMesh();
            var motor = Create(out var enemy, out var agent);
            var target = root.transform.Find("Target");
            var player = new GameObject("Player body"); player.transform.SetParent(root.transform);
            player.transform.position = offset + new Vector3(3, 0, 0.5f);
            var capsule = player.AddComponent<CharacterController>(); capsule.height = 1.8f;
            capsule.center = Vector3.up * 0.9f; capsule.radius = 0.3f;
            target.SetParent(player.transform); target.position = offset + new Vector3(3, 1.8f, 3);
            Assert.That(NavMesh.SamplePosition(target.position, out var headHit, 2, NavMesh.AllAreas), Is.True);
            Assert.That(headHit.position.y, Is.GreaterThan(0.8f), "Fixture must expose the elevated island risk.");
            motor.Configure(target, EnemyKind.Melee, new Vector3[0]); motor.Tick(0.01f);
            for (var i = 0; i < 10 && (agent.pathPending || !agent.hasPath); i++) yield return null;
            Assert.That(agent.destination.y, Is.LessThan(0.2f));
            Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
        }
    }
}
