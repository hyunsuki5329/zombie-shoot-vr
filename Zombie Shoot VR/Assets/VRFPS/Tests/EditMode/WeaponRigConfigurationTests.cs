using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRFPS.Tests
{
    public class WeaponRigConfigurationTests
    {
        [Test]
        public void GunHasTrackedAnchorsDedicatedMasksAndOppositeHandInput()
        {
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/PlayerRig.prefab");
            var gun = rig.GetComponentInChildren<K2Controller>(true);
            Assert.That(gun, Is.Not.Null);
            var settings = new SerializedObject(gun);
            Assert.That(settings.FindProperty("rightAnchor").objectReferenceValue.name, Is.EqualTo("Right Controller"));
            Assert.That(settings.FindProperty("leftAnchor").objectReferenceValue.name, Is.EqualTo("Left Controller"));
            Assert.That(settings.FindProperty("muzzle").objectReferenceValue.name, Is.EqualTo("Muzzle"));
            Assert.That(settings.FindProperty("worldSolidMask").intValue, Is.EqualTo(1 << LayerMask.NameToLayer("WorldSolid")));
            var mask = settings.FindProperty("shotMask").intValue;
            Assert.That(mask & (1 << LayerMask.NameToLayer("Enemy")), Is.Not.Zero);
            Assert.That(mask & (1 << LayerMask.NameToLayer("Weapon")), Is.Zero);
            Assert.That(mask & (1 << LayerMask.NameToLayer("PlayerBody")), Is.Zero);
            var input = new SerializedObject(gun.GetComponent<K2HandInput>());
            Assert.That(input.FindProperty("gun").objectReferenceValue, Is.EqualTo(gun));
            Assert.That(input.FindProperty("left").objectReferenceValue.name, Is.EqualTo("Left Controller"));
            Assert.That(input.FindProperty("foregrip").objectReferenceValue.name, Is.EqualTo("Foregrip"));
            var grip = (InputActionReference)input.FindProperty("gripAction").objectReferenceValue;
            var trigger = (InputActionReference)input.FindProperty("triggerAction").objectReferenceValue;
            Assert.That(grip.action.actionMap.name + "/" + grip.action.name, Is.EqualTo("XRI Left Interaction/Select"));
            Assert.That(trigger.action.actionMap.name + "/" + trigger.action.name, Is.EqualTo("XRI Right Interaction/Activate"));
        }
    }
}
