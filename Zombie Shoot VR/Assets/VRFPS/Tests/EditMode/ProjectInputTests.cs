using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;

namespace VRFPS.Tests
{
    public class ProjectInputTests
    {
        const string Actions = "Assets/VRFPS/Input/VRFPS.inputactions";

        [TestCase("Assets/VRFPS/Scenes/Combat.unity")]
        [TestCase("Assets/VRFPS/Scenes/Tests/InteractionLab.unity")]
        public void PlayableSceneEnablesProjectOwnedActions(string path)
        {
            if (!Application.isBatchMode && Enumerable.Range(0, SceneManager.sceneCount)
                .Any(i => SceneManager.GetSceneAt(i).isDirty))
                Assert.Ignore("Save all open scenes before running scene-loading tests.");
            var original = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            try
            {
                var managers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<InputActionManager>(true)).ToArray();
                Assert.That(managers.Length, Is.EqualTo(1));
                Assert.That(managers[0].actionAssets.Count, Is.EqualTo(1));
                Assert.That(AssetDatabase.GetAssetPath(managers[0].actionAssets[0]), Is.EqualTo(Actions));
            }
            finally
            {
                if (original.Length > 0 && original.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(original);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void PlayerRigEnablesProjectActionsAndHasNoSampleActionReferences()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Actions);
            Assert.That(actions, Is.Not.Null, "The game must own its input configuration.");
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/PlayerRig.prefab");
            Assert.That(rig, Is.Not.Null);
            var manager = rig.GetComponentInChildren<InputActionManager>(true);
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.actionAssets, Does.Contain(actions));
            var referenceCount = 0;
            foreach (var component in rig.GetComponentsInChildren<Component>(true))
            {
                Assert.That(component, Is.Not.Null, "Missing script on the player rig.");
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.objectReferenceValue is InputActionReference reference)
                    {
                        Assert.That(reference.action, Is.Not.Null, component.name + ": " + property.propertyPath);
                        Assert.That(AssetDatabase.GetAssetPath(reference), Is.EqualTo(Actions));
                        referenceCount++;
                    }
                }
            }
            Assert.That(referenceCount, Is.GreaterThan(0), "The rig must use actual action references.");
            Assert.That(actions.FindAction("XRI Left Locomotion/Move", true).bindings.Count, Is.GreaterThan(0));
            Assert.That(actions.FindAction("XRI Right Locomotion/Turn", true).bindings.Count, Is.GreaterThan(0));
        }
    }
}
