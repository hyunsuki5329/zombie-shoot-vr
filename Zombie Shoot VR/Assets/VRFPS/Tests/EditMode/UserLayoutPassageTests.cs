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
    Clear(new Vector3(-1.18f,0,-1.7f),new Vector3(-1.18f,0,-.6f));
    EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Maps/Classroom.unity");Physics.SyncTransforms();
    Clear(new Vector3(-6.6f,0,-1.65f),new Vector3(-6.6f,0,-2.2f));
    Clear(new Vector3(-6.6f,0,-2.2f),new Vector3(-4.7f,0,-2.2f));
    foreach(float x in new[]{-2.4f,0f,2.4f,4.7f})Clear(new Vector3(x,0,-2.4f),new Vector3(x,0,1.1f));
   }finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
 }
}
