#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using GaussianSplatting.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace VRFPS.Tests
{
    public class RoomCapturePreviewTests
    {
        const string Folder="Assets/VRFPS/External/RealityGS/Room";
        const string Package="Packages/org.nesnausk.gaussian-splatting/";
        GameObject host,cameraHost;GaussianRealitySource source;GaussianSplatRenderer renderer;Camera camera;
        RenderPipelineAsset originalQuality,originalDefault;
        [SetUp] public void Setup()
        {
            originalQuality=QualitySettings.renderPipeline;originalDefault=GraphicsSettings.defaultRenderPipeline;
            host=new GameObject("GS test");host.SetActive(false);
            renderer=host.AddComponent<GaussianSplatRenderer>();renderer.enabled=false;
            renderer.m_Asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(Folder+"/room.asset");
            Assert.That(renderer.m_Asset,Is.Not.Null,"Run RoomRealitySetup.Import first");
            renderer.m_ShaderSplats=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/RenderGaussianSplats.shader");
            renderer.m_ShaderComposite=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianComposite.shader");
            renderer.m_ShaderDebugPoints=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianDebugRenderPoints.shader");
            renderer.m_ShaderDebugBoxes=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianDebugRenderBoxes.shader");
            renderer.m_CSSplatUtilities=AssetDatabase.LoadAssetAtPath<ComputeShader>(Package+"Shaders/SplatUtilities.compute");
            source=host.AddComponent<GaussianRealitySource>();source.splats=renderer;source.dataId="room-65f2f8c9";
            host.SetActive(true);
            cameraHost=new GameObject("GS camera");camera=cameraHost.AddComponent<Camera>();camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,0,-2);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.nearClipPlane=.05f;camera.farClipPlane=10;
            camera.GetUniversalAdditionalCameraData();
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(host);Object.DestroyImmediate(cameraHost);
            QualitySettings.renderPipeline=originalQuality;GraphicsSettings.defaultRenderPipeline=originalDefault;
        }
        void UseGsPipeline()
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/VRFPS/Configuration/Intro/GsPipeline.asset");
            Assert.That(pipeline,Is.Not.Null);QualitySettings.renderPipeline=pipeline;GraphicsSettings.defaultRenderPipeline=pipeline;
        }
        [UnityTest] public IEnumerator CaptureFourDirectionsAndRelease()
        {
            UseGsPipeline();
            host.transform.rotation=Quaternion.Euler(180,0,0);
            host.transform.position=new Vector3(0,.85f,0);
            camera.transform.position=new Vector3(.3f,1.6f,0);
            camera.fieldOfView=85;camera.aspect=1.5f;camera.farClipPlane=30;
            yield return source.Initialize();
            Assert.That(source.IsReady,Is.True);
            var output=System.IO.Path.GetFullPath("../tmp/p13/room-preview-frames");
            System.IO.Directory.CreateDirectory(output);
            var rt=new RenderTexture(1200,800,24);rt.Create();
            var picture=new Texture2D(1200,800,TextureFormat.RGB24,false);
            try
            {
                for(var i=0;i<4;i++)
                {
                    camera.transform.rotation=Quaternion.Euler(0,i*90,0);
                    yield return null;
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                    var old=RenderTexture.active;
                    try { RenderTexture.active=rt;picture.ReadPixels(new Rect(0,0,1200,800),0,0);picture.Apply(); }
                    finally { RenderTexture.active=old; }
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,"room-view-"+i+".png"),picture.EncodeToPNG());
                }
            }
            finally { rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(picture); }
            yield return source.Release();
            Assert.That(source.HasResources,Is.False);
        }
    }
}
#endif
