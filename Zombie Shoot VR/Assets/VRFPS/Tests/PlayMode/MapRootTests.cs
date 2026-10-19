using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
namespace VRFPS.Tests
{
    public class MapRootTests
    {
        GameObject root;
        MapRoot mapRoot;
        MapDefinition definition;
        NavMeshSurface surface;
        readonly Vector3 offset=new Vector3(6000,0,0);
        Transform Child(string name,Vector3 position)
        {
            var obj=new GameObject(name);obj.transform.SetParent(root.transform);obj.transform.position=position;return obj.transform;
        }
        [SetUp] public void SetUp()
        {
            root=new GameObject("Map fixture");mapRoot=root.AddComponent<MapRoot>();
            mapRoot.visualRoot=Child("Visual",offset);mapRoot.collisionRoot=Child("Collision",offset);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(mapRoot.collisionRoot);
            floor.layer=LayerMask.NameToLayer("WorldSolid");floor.transform.position=offset+Vector3.down*.1f;floor.transform.localScale=new Vector3(20,.2f,20);
            surface=root.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;
            surface.layerMask=LayerMask.GetMask("WorldSolid");surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.BuildNavMesh();
            mapRoot.playerSpawn=Child("Player spawn",offset);
            mapRoot.enemySpawns=new[]{Child("Valid",offset+Vector3.forward*4),Child("Outside",offset+Vector3.right*50)};
            definition=ScriptableObject.CreateInstance<MapDefinition>();definition.mapId="room";definition.sceneName="Room";
            definition.maxAlive=4;definition.maxRangedAlive=1;definition.minSpawnDistance=2;
        }
        [TearDown] public void TearDown()
        {
            var data=surface.navMeshData;surface.RemoveData();UnityEngine.Object.DestroyImmediate(root);
            if(data!=null)UnityEngine.Object.DestroyImmediate(data);UnityEngine.Object.DestroyImmediate(definition);
        }
        [Test] public void ExcludesInvalidCandidatesAndKeepsReachableSpawn()
        {
            var positions=mapRoot.GetSpawnPositions(definition);
            Assert.That(positions.Length,Is.EqualTo(1));Assert.That(mapRoot.RejectedSpawnCount,Is.EqualTo(1));
            Assert.That(Vector3.Distance(positions[0],offset+Vector3.forward*4),Is.LessThan(.3f));
            Assert.That(Vector3.Distance(mapRoot.PlayerPosition,offset),Is.LessThan(.3f));
        }
        [Test] public void KeepsReachableNearStartAnchorForLaterPlayerMovement()
        {
            mapRoot.enemySpawns=new[]{Child("Near start",offset+Vector3.right),Child("Far start",offset+Vector3.forward*4)};
            var positions=mapRoot.GetSpawnPositions(definition);
            Assert.That(positions.Length,Is.EqualTo(2),"Distance must be checked against the current player at spawn time, not permanently filter anchors near the initial spawn");
        }
        [Test] public void RejectsMissingGeometryBadPlayerAndNoValidEnemies()
        {
            var visual=mapRoot.visualRoot;mapRoot.visualRoot=null;
            Assert.Throws<InvalidOperationException>(()=>mapRoot.GetSpawnPositions(definition));mapRoot.visualRoot=visual;
            mapRoot.playerSpawn.position=offset+Vector3.up*30;
            Assert.Throws<InvalidOperationException>(()=>mapRoot.GetSpawnPositions(definition));mapRoot.playerSpawn.position=offset;
            mapRoot.enemySpawns[0].position=offset+Vector3.forward*.5f;
            Assert.Throws<InvalidOperationException>(()=>mapRoot.GetSpawnPositions(definition));
        }
        [Test] public void ExcludesNavigableButDisconnectedIsland()
        {
            var island=GameObject.CreatePrimitive(PrimitiveType.Cube);island.transform.SetParent(mapRoot.collisionRoot);
            island.layer=LayerMask.NameToLayer("WorldSolid");island.transform.position=offset+new Vector3(50,-.1f,0);
            island.transform.localScale=new Vector3(10,.2f,10);
            var previous=surface.navMeshData;surface.RemoveData();surface.BuildNavMesh();
            if(previous!=null && previous!=surface.navMeshData)UnityEngine.Object.DestroyImmediate(previous);
            Assert.That(NavMesh.SamplePosition(mapRoot.enemySpawns[1].position,out _,.25f,NavMesh.AllAreas),Is.True);
            var positions=mapRoot.GetSpawnPositions(definition);
            Assert.That(positions.Length,Is.EqualTo(1));Assert.That(mapRoot.RejectedSpawnCount,Is.EqualTo(1));
        }    }
}
