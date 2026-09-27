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
        bool configured;
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
        public IEnumerator StartMap(MapDefinition map)
        {
            if(IsSwitching)yield break;
            if(!configured)throw new InvalidOperationException("Configure the combat session first.");
            IsSwitching=true;IsReady=false;LastError=null;
            try
            {
                run.EndRun();yield return RemoveRuntime();
                yield return loader.Load(map,melee.GetComponent<NavMeshAgent>().agentTypeID);
                if(loader.LastError!=null){LastError=loader.LastError;yield break;}
                try
                {
                    runtime=new GameObject("Combat Runtime");SceneManager.MoveGameObjectToScene(runtime,gameObject.scene);
                    Director=runtime.AddComponent<SpawnDirector>();
                    Director.Configure(run,rig.Head,melee,ranged,projectile,loader.SpawnPositions,gun.GetInstanceID());
                    run.StartRun(map);IsReady=true;
                }
                catch(Exception error){LastError=error.Message;run.EndRun();}
                if(LastError!=null){yield return RemoveRuntime();yield return loader.Unload();}
            }
            finally{IsSwitching=false;}
        }
        public IEnumerator Shutdown()
        {
            if(IsSwitching)yield break;
            IsSwitching=true;IsReady=false;
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
            var body=rig.GetComponent<CharacterController>();var wasEnabled=body.enabled;
            body.enabled=false;
            try
            {
                var localHead=rig.transform.InverseTransformPoint(rig.Head.position);
                rig.transform.rotation=Quaternion.Euler(0,loader.CurrentRoot.playerSpawn.eulerAngles.y,0);
                rig.transform.position=loader.CurrentRoot.PlayerPosition-rig.transform.TransformVector(new Vector3(localHead.x,0,localHead.z));
                body.height=Mathf.Clamp(localHead.y,body.radius*2,2.5f);
                body.center=new Vector3(localHead.x,body.height*.5f,localHead.z);
            }
            finally{body.enabled=wasEnabled;}
            hand.Restart();
        }
        void OnDestroy()
        {
            if(run!=null)run.RunStarted-=ResetPlayerAndWeapon;
            if(runtime!=null)Destroy(runtime);
        }
    }
}
