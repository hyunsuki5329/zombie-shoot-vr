#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using NUnit.Framework;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.TestTools;
namespace VRFPS.Tests {
 [PrebuildSetup(typeof(MapSceneTestSetup)),PostBuildCleanup(typeof(MapSceneTestSetup))]
 public class ScannedRoomSessionTests {
  const string Combat="Assets/VRFPS/Scenes/Combat.unity";CombatSceneEntry entry;Scene scene;string recordsPath;
  [UnitySetUp] public IEnumerator Setup(){
   recordsPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"VRFPS-Room-"+Guid.NewGuid().ToString("N"));
   yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Combat,new LoadSceneParameters(LoadSceneMode.Additive));
   scene=SceneManager.GetSceneByPath(Combat);entry=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<CombatSceneEntry>()).Single();
   entry.rig.enabled=false;entry.run.enabled=false;
   foreach(var component in entry.rig.Head.GetComponents<Behaviour>())if(component.GetType().Name=="TrackedPoseDriver")component.enabled=false;
   entry.rig.Head.localPosition=Vector3.up*1.65f;
   entry.Initialize(new RecordStore(recordsPath));entry.menu.CloseInstructions();yield return null;
  }
  [UnityTearDown] public IEnumerator Cleanup(){
   if(entry!=null)yield return entry.Shutdown();if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
   if(Directory.Exists(recordsPath))Directory.Delete(recordsPath,true);
  }
  [UnityTest] public IEnumerator RoomSelectionCombatPauseDeathRecordAndRetry(){
   Assert.That(entry.roomButton.interactable,Is.True);Assert.That(entry.classroomButton.interactable,Is.False);
   yield return entry.SelectMap(entry.room);Assert.That(entry.Session.LastError,Is.Null);Assert.That(entry.Session.IsReady,Is.True);
   var director=entry.Session.Director;director.enabled=false;
   Assert.That(entry.run.Phase,Is.EqualTo(RunPhase.Preparing));entry.run.Tick(8);director.Tick(0);director.Tick(1.6f);
   Assert.That(entry.run.Alive,Is.EqualTo(1));
   entry.menu.Pause();var active=entry.run.ActiveTime;entry.run.Tick(3);director.Tick(3);Assert.That(entry.run.ActiveTime,Is.EqualTo(active));entry.menu.Resume();
   entry.run.ApplyDamage(100,1,entry.run.ActiveTime);Assert.That(entry.Session.Results.SaveSucceeded,Is.True);
   Assert.That(new RecordStore(recordsPath).Read("room").reachedWave,Is.EqualTo(1));
   entry.menu.Retry();Assert.That(entry.run.Phase,Is.EqualTo(RunPhase.Preparing));Assert.That(entry.gun.Magazine.Rounds,Is.EqualTo(30));Assert.That(entry.run.Alive,Is.Zero);
   Assert.That(Vector2.Distance(new Vector2(entry.rig.Head.position.x,entry.rig.Head.position.z),new Vector2(.9f,1.1f)),Is.LessThan(.03f));
  }
  [UnityTest] public IEnumerator CenterStripStillAllowsEnemySpawning(){
   yield return entry.SelectMap(entry.room);var director=entry.Session.Director;director.enabled=false;
   var body=entry.rig.GetComponent<CharacterController>();body.enabled=false;
   entry.rig.transform.position+=new Vector3(.38f,0,0)-entry.rig.transform.TransformPoint(body.center-Vector3.up*(body.height*.5f));
   Physics.SyncTransforms();entry.run.Tick(8);director.Tick(0);director.Tick(1.6f);
   Assert.That(entry.run.Alive,Is.EqualTo(1),"A stationary player in the central aisle must not block every spawn");
  }
  [UnityTest] public IEnumerator TenRoomReloadsKeepOneMapAndRuntime(){
   for(int i=0;i<10;i++){
    yield return entry.SelectMap(entry.room);Assert.That(entry.Session.LastError,Is.Null);
    Assert.That(UnityEngine.Object.FindObjectsByType<MapRoot>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
    Assert.That(UnityEngine.Object.FindObjectsByType<SpawnDirector>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
   }
   yield return entry.Shutdown();Assert.That(SceneManager.GetSceneByPath("Assets/VRFPS/Scenes/Maps/Room.unity").isLoaded,Is.False);
  }
 }
}
#endif
