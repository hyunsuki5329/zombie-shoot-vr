using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace VRFPS.Tests {
 public class ClassroomMapTests {
  [Test] public void SchoolHasCalibratedStructureAndReachableAnchorsWithoutScanDependency(){
   const string path="Assets/VRFPS/Scenes/Maps/Classroom.unity";
   Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path),Is.Not.Null,"Missing actual classroom map");
   var structure=AssetDatabase.LoadAssetAtPath<RoomStructureDefinition>("Assets/VRFPS/Configuration/Maps/ClassroomStructure.asset");
   Assert.That(structure,Is.Not.Null);structure.Validate();
   Assert.That(structure.measuredRoomSize.x,Is.EqualTo(14));Assert.That(structure.measuredRoomSize.z,Is.EqualTo(6.3f));
   Assert.That(structure.mapId,Is.EqualTo("classroom"));Assert.That(structure.themeId,Is.EqualTo("AbandonedSchool"));
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
   try{
   var map=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapRoot>()).Single();
   var config=AssetDatabase.LoadAssetAtPath<MapDefinition>("Assets/VRFPS/Configuration/Maps/Classroom.asset");
   Assert.That(map.GetSpawnPositions(config).Length,Is.EqualTo(map.enemySpawns.Length));Assert.That(map.RejectedSpawnCount,Is.Zero);
   Assert.That(map.visualRoot.GetComponentsInChildren<Renderer>().Length,Is.GreaterThan(30));
   var dependencies=AssetDatabase.GetDependencies(path,true);
   Assert.That(dependencies.Any(p=>p.EndsWith(".fbx")||p.Contains("External/RoomScan")||p.Contains("ClassroomScan")),Is.False);
   Assert.That(map.collisionRoot.GetComponentsInChildren<MeshCollider>().Length,Is.GreaterThan(20),"Furniture collision must use its actual mesh");
   }finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
 }
}

