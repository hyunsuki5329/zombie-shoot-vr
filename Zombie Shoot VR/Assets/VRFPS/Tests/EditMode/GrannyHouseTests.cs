using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace VRFPS.Tests {
 public class GrannyHouseTests {
  [Test] public void FurnitureAndSurfacesUseExtractedAssetsWithoutImportingWholeHouse(){
   const string root="Assets/VRFPS/External/GrannyHouse/Generated/";
   var profile=AssetDatabase.LoadAssetAtPath<EnvironmentThemeProfile>(root+"GrannyHouse.asset");
   Assert.That(profile,Is.Not.Null,"Missing extracted furniture theme");
   Assert.That(profile.visuals.Length,Is.EqualTo(18));
   foreach(var id in new[]{"Bed","Desk","Chair","Wardrobe","Keyboard"}){
    var binding=profile.visuals.Single(x=>x.elementId==id);
    Assert.That(binding.prefab.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.vertexCount),Is.GreaterThan(30));
    Assert.That(binding.prefab.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m.mainTexture!=null)),Is.True);
   }
   var wall=profile.visuals.Single(x=>x.elementId=="Side wall -1").prefab;
   var face=wall.transform.Find("Source surface").GetComponent<Renderer>().bounds;
   var trim=wall.transform.Find("Skirting").GetComponent<Renderer>().bounds;
   Assert.That(face.min.y,Is.GreaterThanOrEqualTo(trim.max.y-.0001f),"Wallpaper must not overlap trim and z-fight");
   var dependencies=AssetDatabase.GetDependencies("Assets/VRFPS/Scenes/Maps/RoomHousePreview.unity",true);
   Assert.That(dependencies.Any(p=>p.StartsWith(root)),Is.True);
   Assert.That(dependencies.Any(p=>p.EndsWith("untitled.fbx")),Is.False,"Only extracted meshes should be packed");
   Assert.That(dependencies.Any(p=>p.Contains("External/RoomScan")),Is.False);
   var scene=EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Maps/Room.unity",OpenSceneMode.Additive);
   try{
    var map=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapRoot>()).Single();
    var filters=map.visualRoot.GetComponentsInChildren<MeshFilter>();
    Assert.That(filters.Length,Is.GreaterThan(18),"Production Room prefab references must survive restoration");
    foreach(var f in filters){Assert.That(f.sharedMesh,Is.Not.Null);foreach(var m in f.GetComponent<Renderer>().sharedMaterials){Assert.That(m,Is.Not.Null);Assert.That(m.mainTexture,Is.Not.Null);}}
   }finally{EditorSceneManager.CloseScene(scene,true);}
  }
 }
}
