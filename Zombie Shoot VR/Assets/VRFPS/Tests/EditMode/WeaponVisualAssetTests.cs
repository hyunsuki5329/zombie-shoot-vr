using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace VRFPS.Tests {
 public class WeaponVisualAssetTests {
  [Test] public void ImportedVisualsPreserveInteractiveAnchorsAndHaveUsableGeometry() {
   var rig=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/PlayerRig.prefab");
   var gun=rig.GetComponentInChildren<K2Controller>(true);
   var visual=gun.transform.Find("GAMGO Visual");
   Assert.That(visual,Is.Not.Null,"K2 final model missing");
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
