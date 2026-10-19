using NUnit.Framework;using UnityEngine;using VRFPS.Player;
namespace VRFPS.Tests { public class ViewHeightTests {
 [Test] public void HeightBoostPreservesTrackedPoseAndDoesNotAccumulate(){
  var type=typeof(PlayerRigController).Assembly.GetType("VRFPS.Player.ViewHeightAdjustment");Assert.That(type,Is.Not.Null,"Height-only view adjustment missing");
  var root=new GameObject("rig");try {var basis=new GameObject("floor offset").transform;basis.SetParent(root.transform);var lift=new GameObject("view lift").transform;lift.SetParent(basis);var head=new GameObject("head").transform;head.SetParent(lift);head.localPosition=new Vector3(.2f,1.6f,.3f);var hand=new GameObject("hand").transform;hand.SetParent(lift);hand.localPosition=new Vector3(.4f,1,.5f);
   var c=root.AddComponent(type);type.GetMethod("Configure").Invoke(c,new object[]{basis,lift,head});var apply=type.GetMethod("Apply");for(int i=0;i<30;i++)apply.Invoke(c,null);
   Assert.That(head.position.y,Is.EqualTo(1.84f).Within(.001));Assert.That(hand.position.y,Is.EqualTo(1.24f).Within(.001));Assert.That(head.localPosition,Is.EqualTo(new Vector3(.2f,1.6f,.3f)));Assert.That(root.transform.localScale,Is.EqualTo(Vector3.one));
   head.localPosition=new Vector3(.2f,.8f,.3f);apply.Invoke(c,null);Assert.That(head.position.y,Is.EqualTo(.92f).Within(.001));
   basis.localPosition=Vector3.up;apply.Invoke(c,null);Assert.That(head.position.y,Is.EqualTo(2.07f).Within(.001));
   root.transform.position=Vector3.up*2;apply.Invoke(c,null);Assert.That(head.position.y,Is.EqualTo(4.07f).Within(.001),"Jump height must not feed into height boost");
  }finally{Object.DestroyImmediate(root);}
 }
}}
