#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GaussianSplatting.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace VRFPS.Tests
{
    public class RoomMirrorContractTests
    {
        Scene scene;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator ReflectedRoomOwnsTexturedWardrobeAndPositiveObservationAnchor()
        {
            scene = EditorSceneManager.LoadSceneInPlayMode("Assets/VRFPS/External/RealityGS/Room/RoomReality.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            var root = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<RealitySceneRoot>()).Single();
            var renderer = root.GetComponentInChildren<GaussianSplatRenderer>();
            Assert.That(renderer.transform.localToWorldMatrix.determinant, Is.LessThan(0), "GS must be horizontally reflected");
            Assert.That(renderer.transform.TransformPoint(Vector3.right).x, Is.EqualTo(-1).Within(.001f));
            root.Validate(AssetDatabase.LoadAssetAtPath<RealityIntroProfile>("Assets/VRFPS/External/RealityGS/Room/RoomIntro.asset"));
            Assert.That(root.observationAnchor.lossyScale, Is.EqualTo(Vector3.one));
            Assert.That(root.observationAnchor.rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(root.observationAnchor.position.x, Is.EqualTo(-.55f).Within(.001f));
            Assert.That(root.virus.transform.position.x, Is.EqualTo(root.observationAnchor.position.x).Within(.001f));
            Assert.That(root.collisionRoot.Find("Bed").position.x, Is.GreaterThan(0));
            var patch = root.transform.Find("FBX Wardrobe Repair"); Assert.That(patch, Is.Not.Null);
            Assert.That(Vector3.Distance(patch.localPosition, new Vector3(.383f, -.126f, .212f)), Is.LessThan(.0001f), "User Inspector position");
            Assert.That(Vector3.Distance(patch.Find("Wardrobe replacement mask").localPosition, new Vector3(2.35f, 1.25f, -.3f)), Is.LessThan(.0001f), "Mask moves with the facade");
            var mesh = patch.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.triangles.Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount));
            var material = patch.GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(material.mainTexture, Is.Not.Null);
            Assert.That(material.mainTexture.width, Is.GreaterThanOrEqualTo(1024));
            Assert.That(patch.GetComponentsInChildren<Collider>(), Is.Empty);
            Assert.That(renderer.m_Cutouts.Any(c => c.name == "Wardrobe replacement mask" && c.m_Invert), Is.True);
            Physics.SyncTransforms();
            var visual = patch.GetComponent<MeshRenderer>().bounds;
            var solid = root.collisionRoot.Find("FBX Wardrobe solid").GetComponent<BoxCollider>();
            Assert.That(solid.bounds.min.x, Is.EqualTo(visual.min.x).Within(.001f), "Stop movement at the visible cabinet front");
            Assert.That(solid.bounds.min.y, Is.EqualTo(visual.min.y).Within(.001f));
            Assert.That(solid.bounds.max.y, Is.EqualTo(visual.max.y).Within(.001f));
            Assert.That(solid.bounds.min.z, Is.EqualTo(visual.min.z).Within(.001f));
            Assert.That(solid.bounds.max.z, Is.EqualTo(visual.max.z).Within(.001f));
            Assert.That(Physics.Raycast(new Vector3(0, 1, visual.center.z), Vector3.right, out var nearest, 4, LayerMask.GetMask("WorldSolid")), Is.True);
            Assert.That(nearest.point.x, Is.EqualTo(visual.min.x).Within(.001f), "No old wall proxy may block the relocated wardrobe front");
            foreach (var origin in new[] { new Vector3(0, 1, -1.7f), new Vector3(0, 2.3f, 2), new Vector3(0, 2.3f, visual.center.z) })
            {
                Assert.That(Physics.Raycast(origin, Vector3.right, out var wallHit, 4, LayerMask.GetMask("WorldSolid")), Is.True);
                Assert.That(wallHit.point.x, Is.EqualTo(2.19f).Within(.001f), "Retain wall collision outside the cabinet span");
            }
        }
    }
}
#endif
