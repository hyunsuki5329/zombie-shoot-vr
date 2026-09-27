#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace VRFPS.Tests
{
    [PrebuildSetup(typeof(MapSceneTestSetup)),PostBuildCleanup(typeof(MapSceneTestSetup))]
    public class MapSceneLoaderTests
    {
        const string Valid="Assets/VRFPS/Tests/Fixtures/MapLoading/ValidMap.unity";
        const string Invalid="Assets/VRFPS/Tests/Fixtures/MapLoading/InvalidMap.unity";
        EditorBuildSettingsScene[] previous;
        MapSceneLoader loader;MapDefinition map;
        [SetUp] public void SetUp()
        {
            previous=EditorBuildSettings.scenes;
            EditorBuildSettings.scenes=previous.Where(x=>x.path!=Valid && x.path!=Invalid).Concat(new[]{new EditorBuildSettingsScene(Valid,true),new EditorBuildSettingsScene(Invalid,true)}).ToArray();
            loader=new MapSceneLoader();map=ScriptableObject.CreateInstance<MapDefinition>();
            map.mapId="room";map.sceneName=Valid;map.maxAlive=4;map.maxRangedAlive=1;map.minSpawnDistance=2;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            yield return loader.Unload();
            foreach(var path in new[]{Valid,Invalid})
            {
                var scene=SceneManager.GetSceneByPath(path);if(scene.IsValid() && scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            }
            EditorBuildSettings.scenes=previous;Object.DestroyImmediate(map);
        }
        [UnityTest] public IEnumerator LoadsValidatedMapAndUnloadsWithoutLeavingScene()
        {
            var count=SceneManager.sceneCount;yield return loader.Load(map);
            Assert.That(loader.LastError,Is.Null);Assert.That(loader.CurrentRoot,Is.Not.Null);
            Assert.That(loader.CurrentMap,Is.SameAs(map));Assert.That(loader.SpawnPositions.Length,Is.EqualTo(1));
            Assert.That(SceneManager.sceneCount,Is.EqualTo(count+1));
            yield return loader.Unload();Assert.That(loader.CurrentRoot,Is.Null);Assert.That(SceneManager.sceneCount,Is.EqualTo(count));
        }
        [UnityTest] public IEnumerator InvalidLoadedMapIsRemovedAndNextLoadCanRecover()
        {
            var count=SceneManager.sceneCount;map.sceneName=Invalid;yield return loader.Load(map);
            Assert.That(loader.LastError,Is.Not.Null.And.Not.Empty);Assert.That(loader.CurrentRoot,Is.Null);
            Assert.That(loader.IsLoading,Is.False);Assert.That(SceneManager.sceneCount,Is.EqualTo(count));
            map.sceneName=Valid;yield return loader.Load(map);Assert.That(loader.LastError,Is.Null);Assert.That(loader.CurrentRoot,Is.Not.Null);
        }
        [UnityTest] public IEnumerator UnavailableSceneDoesNotReplaceCurrentMap()
        {
            yield return loader.Load(map);var current=loader.CurrentRoot;
            var missing=ScriptableObject.CreateInstance<MapDefinition>();
            try
            {
                missing.mapId="classroom";missing.sceneName="NoSuchMap";missing.maxAlive=4;missing.maxRangedAlive=1;missing.minSpawnDistance=2;
                yield return loader.Load(missing);
                Assert.That(loader.LastError,Is.Not.Null.And.Not.Empty);Assert.That(loader.CurrentRoot,Is.SameAs(current));
            }
            finally{Object.DestroyImmediate(missing);}
        }
    }
}
#endif
