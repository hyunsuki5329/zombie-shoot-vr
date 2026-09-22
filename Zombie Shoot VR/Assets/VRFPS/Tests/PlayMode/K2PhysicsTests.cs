using NUnit.Framework;
using UnityEngine;

namespace VRFPS.Tests
{
    public sealed class TestDamageReceiver : MonoBehaviour, IDamageable
    {
        public int Damage { get; private set; }
        public bool ApplyDamage(int amount, int sourceId, double activeTime) { Damage += amount; return true; }
    }

    public class K2PhysicsTests
    {
        GameObject gun, right, left, target, wall;
        K2Controller controller;
        TestDamageReceiver receiver;

        [SetUp]
        public void SetUp()
        {
            gun = new GameObject("K2 under test");
            right = new GameObject("Right hand");
            left = new GameObject("Left hand");
            left.transform.position = Vector3.forward * 0.3f;
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(gun.transform);
            muzzle.localPosition = Vector3.forward * 0.7f;
            controller = gun.AddComponent<K2Controller>();
            controller.Configure(right.transform, left.transform, muzzle, 1 << 8, (1 << 8) | (1 << 9));
            controller.SetCombatEnabled(true);
            controller.SetLeftGrip(true);
            target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.layer = 9;
            target.transform.position = Vector3.forward * 3;
            receiver = target.AddComponent<TestDamageReceiver>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gun); Object.DestroyImmediate(right); Object.DestroyImmediate(left);
            Object.DestroyImmediate(target); if (wall != null) Object.DestroyImmediate(wall);
        }

        void PlaceWall(float z)
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = 8;
            wall.transform.position = Vector3.forward * z;
            wall.transform.localScale = new Vector3(3, 3, 0.1f);
            Physics.SyncTransforms();
        }

        [Test]
        public void ValidTwoHandShotDamagesNearestTargetAndSpendsOneRound()
        {
            Assert.That(controller.Tick(true, 0), Is.True);
            Assert.That(receiver.Damage, Is.EqualTo(25));
            Assert.That(controller.Magazine.Rounds, Is.EqualTo(29));
            controller.SetLeftGrip(false);
            Assert.That(controller.Tick(true, 1), Is.False);
            Assert.That(receiver.Damage, Is.EqualTo(25));
        }

        [TestCase(0f)]
        [TestCase(0.35f)]
        [TestCase(0.7f)]
        public void HandInsideWallOrBarrelCrossingWallBlocksShot(float wallZ)
        {
            PlaceWall(wallZ);
            Assert.That(controller.Tick(true, 0), Is.False);
            Assert.That(receiver.Damage, Is.Zero);
            Assert.That(controller.Magazine.Rounds, Is.EqualTo(30));
        }

        [Test]
        public void WallAheadOfMuzzleTakesShotBeforeTarget()
        {
            PlaceWall(1.5f);
            Assert.That(controller.Tick(true, 0), Is.True);
            Assert.That(receiver.Damage, Is.Zero);
            Assert.That(controller.Magazine.Rounds, Is.EqualTo(29));
        }

        [Test]
        public void GunRemainsAtRightAnchorAndOverlappingHandsRetainLastAim()
        {
            controller.Tick(false, 0);
            var aim = gun.transform.rotation;
            right.transform.position = new Vector3(1, 2, 0);
            left.transform.position = right.transform.position;
            controller.Tick(false, 1);
            Assert.That(gun.transform.position, Is.EqualTo(right.transform.position));
            Assert.That(Quaternion.Angle(gun.transform.rotation, aim), Is.LessThan(0.001f));
            controller.SetLeftGrip(false);
            right.transform.rotation = Quaternion.Euler(0, 30, 0);
            controller.Tick(false, 2);
            Assert.That(Quaternion.Angle(gun.transform.rotation, right.transform.rotation), Is.LessThan(0.001f));
        }

        [Test]
        public void LeftGripRequiresPhysicalForegripProximityAndReleasesImmediately()
        {
            controller.SetLeftGrip(false);
            var anchor = new GameObject("Foregrip").transform;
            anchor.SetParent(gun.transform);
            anchor.localPosition = Vector3.forward * 0.3f;
            var input = gun.AddComponent<K2HandInput>();
            input.enabled = false;
            input.Configure(controller, left.transform, anchor);
            left.transform.position = Vector3.left * 2;
            Assert.That(input.TickInput(true, true, 0), Is.False);
            left.transform.position = Vector3.forward * 0.3f;
            Assert.That(input.TickInput(true, true, 0), Is.True);
            Assert.That(input.TickInput(false, true, 1), Is.False);
            Assert.That(controller.Magazine.Rounds, Is.EqualTo(29));
        }

        [Test]
        public void DisablingCombatClearsGripAndCannotResumeFromStaleGrip()
        {
            controller.SetCombatEnabled(false);
            Assert.That(controller.Tick(true, 0), Is.False);
            controller.SetCombatEnabled(true);
            Assert.That(controller.Tick(true, 1), Is.False);
            controller.SetLeftGrip(true);
            Assert.That(controller.Tick(true, 1), Is.True);
        }
    }
}
