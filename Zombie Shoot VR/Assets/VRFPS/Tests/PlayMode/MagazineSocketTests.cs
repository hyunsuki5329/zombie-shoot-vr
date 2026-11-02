using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VRFPS.Tests
{
    public class MagazineSocketTests
    {
        readonly List<GameObject> objects = new List<GameObject>();
        MagazineSocket socket;
        MagazineState ammo;
        Transform left, right;
        MagazineToken initial;

        GameObject Create(string name)
        {
            var item = new GameObject(name); objects.Add(item); return item;
        }

        MagazineToken Fresh() => Create("Magazine").AddComponent<MagazineToken>();

        [SetUp]
        public void SetUp()
        {
            left = Create("Left hand").transform;
            right = Create("Right hand").transform;
            socket = Create("Socket").AddComponent<MagazineSocket>();
            initial = Fresh(); ammo = new MagazineState(30);
            socket.Initialize(ammo, left, initial);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in objects) if (item != null) Object.DestroyImmediate(item);
            objects.Clear();
        }

        [Test]
        public void InitialFreshTokenRejectsPartiallyConsumedAmmoWithoutMutatingToken()
        {
            var usedAmmo = new MagazineState(30);
            usedAmmo.TryConsume();
            var newToken = Fresh();
            var otherSocket = Create("Other socket").AddComponent<MagazineSocket>();
            Assert.Throws<System.ArgumentException>(() => otherSocket.Initialize(usedAmmo, left, newToken));
            Assert.That(newToken.State, Is.EqualTo(MagazineTokenState.Fresh));
            Assert.That(otherSocket.Installed, Is.Null);
        }

        [Test]
        public void EjectionDiscardsPartialAmmoAndOnlyLeftHandCanRemove()
        {
            Assert.That(initial.State, Is.EqualTo(MagazineTokenState.Inserted));
            Assert.That(socket.TryEject(right), Is.Null);
            for (var i = 0; i < 13; i++) ammo.TryConsume();
            Assert.That(socket.TryEject(left), Is.SameAs(initial));
            Assert.That(initial.State, Is.EqualTo(MagazineTokenState.Discarded));
            Assert.That(initial.HeldBy, Is.SameAs(left));
            Assert.That(ammo.Rounds, Is.Zero);
            Assert.That(ammo.Inserted, Is.False);
            Assert.That(socket.Installed, Is.Null);
            Assert.That(socket.TryInsert(initial), Is.False);
            Assert.That(initial.Release(left), Is.True);
            Assert.That(initial.GetComponent<Rigidbody>().isKinematic, Is.False);
        }

        [Test]
        public void FreshLeftHeldMagazineInSocketInsertsOnce()
        {
            socket.TryEject(left); initial.Release(left);
            var fresh = Fresh();
            Assert.That(fresh.TryGrab(left), Is.True);
            Assert.That(socket.TryInsert(fresh), Is.True);
            Assert.That(socket.Installed, Is.SameAs(fresh));
            Assert.That(fresh.State, Is.EqualTo(MagazineTokenState.Inserted));
            Assert.That(fresh.HeldBy, Is.Null);
            Assert.That(fresh.transform.parent, Is.SameAs(socket.transform));
            Assert.That(fresh.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(ammo.Rounds, Is.EqualTo(30));
            Assert.That(socket.TryInsert(fresh), Is.False);
        }

        [TestCase(0.121f, 0f)]
        [TestCase(0f, 36f)]
        public void MisalignedOrDistantMagazineCannotInsert(float distance, float angle)
        {
            socket.TryEject(left); initial.Release(left);
            var fresh = Fresh(); fresh.TryGrab(left);
            left.position = Vector3.right * distance;
            left.rotation = Quaternion.Euler(0, angle, 0);
            Assert.That(socket.TryInsert(fresh), Is.False);
            Assert.That(ammo.Inserted, Is.False);
            Assert.That(fresh.State, Is.EqualTo(MagazineTokenState.Fresh));
        }

        [Test]
        public void UnheldOrRightHeldTokenCannotInsertAndOwnershipCannotBeStolen()
        {
            socket.TryEject(left); initial.Release(left);
            var fresh = Fresh();
            Assert.That(socket.TryInsert(fresh), Is.False);
            Assert.That(fresh.TryGrab(right), Is.True);
            Assert.That(fresh.TryGrab(left), Is.False);
            Assert.That(fresh.Release(left), Is.False);
            Assert.That(socket.TryInsert(fresh), Is.False);
            Assert.That(fresh.HeldBy, Is.SameAs(right));
        }

        [Test]
        public void DroppedFreshMagazineCanBeRecoveredButDiscardedMagazineCannotBeReused()
        {
            socket.TryEject(left); initial.Release(left);
            Assert.That(initial.TryGrab(left), Is.True);
            Assert.That(socket.TryInsert(initial), Is.False);
            initial.Release(left);
            var fresh = Fresh(); fresh.TryGrab(left); fresh.Release(left);
            Assert.That(fresh.State, Is.EqualTo(MagazineTokenState.Fresh));
            Assert.That(fresh.TryGrab(left), Is.True);
            Assert.That(socket.TryInsert(fresh), Is.True);
        }
    }
}
