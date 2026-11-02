#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
namespace VRFPS.Tests
{
    public sealed class MapSceneTestSetup : IPrebuildSetup,IPostBuildCleanup
    {
        const string Key="VRFPS.Tests.PreviousBuildScenes";
        [Serializable] sealed class Item {public string path;public bool enabled;}
        [Serializable] sealed class Snapshot {public Item[] scenes;}
        public void Setup()
        {
            if(!string.IsNullOrEmpty(SessionState.GetString(Key,"")))throw new InvalidOperationException("Previous test scene setup has not been cleaned up.");
            var previous=EditorBuildSettings.scenes;
            SessionState.SetString(Key,JsonUtility.ToJson(new Snapshot{scenes=previous.Select(x=>new Item{path=x.path,enabled=x.enabled}).ToArray()}));
            var paths=new[]{"Assets/VRFPS/Tests/Fixtures/MapLoading/ValidMap.unity","Assets/VRFPS/Tests/Fixtures/MapLoading/InvalidMap.unity", "Assets/VRFPS/Scenes/Bootstrap.unity","Assets/VRFPS/Scenes/Combat.unity"};
            EditorBuildSettings.scenes=previous.Where(x=>!paths.Contains(x.path)).Concat(paths.Select(x=>new EditorBuildSettingsScene(x,true))).ToArray();
        }
        public void Cleanup()
        {
            var json=SessionState.GetString(Key,"");if(string.IsNullOrEmpty(json))return;
            var saved=JsonUtility.FromJson<Snapshot>(json);
            EditorBuildSettings.scenes=saved.scenes.Select(x=>new EditorBuildSettingsScene(x.path,x.enabled)).ToArray();
            SessionState.EraseString(Key);
        }
    }
}
#endif
