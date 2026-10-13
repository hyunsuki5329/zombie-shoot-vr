using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace VRFPS.Tests
{
 public class RoomHousePreviewTests
 {
  const string Path="Assets/VRFPS/Scenes/Maps/RoomHousePreview.unity";
  [Test] public void HousePreviewUsesIndependentStructureAndPrefabsWithReachableAnchors()
  {
   Assert.That(File.Exists(Path),Is.True,"Missing independent abandoned house preview");
   Assert.That(AssetDatabase.GetDependencies(Path,true).Where(p=>p.Contains("External/RoomScan")),Is.Empty,"Final house must not reference the private scan or its NavMesh");
   var scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
   try
   {
    var map=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapRoot>()).Single();
    var marker=map.visualRoot.GetComponentInChildren<GeneratedThemeRoot>();
    Assert.That(marker,Is.Not.Null);Assert.That(marker.themeId,Is.EqualTo("AbandonedHouse"));
    Assert.That(map.visualRoot.GetComponentsInChildren<Collider>(),Is.Empty);
    var floor=map.collisionRoot.Find("Floor").GetComponent<BoxCollider>();Assert.That(floor.size,Is.EqualTo(new Vector3(4.3f,.2f,3.5f)));
    var definition=AssetDatabase.LoadAssetAtPath<MapDefinition>("Assets/VRFPS/Configuration/Maps/Room.asset");
    Assert.That(map.GetSpawnPositions(definition).Length,Is.EqualTo(7));
    Assert.That(map.visualRoot.GetComponentsInChildren<Renderer>().Length,Is.GreaterThan(20));
    var data=AssetDatabase.LoadAssetAtPath<RoomStructureDefinition>("Assets/VRFPS/Configuration/Maps/RoomStructure.asset");Assert.That(data,Is.Not.Null);data.Validate();
    var theme=AssetDatabase.LoadAssetAtPath<EnvironmentThemeProfile>("Assets/VRFPS/Art/Environment/AbandonedHouse/AbandonedHouse.asset");
    Assert.That(theme,Is.Not.Null);Assert.That(theme.allowPlaceholders,Is.False);Assert.That(theme.visuals.Length,Is.EqualTo(data.elements.Length));
    foreach(var binding in theme.visuals)Assert.That(binding.prefab,Is.Not.Null);
   }finally {EditorSceneManager.CloseScene(scene,true);}
  }
 }
}
