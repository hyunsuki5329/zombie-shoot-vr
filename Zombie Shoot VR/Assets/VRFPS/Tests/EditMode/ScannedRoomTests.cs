using NUnit.Framework;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace VRFPS.Tests {
 public class ScannedRoomTests {
  const string Path="Assets/VRFPS/Scenes/Maps/Room.unity";
  [Test] public void ActualRoomSceneExists(){Assert.That(File.Exists(Path),Is.True,"Actual user scan Room is missing");}
  [Test] public void MeasuredRoomHasSeparateCollisionAndReachableAnchors(){
   var scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
   try {
    var map=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MapRoot>()).Single();
    var floor=map.collisionRoot.Find("Floor").GetComponent<BoxCollider>();
    Assert.That(floor.bounds.size.x,Is.EqualTo(8.6f).Within(.001));Assert.That(floor.bounds.size.z,Is.EqualTo(7f).Within(.001));
    Assert.That(map.visualRoot.GetComponentsInChildren<Collider>(),Is.Empty);
    var theme=map.visualRoot.GetComponentInChildren<GeneratedThemeRoot>();
    if(theme!=null){Assert.That(theme.themeId,Is.EqualTo("AbandonedHouse"));Assert.That(AssetDatabase.GetDependencies(Path,true).Any(p=>p.Contains("External/RoomScan")),Is.False);}
    else Assert.That(map.visualRoot.Find("Calibrated scan surface"),Is.Not.Null,"Expected themed or legacy reference visual");
    var definition=AssetDatabase.LoadAssetAtPath<MapDefinition>("Assets/VRFPS/Configuration/Maps/Room.asset");
    Assert.That(map.GetSpawnPositions(definition).Length,Is.EqualTo(map.enemySpawns.Length));Assert.That(map.RejectedSpawnCount,Is.Zero);
   }finally{EditorSceneManager.CloseScene(scene,true);}
  }
  [Test] public void LocalScanReferenceRetainsOriginalTextureWhenAvailable(){
   const string reference="Assets/VRFPS/External/RoomScan/ReferenceRoom.unity";
   if(!File.Exists(reference))Assert.Ignore("Optional local scan reference is not part of the final themed build");
   var scene=EditorSceneManager.OpenScene(reference,OpenSceneMode.Additive);
   try{
    var map=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MapRoot>()).Single();
    var surface=map.visualRoot.Find("Calibrated scan surface");Assert.That(surface,Is.Not.Null);
    var texture=surface.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap");
    Assert.That(texture,Is.Not.Null);Assert.That(texture.width,Is.EqualTo(4096));
    Assert.That(surface.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3,Is.GreaterThan(100000));
   }finally{EditorSceneManager.CloseScene(scene,true);}
  }
  [Test] public void ReachableFloorHasSpawnCoverage(){
   var scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
   try {
    var root=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MapRoot>()).Single();
    var definition=AssetDatabase.LoadAssetAtPath<MapDefinition>("Assets/VRFPS/Configuration/Maps/Room.asset");
    var points=root.GetSpawnPositions(definition);int sampled=0;
    for(float x=-4.75f;x<5;x+=.25f)for(float z=-3.75f;z<11;z+=.25f){
     var p=new Vector3(x,0,z);
     if(!UnityEngine.AI.NavMesh.SamplePosition(p,out var hit,.25f,UnityEngine.AI.NavMesh.AllAreas))continue;
     if(Vector2.Distance(new Vector2(x,z),new Vector2(hit.position.x,hit.position.z))>.06f)continue;
     var path=new UnityEngine.AI.NavMeshPath();
     if(!UnityEngine.AI.NavMesh.CalculatePath(hit.position,root.PlayerPosition,UnityEngine.AI.NavMesh.AllAreas,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)continue;
     sampled++;Assert.That(points.Any(v=>Vector3.Distance(v,hit.position)>=definition.minSpawnDistance),Is.True,"No spawn coverage at "+hit.position);
    }
    Assert.That(sampled,Is.GreaterThan(100));
   }finally{EditorSceneManager.CloseScene(scene,true);}
  }
  [Test] public void OutsideCorridorConnectsThroughDoorway(){
   var scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
   try {
    var root=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MapRoot>()).Single();
    Assert.That(root.collisionRoot.Find("Corridor floor"),Is.Not.Null);
    var definition=AssetDatabase.LoadAssetAtPath<MapDefinition>("Assets/VRFPS/Configuration/Maps/Room.asset");
    var points=root.GetSpawnPositions(definition);Assert.That(points.Any(p=>p.z>3),Is.True);
    Physics.SyncTransforms();Assert.That(Physics.Raycast(new Vector3(1.3f,1,1.1f)*2f,Vector3.forward,6.25f,LayerMask.GetMask("WorldSolid")),Is.False);
   }finally{EditorSceneManager.CloseScene(scene,true);}
  }
  [Test] public void WallsCeilingAndBedBlockCombatRays(){
   var scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
   try {
    Physics.SyncTransforms();int mask=LayerMask.GetMask("WorldSolid");
    Assert.That(Physics.Raycast(new Vector3(0,1,1.1f)*2f,Vector3.right,out var wall,7.5f,mask),Is.True);Assert.That(wall.collider.name,Does.StartWith("Side wall"));
    Assert.That(Physics.Raycast(new Vector3(.2f,.3f,-.8f)*2f,Vector3.right,out var bed,5,mask),Is.True);Assert.That(bed.collider.transform.name=="Bed" || bed.collider.transform.parent.name=="Bed",Is.True);
    Assert.That(Physics.Raycast(new Vector3(0,1.7f,1.1f)*2f,Vector3.up,out var ceiling,5,mask),Is.True);Assert.That(ceiling.collider.name,Is.EqualTo("Ceiling"));
   }finally{EditorSceneManager.CloseScene(scene,true);}
  }
 }
}
