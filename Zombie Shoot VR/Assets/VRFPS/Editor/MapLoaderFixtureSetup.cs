using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
namespace VRFPS.Editor
{
    public static class MapLoaderFixtureSetup
    {
        const string Folder="Assets/VRFPS/Tests/Fixtures/MapLoading";
        public static void EnableForTests()
        {
            var paths=new[]{Folder+"/ValidMap.unity",Folder+"/InvalidMap.unity"};
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(x=>!paths.Contains(x.path))
                .Concat(paths.Select(x=>new EditorBuildSettingsScene(x,true))).ToArray();
            Debug.Log("VRFPS_MAP_TEST_SCENES_ENABLED");
        }
        public static void Configure()
        {
            if(!Application.isBatchMode)throw new System.InvalidOperationException("Use batchmode.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("SYNTHETIC MAP - TEST ONLY");var map=root.AddComponent<MapRoot>();
            Transform Child(string name,Vector3 position)
            {
                var o=new GameObject(name);o.transform.SetParent(root.transform);o.transform.position=position;return o.transform;
            }
            var offset=new Vector3(8000,0,0);
            map.visualRoot=Child("Visual",offset);map.collisionRoot=Child("Collision",offset);
            map.playerSpawn=Child("PlayerSpawn",offset);map.enemySpawns=new[]{Child("EnemySpawn",offset+Vector3.forward*4)};
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(map.collisionRoot);
            floor.layer=LayerMask.NameToLayer("WorldSolid");floor.transform.position=offset+Vector3.down*.1f;floor.transform.localScale=new Vector3(20,.2f,20);
            var surface=root.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;
            surface.layerMask=LayerMask.GetMask("WorldSolid");surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.BuildNavMesh();
            const string navPath=Folder+"/TestNavigation.asset";
            if(File.Exists(navPath))throw new System.InvalidOperationException("Fixture already exists; do not replace its navigation asset blindly.");
            AssetDatabase.CreateAsset(surface.navMeshData,navPath);
            EditorSceneManager.SaveScene(scene,Folder+"/ValidMap.unity");
            scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("INVALID TEST MAP - NO MAP ROOT");EditorSceneManager.SaveScene(scene,Folder+"/InvalidMap.unity");
            AssetDatabase.SaveAssets();Debug.Log("VRFPS_MAP_FIXTURES_READY");
        }
    }
}
