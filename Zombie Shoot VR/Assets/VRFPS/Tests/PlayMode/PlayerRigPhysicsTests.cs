using NUnit.Framework;
using UnityEngine;
using VRFPS.Player;

namespace VRFPS.Tests
{
    public class PlayerRigPhysicsTests
    {
        GameObject rig, floor, wall;
        PlayerRigController movement;
        Transform head;

        [SetUp]
        public void SetUp()
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0, -0.1f, 0);
            floor.transform.localScale = new Vector3(20, 0.2f, 20);
            rig = new GameObject("Test player");
            var capsule = rig.AddComponent<CharacterController>();
            capsule.radius = 0.2f;
            capsule.skinWidth = 0.02f;
            capsule.stepOffset = 0.2f;
            capsule.minMoveDistance = 0;
            head = new GameObject("Tracked head").transform;
            head.SetParent(rig.transform);
            head.localPosition = new Vector3(0, 1.7f, 0);
            movement = rig.AddComponent<PlayerRigController>();
            movement.enabled = false; // Drive real movement deterministically, without XR hardware.
            movement.Head = head;
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(floor);
            if (wall != null) Object.DestroyImmediate(wall);
        }

        [Test]
        public void StickMotionStopsAtWallAndFloor()
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0, 1.5f, 2);
            wall.transform.localScale = new Vector3(10, 3, 0.2f);
            Physics.SyncTransforms();
            for (var i = 0; i < 240; i++) movement.Tick(Vector2.up, Vector2.zero, 1f / 60);
            Assert.That(rig.transform.position.z, Is.InRange(1.5f, 1.85f));
            Assert.That(rig.transform.position.y, Is.InRange(-0.05f, 0.05f));
        }

        [Test]
        public void TurnIsContinuousAndKeepsOffsetHeadAtPivot()
        {
            head.localPosition = new Vector3(0.6f, 1.7f, -0.3f);
            var before = head.position;
            movement.Tick(Vector2.zero, Vector2.right, 0.1f);
            Assert.That(Mathf.DeltaAngle(0, rig.transform.eulerAngles.y), Is.EqualTo(6).Within(0.01f));
            Assert.That(head.position.x, Is.EqualTo(before.x).Within(0.001f));
            Assert.That(head.position.z, Is.EqualTo(before.z).Within(0.001f));
        }

        [Test]
        public void CrouchAndHorizontalTrackingUpdateCapsule()
        {
            head.localPosition = new Vector3(0.4f, 0.9f, 0.2f);
            movement.Tick(Vector2.zero, Vector2.zero, 1f / 60);
            var capsule = rig.GetComponent<CharacterController>();
            Assert.That(capsule.height, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(capsule.center.x, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(capsule.center.z, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(capsule.center.y, Is.EqualTo(capsule.height / 2).Within(0.001f));
        }

        [Test]
        public void PausedLocomotionLeavesTrackedPoseFreeWithoutMovingOrigin()
        {
            movement.SetLocomotionEnabled(false);
            var before = rig.transform.position;
            head.localPosition = new Vector3(0.3f, 1.1f, 0.2f);
            movement.Tick(Vector2.one, Vector2.right, 1);
            Assert.That(rig.transform.position, Is.EqualTo(before));
            Assert.That(rig.transform.rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(head.localPosition, Is.EqualTo(new Vector3(0.3f, 1.1f, 0.2f)));
            movement.SetLocomotionEnabled(true);
            movement.Tick(Vector2.up, Vector2.zero, 0.1f);
            Assert.That(rig.transform.position.z, Is.GreaterThan(before.z));
        }
    }
}
