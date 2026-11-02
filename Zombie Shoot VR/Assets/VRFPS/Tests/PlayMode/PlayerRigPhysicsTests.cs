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

        [Test] public void IntroRouterAllowsWalkingButPauseAndTransitionBlockIt()
        {
            var run=rig.AddComponent<RunController>();var gun=rig.AddComponent<K2Controller>();
            var router=rig.AddComponent<WeaponContextRouter>();router.Configure(run,gun,movement,null);
            router.SetIntroState(true,false,0);
            for(int i=0;i<10;i++)movement.Tick(Vector2.zero,Vector2.zero,1f/60);
            var before=rig.transform.position;
            Assert.That(movement.TryJump(),Is.False);
            for(int i=0;i<2;i++)movement.Tick(Vector2.up,Vector2.zero,.1f);
            Assert.That(rig.transform.position.z,Is.GreaterThan(before.z+.2f));
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1,1);
            wall.transform.localScale=new Vector3(4,2,.2f);Physics.SyncTransforms();
            for(int i=0;i<60;i++)movement.Tick(Vector2.up,Vector2.zero,1f/60);
            Assert.That(rig.transform.position.z,Is.LessThan(.85f),"Intro collision must stop the capsule at the wall");
            foreach(var blocked in new[]{true,false})
            {
                router.SetIntroState(blocked,blocked,1); // paused preview, then transition
                before=rig.transform.position;movement.Tick(Vector2.down,Vector2.right,.2f);
                Assert.That(Vector3.Distance(rig.transform.position,before),Is.LessThan(.001f));
            }
        }

        [Test] public void IndependentPermissionsCanAllowTurnOnlyThenRestoreCombat()
        {
            for(int i=0;i<10;i++)movement.Tick(Vector2.zero,Vector2.zero,1f/60);
            var before=rig.transform.position;var tracked=head.localPosition;
            movement.SetLocomotionPermissions(false,true,false);
            Assert.That(movement.TryJump(),Is.False);
            movement.Tick(Vector2.one,Vector2.right,.1f);
            Assert.That(Vector3.Distance(rig.transform.position,before),Is.LessThan(.001f));
            Assert.That(Mathf.DeltaAngle(0,rig.transform.eulerAngles.y),Is.EqualTo(6).Within(.01f));
            Assert.That(head.localPosition,Is.EqualTo(tracked));
            movement.SetLocomotionEnabled(false);
            movement.Tick(Vector2.one,Vector2.right,.1f);
            Assert.That(Mathf.DeltaAngle(0,rig.transform.eulerAngles.y),Is.EqualTo(6).Within(.01f));
            movement.SetLocomotionEnabled(true);
            Assert.That(movement.TryJump(),Is.True);
            movement.Tick(Vector2.up,Vector2.zero,.1f);
            Assert.That(rig.transform.position.y,Is.GreaterThan(before.y));
            Assert.That(Vector2.Distance(new Vector2(before.x,before.z),new Vector2(rig.transform.position.x,rig.transform.position.z)),Is.GreaterThan(.1f));
        }

        [Test] public void JumpLandsOnFurnitureAndRejectsAirborneOrPausedRequests()
        {
            var jump=typeof(PlayerRigController).GetMethod("TryJump");
            Assert.That(jump,Is.Not.Null,"A-button jump is missing");
            for(int i=0;i<10;i++)movement.Tick(Vector2.zero,Vector2.zero,1f/60);
            movement.SetLocomotionEnabled(false);Assert.That((bool)jump.Invoke(movement,null),Is.False);
            movement.SetLocomotionEnabled(true);
            Assert.That((bool)jump.Invoke(movement,null),Is.True);
            Assert.That((bool)jump.Invoke(movement,null),Is.False);
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,.6f,1);
            wall.transform.localScale=new Vector3(3,1.2f,1);Physics.SyncTransforms();
            for(int i=0;i<90;i++)movement.Tick(i<40?Vector2.up:Vector2.zero,Vector2.zero,1f/60);
            Assert.That(rig.transform.position.y,Is.InRange(1.15f,1.3f));
            Assert.That(head.localPosition,Is.EqualTo(new Vector3(0,1.7f,0)));
            Assert.That((bool)jump.Invoke(movement,null),Is.True);
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
        public void PhysicalHeadMotionCannotCrossThinWallAndPreservesLocalTracking()
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0, 1.5f, 2);
            wall.transform.localScale = new Vector3(10, 3, 0.2f);
            movement.Tick(Vector2.zero, Vector2.zero, 1f / 60);
            Physics.SyncTransforms();
            head.localPosition = new Vector3(0, 1.7f, 4);
            movement.Tick(Vector2.zero, Vector2.zero, 1f / 60);
            Assert.That(head.position.z, Is.InRange(1.5f, 1.85f));
            Assert.That(head.localPosition.z, Is.EqualTo(4));
            var stopped = head.position.z;
            movement.Tick(Vector2.zero, Vector2.zero, 1f / 60);
            Assert.That(head.position.z, Is.EqualTo(stopped).Within(0.001f));
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
