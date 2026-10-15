using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
namespace VRFPS.Editor {
 public static class UserLayoutSetup {
  public static void Configure(){
   if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch editor");
   foreach(var name in new[]{"Room","Classroom"}){
    var scene=EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Maps/"+name+".unity");
    var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();
    var theme=map.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(name=="Room"?"ThemeGenerated":"SchoolThemeGenerated"));
    if(name=="Classroom" && map.transform.Find("Student aisle clearance 40cm")==null){
     var previous=map.transform.Find("Student aisle clearance applied");
     float shift=previous!=null?.15f:.4f;
     foreach(Transform item in theme)if(item.name.StartsWith("Desk ")||item.name.StartsWith("Chair "))item.position+=Vector3.back*shift;
     if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
     new GameObject("Student aisle clearance 40cm").transform.SetParent(map.transform,false);
    }
    foreach(Transform visual in theme){
     var key=visual.name.ToLowerInvariant();
     if(key.Contains("floor")||key.Contains("ceiling")||key.Contains("wall")||key.Contains("lintel"))continue;
     var collision=map.collisionRoot.Find(visual.name);
     if(collision==null){collision=new GameObject(visual.name).transform;collision.SetParent(map.collisionRoot,false);}
     foreach(var c in collision.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);
     for(int i=collision.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(collision.GetChild(i).gameObject);
     collision.SetPositionAndRotation(Vector3.zero,Quaternion.identity);collision.localScale=Vector3.one;
     collision.gameObject.SetActive(visual.gameObject.activeInHierarchy);
     collision.gameObject.layer=LayerMask.NameToLayer("WorldSolid");
     var mod=collision.GetComponent<NavMeshModifier>()??collision.gameObject.AddComponent<NavMeshModifier>();mod.overrideArea=true;mod.area=1;
     foreach(var f in visual.GetComponentsInChildren<MeshFilter>()){
      if(f.sharedMesh==null)continue;
      var g=new GameObject(f.name);g.layer=LayerMask.NameToLayer("WorldSolid");g.transform.SetParent(collision,false);
      g.transform.SetPositionAndRotation(f.transform.position,f.transform.rotation);g.transform.localScale=f.transform.lossyScale;
      g.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
     }
    }
    Physics.SyncTransforms();var surface=map.GetComponent<NavMeshSurface>();surface.BuildNavMesh();
    var path="Assets/VRFPS/Configuration/Maps/"+(name=="Room"?"RoomHouseNavigation":"ClassroomNavigation")+".asset";
    var old=AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path);var generated=surface.navMeshData;surface.RemoveData();
    EditorUtility.CopySerialized(generated,old);EditorUtility.SetDirty(old);surface.navMeshData=old;surface.AddData();UnityEngine.Object.DestroyImmediate(generated);
    var definition=AssetDatabase.LoadAssetAtPath<MapDefinition>("Assets/VRFPS/Configuration/Maps/"+name+".asset");
    var count=map.GetSpawnPositions(definition).Length;
    Debug.Log("USER_LAYOUT_ANCHORS "+name+" "+count+"/"+map.enemySpawns.Length);
    EditorSceneManager.SaveScene(scene);Debug.Log("USER_LAYOUT_SYNCED "+name);
   }AssetDatabase.SaveAssets();
  }
 }
}
