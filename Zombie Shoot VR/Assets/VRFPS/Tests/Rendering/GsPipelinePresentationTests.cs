#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace VRFPS.Tests
{
    public class GsPipelinePresentationTests
    {
        class Probe : IRealityPresentation
        {
            public bool hold,failLoad,failUnload,resources;
            public Action onLoad,onUnload;
            public bool IsReady{get;private set;}
            public string LastError=>null;
            public Transform ObservationAnchor=>null;
            public IntroVirusTarget Virus=>null;
            public IEnumerator Load(RealityIntroProfile profile){resources=true;onLoad?.Invoke();while(hold)yield return null;if(failLoad)throw new InvalidOperationException("load fault");IsReady=true;}
            public IEnumerator Unload(){onUnload?.Invoke();if(failUnload)throw new InvalidOperationException("release fault");resources=false;IsReady=false;yield break;}
        }
        GameObject cameraHost;Camera camera;UniversalAdditionalCameraData data;
        UniversalRenderPipelineAsset gs;RenderPipelineAsset originalQuality,originalDefault;
        int originalLevel;Probe inner;GsPipelinePresentation presentation;
        static readonly FieldInfo Index=typeof(UniversalAdditionalCameraData).GetField("m_RendererIndex",BindingFlags.Instance|BindingFlags.NonPublic);
        [SetUp] public void Setup()
        {
            originalLevel=QualitySettings.GetQualityLevel();originalQuality=QualitySettings.renderPipeline;originalDefault=GraphicsSettings.defaultRenderPipeline;
            gs=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/VRFPS/External/RealityGS/SyntheticFixture/Pipeline.asset");
            Assert.That(gs,Is.Not.Null,"Create the analytical GS fixture first.");
            cameraHost=new GameObject("Scope camera");camera=cameraHost.AddComponent<Camera>();camera.tag="MainCamera";
            data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(-1);
            inner=new Probe();presentation=new GsPipelinePresentation(inner,gs,camera);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            inner.hold=false;inner.failUnload=false;
            yield return null;yield return null;
            var cleanup=IntroAsyncOperation.Start(presentation.Unload());while(!cleanup.IsDone)yield return null;
            QualitySettings.SetQualityLevel(originalLevel,false);QualitySettings.renderPipeline=originalQuality;
            GraphicsSettings.defaultRenderPipeline=originalDefault;Object.DestroyImmediate(cameraHost);
        }
        [UnityTest] public IEnumerator SelectsGsBeforeLoadAndRestoresOnlyAfterRelease()
        {
            inner.onLoad=()=>{Assert.That(QualitySettings.renderPipeline,Is.SameAs(gs));Assert.That((int)Index.GetValue(data),Is.Zero);};
            inner.onUnload=()=>Assert.That(QualitySettings.renderPipeline,Is.SameAs(gs));
            yield return presentation.Load(null);Assert.That(presentation.IsReady,Is.True);
            Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(originalDefault));
            yield return presentation.Unload();Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalQuality));
            Assert.That((int)Index.GetValue(data),Is.EqualTo(-1));Assert.That(presentation.IsReady,Is.False);
        }
        [UnityTest] public IEnumerator FailedReleaseKeepsScopeForRetry()
        {
            yield return presentation.Load(null);inner.failUnload=true;
            var failed=IntroAsyncOperation.Start(presentation.Unload());while(!failed.IsDone)yield return null;
            Assert.That(failed.Error,Is.Not.Null);Assert.That(QualitySettings.renderPipeline,Is.SameAs(gs));Assert.That(inner.resources,Is.True);
            Assert.That(presentation.IsReady,Is.False,"A failed unload must not expose the presentation as ready.");
            inner.failUnload=false;yield return presentation.Unload();Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalQuality));
        }
        [UnityTest] public IEnumerator DisposedCallerDoesNotReleasePendingOperationLock()
        {
            inner.hold=true;var caller=presentation.Load(null);Assert.That(caller.MoveNext(),Is.True);((IDisposable)caller).Dispose();
            var conflict=IntroAsyncOperation.Start(presentation.Unload());while(!conflict.IsDone)yield return null;
            Assert.That(conflict.Error,Is.Not.Null);Assert.That(QualitySettings.renderPipeline,Is.SameAs(gs));
            inner.hold=false;var deadline=Time.realtimeSinceStartup+5;
            while(!presentation.IsReady){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null;}
            yield return presentation.Unload();Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalQuality));
        }
        [UnityTest] public IEnumerator SecondOwnerCannotRestoreFirstOwnersScope()
        {
            yield return presentation.Load(null);
            var otherInner=new Probe();var other=new GsPipelinePresentation(otherInner,gs,camera);
            var conflict=IntroAsyncOperation.Start(other.Load(null));while(!conflict.IsDone)yield return null;
            Assert.That(conflict.Error,Is.Not.Null);
            yield return other.Unload();Assert.That(QualitySettings.renderPipeline,Is.SameAs(gs));Assert.That(otherInner.resources,Is.False);
            yield return presentation.Unload();Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalQuality));
        }
        [UnityTest] public IEnumerator LoadFailureRestoresThroughExplicitCleanup()
        {
            inner.failLoad=true;var failed=IntroAsyncOperation.Start(presentation.Load(null));while(!failed.IsDone)yield return null;
            Assert.That(failed.Error,Is.Not.Null);Assert.That(QualitySettings.renderPipeline,Is.SameAs(gs));
            yield return presentation.Unload();Assert.That(inner.resources,Is.False);Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalQuality));
        }
        [UnityTest] public IEnumerator ExternalOverrideIsNotOverwrittenOnCleanup()
        {
            yield return presentation.Load(null);QualitySettings.renderPipeline=null;data.SetRenderer(-1);
            Assert.That(presentation.IsReady,Is.False);yield return presentation.Unload();
            Assert.That(QualitySettings.renderPipeline,Is.Null);Assert.That((int)Index.GetValue(data),Is.EqualTo(-1));
        }
        [UnityTest] public IEnumerator QualityChangeRestoresOriginalSlotAndPreservesNewSelection()
        {
            var next=(originalLevel+1)%QualitySettings.names.Length;
            var nextPipeline=QualitySettings.GetRenderPipelineAssetAt(next);
            yield return presentation.Load(null);QualitySettings.SetQualityLevel(next,false);
            yield return presentation.Unload();
            Assert.That(QualitySettings.GetQualityLevel(),Is.EqualTo(next));
            Assert.That(QualitySettings.renderPipeline,Is.SameAs(nextPipeline));
            Assert.That(QualitySettings.GetRenderPipelineAssetAt(originalLevel),Is.SameAs(originalQuality));
        }
        [UnityTest] public IEnumerator DestroyedCameraStillAllowsPipelineCleanup()
        {
            yield return presentation.Load(null);Object.DestroyImmediate(cameraHost);
            yield return presentation.Unload();Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalQuality));
        }
        [UnityTest] public IEnumerator SynchronousUnloadCallbackCannotReenter()
        {
            yield return presentation.Load(null);IntroAsyncOperation reentry=null;
            inner.onUnload=()=>reentry=IntroAsyncOperation.Start(presentation.Load(null));
            yield return presentation.Unload();Assert.That(reentry.IsDone,Is.True);Assert.That(reentry.Error,Is.Not.Null);
            Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalQuality));
        }
    }
}
#endif
