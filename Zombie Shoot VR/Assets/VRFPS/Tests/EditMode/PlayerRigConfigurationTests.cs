using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using VRFPS.Player;

namespace VRFPS.Tests
{
    public class PlayerRigConfigurationTests
    {
        [Test]
        public void ProjectRigUsesOnlyProjectLocomotionWithSeparateSticks()
        {
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/PlayerRig.prefab");
            var controller = rig.GetComponent<PlayerRigController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.enabled, Is.True);
            var settings = new SerializedObject(controller);
            Assert.That(settings.FindProperty("head").objectReferenceValue, Is.Not.Null);
            var move = (InputActionReference)settings.FindProperty("moveAction").objectReferenceValue;
            var turn = (InputActionReference)settings.FindProperty("turnAction").objectReferenceValue;
            Assert.That(move.action.actionMap.name + "/" + move.action.name, Is.EqualTo("XRI Left Locomotion/Move"));
            Assert.That(turn.action.actionMap.name + "/" + turn.action.name, Is.EqualTo("XRI Right Locomotion/Turn"));
            foreach (var provider in rig.GetComponentsInChildren<LocomotionProvider>(true))
                Assert.That(provider.enabled, Is.False, provider.GetType().Name + " would compete for movement.");
        }
    }
}
