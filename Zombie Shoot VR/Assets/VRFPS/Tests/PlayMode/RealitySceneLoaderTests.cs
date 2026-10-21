#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace VRFPS.Tests
{
    [PrebuildSetup(typeof(MapSceneTestSetup)),PostBuildCleanup(typeof(MapSceneTestSetup))]
    public class RealitySceneLoaderTests
    {
        public sealed class ProbeRenderer : RealityRenderSource
        {
            public bool ready,resources,failRelease,notReady,retainResources;public string id="fixture-data";public System.Action releasing;
            public override string DataId=>id;public override bool IsReady=>ready;public override bool HasResources=>resources;
            public override IEnumerator Initialize(){resources=true;yield return null;ready=!notReady;}
            public override IEnumerator Release()
            {
                var callback=releasing;releasing=null;callback?.Invoke();
                if(failRelease)throw new System.InvalidOperationException("Injected release failure");
                ready=false;resources=retainResources;yield return null;
                if(failRelease)throw new System.InvalidOperationException("Injected release failure");
            }
        }
        const string Path="Assets/VRFPS/Tests/Fixtures/MapLoading/ValidMap.unity";
        RealityIntroProfile profile;SceneRealityPresentation presentation;ProbeRenderer renderer;bool invalid;System.Action<ProbeRenderer> configure;
        void OnLoaded(Scene scene,LoadSceneMode mode)
        {
            if(scene.path!=Path)return;
            // Reuse only the included scene shell. All fixture conversion is runtime-only.
            foreach(var old in scene.GetRootGameObjects())Object.DestroyImmediate(old);
            var go=new GameObject("Reality fixture");SceneManager.MoveGameObjectToScene(go,scene);
            var root=go.AddComponent<RealitySceneRoot>();root.mapId="room";root.dataId=invalid?"wrong":"fixture-data";
            Transform Child(string name){var child=new GameObject(name);child.transform.SetParent(go.transform,false);return child.transform;}
            root.dataRoot=Child("Data");renderer=root.dataRoot.gameObject.AddComponent<ProbeRenderer>();configure?.Invoke(renderer);
            root.observationAnchor=Child("observation");root.collisionRoot=Child("Collision");root.collisionRoot.gameObject.layer=LayerMask.NameToLayer("WorldSolid");root.collisionRoot.gameObject.AddComponent<BoxCollider>();
            root.virus=Child("Virus").gameObject.AddComponent<IntroVirusTarget>();root.virus.gameObject.layer=LayerMask.NameToLayer("IntroTarget");root.virus.gameObject.AddComponent<SphereCollider>().radius=.32f;
        }
        [SetUp] public void Setup()
        {
            invalid=false;configure=null;renderer=null;presentation=new SceneRealityPresentation();profile=ScriptableObject.CreateInstance<RealityIntroProfile>();
            profile.mapId="room";profile.dataId="fixture-data";profile.realityScenePath=Path;profile.realityAnchorKey="observation";profile.themeAnchorKey="playerSpawn";
            SceneManager.sceneLoaded+=OnLoaded;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(renderer!=null){renderer.failRelease=false;renderer.retainResources=false;renderer.releasing=null;}
            yield return presentation.Unload();SceneManager.sceneLoaded-=OnLoaded;
            var scene=SceneManager.GetSceneByPath(Path);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            Object.Destroy(profile);
        }
        [UnityTest] public IEnumerator RealSceneLoadValidateAndReleaseTenCycles()
        {
            var baseline=SceneManager.sceneCount;
            for(var i=0;i<10;i++)
            {
                yield return presentation.Load(profile);Assert.That(presentation.IsReady,Is.True);
                Assert.That(presentation.ObservationAnchor,Is.Not.Null);Assert.That(presentation.Virus,Is.Not.Null);Assert.That(renderer.resources,Is.True);
                yield return presentation.Unload();Assert.That(presentation.IsReady,Is.False);Assert.That(SceneManager.sceneCount,Is.EqualTo(baseline));
            }
        }
        [UnityTest] public IEnumerator InvalidCompositionIsNotReadyAndOwnedSceneCanBeCleaned()
        {
            invalid=true;var job=IntroAsyncOperation.Start(presentation.Load(profile));while(!job.IsDone)yield return null;
            Assert.That(job.Error,Is.Not.Null);Assert.That(presentation.IsReady,Is.False);Assert.That(presentation.LastError,Is.Not.Null);
            Assert.That(renderer.ready,Is.False);yield return presentation.Unload();Assert.That(SceneManager.GetSceneByPath(Path).isLoaded,Is.False);
        }
        [UnityTest] public IEnumerator FailedResourceReleaseKeepsSceneOwnedUntilRetry()
        {
            yield return presentation.Load(profile);Assert.That(presentation.IsReady,Is.True);renderer.failRelease=true;
            var job=IntroAsyncOperation.Start(presentation.Unload());while(!job.IsDone)yield return null;
            Assert.That(job.Error,Is.Not.Null);Assert.That(presentation.IsReady,Is.False);Assert.That(SceneManager.GetSceneByPath(Path).isLoaded,Is.True);
            renderer.failRelease=false;yield return presentation.Unload();Assert.That(SceneManager.GetSceneByPath(Path).isLoaded,Is.False);
        }
        [UnityTest] public IEnumerator ReentrantReleaseIsRejectedBeforeStartingAnotherCore()
        {
            yield return presentation.Load(profile);IntroAsyncOperation reentry=null;
            renderer.releasing=()=>
            {
                reentry=IntroAsyncOperation.Start(presentation.Unload());
                // Keep an erroneously accepted duplicate from racing into native unload in RED.
                if(!reentry.IsDone)renderer.failRelease=true;
            };
            var outer=IntroAsyncOperation.Start(presentation.Unload());while(!outer.IsDone||reentry==null||!reentry.IsDone)yield return null;
            Assert.That(reentry.Error,Is.Not.Null);StringAssert.Contains("already running",reentry.Error.Message);
            Assert.That(outer.Error,Is.Null);
        }
        [UnityTest] public IEnumerator RendererIdentityAndReadinessMustBeConfirmed()
        {
            configure=value=>value.id="other-data";
            var wrong=IntroAsyncOperation.Start(presentation.Load(profile));while(!wrong.IsDone)yield return null;
            Assert.That(wrong.Error,Is.Not.Null);Assert.That(renderer.resources,Is.False);yield return presentation.Unload();
            configure=value=>value.notReady=true;
            var incomplete=IntroAsyncOperation.Start(presentation.Load(profile));while(!incomplete.IsDone)yield return null;
            Assert.That(incomplete.Error,Is.Not.Null);Assert.That(presentation.IsReady,Is.False);Assert.That(renderer.resources,Is.True);
            yield return presentation.Unload();
        }
        [UnityTest] public IEnumerator ClaimedReleaseWithRemainingResourcesCannotUnloadScene()
        {
            yield return presentation.Load(profile);renderer.retainResources=true;
            var job=IntroAsyncOperation.Start(presentation.Unload());while(!job.IsDone)yield return null;
            Assert.That(job.Error,Is.Not.Null);Assert.That(SceneManager.GetSceneByPath(Path).isLoaded,Is.True);
            renderer.retainResources=false;yield return presentation.Unload();
        }
        [UnityTest] public IEnumerator DisposedCallerCannotCancelOwnedLoadOrUnlockAnotherOperation()
        {
            var request=presentation.Load(profile);Assert.That(request.MoveNext(),Is.True);(request as System.IDisposable)?.Dispose();
            var competing=IntroAsyncOperation.Start(presentation.Unload());
            Assert.That(competing.IsDone,Is.True);Assert.That(competing.Error,Is.Not.Null);
            var deadline=Time.realtimeSinceStartup+5;
            while(!presentation.IsReady){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null;}
            yield return presentation.Unload();Assert.That(SceneManager.GetSceneByPath(Path).isLoaded,Is.False);
        }
        [UnityTest] public IEnumerator ForeignSceneCannotBeAdoptedOrUnloaded()
        {
            yield return SceneManager.LoadSceneAsync(Path,LoadSceneMode.Additive);
            var job=IntroAsyncOperation.Start(presentation.Load(profile));while(!job.IsDone)yield return null;
            Assert.That(job.Error,Is.Not.Null);yield return presentation.Unload();Assert.That(SceneManager.GetSceneByPath(Path).isLoaded,Is.True);
        }
    }
}
#endif
