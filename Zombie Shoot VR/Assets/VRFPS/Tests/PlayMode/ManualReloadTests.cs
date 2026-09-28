using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VRFPS.Tests
{
    public class ManualReloadTests
    {
        readonly List<GameObject> objects = new List<GameObject>();
        K2Controller gun;
        Transform left, foregrip;
        MagazineSocket socket;
        AmmoPouch pouch;
        LeftHandInteractor interactor;

        GameObject Create(string name) { var item = new GameObject(name); objects.Add(item); return item; }

        [SetUp]
        public void SetUp()
        {
            gun = Create("Gun").AddComponent<K2Controller>();
            left = Create("Left").transform;
            foregrip = Create("Foregrip").transform; foregrip.position = Vector3.forward * 0.3f;
            socket = Create("Socket").AddComponent<MagazineSocket>(); socket.transform.position = Vector3.down * 0.2f;
            pouch = Create("Pouch").AddComponent<AmmoPouch>(); pouch.transform.position = Vector3.left * 0.5f;
            var prefab = Create("Template").AddComponent<MagazineToken>(); prefab.gameObject.SetActive(false);
            pouch.Configure(prefab, left);
            var initial = Create("Initial").AddComponent<MagazineToken>();
            interactor = Create("Ownership").AddComponent<LeftHandInteractor>();
            interactor.Configure(gun, left, foregrip, socket, pouch, initial);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in objects) if (item != null) Object.DestroyImmediate(item);
            objects.Clear();
        }

        void GripAt(Vector3 position) { left.position = position; interactor.Tick(true, true); }
        void Release() => interactor.Tick(false, true);

        [Test] public void MagazineRegistryTracksOwnedTokensAndRecyclesOnlyLooseTokens()
        {
            var registry=pouch.GetComponent<PoolRegistry>();Assert.That(registry,Is.Not.Null);
            Assert.That(registry.GetCounts("magazines").Current,Is.EqualTo(1));
            for(var i=0;i<100;i++){Release();GripAt(pouch.transform.position);}
            var held=interactor.HeldToken;var mounted=socket.Installed;
            Assert.That(registry.GetCounts("magazines").Created,Is.LessThanOrEqualTo(10));
            pouch.TickLifetime(20);
            Assert.That(held.HeldBy,Is.SameAs(left));Assert.That(mounted.State,Is.EqualTo(MagazineTokenState.Inserted));
            Assert.That(registry.GetCounts("magazines").Current,Is.EqualTo(2));
            Release();var body=held.GetComponent<Rigidbody>();body.linearVelocity=Vector3.one*5;
            pouch.TickLifetime(20);
            Assert.That(registry.GetCounts("magazines").Current,Is.EqualTo(1));
            Assert.That(held.gameObject.activeSelf,Is.False);Assert.That(body.isKinematic,Is.True);
            GripAt(pouch.transform.position);Assert.That(interactor.HeldToken.State,Is.EqualTo(MagazineTokenState.Fresh));
            interactor.Restart();
            Assert.That(registry.GetCounts("magazines").Current,Is.EqualTo(1));
            Assert.That(registry.GetCounts("magazines").Peak,Is.LessThanOrEqualTo(10));
        }

        [Test] public void RestartRestoresOneMountedMagazineAndClearsOwnership()
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                for (var shot = 0; shot < 13; shot++) gun.Magazine.TryConsume();
                Release(); GripAt(socket.transform.position); Release(); GripAt(pouch.transform.position);
                Assert.That(interactor.HeldToken, Is.Not.Null);
                gun.SetPhase(RunPhase.Paused); pouch.SetSimulationEnabled(false);
                interactor.Restart();
                Assert.That(interactor.Owner, Is.EqualTo(LeftHandOwner.None));
                Assert.That(interactor.HeldToken, Is.Null);
                Assert.That(gun.Magazine.Rounds, Is.EqualTo(30));
                Assert.That(gun.Magazine.Inserted, Is.True);
                Assert.That(socket.Installed.State, Is.EqualTo(MagazineTokenState.Inserted));
                Assert.That(socket.Installed.HeldBy, Is.Null);
                var active = Object.FindObjectsByType<MagazineToken>(FindObjectsSortMode.None);
                Assert.That(active.Length, Is.EqualTo(1), "Only the mounted magazine survives each retry.");
                Assert.That(pouch.TokenCount, Is.EqualTo(2), "The existing pool is reused across retries.");
                pouch.SetSimulationEnabled(true);
                left.position = socket.transform.position;
                interactor.Tick(true, true);
                Assert.That(gun.Magazine.Inserted, Is.True, "A held grip across retry must not immediately eject.");
            }
        }

        [Test]
        public void ReloadSequenceDiscardsAmmoAndRequiresASeparateForegripGrab()
        {
            for (var i = 0; i < 13; i++) gun.Magazine.TryConsume();
            GripAt(foregrip.position);
            Assert.That(interactor.Owner, Is.EqualTo(LeftHandOwner.Foregrip));
            GripAt(socket.transform.position);
            Assert.That(gun.Magazine.Inserted, Is.True, "Holding foregrip cannot also eject.");
            Release(); GripAt(socket.transform.position);
            Assert.That(interactor.Owner, Is.EqualTo(LeftHandOwner.LooseMagazine));
            Assert.That(interactor.HeldToken.State, Is.EqualTo(MagazineTokenState.Discarded));
            Assert.That(gun.Magazine.Rounds, Is.Zero);
            Release(); GripAt(pouch.transform.position);
            Assert.That(interactor.HeldToken.State, Is.EqualTo(MagazineTokenState.Fresh));
            GripAt(socket.transform.position);
            Assert.That(socket.Installed, Is.Not.Null);
            Assert.That(gun.Magazine.Rounds, Is.EqualTo(30));
            Assert.That(interactor.Owner, Is.EqualTo(LeftHandOwner.None));
            GripAt(foregrip.position);
            Assert.That(interactor.Owner, Is.EqualTo(LeftHandOwner.None));
            Release(); GripAt(foregrip.position);
            Assert.That(interactor.Owner, Is.EqualTo(LeftHandOwner.Foregrip));
        }

        [Test]
        public void HeldGripCannotSupplyEveryFrameOrSwitchFromMagazineToForegrip()
        {
            GripAt(pouch.transform.position);
            var token = interactor.HeldToken;
            Assert.That(token, Is.Not.Null);
            for (var i = 0; i < 60; i++) interactor.Tick(true, true);
            Assert.That(pouch.TokenCount, Is.EqualTo(2)); // Initial + one fresh.
            GripAt(foregrip.position);
            Assert.That(interactor.HeldToken, Is.SameAs(token));
            Assert.That(interactor.Owner, Is.EqualTo(LeftHandOwner.LooseMagazine));
        }

        [Test]
        public void DisabledInteractionCannotEjectOrSupplyAndRequiresNewPress()
        {
            left.position = socket.transform.position;
            interactor.Tick(true, false);
            interactor.Tick(true, true);
            Assert.That(gun.Magazine.Inserted, Is.True);
            Release(); interactor.Tick(true, true);
            Assert.That(gun.Magazine.Inserted, Is.False);
        }

        [Test]
        public void PouchIsBoundedAndHeldTokenSurvivesLifetimeCleanup()
        {
            for (var i = 0; i < 30; i++) { Release(); GripAt(pouch.transform.position); }
            var held = interactor.HeldToken;
            Assert.That(held, Is.Not.Null);
            Assert.That(pouch.TokenCount, Is.LessThanOrEqualTo(10));
            pouch.TickLifetime(20);
            Assert.That(held.gameObject.activeSelf, Is.True);
            Assert.That(held.HeldBy, Is.SameAs(left));
            Release(); pouch.TickLifetime(20);
            Assert.That(held.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ReleasedMagazineFallsAndStopsOnSolidFloor()
        {
            var scene = SceneManager.CreateScene("Magazine drop physics", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var hand = new GameObject("Drop hand");
            var tokenObject = new GameObject("Drop token");
            AsyncOperation unload;
            try
            {
                SceneManager.MoveGameObjectToScene(floor, scene);
                SceneManager.MoveGameObjectToScene(hand, scene);
                SceneManager.MoveGameObjectToScene(tokenObject, scene);
                floor.transform.position = Vector3.down * 0.1f;
                floor.transform.localScale = new Vector3(4, 0.2f, 4);
                hand.transform.position = Vector3.up * 2;
                var token = tokenObject.AddComponent<MagazineToken>();
                token.GetComponent<BoxCollider>().size = new Vector3(0.1f, 0.2f, 0.1f);
                token.TryGrab(hand.transform); token.Release(hand.transform);
                Physics.SyncTransforms();
                var physics = scene.GetPhysicsScene();
                for (var i = 0; i < 120; i++) physics.Simulate(1f / 60);
                Assert.That(token.transform.position.y, Is.InRange(0.07f, 0.14f));
                Assert.That(token.State, Is.EqualTo(MagazineTokenState.Fresh));
            }
            finally
            {
                Object.DestroyImmediate(tokenObject); Object.DestroyImmediate(hand); Object.DestroyImmediate(floor);
                unload = SceneManager.UnloadSceneAsync(scene);
            }
            yield return unload;
        }
    }
}
