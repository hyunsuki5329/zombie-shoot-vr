using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace VRFPS.Tests {
 public class WeaponVisualAssetTests {
  [Test] public void ImportedVisualsPreserveInteractiveAnchorsAndHaveUsableGeometry() {
   var rig=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/PlayerRig.prefab");
   var gun=rig.GetComponentInChildren<K2Controller>(true);
   Assert.That(gun.transform.localScale,Is.EqualTo(Vector3.one*1.2f));
   Assert.That(Vector3.Distance(gun.GetComponentInChildren<MagazineToken>(true).transform.lossyScale,Vector3.one*1.2f),Is.LessThan(.0001f));
   var visual=gun.transform.Find("GAMGO Visual");
   Assert.That(visual,Is.Not.Null,"K2 final model missing");
   foreach(var path in new[]{"Assets/VRFPS/Prefabs/Weapons/K2.prefab","Assets/VRFPS/Prefabs/Weapons/Magazine.prefab"}) {
    foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText(path), @"m_SourcePrefab: \{fileID: [^,]+, guid: ([0-9a-f]+)"))
     Assert.That(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value),Is.Not.Empty,"Unresolved serialized nested model in "+path);
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
    foreach(var t in prefab.GetComponentsInChildren<Transform>(true))
     Assert.That(PrefabUtility.IsPrefabAssetMissing(t.gameObject),Is.False,"Missing nested prefab: "+t.name);
   }
   Assert.That(gun.transform.Find("Muzzle").localPosition,Is.EqualTo(new Vector3(0,0,.72f)));
   Assert.That(gun.transform.Find("Foregrip").localPosition,Is.EqualTo(new Vector3(0,0,.3f)));
   Assert.That(gun.transform.Find("Foregrip").GetComponent<Renderer>().enabled,Is.False);
   Assert.That(visual.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(3));
   Assert.That(visual.GetComponentsInChildren<Collider>().Length,Is.Zero);
   foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>()) {
    Assert.That(renderer.gameObject.layer,Is.EqualTo(gun.gameObject.layer));
    foreach(var material in renderer.sharedMaterials){
     Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
     Assert.That(material.GetTexture("_BaseMap"),Is.Not.Null);
     Assert.That(material.GetTexture("_BumpMap"),Is.Not.Null);
    }
   }
   var magazine=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/Weapons/Magazine.prefab");
   Assert.That(magazine.transform.localScale,Is.EqualTo(Vector3.one*1.2f));
   var magVisual=magazine.transform.Find("GAMGO Visual");
   Assert.That(magVisual,Is.Not.Null);
   Assert.That(magazine.GetComponent<MagazineToken>(),Is.Not.Null);
   Assert.That(magazine.GetComponentsInChildren<Collider>().Length,Is.EqualTo(1));
   Assert.That(magazine.GetComponent<BoxCollider>().size,Is.EqualTo(new Vector3(.06f,.16f,.09f)));
   Assert.That(magVisual.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(1));
   Assert.That(magazine.transform.Find("Visual").gameObject.activeSelf,Is.False);
  }
 }
}
