using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace VRFPS.Tests
{
    public class ReloadRigConfigurationTests
    {
        [Test]
        public void SavedRigConnectsOneOwnerSocketInitialMagazineAndWaistSupply()
        {
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/PlayerRig.prefab");
            Assert.That(rig.GetComponentsInChildren<LeftHandInteractor>(true).Length, Is.EqualTo(1));
            var owner = rig.GetComponent<LeftHandInteractor>();
            var fields = new SerializedObject(owner);
            foreach (var name in new[] { "gun", "left", "foregrip", "socket", "pouch", "initial" })
                Assert.That(fields.FindProperty(name).objectReferenceValue, Is.Not.Null, name);
            var gun = (K2Controller)fields.FindProperty("gun").objectReferenceValue;
            var input = new SerializedObject(gun.GetComponent<K2HandInput>());
            Assert.That(input.FindProperty("leftHandOwnership").objectReferenceValue, Is.EqualTo(owner));
            var token = (MagazineToken)fields.FindProperty("initial").objectReferenceValue;
            Assert.That(token.transform.parent.GetComponent<MagazineSocket>(), Is.Not.Null);
            Assert.That(token.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Magazine")));
            var pouch = new SerializedObject((AmmoPouch)fields.FindProperty("pouch").objectReferenceValue);
            Assert.That(pouch.FindProperty("prefab").objectReferenceValue, Is.Not.Null);
            Assert.That(pouch.FindProperty("head").objectReferenceValue, Is.Not.Null);
            Assert.That(pouch.FindProperty("leftHand").objectReferenceValue, Is.EqualTo(fields.FindProperty("left").objectReferenceValue));
            Assert.That(gun.transform.Find("Magazine (placeholder)").gameObject.activeSelf, Is.False);
        }
    }
}
