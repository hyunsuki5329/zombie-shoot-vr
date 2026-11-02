using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VRFPS.Tests
{
    public sealed class EnemyDamageProbe : MonoBehaviour, IDamageable
    {
        public int Damage, Hits;
        public bool ApplyDamage(int amount, int sourceId, double activeTime)
        { Damage += amount; Hits++; return true; }
    }

    public class EnemyAttackTests
    {
        readonly List<GameObject> objects = new List<GameObject>();
        const int Solid = 1 << 8, Player = 1 << 9;
        GameObject Make(string name, Vector3 position)
        {
            var go = new GameObject(name); go.transform.position = position;
            objects.Add(go); return go;
        }
        EnemyDamageProbe Target(Vector3 position)
        {
            var go = Make("Target", position); go.layer = 9;
            go.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            return go.AddComponent<EnemyDamageProbe>();
        }
        GameObject Wall(float z)
        {
            var wall = Make("Wall", Vector3.forward * z); wall.layer = 8;
            wall.AddComponent<BoxCollider>().size = new Vector3(4, 4, 0.05f);
            Physics.SyncTransforms(); return wall;
        }
        ZombieController Zombie(EnemyKind kind, Transform target)
        {
            var zombie = Make("Zombie", Vector3.zero).AddComponent<ZombieController>();
            zombie.Configure(42, kind, target, zombie.transform, Solid);
            return zombie;
        }
        AcidProjectile Projectile()
        {
            var shot = Make("Acid", Vector3.zero).AddComponent<AcidProjectile>();
            shot.Launch(Vector3.zero, Vector3.forward, 42, 15, Solid, Player);
            return shot;
        }
        [TearDown] public void Cleanup()
        { foreach (var go in objects) if (go != null) Object.DestroyImmediate(go); objects.Clear(); }

        [Test] public void CrawlerHitsStandingPlayerTorsoAndWallStillBlocks()
        {
            var player=Make("Player",Vector3.forward*.8f);var body=player.AddComponent<CharacterController>();body.height=1.8f;body.center=Vector3.up*.9f;
            var damage=player.AddComponent<EnemyDamageProbe>();var head=Make("Head",player.transform.position+Vector3.up*1.8f).transform;head.SetParent(player.transform);
            var origin=Make("Crawl",Vector3.up*.4f).transform;var enemy=origin.gameObject.AddComponent<ZombieController>();
            enemy.Configure(42,EnemyKind.Melee,head,origin,Solid,6,EnemyVariant.Crawler);
            enemy.Tick(0,0);Assert.That(enemy.IsWindingUp,Is.True);enemy.Tick(.46f,.46);Assert.That(damage.Hits,Is.EqualTo(1));
            enemy.Tick(1,2);Wall(.4f);enemy.Tick(.46f,2.5);Assert.That(damage.Hits,Is.EqualTo(1));
        }
        [Test] public void BossHasLongerWarningAndPauseStopsImpact()
        {
            var target=Target(Vector3.forward);var enemy=Make("Boss",Vector3.zero).AddComponent<ZombieController>();
            enemy.Configure(42,EnemyKind.Melee,target.transform,enemy.transform,Solid,5,EnemyVariant.Boss);
            Assert.That(enemy.Health,Is.EqualTo(600));enemy.Tick(0,0);enemy.Tick(.7f,.7);Assert.That(target.Hits,Is.Zero);
            enemy.SetCombatEnabled(false);enemy.Tick(10,10);Assert.That(target.Hits,Is.Zero);
            enemy.SetCombatEnabled(true);enemy.Tick(.31f,1.01);Assert.That(target.Damage,Is.EqualTo(27));
        }

        [Test] public void MeleeTelegraphsThenHitsOnceAndWaitsForCooldown()
        {
            var target = Target(Vector3.forward);
            var enemy = Zombie(EnemyKind.Melee, target.transform);
            enemy.Tick(0, 0); Assert.That(enemy.IsWindingUp, Is.True);
            enemy.Tick(0.64f, 0.64); Assert.That(target.Hits, Is.Zero);
            enemy.Tick(0.02f, 0.66); Assert.That(target.Damage, Is.EqualTo(10));
            enemy.Tick(0.1f, 0.76); Assert.That(target.Hits, Is.EqualTo(1));
        }
        [TestCase(true)] [TestCase(false)]
        public void MeleeRechecksWallAndDistanceAtImpact(bool useWall)
        {
            var target = Target(Vector3.forward);
            var enemy = Zombie(EnemyKind.Melee, target.transform);
            enemy.Tick(0, 0); Assert.That(enemy.IsWindingUp, Is.True);
            if (useWall) Wall(0.5f); else target.transform.position = Vector3.forward * 3;
            enemy.Tick(0.7f, 0.7); Assert.That(target.Hits, Is.Zero);
        }
        [Test] public void RangedTelegraphUsesReleaseAimAndIsBlockedByWall()
        {
            var target = Target(Vector3.forward * 4);
            var enemy = Zombie(EnemyKind.Ranged, target.transform);
            var count = 0; var direction = Vector3.zero;
            enemy.ProjectileRequested += (origin, aim, id, damage) => { count++; direction = aim; };
            enemy.Tick(0, 0); enemy.Tick(0.79f, 0.79); Assert.That(count, Is.Zero);
            target.transform.position += Vector3.right;
            enemy.Tick(0.02f, 0.81); Assert.That(count, Is.EqualTo(1));
            Assert.That(Vector3.Angle(direction, target.transform.position), Is.LessThan(0.1f));
            enemy.Tick(4, 4.81); enemy.Tick(0, 4.81);
            Wall(2); enemy.Tick(0.81f, 5.62); Assert.That(count, Is.EqualTo(1));
        }
        [Test] public void DeathNotifiesOnceAndStopsPendingAttack()
        {
            var target = Target(Vector3.forward);
            var enemy = Zombie(EnemyKind.Melee, target.transform);
            var deaths = 0; enemy.Died += (id, source) => { Assert.That(id, Is.EqualTo(42)); deaths++; };
            enemy.Tick(0, 0);
            Assert.That(enemy.ApplyDamage(100, 7, 0.1), Is.True);
            Assert.That(enemy.ApplyDamage(100, 7, 0.2), Is.False);
            enemy.Tick(10, 10); Assert.That(deaths, Is.EqualTo(1)); Assert.That(target.Hits, Is.Zero);
        }
        [Test] public void DisabledCombatFreezesWindup()
        {
            var target = Target(Vector3.forward);
            var enemy = Zombie(EnemyKind.Melee, target.transform);
            enemy.Tick(0, 0); enemy.SetCombatEnabled(false); enemy.Tick(10, 10);
            Assert.That(target.Hits, Is.Zero);
            enemy.SetCombatEnabled(true); enemy.Tick(0.66f, 10.66);
            Assert.That(target.Hits, Is.EqualTo(1));
        }
        [TestCase(true)] [TestCase(false)]
        public void ProjectileSweepsThinObstaclesAndResolvesOnlyOnce(bool wallFirst)
        {
            var target = Target(Vector3.forward * 2);
            if (wallFirst) Wall(1);
            Physics.SyncTransforms(); var shot = Projectile();
            shot.Tick(1, 1); shot.Tick(1, 2);
            Assert.That(shot.Flying, Is.False);
            Assert.That(target.Hits, Is.EqualTo(wallFirst ? 0 : 1));
        }
        [Test] public void ProjectileExpiresBeforeDistantTargetAndDoesNotHome()
        {
            var target = Target(Vector3.forward * 26); Physics.SyncTransforms();
            var shot = Projectile(); shot.Tick(10, 10);
            Assert.That(shot.Flying, Is.False); Assert.That(target.Hits, Is.Zero);
            shot.Launch(Vector3.zero, Vector3.forward, 42, 15, Solid, Player);
            target.transform.position = new Vector3(1, 0, 2); Physics.SyncTransforms();
            shot.Tick(1, 11); Assert.That(shot.transform.position.x, Is.EqualTo(0));
            Assert.That(target.Hits, Is.Zero);
        }

        [TestCase(2f)] [TestCase(8f)]
        public void RangedRejectsInvalidRangeBeforeAndAfterTelegraph(float distance)
        {
            var target = Target(Vector3.forward * distance);
            var enemy = Zombie(EnemyKind.Ranged, target.transform);
            var shots = 0; enemy.ProjectileRequested += (p, d, id, amount) => shots++;
            enemy.Tick(0, 0); Assert.That(enemy.IsWindingUp, Is.False);
            target.transform.position = Vector3.forward * 4;
            enemy.Tick(0, 0); Assert.That(enemy.IsWindingUp, Is.True);
            target.transform.position = Vector3.forward * distance;
            enemy.Tick(0.81f, 0.81); Assert.That(shots, Is.Zero);
        }

        [TestCase(EnemyKind.Melee, 0.65f, 0.85f, 1f)]
        [TestCase(EnemyKind.Ranged, 0.8f, 2.7f, 4f)]
        public void CooldownAndWindupTogetherPreserveAttackInterval(EnemyKind kind, float windup, float cooldown, float distance)
        {
            var target = Target(Vector3.forward * distance);
            var enemy = Zombie(kind, target.transform);
            var shots = 0; enemy.ProjectileRequested += (p, d, id, amount) => shots++;
            enemy.Tick(0, 0); enemy.Tick(windup + 0.001f, windup);
            enemy.Tick(cooldown - 0.01f, windup + cooldown - 0.01f);
            Assert.That(enemy.IsWindingUp, Is.False);
            enemy.Tick(0.02f, windup + cooldown + 0.01f);
            Assert.That(enemy.IsWindingUp, Is.True);
            Assert.That(kind == EnemyKind.Melee ? target.Hits : shots, Is.EqualTo(1));
            enemy.Tick(windup + 0.001f, 2 * windup + cooldown + 0.01f);
            Assert.That(kind == EnemyKind.Melee ? target.Hits : shots, Is.EqualTo(2));
        }

        [Test] public void ProjectileTravelsAtSixMetresPerSecondUntilFourSecondLifetime()
        {
            var shot = Projectile(); shot.Tick(1, 1);
            Assert.That(shot.transform.position.z, Is.EqualTo(6).Within(0.001));
            shot.Tick(2.9f, 3.9); Assert.That(shot.Flying, Is.True);
            shot.Tick(1, 4.9); Assert.That(shot.Flying, Is.False);
            Assert.That(shot.transform.position.z, Is.EqualTo(24).Within(0.001));
        }

        [Test] public void ProjectileBornInsideWallCannotDamageOverlappingPlayer()
        {
            var target = Target(Vector3.zero); Wall(0);
            var shot = Projectile(); shot.Tick(0.1f, 0.1);
            Assert.That(shot.Flying, Is.False); Assert.That(target.Hits, Is.Zero);
        }
    }
}
