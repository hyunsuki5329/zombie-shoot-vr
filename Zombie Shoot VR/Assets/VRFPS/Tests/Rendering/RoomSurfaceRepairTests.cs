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
    public class RoomSurfaceRepairTests
    {
        [UnityTest] public IEnumerator AuthoredRepairsToggleRenderAndRelease()
        {
            const string path="Assets/VRFPS/External/RealityGS/Room/RoomReality.unity";
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
                var group=root.transform.Find("Room Surface Repairs");Assert.That(group,Is.Not.Null,"Room repairs have not been authored");
                var cuts=group.GetComponentsInChildren<GaussianCutout>(true);
                Assert.That(cuts.Length,Is.EqualTo(4));Assert.That(cuts.All(x=>x.m_Invert),Is.True,"Remove inside volumes, not outside");
                Assert.That(group.GetComponentsInChildren<Collider>(true),Is.Empty);
                Assert.That(group.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(3));
                Assert.That(group.Find("Wardrobe side backing"),Is.Null);
                Assert.That(group.Find("Wardrobe side noise"),Is.Null);
                var overhead=group.Find("Wardrobe overhead noise");Assert.That(overhead,Is.Not.Null);
                Assert.That(overhead.position.y-overhead.lossyScale.y,Is.GreaterThanOrEqualTo(2.69f),"Retain wardrobe detail below overhead cleanup");
                var renderer=root.GetComponentInChildren<GaussianSplatRenderer>();
                Assert.That(cuts.All(x=>renderer.m_Cutouts.Contains(x)),Is.True);
                source=root.GetComponentInChildren<GaussianRealitySource>();
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/VRFPS/Configuration/Intro/GsPipeline.asset");
                QualitySettings.renderPipeline=pipeline;GraphicsSettings.defaultRenderPipeline=pipeline;
                foreach(var previous in previousMain)previous.tag="Untagged";
                var camera=cameraObject.AddComponent<Camera>();camera.tag="MainCamera";camera.GetUniversalAdditionalCameraData();
                Assert.That(Camera.main,Is.SameAs(camera));
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                camera.transform.position=new Vector3(.3f,1.6f,0);camera.fieldOfView=85;camera.aspect=1.5f;camera.nearClipPlane=.05f;camera.farClipPlane=30;
                yield return source.Initialize();Assert.That(source.IsReady,Is.True);
                var output=Path.GetFullPath("../tmp/p13/room-surface-repair");Directory.CreateDirectory(output);rt.Create();
                for(var i=0;i<8;i++)
                {
                    group.gameObject.SetActive(i>=4);
                    Assert.That(cuts.All(x=>x.isActiveAndEnabled==(i>=4)),Is.True);
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
