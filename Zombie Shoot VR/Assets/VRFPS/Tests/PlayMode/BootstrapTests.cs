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
                yield return entry.SelectMap(fixture);
                Assert.That(entry.Session.IsReady,Is.True);Assert.That(entry.menu.Screen,Is.EqualTo(MenuScreen.Hidden));
                entry.run.enabled=false;entry.Session.Director.enabled=false;entry.run.Tick(8);entry.run.ApplyDamage(100,1,0);
                Assert.That(entry.menu.Screen,Is.EqualTo(MenuScreen.Result));
                Assert.That(entry.status.text,Does.Contain("Saved"));
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
