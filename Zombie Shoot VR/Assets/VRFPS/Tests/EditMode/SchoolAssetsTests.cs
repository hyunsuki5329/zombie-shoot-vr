using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace VRFPS.Tests {
 public class SchoolAssetsTests {
  [Test] public void FurnitureUsesStandaloneMeshesAndNormalizedTextureTiles(){
   const string root="Assets/VRFPS/External/AbandonedSchool/Generated/";
   foreach(var name in new[]{"Desk","Chair","TeacherDesk","Board","Wall","Floor"}){
    var p=AssetDatabase.LoadAssetAtPath<GameObject>(root+name+".prefab");
    Assert.That(p,Is.Not.Null,"Missing "+name);
    var filters=p.GetComponentsInChildren<MeshFilter>();
    Assert.That(filters.Length,Is.GreaterThan(0));
    foreach(var f in filters){
     Assert.That(f.sharedMesh.triangles.Length,Is.GreaterThan(0));
     foreach(var uv in f.sharedMesh.uv){Assert.That(uv.x,Is.InRange(-.001f,1.001f));Assert.That(uv.y,Is.InRange(-.001f,1.001f));}
     var m=f.GetComponent<Renderer>().sharedMaterial;
     Assert.That(m.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(m.mainTexture,Is.Not.Null);
     Assert.That(m.GetTexture("_MetallicGlossMap"),Is.Not.Null,"Missing source metal/roughness response");
     Assert.That(m.IsKeywordEnabled("_METALLICSPECGLOSSMAP"),Is.True);
     Assert.That(m.GetFloat("_Smoothness"),Is.EqualTo(1));
     var packedImporter=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m.GetTexture("_MetallicGlossMap")));
     Assert.That(packedImporter.sRGBTexture,Is.False);
     Assert.That(m.GetTexture("_BumpMap"),Is.Not.Null);Assert.That(m.IsKeywordEnabled("_NORMALMAP"),Is.True);
    }
    Assert.That(AssetDatabase.GetDependencies(root+name+".prefab",true).Any(x=>x.EndsWith(".fbx")),Is.False,"Do not pack entire original school");
   }
  }
 }
}
