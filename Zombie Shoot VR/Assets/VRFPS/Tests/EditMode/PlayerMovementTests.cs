using NUnit.Framework;
using UnityEngine;
using VRFPS.Player;

namespace VRFPS.Tests
{
    public class PlayerMovementTests
    {
        [TestCase(75f)]
        [TestCase(-75f)]
        [TestCase(0f)]
        public void LookingUpOrDownDoesNotChangeGroundSpeed(float pitch)
        {
            var forward = Quaternion.Euler(pitch, 30, 0) * Vector3.forward;
            var movement = PlayerRigController.GetPlanarMove(forward, Vector2.up);
            Assert.That(movement.y, Is.EqualTo(0).Within(0.0001f));
            Assert.That(movement.magnitude, Is.EqualTo(1).Within(0.0001f));
            Assert.That(Vector3.Angle(movement, Quaternion.Euler(0, 30, 0) * Vector3.forward), Is.LessThan(0.01f));
        }

        [Test]
        public void DiagonalInputCannotExceedFullSpeed()
        {
            Assert.That(PlayerRigController.GetPlanarMove(Vector3.forward, Vector2.one).magnitude,
                Is.EqualTo(1).Within(0.0001f));
            Assert.That(PlayerRigController.GetPlanarMove(Vector3.forward, Vector2.up * 0.4f).z,
                Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void VerticalHeadDirectionCannotIntroduceInvalidMovement()
        {
            Assert.That(PlayerRigController.GetPlanarMove(Vector3.up, Vector2.one), Is.EqualTo(Vector3.zero));
        }
    }
}
