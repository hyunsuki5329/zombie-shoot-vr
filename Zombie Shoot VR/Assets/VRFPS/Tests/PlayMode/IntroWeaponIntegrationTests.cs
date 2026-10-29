using NUnit.Framework;
using UnityEngine;

namespace VRFPS.Tests
{
 public class IntroWeaponIntegrationTests
 {
  GameObject root,target,wall;
  K2Controller gun;K2HandInput input;RunController run;WeaponContextRouter router;
  int hits;bool preview;
  [SetUp] public void Setup()
  {
   root=new GameObject("Intro weapon");
   var right=new GameObject("right").transform;right.SetParent(root.transform);
   var left=new GameObject("left").transform;left.SetParent(root.transform);left.localPosition=Vector3.forward*.3f;
   var muzzle=new GameObject("muzzle").transform;muzzle.SetParent(root.transform);muzzle.localPosition=Vector3.forward*.7f;
   gun=root.AddComponent<K2Controller>();gun.Configure(right,left,muzzle,1<<8,(1<<8)|(1<<9));
   input=root.AddComponent<K2HandInput>();input.enabled=false;input.Configure(gun,left,left);
   run=root.AddComponent<RunController>();run.enabled=false;
   router=root.AddComponent<WeaponContextRouter>();router.Configure(run,gun,null,null);
   target=GameObject.CreatePrimitive(PrimitiveType.Sphere);target.layer=9;target.transform.position=Vector3.forward*3;
   preview=true;hits=0;
   var virus=target.AddComponent<IntroVirusTarget>();virus.Configure(gun.GetInstanceID(),()=>preview);
   virus.Triggered+=()=>hits++;
   router.SetIntroState(true,false,0);Physics.SyncTransforms();
  }
  [TearDown] public void Cleanup()
  {
   Object.DestroyImmediate(root);Object.DestroyImmediate(target);if(wall!=null)Object.DestroyImmediate(wall);
  }
  [Test] public void ActualRaycastTriggersVirusOnceWithoutStartingCombat()
  {
   input.TickInput(true,false,0);
   Assert.That(input.TickInput(true,true,0),Is.True);
   input.TickInput(true,true,.2);
   Assert.That(hits,Is.EqualTo(1));Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));
   Assert.That(run.Kills,Is.Zero);Assert.That(run.ReachedWave,Is.Zero);
  }
  [Test] public void WallOcclusionStillBlocksVirus()
  {
   wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=8;
   wall.transform.position=Vector3.forward*1.5f;wall.transform.localScale=new Vector3(3,3,.1f);
   Physics.SyncTransforms();input.TickInput(true,false,0);
   Assert.That(input.TickInput(true,true,0),Is.True);Assert.That(hits,Is.Zero);
  }
  [Test] public void HeldTriggerCannotFireAfterPauseUntilReleased()
  {
   input.TickInput(true,false,0);input.TickInput(true,true,0);
   router.SetIntroState(true,true,.2);
   Assert.That(input.TickInput(true,true,.2),Is.False);
   router.SetIntroState(true,false,.2);
   Assert.That(input.TickInput(true,true,.2),Is.False);
   input.TickInput(true,false,.2);
   Assert.That(input.TickInput(true,true,.2),Is.True);
   Assert.That(gun.Magazine.Rounds,Is.EqualTo(28));
  }
  [Test] public void IntroFeedbackUsesExistingShotEmptyAndSupplyEvents()
  {
   var feedback=root.AddComponent<CombatFeedback>();feedback.Configure(gun,null);
   input.TickInput(true,false,0);input.TickInput(true,true,0);
   Assert.That(feedback.CueCount,Is.EqualTo(1));
   while(gun.Magazine.TryConsume()){}
   input.TickInput(true,false,.2);input.TickInput(true,true,.2);
   Assert.That(feedback.CueCount,Is.EqualTo(2));
   feedback.OnFreshMagazineSupplied();Assert.That(feedback.CueCount,Is.EqualTo(3));
   router.SetIntroState(true,true,.2);
   feedback.OnFreshMagazineSupplied();Assert.That(feedback.CueCount,Is.EqualTo(3));
  }
  [Test] public void NormalPreparationKeepsExistingHeldTriggerBehavior()
  {
   var map=ScriptableObject.CreateInstance<MapDefinition>();
   map.mapId="room";map.sceneName="test";map.maxAlive=4;map.maxRangedAlive=1;map.minSpawnDistance=1;
   try
   {
    router.ReleaseIntro();run.StartRun(map);
    input.TickInput(true,false,0);
    Assert.That(input.TickInput(true,true,0),Is.False);
    run.Tick(8);
    Assert.That(input.TickInput(true,true,0),Is.True,
        "Normal Preparing→Combat is the same weapon owner and must retain established held-trigger behavior.");
   }
   finally{Object.DestroyImmediate(map);}
  }
 }
}
