#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace VRFPS.Tests
{
    [PrebuildSetup(typeof(MapSceneTestSetup)),PostBuildCleanup(typeof(MapSceneTestSetup))]
    public class BootstrapTests
    {
        sealed class RealityFixture : IRealityPresentation
        {
            public GameObject root;public bool IsReady{get;private set;}public string LastError=>null;
            public Transform ObservationAnchor=>root.transform;public IntroVirusTarget Virus=>root.GetComponent<IntroVirusTarget>();
            public IEnumerator Load(RealityIntroProfile profile){IsReady=true;yield break;}
            public IEnumerator Unload(){IsReady=false;yield break;}
        }
        const string Boot="Assets/VRFPS/Scenes/Bootstrap.unity";
        GameBootstrap bootstrap;string directory;
        void InjectDirectory(Scene scene,LoadSceneMode mode)
        {
            if(scene.path!=Boot)return;
            foreach(var root in scene.GetRootGameObjects())
            {
                var candidate=root.GetComponent<GameBootstrap>();
                if(candidate!=null)candidate.RecordDirectoryOverride=directory;
            }
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded-=InjectDirectory;
            if(bootstrap!=null)yield return bootstrap.Shutdown();
            foreach(var path in new[]{"Assets/VRFPS/Tests/Fixtures/MapLoading/ValidMap.unity","Assets/VRFPS/Scenes/Combat.unity",Boot})
            {
                var scene=SceneManager.GetSceneByPath(path);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            }
            if(directory!=null && Directory.Exists(directory))Directory.Delete(directory,true);
        }
        [UnityTest] public IEnumerator BootstrapOpensSelectionAndStartsCommonCombatFromValidatedMap()
        {
            directory=Path.Combine(Path.GetTempPath(),"VRFPS-bootstrap-"+Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded+=InjectDirectory;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Boot,new LoadSceneParameters(LoadSceneMode.Additive));
            bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();Assert.That(bootstrap,Is.Not.Null);
            var deadline=Time.realtimeSinceStartup+30;
            while(!bootstrap.IsReady && bootstrap.LastError==null && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(bootstrap.LastError,Is.Null);Assert.That(bootstrap.IsReady,Is.True);
            var entry=bootstrap.Entry;
            var sounds=entry.run.GetComponent<RunFeedback>();
            Assert.That(sounds,Is.Not.Null);Assert.That(sounds.output,Is.Not.Null);
            Assert.That(sounds.damageClip,Is.Not.Null);Assert.That(sounds.waveClip,Is.Not.Null);Assert.That(sounds.lowHealthClip,Is.Not.Null);
            Assert.That(entry.menu.Screen,Is.EqualTo(MenuScreen.Instructions));
            var guide=entry.view.transform.Find("Panel/InstructionPanel");
            Assert.That(guide,Is.Not.Null);Assert.That(guide.gameObject.activeInHierarchy,Is.True);
            entry.view.transform.Find("Panel/Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(entry.menu.Screen,Is.EqualTo(MenuScreen.MapSelection));Assert.That(entry.roomButton.interactable,Is.True);
            Assert.That(entry.classroomButton.interactable,Is.True);
            var fixture=ScriptableObject.CreateInstance<MapDefinition>();
            try
            {
                fixture.mapId="room";fixture.sceneName="Assets/VRFPS/Tests/Fixtures/MapLoading/ValidMap.unity";
                fixture.maxAlive=4;fixture.maxRangedAlive=1;fixture.minSpawnDistance=2;
                yield return entry.SelectReality(null,fixture);
                Assert.That(entry.Session.IsReady,Is.False);Assert.That(entry.status.text,Does.Contain("GS"));
                var reality=new RealityFixture{root=new GameObject("Bootstrap reality fixture")};reality.root.AddComponent<IntroVirusTarget>();
                var profile=ScriptableObject.CreateInstance<RealityIntroProfile>();
                try
                {
                    profile.mapId="room";profile.dataId="test";profile.realityScenePath="fake";profile.realityAnchorKey="anchor";profile.themeAnchorKey="playerSpawn";
                    profile.fadeInSeconds=.03f;profile.fadeOutSeconds=.03f;profile.minBlackSeconds=.03f;
                    var fade=entry.gameObject.AddComponent<VrScreenFade>();var intro=entry.gameObject.AddComponent<ComparisonIntroController>();
                    intro.Configure(reality,entry.Session,entry.rig,entry.gun,entry.run.GetComponent<WeaponContextRouter>(),fade);
                    entry.BindComparison(intro);entry.run.enabled=false;
                    var request=IntroAsyncOperation.Start(entry.SelectReality(profile,fixture));deadline=Time.realtimeSinceStartup+5;
                    while(intro.Phase!=IntroPhase.RealityPreview&&!request.IsDone&&Time.realtimeSinceStartup<deadline)yield return null;
                    Assert.That(intro.Phase,Is.EqualTo(IntroPhase.RealityPreview));entry.menu.Pause();
                    Assert.That(entry.menu.Screen,Is.EqualTo(MenuScreen.Pause));entry.menu.Resume();
                    Assert.That(reality.Virus.ApplyDamage(1,entry.gun.GetInstanceID(),0),Is.True);
                    while(!request.IsDone&&Time.realtimeSinceStartup<deadline)yield return null;
                    Assert.That(request.IsDone,Is.True);Assert.That(request.Error,Is.Null);
                    Assert.That(entry.Session.IsReady,Is.True);Assert.That(entry.run.PreparationRemaining,Is.EqualTo(8));
                    entry.menu.ShowMapSelection();
                }
                finally{UnityEngine.Object.DestroyImmediate(reality.root);UnityEngine.Object.DestroyImmediate(profile);}
                yield return entry.SelectMap(fixture);
                Assert.That(entry.Session.IsReady,Is.True);Assert.That(entry.menu.Screen,Is.EqualTo(MenuScreen.Hidden));
                entry.run.enabled=false;entry.Session.Director.enabled=false;entry.run.Tick(8);entry.run.ApplyDamage(100,1,0);
                Assert.That(entry.menu.Screen,Is.EqualTo(MenuScreen.Result));
                Assert.That(entry.status.text,Does.Contain("저장 완료"));
                Assert.That(new RecordStore(directory).Read("room").reachedWave,Is.EqualTo(1));
                var maps=entry.view.transform.Find("Panel/Maps unavailable").GetComponent<UnityEngine.UI.Button>();
                Assert.That(maps.interactable,Is.True);maps.onClick.Invoke();
                Assert.That(entry.menu.Screen,Is.EqualTo(MenuScreen.MapSelection));
                Assert.That(entry.roomLabel.text,Does.Contain("1"));
                yield return bootstrap.Shutdown();Assert.That(SceneManager.GetSceneByPath("Assets/VRFPS/Scenes/Combat.unity").isLoaded,Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(fixture);}
        }
        [UnityTest] public IEnumerator ShutdownDuringStartupWaitsForOwnedSceneCleanup()
        {
            directory=Path.Combine(Path.GetTempPath(),"VRFPS-bootstrap-"+Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded+=InjectDirectory;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Boot,new LoadSceneParameters(LoadSceneMode.Additive));
            bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();
            var deadline=Time.realtimeSinceStartup+30;
            while(!bootstrap.IsStarting && !bootstrap.IsReady && bootstrap.LastError==null && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(bootstrap.IsStarting,Is.True,"Exercise shutdown while loading is still in flight.");
            yield return bootstrap.Shutdown();
            Assert.That(bootstrap.IsStarting,Is.False);Assert.That(bootstrap.IsReady,Is.False);
            Assert.That(SceneManager.GetSceneByPath("Assets/VRFPS/Scenes/Combat.unity").isLoaded,Is.False);
        }
    }
}
#endif
