using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace VRFPS.Editor {
 // One-time migration of the saved, user-authored layouts. Never regenerates furniture placement.
 public static class AuthoredMapScaleSetup {
  const string Marker="Authored scale 20261016 space2 furniture1.5";
  static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new InvalidOperationException("Missing visual "+t.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  static bool Shell(string name){var n=name.ToLowerInvariant();return n.StartsWith("corridor")||n.Contains("floor")||n.Contains("ceiling")||n.Contains("wall")||n.Contains("lintel");}
  public static void Configure(){
   if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch editor");
   CleanChair();
   foreach(var name in new[]{"Room","Classroom"}){
    var scene=EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Maps/"+name+".unity");var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();
    if(map.transform.Find(Marker)!=null)continue;
    float space=name=="Room"?.8f:2f,furniture=name=="Room"?.6f:1.5f;
    var theme=map.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(name=="Room"?"ThemeGenerated":"SchoolThemeGenerated"));
    foreach(Transform t in theme){
     if(Shell(t.name)||(name=="Classroom"&&!t.name.ToLowerInvariant().Contains("desk")&&!t.name.ToLowerInvariant().Contains("chair"))){t.position*=space;t.localScale*=space;continue;}
     var before=BoundsOf(t);var target=new Vector3(before.center.x*space,before.min.y*space,before.center.z*space);
     t.localScale*=furniture;var after=BoundsOf(t);t.position+=target-new Vector3(after.center.x,after.min.y,after.center.z);
    }
    foreach(Transform t in map.collisionRoot)if(Shell(t.name)||(name=="Classroom"&&!t.name.ToLowerInvariant().Contains("desk")&&!t.name.ToLowerInvariant().Contains("chair"))){t.position*=space;t.localScale*=space;}
    map.playerSpawn.position*=space;foreach(var spawn in map.enemySpawns)spawn.position*=space;
    foreach(var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()))if(light.type!=LightType.Directional){light.transform.position*=space;light.range*=space;}
    new GameObject(Marker).transform.SetParent(map.transform,false);
    EditorSceneManager.SaveScene(scene);Debug.Log("AUTHORED_SCALE_APPLIED "+name+" space="+space+" furniture="+furniture);
   }
   AssetDatabase.SaveAssets();UserLayoutSetup.Configure();
   System.IO.File.Copy("Assets/VRFPS/Scenes/Maps/Room.unity",RoomHouseSetup.Preview,true);AssetDatabase.Refresh();
  }
  static void CleanChair(){
   var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(GrannyHouseSetup.Output+"Chair-0.asset");if(mesh==null)throw new InvalidOperationException("Restore chair asset first");
   // The source region included a disconnected tall plank beside the real front leg.
   // Its vertices are isolated at x[-.75,-.569], z[-.725,-.520]; keep every actual chair component.
   var vertices=mesh.vertices;var source=mesh.triangles;var kept=new System.Collections.Generic.List<int>();int removed=0;
   bool Plank(int i)=>vertices[i].x<-.5f && vertices[i].z<-.5f;
   for(int i=0;i<source.Length;i+=3){if(Plank(source[i])&&Plank(source[i+1])&&Plank(source[i+2])){removed++;continue;}kept.Add(source[i]);kept.Add(source[i+1]);kept.Add(source[i+2]);}
   if(removed>0){mesh.triangles=kept.ToArray();EditorUtility.SetDirty(mesh);}
   Debug.Log("CHAIR_STRAY_PLANK_TRIANGLES_REMOVED "+removed);
  }
 }
}
