using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRFPS.Player;

namespace VRFPS.Tests
{
    public class RunPauseTests
    {
        GameObject root;
        MapDefinition map;
        RunController run;
        K2Controller gun;
        PlayerRigController movement;
        AmmoPouch pouch;
        Transform head, left, right;

        GameObject Child(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform); return go;
        }
        [SetUp] public void Setup()
        {
            root = new GameObject("Pause fixture");
            run = root.AddComponent<RunController>(); run.enabled = false;
            map = ScriptableObject.CreateInstance<MapDefinition>();
            map.mapId = "room"; map.sceneName = "Room";
            map.maxAlive = 4; map.maxRangedAlive = 1; map.minSpawnDistance = 2;
            var rig = Child("Rig");
            var capsule = rig.AddComponent<CharacterController>();
            capsule.radius = 0.2f; capsule.height = 1.7f; capsule.center = Vector3.up * 0.85f;
            movement = rig.AddComponent<PlayerRigController>(); movement.enabled = false;
            head = Child("Head").transform; head.SetParent(rig.transform); head.localPosition = Vector3.up * 1.7f;
            movement.Head = head;
            left = Child("Left hand").transform; right = Child("Right hand").transform;
            gun = Child("Gun").AddComponent<K2Controller>();
            var muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(gun.transform);
            muzzle.localPosition = Vector3.forward * 0.2f;
            gun.Configure(right, left, muzzle, 1 << 8, 1 << 10);
            pouch = Child("Pouch").AddComponent<AmmoPouch>(); pouch.enabled = false;
            root.AddComponent<RunCombatBindings>().Configure(run, gun, movement, pouch);
            run.StartRun(map); run.Tick(8);
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(root); Object.DestroyImmediate(map);
        }

        [Test] public void PauseStopsSticksAndFireButTrackedHeadAndGunContinueMoving()
        {
            Assert.That(gun.Phase, Is.EqualTo(RunPhase.Active));
            gun.SetLeftGrip(true);
            Assert.That(gun.Tick(true, 0), Is.True);
            run.SetPaused(true);
            var position = movement.transform.position;
            var rotation = movement.transform.rotation;
            head.localPosition = new Vector3(0.2f, 1.1f, 0.1f);
            movement.Tick(Vector2.up, Vector2.right, 0.1f);
            right.position = new Vector3(1, 1, 2);
            gun.UpdatePose();
            Assert.That(Vector3.Distance(movement.transform.position, position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(movement.transform.rotation, rotation), Is.LessThan(0.001f));
            Assert.That(head.localPosition, Is.EqualTo(new Vector3(0.2f, 1.1f, 0.1f)));
            Assert.That(gun.transform.position, Is.EqualTo(right.position));
            Assert.That(gun.Tick(true, 1), Is.False);
            run.SetPaused(false);
            Assert.That(gun.Tick(true, 2), Is.False, "Resume must not preserve a stale foregrip flag.");
            gun.SetLeftGrip(true);
            Assert.That(gun.Tick(true, 2), Is.True);
            run.EndRun();
            Assert.That(gun.Phase, Is.EqualTo(RunPhase.Dead));
        }

        [UnityTest] public IEnumerator DroppedMagazineFreezesMotionAndLifetimeThenResumesBoth()
        {
            var scene = SceneManager.CreateScene("Pause magazine physics", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            var tokenObject = new GameObject("Falling magazine");
            var hand = new GameObject("Drop hand");
            AsyncOperation unload;
            try
            {
                SceneManager.MoveGameObjectToScene(tokenObject, scene);
                SceneManager.MoveGameObjectToScene(hand, scene);
                hand.transform.position = Vector3.up * 10;
                var token = tokenObject.AddComponent<MagazineToken>();
                token.TryGrab(hand.transform); token.Release(hand.transform);
                pouch.Register(token);
                var body = token.GetComponent<Rigidbody>();
                body.linearVelocity = new Vector3(1, -2, 0);
                body.angularVelocity = Vector3.up;
                var velocity = body.linearVelocity;
                var angular = body.angularVelocity;
                run.SetPaused(true);
                var position = token.transform.position;
                Physics.SyncTransforms();
                var physics = scene.GetPhysicsScene();
                for (var i = 0; i < 60; i++) physics.Simulate(1f / 60);
                pouch.TickLifetime(20);
                Assert.That(Vector3.Distance(token.transform.position, position), Is.LessThan(0.001f));
                Assert.That(token.gameObject.activeSelf, Is.True);
                run.SetPaused(false);
                Assert.That(body.isKinematic, Is.False);
                Assert.That(body.linearVelocity, Is.EqualTo(velocity));
                Assert.That(body.angularVelocity, Is.EqualTo(angular));
                physics.Simulate(0.1f);
                Assert.That(token.transform.position.y, Is.LessThan(position.y - 0.1f));
                pouch.TickLifetime(11.9f);
                Assert.That(token.gameObject.activeSelf, Is.True);
                pouch.TickLifetime(0.2f);
                Assert.That(token.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(tokenObject); Object.DestroyImmediate(hand);
                unload = SceneManager.UnloadSceneAsync(scene);
            }
            yield return unload;
        }
    }
}
