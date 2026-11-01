#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using GaussianSplatting.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace VRFPS.Tests
{
    // Real production adapters and theme scenes; analytical GS replaces a private scan.
    // Runtime-only scene conversion never changes the fixture or the user's map files.
    [PrebuildSetup("VRFPS.Tests.MapSceneTestSetup"),PostBuildCleanup("VRFPS.Tests.MapSceneTestSetup")]
    public class GaussianComparisonFlowTests
    {
        const string Boot="Assets/VRFPS/Scenes/Bootstrap.unity";
        const string Observation="Assets/VRFPS/Tests/Fixtures/MapLoading/ValidMap.unity";
        const string Package="Packages/org.nesnausk.gaussian-splatting/";
        GameBootstrap bootstrap;string directory;RealityIntroProfile profile;
        GaussianRealitySource source;GaussianSplatRenderer splats;IntroVirusTarget virus;
        RenderPipelineAsset savedPipeline;int savedQuality;bool settingsSaved;
        [SetUp] public void SaveSettings()
        {savedQuality=QualitySettings.GetQualityLevel();savedPipeline=QualitySettings.renderPipeline;settingsSaved=true;}
        void Loaded(Scene scene,LoadSceneMode mode)
        {
            if(scene.path==Boot)
            {
                bootstrap=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameBootstrap>()).Single();
                bootstrap.RecordDirectoryOverride=directory;return;
            }
            if(scene.path!=Observation)return;
            foreach(var old in scene.GetRootGameObjects())Object.DestroyImmediate(old);
            var host=new GameObject("Analytical GS observation - not a capture");SceneManager.MoveGameObjectToScene(host,scene);
            host.SetActive(false);var root=host.AddComponent<RealitySceneRoot>();root.mapId="room";root.dataId="synthetic-256";
            Transform Child(string name){var go=new GameObject(name);go.transform.SetParent(host.transform,false);return go.transform;}
            root.dataRoot=Child("Data");splats=root.dataRoot.gameObject.AddComponent<GaussianSplatRenderer>();splats.enabled=false;
            splats.m_Asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>("Assets/VRFPS/External/RealityGS/SyntheticFixture/synthetic.asset");
            splats.m_ShaderSplats=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/RenderGaussianSplats.shader");
            splats.m_ShaderComposite=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianComposite.shader");
            splats.m_ShaderDebugPoints=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianDebugRenderPoints.shader");
            splats.m_ShaderDebugBoxes=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianDebugRenderBoxes.shader");
            splats.m_CSSplatUtilities=AssetDatabase.LoadAssetAtPath<ComputeShader>(Package+"Shaders/SplatUtilities.compute");
            source=root.dataRoot.gameObject.AddComponent<GaussianRealitySource>();source.splats=splats;source.dataId=root.dataId;
            root.observationAnchor=Child("observation");root.collisionRoot=Child("Collision");
            root.collisionRoot.position=new Vector3(0,-.1f,0);root.collisionRoot.gameObject.layer=LayerMask.NameToLayer("WorldSolid");
            root.collisionRoot.gameObject.AddComponent<BoxCollider>().size=new Vector3(20,.2f,20);
            virus=Child("Virus").gameObject.AddComponent<IntroVirusTarget>();root.virus=virus;
            virus.gameObject.layer=LayerMask.NameToLayer("IntroTarget");virus.gameObject.AddComponent<SphereCollider>().radius=.32f;
            virus.transform.position=new Vector3(0,1.15f,2);host.SetActive(true);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded-=Loaded;
            try
            {
                if(bootstrap)yield return bootstrap.Shutdown();
                foreach(var path in new[]{Observation,"Assets/VRFPS/Scenes/Combat.unity",Boot})
                {var scene=SceneManager.GetSceneByPath(path);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);}
            }
            finally
            {
                // Independent isolation even when the production restoration assertion fails.
                if(settingsSaved){QualitySettings.SetQualityLevel(savedQuality,false);QualitySettings.renderPipeline=savedPipeline;}
                if(profile)Object.DestroyImmediate(profile);
                if(directory!=null&&Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        [UnityTest] public IEnumerator ActualGsThroughK2FadeAndRoomCombatTenCycles()
        {
            Assert.That(SystemInfo.graphicsDeviceType,Is.EqualTo(GraphicsDeviceType.Direct3D12));
            directory=Path.Combine(Path.GetTempPath(),"VRFPS-gs-flow-"+Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded+=Loaded;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Boot,new LoadSceneParameters(LoadSceneMode.Additive));
            var deadline=Time.realtimeSinceStartup+30;
            while(!bootstrap.IsReady&&bootstrap.LastError==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(bootstrap.IsReady,Is.True,bootstrap.LastError);
            var entry=bootstrap.Entry;var intro=entry.GetComponent<ComparisonIntroController>();var fade=entry.GetComponent<VrScreenFade>();
            var camera=entry.rig.Head.GetComponent<Camera>();var originalPipeline=QualitySettings.renderPipeline;
            entry.run.enabled=false; // Freeze the run clock so the exact initial 8 seconds can be inspected.
            profile=ScriptableObject.CreateInstance<RealityIntroProfile>();profile.mapId="room";profile.dataId="synthetic-256";
            profile.realityScenePath=Observation;profile.realityAnchorKey="observation";profile.themeAnchorKey="playerSpawn";
            entry.ConfigureRealityOptions(profile,null);entry.menu.ShowMapSelection();
            var starts=0;var startBoundaryValid=true;
            entry.run.RunStarted+=()=>
            {
                starts++;
                // Capture the synchronous boundary, not only the next rendered frame.
                startBoundaryValid &= fade.Alpha==0&&!SceneManager.GetSceneByPath(Observation).isLoaded&&Released(splats);
            };
            var baselineScenes=SceneManager.sceneCount;
            for(var cycle=0;cycle<10;cycle++)
            {
                Assert.That(entry.roomRealityButton.interactable,Is.True);entry.roomRealityButton.onClick.Invoke();
                deadline=Time.realtimeSinceStartup+30;
                while(intro.Phase!=IntroPhase.RealityPreview&&intro.IsBusy&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(intro.Phase,Is.EqualTo(IntroPhase.RealityPreview),intro.LastError);Assert.That(starts,Is.EqualTo(cycle));
                Assert.That(source.IsReady&&source.HasResources,Is.True);Assert.That(entry.run.Kills,Is.Zero);
                Assert.That(fade.Alpha,Is.Zero);Assert.That(entry.Session.IsReady,Is.False);
                source.transform.SetPositionAndRotation(camera.transform.position+camera.transform.forward*2,camera.transform.rotation);
                yield return null;
                Assert.That(RenderPixels(camera).Count(p=>p.r>60&&p.r>p.g*2&&p.r>p.b*2),Is.GreaterThan(100),"Actual GS pixels in installed flow");
                entry.menu.Pause();Assert.That(virus.ApplyDamage(25,entry.gun.GetInstanceID(),0),Is.False);entry.menu.Resume();
                // Use the existing K2 raycast; controller/device grip input is separately tested.
                entry.gun.SetLeftGrip(true);entry.gun.Tick(false,0);entry.gun.UpdatePose();
                var muzzle=(Transform)typeof(K2Controller).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(entry.gun);
                virus.transform.position=muzzle.position+muzzle.forward*1.5f;Physics.SyncTransforms();
                Assert.That(entry.gun.Tick(true,.2),Is.True);Assert.That(virus.ApplyDamage(25,entry.gun.GetInstanceID(),.2),Is.False);
                var sawBlack=false;var sawFadeIn=false;deadline=Time.realtimeSinceStartup+30;
                while(intro.IsBusy&&Time.realtimeSinceStartup<deadline)
                {
                    if(intro.Phase==IntroPhase.Switching&&!sawBlack)
                    {
                        sawBlack=true;Assert.That(fade.Alpha,Is.EqualTo(1));
                        Assert.That(RenderPixels(camera).All(p=>p.r<=2&&p.g<=2&&p.b<=2),Is.True,"Mono black covers actual world and gun");
                    }
                    if(intro.Phase==IntroPhase.FadingIn)sawFadeIn=true;
                    if(intro.Phase!=IntroPhase.Complete)Assert.That(starts,Is.EqualTo(cycle));
                    yield return null;
                }
                Assert.That(intro.IsBusy,Is.False);Assert.That(intro.LastError,Is.Null);Assert.That(intro.Phase,Is.EqualTo(IntroPhase.Complete));
                Assert.That(sawBlack&&sawFadeIn,Is.True);Assert.That(starts,Is.EqualTo(cycle+1));Assert.That(fade.Alpha,Is.Zero);
                Assert.That(startBoundaryValid,Is.True,"RunStarted requires alpha0, observation unloaded and GS buffers released");
                Assert.That(SceneManager.GetSceneByPath(Observation).isLoaded,Is.False);
                Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalPipeline));AssertReleased(splats);
                Assert.That(entry.Session.IsReady,Is.True);Assert.That(entry.run.Map,Is.SameAs(entry.room));
                Assert.That(entry.run.PreparationRemaining,Is.EqualTo(8));Assert.That(entry.run.Health,Is.EqualTo(100));
                Assert.That(entry.gun.Magazine.Rounds,Is.EqualTo(30));Assert.That(entry.run.ReachedWave,Is.Zero);Assert.That(entry.run.Kills,Is.Zero);
                Assert.That(entry.rig.GetComponentsInChildren<Camera>(true).Length,Is.EqualTo(1));
                entry.menu.ShowMapSelection();yield return entry.Session.Shutdown();yield return null;
                Assert.That(SceneManager.sceneCount,Is.EqualTo(baselineScenes));
                var record=new RecordStore(directory).Read("room");Assert.That(record.kills,Is.Zero);Assert.That(record.reachedWave,Is.Zero);
            }
            yield return entry.SelectMap(entry.classroom);
            Assert.That(entry.Session.IsReady,Is.True,entry.Session.LastError);Assert.That(entry.run.Map,Is.SameAs(entry.classroom));
            Assert.That(entry.run.PreparationRemaining,Is.EqualTo(8));Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalPipeline));
        }
        static Color32[] RenderPixels(Camera camera)
        {
            var target=new RenderTexture(256,256,24);target.Create();var image=new Texture2D(256,256,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();return image.GetPixels32();
            }
            finally{RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
        static void AssertReleased(GaussianSplatRenderer renderer)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
            foreach(var field in typeof(GaussianSplatRenderer).GetFields(flags).Where(f=>f.FieldType==typeof(GraphicsBuffer)))
                Assert.That(field.GetValue(renderer),Is.Null,field.Name);
            Assert.That(typeof(GaussianSplatRenderer).GetField("m_Registered",flags).GetValue(renderer),Is.EqualTo(false));
            Assert.That(Released(renderer),Is.True,"Direct/sorter buffers, texture, materials and registration must be released");
        }
        static bool Released(GaussianSplatRenderer renderer)
        {
            if(ReferenceEquals(renderer,null))return false;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
            var args=(GpuSorting.Args)typeof(GaussianSplatRenderer).GetField("m_SorterArgs",flags).GetValue(renderer);
            return typeof(GaussianSplatRenderer).GetFields(flags).Where(f=>f.FieldType==typeof(GraphicsBuffer)).All(f=>f.GetValue(renderer)==null)
                && args.resources.altBuffer==null&&args.resources.altPayloadBuffer==null&&args.resources.passHistBuffer==null&&args.resources.globalHistBuffer==null
                && new[]{"m_GpuColorData","m_MatSplats","m_MatComposite","m_MatDebugPoints","m_MatDebugBoxes"}
                    .All(name=>(Object)typeof(GaussianSplatRenderer).GetField(name,flags).GetValue(renderer)==null)
                && Equals(typeof(GaussianSplatRenderer).GetField("m_Registered",flags).GetValue(renderer),false);
        }
    }
}
#endif
