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
    public class GaussianRealitySourceTests
    {
        const string Folder="Assets/VRFPS/External/RealityGS/SyntheticFixture";
        const string Package="Packages/org.nesnausk.gaussian-splatting/";
        GameObject host,cameraHost;GaussianRealitySource source;GaussianSplatRenderer renderer;Camera camera;
        RenderPipelineAsset originalQuality,originalDefault;
        [SetUp] public void Setup()
        {
            originalQuality=QualitySettings.renderPipeline;originalDefault=GraphicsSettings.defaultRenderPipeline;
            host=new GameObject("GS test");host.SetActive(false);
            renderer=host.AddComponent<GaussianSplatRenderer>();renderer.enabled=false;
            renderer.m_Asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(Folder+"/synthetic.asset");
            Assert.That(renderer.m_Asset,Is.Not.Null,"Run GaussianRealityFixtureSetup.Create first");
            renderer.m_ShaderSplats=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/RenderGaussianSplats.shader");
            renderer.m_ShaderComposite=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianComposite.shader");
            renderer.m_ShaderDebugPoints=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianDebugRenderPoints.shader");
            renderer.m_ShaderDebugBoxes=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianDebugRenderBoxes.shader");
            renderer.m_CSSplatUtilities=AssetDatabase.LoadAssetAtPath<ComputeShader>(Package+"Shaders/SplatUtilities.compute");
            source=host.AddComponent<GaussianRealitySource>();source.splats=renderer;source.dataId="synthetic-256";
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
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder+"/Pipeline.asset");
            Assert.That(pipeline,Is.Not.Null);QualitySettings.renderPipeline=pipeline;GraphicsSettings.defaultRenderPipeline=pipeline;
        }
        [Test] public void MissingDataIsRejectedWithoutResourceAllocation()
        {
            renderer.m_Asset=null;
            Assert.Throws<InvalidOperationException>(()=>source.Initialize().MoveNext());
            Assert.That(source.IsReady,Is.False);Assert.That(source.HasResources,Is.False);
        }
        [Test] public void ExistingGamePipelineWithoutGsFeatureIsRejected()
        {
            Assert.Throws<InvalidOperationException>(()=>source.Initialize().MoveNext());
            Assert.That(source.HasResources,Is.False);
        }
        [TestCase("shader")][TestCase("compute")][TestCase("feature")][TestCase("MSAA")][TestCase("camera")]
        public void IncompleteConfigurationDoesNotAllocate(string missing)
        {
            UseGsPipeline();
            var pipeline=(UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
            var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Folder+"/Renderer.asset");
            try
            {
                if(missing=="shader")renderer.m_ShaderSplats=null;
                if(missing=="compute")renderer.m_CSSplatUtilities=null;
                if(missing=="feature")data.rendererFeatures[0].SetActive(false);
                if(missing=="MSAA")pipeline.msaaSampleCount=4;
                if(missing=="camera")camera.tag="Untagged";
                Assert.Throws<InvalidOperationException>(()=>source.Initialize().MoveNext());
                Assert.That(source.HasResources,Is.False);Assert.That(renderer.enabled,Is.False);
            }
            finally {data.rendererFeatures[0].SetActive(true);pipeline.msaaSampleCount=1;}
        }
        [UnityTest] public IEnumerator DisableReleasesResourcesAndReenableDoesNotAutostart()
        {
            UseGsPipeline();yield return null;yield return null;
            yield return source.Release();yield return source.Initialize();
            Assert.Throws<InvalidOperationException>(()=>source.Initialize().MoveNext());
            source.enabled=false;
            Assert.That(source.HasResources,Is.False);Assert.That(source.IsReady,Is.False);AssertReleasedIndependently();
            source.enabled=true;yield return null;
            Assert.That(source.HasResources,Is.False);Assert.That(source.IsReady,Is.False);
            yield return source.Initialize();Assert.That(source.IsReady,Is.True);
            renderer.m_Asset=null;Assert.That(source.IsReady,Is.False);
            yield return source.Release();AssertReleasedIndependently();
        }
        [UnityTest] public IEnumerator ForeignRendererIsRejectedAndNeverReleased()
        {
            UseGsPipeline();yield return null;yield return null;
            var owner=new GameObject("Foreign GS owner");
            var foreign=Object.Instantiate(host,owner.transform);
            // Strip the copied source so the foreign renderer has an independent owner.
            Object.DestroyImmediate(foreign.GetComponent<GaussianRealitySource>());
            var foreignRenderer=foreign.GetComponent<GaussianSplatRenderer>();foreignRenderer.enabled=true;
            try
            {
                source.splats=foreignRenderer;
                Assert.Throws<InvalidOperationException>(()=>source.Initialize().MoveNext());
                yield return source.Release();source.enabled=false;
                Assert.That(foreignRenderer.enabled,Is.True);Assert.That(foreignRenderer.HasValidRenderSetup,Is.True);
            }
            finally {source.splats=renderer;Object.DestroyImmediate(owner);}
        }
        [UnityTest] public IEnumerator FailedSorterInitializationCanRetryAfterCleanup()
        {
            UseGsPipeline();yield return null;yield return null;
            // A failed device/kernel setup leaves this exact upstream state: a non-null
            // cached sorter whose Valid flag is false. Inject it without GPU error logs.
            var invalid=(GpuSorting)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GpuSorting));
            Assert.That(invalid.Valid,Is.False);
            typeof(GaussianSplatRenderer).GetField("m_Sorter",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(renderer,invalid);
            Assert.Throws<InvalidOperationException>(()=>source.Initialize().MoveNext());
            Assert.That(source.HasResources,Is.False);AssertReleasedIndependently();
            yield return source.Initialize();Assert.That(source.IsReady,Is.True);
            yield return source.Release();AssertReleasedIndependently();
        }
        [UnityTest] public IEnumerator MissingRequiredGpuResourceNeverReportsReady()
        {
            UseGsPipeline();yield return null;yield return null;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
            foreach(var name in new[]{"m_GpuView","m_GpuIndexBuffer","m_GpuSortDistances","m_GpuSortKeys","m_GpuSHData","m_GpuPosData","m_GpuOtherData","m_GpuChunks",
                "altBuffer","altPayloadBuffer","passHistBuffer","globalHistBuffer","m_GpuColorData","m_MatSplats","m_MatComposite","m_MatDebugPoints","m_MatDebugBoxes","disposedView"})
            {
                yield return source.Initialize();Assert.That(source.IsReady,Is.True,name);
                if(name.EndsWith("Buffer") && !name.StartsWith("m_"))
                {
                    var field=typeof(GaussianSplatRenderer).GetField("m_SorterArgs",flags);
                    var args=(GpuSorting.Args)field.GetValue(renderer);object resources=args.resources;
                    var scratch=typeof(GpuSorting.SupportResources).GetField(name);
                    ((GraphicsBuffer)scratch.GetValue(resources)).Dispose();scratch.SetValue(resources,null);
                    args.resources=(GpuSorting.SupportResources)resources;field.SetValue(renderer,args);
                }
                else
                {
                    var field=typeof(GaussianSplatRenderer).GetField(name=="disposedView"?"m_GpuView":name,flags);
                    if(field.GetValue(renderer) is GraphicsBuffer buffer)buffer.Dispose();
                    else Object.DestroyImmediate((Object)field.GetValue(renderer));
                    if(name!="disposedView")field.SetValue(renderer,null);
                }
                Assert.That(source.IsReady,Is.False,"Missing/invalid "+name);
                yield return source.Release();AssertReleasedIndependently();
            }
        }
        [UnityTest] public IEnumerator OpaqueGeometryOccludesGsAndFadeCoversBoth()
        {
            UseGsPipeline();yield return null;yield return null;yield return source.Initialize();yield return null;
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.position=new Vector3(0,0,-.5f);cube.transform.localScale=new Vector3(3,3,.1f);
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",Color.green);
            cube.GetComponent<Renderer>().sharedMaterial=material;
            var fade=cameraHost.AddComponent<VrScreenFade>();fade.Configure(camera,Shader.Find("VRFPS/IntroScreenFade"));
            var target=new RenderTexture(128,128,24);target.Create();var image=new Texture2D(128,128,TextureFormat.RGB24,false);
            try
            {
                foreach(var state in new[]{0,1,2})
                {
                    cube.SetActive(state!=2);fade.SetImmediate(state==1?1:0);
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    var old=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=old;
                    var pixels=image.GetPixels32();
                    if(state==0) {Assert.That(pixels.Count(p=>p.g>40),Is.GreaterThan(1000));Assert.That(pixels.Count(p=>p.r>40),Is.Zero,"Opaque depth must occlude GS");}
                    if(state==1)Assert.That(pixels.Count(p=>p.r>3||p.g>3||p.b>3),Is.Zero,"Fade must cover opaque and GS");
                    if(state==2)Assert.That(pixels.Count(p=>p.r>40),Is.GreaterThan(100),"GS visible after restore");
                }
                yield return source.Release();
            }
            finally {Object.DestroyImmediate(cube);Object.DestroyImmediate(material);target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
        [UnityTest] public IEnumerator TenGpuLifecyclesRenderAndReleaseAllOwnedResources()
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.EqualTo(GraphicsDeviceType.Direct3D12),"Run with -force-d3d12; XR is separate");
            UseGsPipeline();yield return null;yield return null;
            var target=new RenderTexture(128,128,24);target.Create();var image=new Texture2D(128,128,TextureFormat.RGB24,false);
            try
            {
                for(var i=0;i<10;i++)
                {
                    yield return source.Initialize();Assert.That(source.IsReady,Is.True);Assert.That(source.HasResources,Is.True);
                    yield return null;
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    var old=RenderTexture.active;RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=old;
                    Assert.That(image.GetPixels32().Count(p=>p.r>40),Is.GreaterThan(100),"Actual GS pixels, cycle "+i);
                    yield return source.Release();yield return source.Release();
                    Assert.That(source.IsReady,Is.False);Assert.That(source.HasResources,Is.False);
                    AssertReleasedIndependently();
                }
            }
            finally {target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
        void AssertReleasedIndependently()
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
            var fields=typeof(GaussianSplatRenderer).GetFields(flags);
            Assert.That(fields.Count(f=>f.FieldType==typeof(GraphicsBuffer)),Is.EqualTo(15));
            foreach(var field in fields.Where(f=>f.FieldType==typeof(GraphicsBuffer)))Assert.That(field.GetValue(renderer),Is.Null,field.Name);
            var args=(GpuSorting.Args)typeof(GaussianSplatRenderer).GetField("m_SorterArgs",flags).GetValue(renderer);
            Assert.That(args.resources.altBuffer,Is.Null);Assert.That(args.resources.altPayloadBuffer,Is.Null);
            Assert.That(args.resources.passHistBuffer,Is.Null);Assert.That(args.resources.globalHistBuffer,Is.Null);
            foreach(var name in new[]{"m_GpuColorData","m_MatSplats","m_MatComposite","m_MatDebugPoints","m_MatDebugBoxes"})
                Assert.That((Object)typeof(GaussianSplatRenderer).GetField(name,flags).GetValue(renderer)==null,Is.True,name);
            Assert.That(typeof(GaussianSplatRenderer).GetField("m_Registered",flags).GetValue(renderer),Is.EqualTo(false));
        }
        sealed class GpuPresentation : IRealityPresentation
        {
            readonly GaussianRealitySource source;
            public GpuPresentation(GaussianRealitySource source){this.source=source;}
            public bool IsReady=>source.IsReady;
            public string LastError=>null;
            public Transform ObservationAnchor=>null;
            public IntroVirusTarget Virus=>null;
            public IEnumerator Load(RealityIntroProfile profile)=>source.Initialize();
            public IEnumerator Unload()=>source.Release();
        }
        [UnityTest] public IEnumerator ScopedPipelineRendersActualGsThenRestoresGamePipeline()
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder+"/Pipeline.asset");
            var scoped=new GsPipelinePresentation(new GpuPresentation(source),pipeline,camera);
            var target=new RenderTexture(128,128,24);target.Create();var image=new Texture2D(128,128,TextureFormat.RGB24,false);
            try
            {
                yield return scoped.Load(null);Assert.That(scoped.IsReady,Is.True);yield return null;
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                var old=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=old;
                Assert.That(image.GetPixels32().Count(p=>p.r>40),Is.GreaterThan(100));
                yield return scoped.Unload();Assert.That(source.HasResources,Is.False);AssertReleasedIndependently();
                Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalQuality));Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(originalDefault));
                camera.backgroundColor=Color.green;
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                old=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=old;
                Assert.That(image.GetPixels32().Count(p=>p.g>40&&p.r<10),Is.GreaterThan(1000),"Original game pipeline renders after restoration");
            }
            finally
            {
                IntroAsyncOperation.Start(scoped.Unload());target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
            }
        }
    }
}
#endif
