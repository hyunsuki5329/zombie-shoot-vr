using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.XR.CoreUtils;
using System.Linq;

namespace VRFPS.Tests
{
    public class EnvironmentTests
    {
        const string Lab = "Assets/VRFPS/Scenes/Tests/InteractionLab.unity";

        [Test]
        public void InteractionLabProvidesTrackedCameraAndSolidScaleReference()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(Lab), Is.Not.Null,
                "A dedicated interaction lab must exist before hardware testing.");
            if (!Application.isBatchMode && Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                .Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
                Assert.Ignore("Save all open scenes before running this scene-loading test interactively.");
            var original = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.OpenScene(Lab, OpenSceneMode.Single);
            try
            {
                XROrigin origin = null;
                GameObject scaleReference = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.GetComponentInChildren<XROrigin>() != null)
                        origin = root.GetComponentInChildren<XROrigin>();
                    if (root.name == "One Metre Cube") scaleReference = root;
                }
                Assert.That(origin, Is.Not.Null);
                Assert.That(origin.Camera, Is.Not.Null);
                Assert.That(origin.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Left Controller"), Is.True);
                Assert.That(origin.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Right Controller"), Is.True);
                Assert.That(scaleReference, Is.Not.Null);
                Assert.That(scaleReference.GetComponent<BoxCollider>(), Is.Not.Null);
                Assert.That(scaleReference.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                if (original.Length > 0 && original.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(original);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [TestCase("Assets/VRFPS/Scenes/Bootstrap.unity")]
        [TestCase("Assets/VRFPS/Scenes/Combat.unity")]
        public void SharedSceneCanBeLoaded(string path)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null);
        }
    }
}
