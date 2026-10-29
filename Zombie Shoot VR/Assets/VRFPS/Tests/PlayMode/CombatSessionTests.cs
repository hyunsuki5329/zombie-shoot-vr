#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRFPS.Player;
namespace VRFPS.Tests
{
    [PrebuildSetup(typeof(MapSceneTestSetup)),PostBuildCleanup(typeof(MapSceneTestSetup))]
    public class CombatSessionTests
    {
        const string Lab="Assets/VRFPS/Scenes/Tests/WaveLab.unity";
        const string Map="Assets/VRFPS/Tests/Fixtures/MapLoading/ValidMap.unity";
        CombatSession session;RunController run;PlayerRigController rig;K2Controller gun;
        MapDefinition map;string directory;Scene lab;RecordStore records;
        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"VRFPS-session-"+Guid.NewGuid().ToString("N"));records=new RecordStore(directory);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Lab,new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            lab=SceneManager.GetSceneByPath(Lab);
            rig=UnityEngine.Object.FindFirstObjectByType<PlayerRigController>();run=rig.GetComponent<RunController>();run.enabled=false;rig.enabled=false;
            gun=UnityEngine.Object.FindFirstObjectByType<K2Controller>(FindObjectsInactive.Include);
            UnityEngine.Object.Destroy(rig.GetComponent<WaveLabSession>());UnityEngine.Object.Destroy(GameObject.Find("Wave Runtime"));yield return null;run.EndRun();
            var folder="Assets/VRFPS/Prefabs/Enemies/";
            session=rig.gameObject.AddComponent<CombatSession>();
            session.Configure(run,rig,gun,rig.GetComponentInChildren<LeftHandInteractor>(true),
                AssetDatabase.LoadAssetAtPath<GameObject>(folder+"MeleeZombie.prefab").GetComponent<EnemyActor>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(folder+"RangedZombie.prefab").GetComponent<EnemyActor>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(folder+"AcidProjectile.prefab").GetComponent<AcidProjectile>(),records);
            map=ScriptableObject.CreateInstance<MapDefinition>();map.mapId="room";map.sceneName=Map;map.maxAlive=4;map.maxRangedAlive=1;map.minSpawnDistance=2;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(session!=null)yield return session.Shutdown();
            if(lab.IsValid() && lab.isLoaded)yield return SceneManager.UnloadSceneAsync(lab);
            var scene=SceneManager.GetSceneByPath(Map);if(scene.IsValid() && scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            UnityEngine.Object.DestroyImmediate(map);if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        [UnityTest] public IEnumerator PreparedMapDoesNotStartUntilRequestedAndStartsOnlyOnce()
        {
            var starts=0;run.RunStarted+=()=>starts++;
            Assert.That(session.BeginPreparedRun(),Is.False);
            yield return session.PrepareMap(map);
            Assert.That(session.LastError,Is.Null);
            Assert.That(session.HasPreparedMap,Is.True);
            Assert.That(session.IsReady,Is.False);
            Assert.That(starts,Is.Zero);Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));
            Assert.That(run.Alive,Is.Zero);
            Assert.That(session.BeginPreparedRun(),Is.True);
            Assert.That(session.BeginPreparedRun(),Is.False);
            Assert.That(starts,Is.EqualTo(1));Assert.That(session.IsReady,Is.True);
            Assert.That(run.PreparationRemaining,Is.EqualTo(8));
            Assert.That(run.Health,Is.EqualTo(100));Assert.That(gun.Magazine.Rounds,Is.EqualTo(30));
            Assert.That(run.ReachedWave,Is.Zero);Assert.That(run.Kills,Is.Zero);
        }
        [UnityTest] public IEnumerator PreparedPlacementSurvivesFirstStartAndRetryReturnsToDefaultSpawn()
        {
            foreach(var component in rig.Head.GetComponents<Behaviour>())
                if(component.GetType().Name=="TrackedPoseDriver")component.enabled=false;
            rig.Head.localPosition=new Vector3(.3f,1.7f,.2f);
            yield return session.PrepareMap(map);
            var anchor=new GameObject("Intro theme anchor");
            try
            {
                anchor.transform.SetParent(UnityEngine.Object.FindFirstObjectByType<MapRoot>().transform);
                anchor.transform.position=new Vector3(8001,0,1);
                anchor.transform.rotation=Quaternion.Euler(0,40,0);
                session.PlacePreparedPlayer(anchor.transform,25);
                var position=rig.transform.position;var rotation=rig.transform.rotation;
                Assert.That(Vector2.Distance(new Vector2(rig.Head.position.x,rig.Head.position.z),new Vector2(8001,1)),Is.LessThan(.02f));
                Assert.That(Mathf.DeltaAngle(rig.transform.eulerAngles.y,65),Is.EqualTo(0).Within(.01));
                Assert.That(session.BeginPreparedRun(),Is.True);
                Assert.That(rig.transform.position,Is.EqualTo(position));
                Assert.That(Quaternion.Angle(rig.transform.rotation,rotation),Is.LessThan(.001f));
                run.StartRun(map);
                Assert.That(Vector2.Distance(new Vector2(rig.Head.position.x,rig.Head.position.z),new Vector2(8000,0)),Is.LessThan(.02f));
            }
            finally{UnityEngine.Object.DestroyImmediate(anchor);}
        }
        [UnityTest] public IEnumerator PreparedPlacementRejectsForeignOrUnreachableAnchorsWithoutMovingPlayer()
        {
            yield return session.PrepareMap(map);
            var anchor=new GameObject("Invalid intro anchor");var position=rig.transform.position;
            try
            {
                anchor.transform.position=new Vector3(8001,0,1);
                Assert.Throws<InvalidOperationException>(()=>session.PlacePreparedPlayer(anchor.transform,0));
                anchor.transform.SetParent(UnityEngine.Object.FindFirstObjectByType<MapRoot>().transform);
                anchor.transform.position=new Vector3(8001,100,1);
                Assert.Throws<InvalidOperationException>(()=>session.PlacePreparedPlayer(anchor.transform,0));
                anchor.transform.position=new Vector3(8001,0,1);map.minSpawnDistance=100000;
                Assert.Throws<InvalidOperationException>(()=>session.PlacePreparedPlayer(anchor.transform,0));
                Assert.That(rig.transform.position,Is.EqualTo(position));
            }
            finally{UnityEngine.Object.DestroyImmediate(anchor);}
        }
        [UnityTest] public IEnumerator FailedStartReturnsErrorAndLegacyWrapperUnloadsOwnedMap()
        {
            run.RunStarted+=()=>throw new InvalidOperationException("start failure probe");
            yield return session.StartMap(map);
            Assert.That(session.LastError,Is.EqualTo("start failure probe"));
            Assert.That(session.IsReady,Is.False);Assert.That(session.HasPreparedMap,Is.False);
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));Assert.That(session.Director,Is.Null);
            Assert.That(SceneManager.GetSceneByPath(Map).isLoaded,Is.False);
        }
        [UnityTest] public IEnumerator MapStartsAtSpawnAndDeathPersistsSameRun()
        {
            foreach(var component in rig.Head.GetComponents<Behaviour>())
                if(component.GetType().Name=="TrackedPoseDriver")component.enabled=false;
            var trackedPosition=new Vector3(.3f,1.7f,.2f);var trackedRotation=Quaternion.Euler(10,15,0);
            rig.Head.localPosition=trackedPosition;rig.Head.localRotation=trackedRotation;
            yield return session.StartMap(map);
            Assert.That(rig.Head.localPosition,Is.EqualTo(trackedPosition));
            Assert.That(Quaternion.Angle(rig.Head.localRotation,trackedRotation),Is.LessThan(.001f));
            Assert.That(session.IsReady,Is.True);Assert.That(session.LastError,Is.Null);
            Assert.That(Vector2.Distance(new Vector2(rig.Head.position.x,rig.Head.position.z),new Vector2(8000,0)),Is.LessThan(.02f));
            Assert.That(run.Health,Is.EqualTo(100));Assert.That(gun.Magazine.Rounds,Is.EqualTo(30));Assert.That(run.Phase,Is.EqualTo(RunPhase.Preparing));
            session.Director.enabled=false;run.Tick(8);session.Director.Tick(0);session.Director.Tick(1.6f);
            Assert.That(run.Alive,Is.EqualTo(1));
            run.ApplyDamage(100,1,0);Assert.That(session.Results.SaveSucceeded,Is.True);
            Assert.That(new RecordStore(directory).Read("room").reachedWave,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RepeatedMapChangesKeepOneRuntimeAndShutdownUnloadsMap()
        {
            var count=SceneManager.sceneCount;
            for(int i=0;i<10;i++)
            {
                yield return session.StartMap(map);Assert.That(session.IsReady,Is.True);
                Assert.That(UnityEngine.Object.FindObjectsByType<SpawnDirector>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
                Assert.That(SceneManager.sceneCount,Is.EqualTo(count+1));
            }
            yield return session.Shutdown();Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));
            Assert.That(session.IsReady,Is.False);Assert.That(SceneManager.sceneCount,Is.EqualTo(count));
        }
    }
}
#endif
