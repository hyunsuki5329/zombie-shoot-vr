using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace VRFPS
{
    public sealed class SceneRealityPresentation : IRealityPresentation
    {
        static readonly HashSet<string> Reservations=new HashSet<string>(StringComparer.Ordinal);
        Scene ownedScene;string reservedPath;RealitySceneRoot root;RealityRenderSource source;
        IntroAsyncOperation current;bool ready,starting;
        public bool IsReady=>ready&&ownedScene.IsValid()&&ownedScene.isLoaded&&source!=null&&source.isActiveAndEnabled&&source.IsReady&&source.HasResources;
        public string LastError=>current?.Error?.Message;
        public Transform ObservationAnchor=>root!=null?root.observationAnchor:null;
        public IntroVirusTarget Virus=>root!=null?root.virus:null;
        public IEnumerator Load(RealityIntroProfile profile)=>Run(LoadCore(profile));
        public IEnumerator Unload()=>Run(UnloadCore());
        IEnumerator Run(IEnumerator operation)
        {
            if(starting||(current!=null&&!current.IsDone))throw new InvalidOperationException("Reality scene operation is already running.");
            // Keep observing Unity scene operations even if the requesting object is destroyed.
            IntroAsyncOperation job;starting=true;
            try{job=IntroAsyncOperation.Start(operation);current=job;}
            finally{starting=false;}
            while(!job.IsDone)yield return null;
            if(job.Error!=null)throw new InvalidOperationException(job.Error.Message,job.Error);
        }
        IEnumerator LoadCore(RealityIntroProfile profile)
        {
            if(profile==null)throw new ArgumentNullException(nameof(profile));profile.Validate();
            if(ownedScene.IsValid())throw new InvalidOperationException("Release the current reality scene before loading another.");
            var path=profile.realityScenePath;
            if(!path.StartsWith("Assets/",StringComparison.Ordinal)||!path.EndsWith(".unity",StringComparison.Ordinal)||!Application.CanStreamedLevelBeLoaded(path))
                throw new InvalidOperationException("Reality scene path is not included in this build.");
            var existing=SceneManager.GetSceneByPath(path);
            if(existing.IsValid()||Reservations.Contains(path))throw new InvalidOperationException("Reality scene is already owned by another session.");
            Reservations.Add(path);reservedPath=path;ready=false;
            AsyncOperation loading;
            try
            {
                loading=SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);
                if(loading==null)throw new InvalidOperationException("Reality scene loading did not start.");
            }
            catch{ReleaseReservation();throw;}
            yield return loading;
            ownedScene=SceneManager.GetSceneByPath(path);
            if(!ownedScene.IsValid()||!ownedScene.isLoaded){ReleaseReservation();throw new InvalidOperationException("Reality scene did not finish loading.");}
            var roots=Components<RealitySceneRoot>();
            if(roots.Length!=1)throw new InvalidOperationException("Reality scene must have exactly one RealitySceneRoot.");
            root=roots[0];root.Validate(profile);
            var renderers=Components<RealityRenderSource>();
            if(renderers.Length!=1||!renderers[0].transform.IsChildOf(root.dataRoot)||!renderers[0].isActiveAndEnabled)
                throw new InvalidOperationException("Reality scene requires one active render source under its data root.");
            source=renderers[0];
            if(source.DataId!=profile.dataId)throw new InvalidOperationException("Reality renderer data identity does not match the profile.");
            yield return source.Initialize();
            if(source==null||!source.isActiveAndEnabled||!source.IsReady||!source.HasResources||source.DataId!=profile.dataId)
                throw new InvalidOperationException("Reality renderer did not confirm data and resource readiness.");
            ready=true;
        }
        IEnumerator UnloadCore()
        {
            ready=false;
            if(ownedScene.IsValid()&&ownedScene.isLoaded)
            {
                // Also release sources from an invalid composition that failed before Initialize.
                foreach(var item in Components<RealityRenderSource>())
                {
                    yield return item.Release();
                    if(item==null||item.HasResources)throw new InvalidOperationException("Reality renderer has not confirmed resource release.");
                }
                var unloading=SceneManager.UnloadSceneAsync(ownedScene);
                if(unloading==null)throw new InvalidOperationException("Reality scene unloading did not start.");
                yield return unloading;
                if(ownedScene.IsValid()&&ownedScene.isLoaded)throw new InvalidOperationException("Reality scene is still loaded.");
            }
            root=null;source=null;ownedScene=default;ReleaseReservation();
        }
        T[] Components<T>() where T:Component=>ownedScene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).ToArray();
        void ReleaseReservation(){if(reservedPath!=null)Reservations.Remove(reservedPath);reservedPath=null;}
    }
}
