using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Unity.AI.Navigation;

namespace VRFPS.Editor
{
    public static class ScannedRoomSetup
    {
        public const string Source = "Assets/VRFPS/External/RoomScan/";
        public const string ScenePath = "Assets/VRFPS/Scenes/Maps/Room.unity";
        const string Generated = Source + "Generated/";
        public static Vector3 Calibrate(Vector3 p) => new Vector3((p.x - .015f) * (4.3f / 4.18f), p.y - .02f, (p.z - .025f) * (3.5f / 3.75f));

        public static void Configure()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use an isolated batch editor.");
            if (!File.Exists(Source + "Room.fbx")) throw new FileNotFoundException("Restore the user's room FBX first.");
            Directory.CreateDirectory(Generated); Directory.CreateDirectory(Source + "Textures");
            Directory.CreateDirectory("Assets/VRFPS/Scenes/Maps"); AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(Source + "Room.fbx");
            importer.isReadable = true; importer.addCollider = false; importer.SaveAndReimport();
            importer.ExtractTextures(Source + "Textures"); AssetDatabase.Refresh();
            var texturePath = AssetDatabase.FindAssets("t:Texture2D", new[] { Source + "Textures" }).Select(AssetDatabase.GUIDToAssetPath).Single();
            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            textureImporter.maxTextureSize = 4096; textureImporter.mipmapEnabled = true; textureImporter.sRGBTexture = true;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ; textureImporter.SaveAndReimport();
            var scanMaterial = Material("ScannedRoom", Color.white);
            scanMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            EditorUtility.SetDirty(scanMaterial);
            var mesh = PrepareMesh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Room - User Scan 2026-10-12");
            var map = root.AddComponent<MapRoot>();
            Transform Child(string name) { var o = new GameObject(name); o.transform.SetParent(root.transform, false); return o.transform; }
            map.visualRoot = Child("VisualRoot"); map.collisionRoot = Child("CollisionRoot");
            var scan = new GameObject("Calibrated scan surface", typeof(MeshFilter), typeof(MeshRenderer));
            scan.transform.SetParent(map.visualRoot, false); scan.GetComponent<MeshFilter>().sharedMesh = mesh;
            scan.GetComponent<MeshRenderer>().sharedMaterial = scanMaterial;
            scan.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var plaster = Material("Wall repairs", new Color(.72f, .70f, .64f));
            var floor = Material("Floor repairs", new Color(.31f, .20f, .12f));
            var furniture = Material("Furniture repairs", new Color(.27f, .23f, .18f));
            // Backing surfaces fill only holes; the original textured scan remains in front.
            Box(map.visualRoot, "Floor backing", new Vector3(0,-.08f,0), new Vector3(4.5f,.06f,3.7f), floor, false);
            Box(map.visualRoot, "Ceiling repair", new Vector3(0,2.50f,0), new Vector3(4.5f,.12f,3.7f), plaster, false);
            foreach (int sign in new[] {-1,1})
            {
                Box(map.visualRoot, "Side wall repair " + sign, new Vector3(sign*2.27f,1.23f,0), new Vector3(.10f,2.6f,3.7f), plaster, false);
                if(sign<0)Box(map.visualRoot, "End wall repair " + sign, new Vector3(0,1.23f,sign*1.88f), new Vector3(4.5f,2.6f,.10f), plaster, false);
            }
            Solid(map.collisionRoot,"Floor",new Vector3(0,-.1f,0),new Vector3(4.3f,.2f,3.5f));
            Solid(map.collisionRoot,"Ceiling",new Vector3(0,2.55f,0),new Vector3(4.6f,.2f,3.8f));
            foreach(int sign in new[]{-1,1})
            {
                Solid(map.collisionRoot,"Side wall " + sign,new Vector3(sign*2.225f,1.225f,0),new Vector3(.15f,2.45f,3.8f));
                if(sign<0)Solid(map.collisionRoot,"End wall " + sign,new Vector3(0,1.225f,sign*1.825f),new Vector3(4.6f,2.45f,.15f));
            }
            // Game-authored entrance extension, not part of the measured scan.
            Solid(map.collisionRoot,"Door wall left",new Vector3(-.775f,1.225f,1.825f),new Vector3(2.75f,2.45f,.15f));
            Solid(map.collisionRoot,"Door wall right",new Vector3(2.075f,1.225f,1.825f),new Vector3(.15f,2.45f,.15f));
            Solid(map.collisionRoot,"Door lintel",new Vector3(1.3f,2.4f,1.825f),new Vector3(1.4f,.1f,.15f));
            Box(map.visualRoot,"Door backing left",new Vector3(-.775f,1.225f,1.88f),new Vector3(2.75f,2.45f,.1f),plaster,false);
            Box(map.visualRoot,"Door backing right",new Vector3(2.15f,1.225f,1.88f),new Vector3(.3f,2.45f,.1f),plaster,false);
            Box(map.visualRoot,"Door lintel repair",new Vector3(1.3f,2.4f,1.88f),new Vector3(1.4f,.1f,.1f),plaster,false);
            Solid(map.collisionRoot,"Corridor floor",new Vector3(1.05f,-.1f,3.15f),new Vector3(2.2f,.2f,2.8f),true);
            Solid(map.collisionRoot,"Corridor ceiling",new Vector3(1.05f,2.55f,3.15f),new Vector3(2.4f,.2f,3));
            Solid(map.collisionRoot,"Corridor left",new Vector3(-.125f,1.225f,3.15f),new Vector3(.15f,2.45f,2.8f));
            Solid(map.collisionRoot,"Corridor right",new Vector3(2.225f,1.225f,3.15f),new Vector3(.15f,2.45f,2.8f));
            Solid(map.collisionRoot,"Corridor end",new Vector3(1.05f,1.225f,4.625f),new Vector3(2.5f,2.45f,.15f));
            Box(map.visualRoot,"Corridor floor",new Vector3(1.05f,-.03f,3.15f),new Vector3(2.2f,.06f,2.8f),floor,false);
            Box(map.visualRoot,"Corridor ceiling",new Vector3(1.05f,2.50f,3.15f),new Vector3(2.4f,.1f,3),plaster,false);
            Box(map.visualRoot,"Corridor left",new Vector3(-.125f,1.225f,3.15f),new Vector3(.15f,2.45f,2.8f),plaster,false);
            Box(map.visualRoot,"Corridor right",new Vector3(2.225f,1.225f,3.15f),new Vector3(.15f,2.45f,2.8f),plaster,false);
            Box(map.visualRoot,"Corridor end",new Vector3(1.05f,1.225f,4.625f),new Vector3(2.5f,2.45f,.15f),plaster,false);
            Solid(map.collisionRoot,"Bed",new Vector3(1.54f,.32f,-.76f),new Vector3(1.22f,.64f,1.98f));
            Solid(map.collisionRoot,"Desk",new Vector3(-1.41f,.42f,.05f),new Vector3(1.48f,.84f,1.22f));
            Solid(map.collisionRoot,"Chair",new Vector3(-.47f,.48f,.04f),new Vector3(.60f,.96f,.65f));
            Solid(map.collisionRoot,"Wardrobe",new Vector3(-1.42f,1.15f,1.22f),new Vector3(1.46f,2.30f,1.06f));
            Solid(map.collisionRoot,"Keyboard",new Vector3(-1.96f,.45f,-1.19f),new Vector3(.38f,.9f,1.0f));
            // Keep the floor visible under the tabletop, but close the worst torn desk faces.
            Box(map.visualRoot,"Desk surface repair",new Vector3(-1.44f,.76f,.04f),new Vector3(1.35f,.06f,1.05f),furniture,false);
            var surface = root.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
            surface.layerMask = LayerMask.GetMask("WorldSolid"); surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = .04f; surface.BuildNavMesh();
            var data = Save(surface.navMeshData, Generated + "RoomNavigation.asset"); surface.RemoveData(); surface.navMeshData = data; surface.AddData();
            map.playerSpawn = Child("PlayerSpawn"); map.playerSpawn.position = new Vector3(.9f,0,1.1f); map.playerSpawn.rotation = Quaternion.Euler(0,180,0);
            map.enemySpawns = new[]{new Vector3(-.95f,0,-1.10f),new Vector3(-.3f,0,-1.15f),new Vector3(.3f,0,-1.10f),new Vector3(1.5f,0,1.15f),new Vector3(.10f,0,1.1f),new Vector3(.65f,0,3.7f),new Vector3(1.55f,0,3.7f)}
                .Select((p,i)=> { var t=Child("EnemySpawn "+i);t.position=p;return t; }).ToArray();
            var definition = AssetDatabase.LoadAssetAtPath<MapDefinition>("Assets/VRFPS/Configuration/Maps/Room.asset");
            var positions = map.GetSpawnPositions(definition);
            if (positions.Length != map.enemySpawns.Length) throw new InvalidOperationException("Fix invalid room spawn anchors before saving: "+map.RejectedSpawnCount);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(x=>x.path!=ScenePath).Append(new EditorBuildSettingsScene(ScenePath,true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("VRFPS_SCANNED_ROOM_CONFIGURED spawns="+positions.Length+" triangles="+mesh.triangles.Length/3);
        }
        static Mesh PrepareMesh()
        {
            var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Room.fbx"));
            try
            {
                var filter=instance.GetComponentInChildren<MeshFilter>(); var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);
                var vertices=mesh.vertices.Select(p=>Calibrate(filter.transform.TransformPoint(p))).ToArray();
                var indices=mesh.triangles;var kept=new List<int>(indices.Length);
                for(int i=0;i<indices.Length;i+=3)
                {
                    var p=(vertices[indices[i]]+vertices[indices[i+1]]+vertices[indices[i+2]])/3;
                    // Remove scan fragments suspended in the unobstructed central walking strip.
                    if(p.x>.6f && p.x<2.0f && p.z>1.58f && p.y>-.03f && p.y<2.35f)continue;
                    if(p.x>-.04f && p.x<.72f && p.z>-1.48f && p.z<1.48f && p.y>.12f && p.y<1.9f)continue;
                    kept.Add(indices[i]);kept.Add(indices[i+1]);kept.Add(indices[i+2]);
                }
                mesh.name="Calibrated Room Scan";mesh.vertices=vertices;mesh.triangles=kept.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
                return Save(mesh,Generated+"RoomSurface.asset");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
        static T Save<T>(T asset,string path) where T:UnityEngine.Object
        {
            var previous=AssetDatabase.LoadAssetAtPath<T>(path);
            if(previous==null){AssetDatabase.CreateAsset(asset,path);return asset;}
            EditorUtility.CopySerialized(asset,previous);EditorUtility.SetDirty(previous);UnityEngine.Object.DestroyImmediate(asset);return previous;
        }
        static Material Material(string name,Color color)
        {
            var path=Generated+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Cull",2);EditorUtility.SetDirty(material);return material;
        }
        static GameObject Box(Transform parent,string name,Vector3 center,Vector3 size,Material material,bool solid)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=center;o.transform.localScale=size;
            var renderer=o.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            if(!solid)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
            return o;
        }
        static void Solid(Transform parent,string name,Vector3 center,Vector3 size,bool walkable=false)
        {
            var o=new GameObject(name,typeof(BoxCollider));o.transform.SetParent(parent,false);o.transform.localPosition=center;o.GetComponent<BoxCollider>().size=size;
            o.layer=LayerMask.NameToLayer("WorldSolid");
            if(name!="Floor" && !walkable) {var modifier=o.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;}
        }
        public static void Build()
        {
            var scenes=new[]{"Assets/VRFPS/Scenes/Bootstrap.unity","Assets/VRFPS/Scenes/Combat.unity",ScenePath};
            if(scenes.Any(p=>!File.Exists(p)))throw new FileNotFoundException("Configure the actual Room scene before building.");
            var previous=PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);var timing=PlayerSettings.enableFrameTimingStats;
            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.enableFrameTimingStats=true;
                var path=Path.GetFullPath("Builds/Windows/RoomScan/RoomScan.exe");Directory.CreateDirectory(Path.GetDirectoryName(path));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Room build failed: "+report.summary.result);
                Debug.Log("VRFPS_ROOM_BUILD_SUCCEEDED "+path);
            }
            finally {PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,previous);PlayerSettings.enableFrameTimingStats=timing;}
        }
    }
}
