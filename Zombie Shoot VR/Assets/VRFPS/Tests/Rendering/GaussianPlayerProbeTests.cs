#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using GaussianSplatting.Runtime;
using UnityEngine.Rendering.Universal;
namespace VRFPS.Tests
{
    public class GaussianPlayerProbeTests
    {
        [UnityTest] public IEnumerator ProbeRendersBlackensAndReleasesTenCycles()=>RunGpuProbe(false);
        [UnityTest] public IEnumerator UnexpectedFinalObservationErrorStillReturnsFailureReport()=>RunGpuProbe(true);
        IEnumerator RunGpuProbe(bool detachSource)
        {
            var type=typeof(GaussianRealitySource).Assembly.GetType("VRFPS.GaussianPlayerProbe");
            Assert.That(type,Is.Not.Null,"Native Player GS probe has not been implemented");
            const string package="Packages/org.nesnausk.gaussian-splatting/";
            var host=new GameObject("Probe GPU test");host.SetActive(false);
            var cameraHost=new GameObject("Probe camera");var camera=cameraHost.AddComponent<Camera>();
            camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-2);camera.nearClipPlane=.05f;camera.farClipPlane=10;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.GetUniversalAdditionalCameraData();
            var original=QualitySettings.renderPipeline;
            try
            {
                var renderer=host.AddComponent<GaussianSplatRenderer>();renderer.enabled=false;
                renderer.m_Asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>("Assets/VRFPS/External/RealityGS/SyntheticFixture/synthetic.asset");
                renderer.m_ShaderSplats=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/RenderGaussianSplats.shader");
                renderer.m_ShaderComposite=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/GaussianComposite.shader");
                renderer.m_ShaderDebugPoints=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/GaussianDebugRenderPoints.shader");
                renderer.m_ShaderDebugBoxes=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/GaussianDebugRenderBoxes.shader");
                renderer.m_CSSplatUtilities=AssetDatabase.LoadAssetAtPath<ComputeShader>(package+"Shaders/SplatUtilities.compute");
                var source=host.AddComponent<GaussianRealitySource>();source.splats=renderer;source.dataId="synthetic-256";
                var probe=host.AddComponent(type);type.GetField("source").SetValue(probe,source);type.GetField("viewCamera").SetValue(probe,camera);
                type.GetField("pipeline").SetValue(probe,AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/VRFPS/Configuration/Intro/GsPipeline.asset"));
                type.GetField("fadeShader").SetValue(probe,Shader.Find("VRFPS/IntroScreenFade"));host.SetActive(true);
                var operation=IntroAsyncOperation.Start((IEnumerator)type.GetMethod("RunProbe").Invoke(probe,null));
                if(detachSource)
                {
                    while(!source.HasResources&&!operation.IsDone)yield return null;
                    Assert.That(source.HasResources,Is.True);
                    // The owned presentation still releases its source, but the probe's
                    // final observation now throws. Its outer reporting boundary must catch it.
                    type.GetField("source").SetValue(probe,null);
                }
                while(!operation.IsDone)yield return null;
                Assert.That(operation.Error,Is.Null,"All probe errors must become failure reports, not abandoned Player coroutines");
                var report=type.GetProperty("Report").GetValue(probe);var reportType=report.GetType();
                Assert.That(reportType.GetField("success").GetValue(report),Is.EqualTo(!detachSource),(string)reportType.GetField("error").GetValue(report));
                if(!detachSource)
                {
                    Assert.That(reportType.GetField("completedCycles").GetValue(report),Is.EqualTo(10));
                    Assert.That(reportType.GetField("blackFrames").GetValue(report),Is.EqualTo(10));
                    Assert.That((int)reportType.GetField("minVisiblePixels").GetValue(report),Is.GreaterThan(100));
                }
                else Assert.That((string)reportType.GetField("error").GetValue(report),Is.Not.Null.And.Not.Empty);
                Assert.That(source.HasResources,Is.False);Assert.That(QualitySettings.renderPipeline,Is.SameAs(original));
            }
            finally {UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(cameraHost);QualitySettings.renderPipeline=original;}
        }
        [UnityTest] public IEnumerator MissingReferencesProduceFailureWithoutChangingPipeline()
        {
            var type=typeof(GaussianRealitySource).Assembly.GetType("VRFPS.GaussianPlayerProbe");
            Assert.That(type,Is.Not.Null,"Native Player GS probe has not been implemented");
            var host=new GameObject("Probe test");var original=QualitySettings.renderPipeline;
            try
            {
                var probe=host.AddComponent(type);
                yield return (IEnumerator)type.GetMethod("RunProbe").Invoke(probe,null);
                var report=type.GetProperty("Report").GetValue(probe);
                Assert.That(report,Is.Not.Null);
                Assert.That(report.GetType().GetField("success").GetValue(report),Is.EqualTo(false));
                Assert.That(report.GetType().GetField("completedCycles").GetValue(report),Is.EqualTo(0));
                Assert.That((string)report.GetType().GetField("error").GetValue(report),Does.Contain("references"));
                Assert.That(QualitySettings.renderPipeline,Is.SameAs(original));
            }
            finally {UnityEngine.Object.DestroyImmediate(host);}
        }
    }
}
#endif
