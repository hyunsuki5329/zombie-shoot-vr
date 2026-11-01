using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using VRFPS.Player;
namespace VRFPS
{
    public sealed class CombatSession : MonoBehaviour
    {
        readonly MapSceneLoader loader=new MapSceneLoader();
        RunController run;PlayerRigController rig;K2Controller gun;LeftHandInteractor hand;
        EnemyActor melee,ranged;AcidProjectile projectile;
        GameObject runtime;
        bool configured,preparedRunStarted,preservePreparedPlacement;
        public bool IsReady { get; private set; }
        public bool IsSwitching { get; private set; }
        public string LastError { get; private set; }
        public SpawnDirector Director { get; private set; }
        public RunRecordBinding Results { get; private set; }
        public void Configure(RunController run,PlayerRigController rig,K2Controller gun,LeftHandInteractor hand,
            EnemyActor melee,EnemyActor ranged,AcidProjectile projectile,RecordStore records)
        {
            if(configured)throw new InvalidOperationException("Combat session is already configured.");
            if(run==null || rig==null || rig.Head==null || gun==null || hand==null || melee==null || ranged==null || projectile==null || records==null)
                throw new ArgumentException("Combat session references are incomplete.");
            this.run=run;this.rig=rig;this.gun=gun;this.hand=hand;this.melee=melee;this.ranged=ranged;this.projectile=projectile;
            var bindings=run.GetComponent<RunCombatBindings>() ?? run.gameObject.AddComponent<RunCombatBindings>();
            bindings.Configure(run,gun,rig,rig.GetComponentInChildren<AmmoPouch>(true));
            gun.GetComponent<K2HandInput>().UseRunClock(run);
            Results=run.GetComponent<RunRecordBinding>() ?? run.gameObject.AddComponent<RunRecordBinding>();
            Results.Configure(run,records);
            run.RunStarted+=ResetPlayerAndWeapon;configured=true;
        }
        public bool HasPreparedMap {get;private set;}
        public IEnumerator StartMap(MapDefinition map)
        {
            if(IsSwitching)yield break;
            yield return PrepareMap(map);
            if(HasPreparedMap && !BeginPreparedRun())yield return Shutdown();
        }
        public bool BeginPreparedRun()
        {
            if(IsSwitching || !HasPreparedMap || preparedRunStarted)return false;
            preparedRunStarted=true;preservePreparedPlacement=true;
            try
            {
                run.StartRun(loader.CurrentMap);IsReady=true;
                CombatDiagnostics.AttachIfRequested(run,Director,rig.GetComponentInChildren<AmmoPouch>(true),gun.GetComponent<K2HandInput>(),"map-session");
                FrameTimingCapture.AttachIfRequested(run,Director,"map-session");
                return true;
            }
            catch(Exception error)
            {
                LastError=error.Message;IsReady=false;HasPreparedMap=false;
                run.EndRun();return false;
            }
            finally{preservePreparedPlacement=false;}
        }
        public Transform FindPreparedAnchor(string key)
        {
            if(!HasPreparedMap || loader.CurrentRoot==null)throw new InvalidOperationException("No prepared map.");
            if(key=="playerSpawn")return loader.CurrentRoot.playerSpawn;
            Transform found=null;
            foreach(var candidate in loader.CurrentRoot.GetComponentsInChildren<Transform>())
                if(candidate.name==key)
                {
                    if(found!=null)throw new InvalidOperationException("Theme anchor key is ambiguous.");
                    found=candidate;
                }
            if(found==null)throw new InvalidOperationException("Theme anchor was not found.");
            return found;
        }
        public void PlacePreparedPlayer(Transform anchor,float relativeYaw)
        {
            if(!HasPreparedMap || IsSwitching || preparedRunStarted)
                throw new InvalidOperationException("A prepared, unstarted map is required.");
            if(anchor==null)throw new ArgumentNullException(nameof(anchor));
            if(float.IsNaN(relativeYaw)||float.IsInfinity(relativeYaw))throw new ArgumentOutOfRangeException(nameof(relativeYaw));
            if(!anchor.IsChildOf(loader.CurrentRoot.transform) || !anchor.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Theme anchor must belong to the prepared map.");
            var position=anchor.position;
            var filter=new NavMeshQueryFilter{agentTypeID=melee.GetComponent<NavMeshAgent>().agentTypeID,areaMask=NavMesh.AllAreas};
            if(!Finite(position) || !NavMesh.SamplePosition(position,out var hit,.25f,filter))
                throw new InvalidOperationException("Theme anchor is not on the navigation mesh.");
            var path=new NavMeshPath();var reachable=false;
            foreach(var spawn in loader.SpawnPositions)
                if(Vector3.Distance(spawn,hit.position)>=loader.CurrentMap.minSpawnDistance &&
                    NavMesh.CalculatePath(spawn,hit.position,filter,path) && path.status==NavMeshPathStatus.PathComplete)
                {reachable=true;break;}
            if(!reachable)throw new InvalidOperationException("Theme anchor has no reachable opening enemy spawn candidates.");
            PlacePlayer(hit.position,anchor.eulerAngles.y+relativeYaw);
        }
        static bool Finite(Vector3 value)=> !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
            !float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&!float.IsNaN(value.z)&&!float.IsInfinity(value.z);
        public IEnumerator PrepareMap(MapDefinition map,Func<bool> canCommit=null)
        {
            if(IsSwitching)yield break;
            if(!configured)throw new InvalidOperationException("Configure the combat session first.");
            IsSwitching=true;IsReady=false;HasPreparedMap=false;preparedRunStarted=false;LastError=null;
            try
            {
                run.EndRun();yield return RemoveRuntime();
                yield return loader.Load(map,melee.GetComponent<NavMeshAgent>().agentTypeID);
                if(loader.LastError!=null){LastError=loader.LastError;yield break;}
                if(canCommit!=null&&!canCommit()){yield return loader.Unload();yield break;}
                try
                {
                    runtime=new GameObject("Combat Runtime");SceneManager.MoveGameObjectToScene(runtime,gameObject.scene);
                    Director=runtime.AddComponent<SpawnDirector>();
                    Director.Configure(run,rig.Head,melee,ranged,projectile,loader.SpawnPositions,gun.GetInstanceID());
                    PlacePlayer(loader.CurrentRoot.PlayerPosition,loader.CurrentRoot.playerSpawn.eulerAngles.y);
                    HasPreparedMap=true;
                }
                catch(Exception error){LastError=error.Message;run.EndRun();}
                if(LastError!=null){yield return RemoveRuntime();yield return loader.Unload();}
            }
            finally{IsSwitching=false;}
        }
        public IEnumerator Shutdown()
        {
            if(IsSwitching)yield break;
            IsSwitching=true;IsReady=false;HasPreparedMap=false;preparedRunStarted=false;
            try
            {
                if(run!=null)run.EndRun();yield return RemoveRuntime();yield return loader.Unload();
            }
            finally{IsSwitching=false;}
        }
        IEnumerator RemoveRuntime()
        {
            Director=null;
            if(runtime!=null){Destroy(runtime);runtime=null;yield return null;}
        }
        void ResetPlayerAndWeapon()
        {
            if(loader.CurrentRoot==null)return;
            if(!preservePreparedPlacement)PlacePlayer(loader.CurrentRoot.PlayerPosition,loader.CurrentRoot.playerSpawn.eulerAngles.y);
            hand.Restart();
        }
        void PlacePlayer(Vector3 position,float yaw)
        {
            var body=rig.GetComponent<CharacterController>();var wasEnabled=body.enabled;
            body.enabled=false;
            try
            {
                var localHead=rig.transform.InverseTransformPoint(rig.Head.position);
                rig.transform.rotation=Quaternion.Euler(0,yaw,0);
                rig.transform.position=position-rig.transform.TransformVector(new Vector3(localHead.x,0,localHead.z));
                body.height=Mathf.Clamp(localHead.y,body.radius*2,2.5f);
                body.center=new Vector3(localHead.x,body.height*.5f,localHead.z);
            }
            finally{body.enabled=wasEnabled;}
        }
        static IEnumerator ReleaseOwnedMap(MapSceneLoader ownedLoader)
        {
            while(ownedLoader.IsLoading)yield return null;
            yield return ownedLoader.Unload();
        }
        void OnDestroy()
        {
            if(run!=null)run.RunStarted-=ResetPlayerAndWeapon;
            if(runtime!=null)Destroy(runtime);
            IntroAsyncOperation.Start(ReleaseOwnedMap(loader));
        }
    }
}
