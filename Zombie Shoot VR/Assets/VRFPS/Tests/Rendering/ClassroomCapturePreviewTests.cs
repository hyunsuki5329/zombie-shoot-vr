#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using GaussianSplatting.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace VRFPS.Tests
{
    public class ClassroomCapturePreviewTests
    {
        [UnityTest] public IEnumerator ClassroomRectangleRendersAndReleases()
        {
            const string path="Assets/VRFPS/External/RealityGS/Classroom/ClassroomReality.unity";
            var previousQuality=QualitySettings.renderPipeline;var previousDefault=GraphicsSettings.defaultRenderPipeline;
            var scene=EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Additive));
            var cameraObject=new GameObject("Repair preview camera");
            var rt=new RenderTexture(1200,800,24);var picture=new Texture2D(1200,800,TextureFormat.RGB24,false);
            GaussianRealitySource source=null;AsyncOperation unload=null;
            var previousMain=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(x=>x.CompareTag("MainCamera")).ToArray();
            try
            {
                yield return null;
                var root=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<RealitySceneRoot>()).Single();
                root.virus.gameObject.SetActive(false); // Keep inspection views unobstructed; scene asset remains unchanged.
                var renderer=root.GetComponentInChildren<GaussianSplatRenderer>();
                Assert.That(renderer.m_Asset.splatCount,Is.EqualTo(459968));
                Assert.That(renderer.m_Cutouts.Length,Is.EqualTo(1));
                Assert.That(renderer.m_Cutouts[0].m_Invert,Is.False);
                Assert.That(renderer.transform.localToWorldMatrix.determinant,Is.LessThan(0),"FBX handedness correction");
                source=root.GetComponentInChildren<GaussianRealitySource>();
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/VRFPS/Configuration/Intro/GsPipeline.asset");
                QualitySettings.renderPipeline=pipeline;GraphicsSettings.defaultRenderPipeline=pipeline;
                foreach(var previous in previousMain)previous.tag="Untagged";
                var camera=cameraObject.AddComponent<Camera>();camera.tag="MainCamera";camera.GetUniversalAdditionalCameraData();
                Assert.That(Camera.main,Is.SameAs(camera));
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                camera.transform.position=new Vector3(-3.8f,1.6f,1.65f);camera.fieldOfView=85;camera.aspect=1.5f;camera.nearClipPlane=.05f;camera.farClipPlane=30;
                yield return source.Initialize();Assert.That(source.IsReady,Is.True);
                var output=Path.GetFullPath("../tmp/p14/hybrid-preview");Directory.CreateDirectory(output);rt.Create();
                for(var i=0;i<8;i++)
                {
                    renderer.m_Cutouts[0].gameObject.SetActive(true);
                    var hybrid = root.transform.Find("FBX gap repairs");
                    Assert.That(hybrid, Is.Not.Null);
                    Assert.That(hybrid.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
                    hybrid.gameObject.SetActive(i >= 4);
                    foreach(var surface in root.collisionRoot.GetComponentsInChildren<MeshRenderer>())surface.enabled=true;
                    camera.transform.rotation=Quaternion.Euler(0,(i%4)*90,0);yield return null;
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                    var old=RenderTexture.active;
                    try {RenderTexture.active=rt;picture.ReadPixels(new Rect(0,0,1200,800),0,0);picture.Apply();}
                    finally {RenderTexture.active=old;}
                    File.WriteAllBytes(Path.Combine(output,(i<4?"before-":"after-")+(i%4)+".png"),picture.EncodeToPNG());
                }
                yield return source.Release();Assert.That(source.HasResources,Is.False);
            }
            finally
            {
                if(source)source.enabled=false;
                foreach(var previous in previousMain)if(previous)previous.tag="MainCamera";
                Object.DestroyImmediate(cameraObject);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(picture);
                QualitySettings.renderPipeline=previousQuality;GraphicsSettings.defaultRenderPipeline=previousDefault;
                unload=SceneManager.UnloadSceneAsync(scene);
            }
            yield return unload;
        }
    }
}
#endif
