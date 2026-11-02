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
    var visuals=theme.Cast<Transform>().ToArray();
    if(visuals.Select(t=>t.name).Distinct().Count()!=visuals.Length)throw new InvalidOperationException("Duplicate furniture names: "+name);
    var names=visuals.Select(t=>t.name).ToHashSet();
    // Old manual duplicates lived directly under MapRoot, outside the generated collision root.
    // Only strip invisible duplicates whose names match a current visual; preserve other authored objects.
    foreach(var duplicate in map.transform.Cast<Transform>().Where(t=>names.Contains(t.name)&&t.GetComponentsInChildren<Renderer>(true).Length==0).ToArray()){
     foreach(var collider in duplicate.GetComponentsInChildren<Collider>(true)){Debug.Log("REMOVED_STALE_COLLIDER "+name+"/"+duplicate.name);UnityEngine.Object.DestroyImmediate(collider);}
     foreach(var modifier in duplicate.GetComponentsInChildren<NavMeshModifier>(true))UnityEngine.Object.DestroyImmediate(modifier);
    }

    foreach(var stale in map.collisionRoot.Cast<Transform>().Where(t=>!names.Contains(t.name)).ToArray())UnityEngine.Object.DestroyImmediate(stale.gameObject);
    foreach(Transform visual in theme){
     var key=visual.name.ToLowerInvariant();
     if(key.Contains("floor")||key.Contains("ceiling")||key.Contains("lintel"))continue;
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
   System.IO.File.Copy("Assets/VRFPS/Scenes/Maps/Room.unity",RoomHouseSetup.Preview,true);AssetDatabase.Refresh();
  }
 }
}
