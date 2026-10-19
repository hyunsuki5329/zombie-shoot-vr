using System.Linq;using NUnit.Framework;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace VRFPS.Tests {
 public class AuthoredMapScaleTests {
  [TestCase("Room",8.6f,7f,.6f)] [TestCase("Classroom",28f,12.6f,1.5f)]
  public void SavedMapSeparatesSpaceAndFurnitureScale(string name,float width,float depth,float furnitureScale){
   try {EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Maps/"+name+".unity");var map=Object.FindFirstObjectByType<MapRoot>();Physics.SyncTransforms();
    var floor=map.collisionRoot.Find("Floor").GetComponent<Collider>();Assert.That(floor.bounds.size.x,Is.EqualTo(width).Within(.01));Assert.That(floor.bounds.size.z,Is.EqualTo(depth).Within(.01));
    if(name=="Room")foreach(var id in new[]{"Corridor left","Corridor right","Corridor end"}){var rs=map.visualRoot.GetComponentsInChildren<Transform>().Single(t=>t.name==id).GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);Assert.That(b.size.y,Is.EqualTo(4.9f).Within(.02),id);}
    var theme=map.GetComponentsInChildren<Transform>().Single(t=>t.name==(name=="Room"?"ThemeGenerated":"SchoolThemeGenerated"));
    foreach(Transform t in theme){if(name=="Room"?new[]{"Chair","Bed","Desk","Wardrobe","Keyboard"}.Contains(t.name):t.name.StartsWith("Desk ")||t.name.StartsWith("Chair ")||t.name.StartsWith("Teacher ")){
     var meshes=t.GetComponentsInChildren<MeshFilter>();var colliders=map.collisionRoot.Find(t.name).GetComponentsInChildren<MeshCollider>();Assert.That(colliders.Length,Is.EqualTo(meshes.Length),t.name);
     for(int i=0;i<meshes.Length;i++){Assert.That(colliders[i].sharedMesh,Is.SameAs(meshes[i].sharedMesh),t.name);foreach(var point in new[]{Vector3.zero,Vector3.one,Vector3.up})Assert.That(Vector3.Distance(colliders[i].transform.TransformPoint(point),meshes[i].transform.TransformPoint(point)),Is.LessThan(.001f),t.name);}

    }}
   }finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
  [Test] public void ChairKeepsRealLegsWithoutSeparatePlank(){var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/VRFPS/External/GrannyHouse/Generated/Chair-0.asset");Assert.That(mesh,Is.Not.Null);var v=mesh.vertices;var ids=mesh.triangles;Assert.That(ids.Any(i=>v[i].x<-.5f&&v[i].z<-.5f),Is.False);Assert.That(ids.Any(i=>v[i].x<0&&v[i].y< -1&&v[i].z<-.5f),Is.True);Assert.That(ids.Any(i=>v[i].x>0&&v[i].y< -1&&v[i].z<-.5f),Is.True);}
 }
}
