using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace VRFPS
{
    public sealed class MapSceneLoader
    {
        Scene ownedScene;
        public bool IsLoading { get; private set; }
        public string LastError { get; private set; }
        public MapRoot CurrentRoot { get; private set; }
        public MapDefinition CurrentMap { get; private set; }
        public Vector3[] SpawnPositions { get; private set; } = Array.Empty<Vector3>();
        public IEnumerator Load(MapDefinition map,int agentTypeId=0)
        {
            if(IsLoading)yield break;
            LastError=null;
            try
            {
                if(map==null)throw new ArgumentNullException(nameof(map));
                map.Validate();
                if(!Application.CanStreamedLevelBeLoaded(map.sceneName))
                    throw new InvalidOperationException("The selected map is not included in this build.");
                var existing=FindScene(map.sceneName);
                if(existing.IsValid() && existing.isLoaded && existing!=ownedScene)
                    throw new InvalidOperationException("The selected scene is already owned by another session.");
            }
            catch(Exception error) {LastError=error.Message;}
            if(LastError!=null)yield break;
            IsLoading=true;
            try
            {
                yield return UnloadCurrent();
                AsyncOperation operation=null;
                try {operation=SceneManager.LoadSceneAsync(map.sceneName,LoadSceneMode.Additive);}
                catch(Exception error){LastError=error.Message;}
                if(operation==null)
                {
                    if(LastError==null)LastError="Map loading did not start.";
                    yield break;
                }
                yield return operation;
                ownedScene=FindScene(map.sceneName);
                try
                {
                    if(!ownedScene.IsValid() || !ownedScene.isLoaded)throw new InvalidOperationException("Map scene did not load.");
                    var roots=ownedScene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MapRoot>(true)).ToArray();
                    if(roots.Length!=1)throw new InvalidOperationException("A map scene must contain exactly one MapRoot.");
                    var positions=roots[0].GetSpawnPositions(map,agentTypeId);
                    CurrentRoot=roots[0];CurrentMap=map;SpawnPositions=positions;
                }
                catch(Exception error){LastError=error.Message;}
                if(LastError!=null)yield return UnloadCurrent();
            }
            finally {IsLoading=false;}
        }
        public IEnumerator Unload()
        {
            if(IsLoading)yield break;
            IsLoading=true;
            try {yield return UnloadCurrent();LastError=null;}
            finally {IsLoading=false;}
        }
        IEnumerator UnloadCurrent()
        {
            CurrentRoot=null;CurrentMap=null;SpawnPositions=Array.Empty<Vector3>();
            if(ownedScene.IsValid() && ownedScene.isLoaded)
            {
                var operation=SceneManager.UnloadSceneAsync(ownedScene);
                if(operation!=null)yield return operation;
            }
            ownedScene=default;
        }
        static Scene FindScene(string name)
        {
            var scene=SceneManager.GetSceneByPath(name);
            return scene.IsValid() ? scene : SceneManager.GetSceneByName(name);
        }
    }
}
