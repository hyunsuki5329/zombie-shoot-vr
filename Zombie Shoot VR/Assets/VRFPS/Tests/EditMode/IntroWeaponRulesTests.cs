using NUnit.Framework;
using UnityEngine;

namespace VRFPS.Tests
{
 public class IntroWeaponRulesTests
 {
  [Test] public void IntroUsesExistingGripAmmoAndWallRules()
  {
   Assert.That(FireGate.CanFire(true,true,30,WeaponContext.Intro,false),Is.True);
   Assert.That(FireGate.CanFire(false,true,30,WeaponContext.Intro,false),Is.False);
   Assert.That(FireGate.CanFire(true,false,30,WeaponContext.Intro,false),Is.False);
   Assert.That(FireGate.CanFire(true,true,0,WeaponContext.Intro,false),Is.False);
   Assert.That(FireGate.CanFire(true,true,30,WeaponContext.Intro,true),Is.False);
   Assert.That(FireGate.CanFire(true,true,30,WeaponContext.Disabled,false),Is.False);
   Assert.That(FireGate.CanFire(true,true,30,WeaponContext.Preparing,false),Is.False);
  }
  [Test] public void IntroNeverStartsRunAndOwnsClockUntilReleased()
  {
   var root=new GameObject("Context");var run=root.AddComponent<RunController>();
   var gun=root.AddComponent<K2Controller>();var router=root.AddComponent<WeaponContextRouter>();
   try
   {
    int starts=0;run.RunStarted+=()=>starts++;router.Configure(run,gun,null,null);
    router.SetIntroState(true,false,123);
    Assert.That(router.Context,Is.EqualTo(WeaponContext.Intro));
    Assert.That(gun.Context,Is.EqualTo(WeaponContext.Intro));
    Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));
    Assert.That(router.InputTime,Is.EqualTo(123));
    router.SetIntroState(true,true,123);
    Assert.That(router.Context,Is.EqualTo(WeaponContext.Disabled));
    router.ReleaseIntro();
    Assert.That(router.Context,Is.EqualTo(WeaponContext.Disabled));
    Assert.That(router.InputTime,Is.Zero);Assert.That(starts,Is.Zero);
   }
   finally{Object.DestroyImmediate(root);}
  }
  [Test] public void NewContextResetsCadenceWithoutRefillingMagazine()
  {
   var root=new GameObject("Gun");var right=new GameObject("Right");var left=new GameObject("Left");
   var muzzle=new GameObject("Muzzle");var gun=root.AddComponent<K2Controller>();
   try
   {
    right.transform.position=new Vector3(9000,10,0);left.transform.position=new Vector3(9000,10,1);
    muzzle.transform.SetParent(root.transform,false);muzzle.transform.localPosition=Vector3.forward*.5f;
    gun.Configure(right.transform,left.transform,muzzle.transform,1,1);
    gun.SetContext(WeaponContext.Intro);gun.SetLeftGrip(true);
    Assert.That(gun.Tick(true,123),Is.True);
    gun.SetContext(WeaponContext.Disabled);gun.SetContext(WeaponContext.Intro);gun.SetLeftGrip(true);
    Assert.That(gun.Tick(true,123.01),Is.False,"Pausing the same owner must preserve the remaining fire interval.");
    gun.SetContext(WeaponContext.Combat);gun.SetLeftGrip(true);
    Assert.That(gun.Tick(true,0),Is.True);
    Assert.That(gun.Magazine.Rounds,Is.EqualTo(28));
   }
   finally{Object.DestroyImmediate(root);Object.DestroyImmediate(right);Object.DestroyImmediate(left);}
  }
 }
}
