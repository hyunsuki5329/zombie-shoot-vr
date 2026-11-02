using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace VRFPS.Tests {
 public class UserLayoutPassageTests {
  static void Clear(Vector3 a,Vector3 b){
   int mask=LayerMask.GetMask("WorldSolid");Vector3 lo=a+Vector3.up*.25f,hi=a+Vector3.up*1.5f;
   Assert.That(Physics.CheckCapsule(lo,hi,.2f,mask),Is.False,"Blocked at "+a);
   Vector3 delta=b-a;
   Assert.That(Physics.CapsuleCast(lo,hi,.2f,delta.normalized,delta.magnitude,mask),Is.False,"Blocked route "+a+" to "+b);
  }
  [Test] public void RemovedChairPositionAndTeacherSideRouteAreClear(){
   try{
    EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Maps/Room.unity");Physics.SyncTransforms();
    Clear(new Vector3(-1.18f,0,-1.7f)*.8f,new Vector3(-1.18f,0,-.6f)*.8f);
    EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Maps/Classroom.unity");Physics.SyncTransforms();
    Clear(new Vector3(-6.6f,0,-1.65f)*2,new Vector3(-6.6f,0,-2.2f)*2);
    Clear(new Vector3(-6.6f,0,-2.2f)*2,new Vector3(-4.7f,0,-2.2f)*2);
    foreach(float x in new[]{-2.4f,0f,2.4f,4.7f})Clear(new Vector3(x,0,-2.4f)*2,new Vector3(x*2,0,1f));
   }finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
 }
}
