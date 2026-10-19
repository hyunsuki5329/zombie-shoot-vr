using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.AI.Navigation;

namespace VRFPS.Editor
{
    public static class RoomHouseSetup
    {
        public const string Art="Assets/VRFPS/Art/Environment/AbandonedHouse/";
        public const string Preview="Assets/VRFPS/Scenes/Maps/RoomHousePreview.unity";
        public const string Navigation="Assets/VRFPS/Configuration/Maps/RoomHouseNavigation.asset";
        public const float GameplayScale=2.5f;
        static Mesh cube;
        struct Part { public Vector3 center,size;public Material material; }

        public static void ConfigurePreview()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch editor");
            if(AssetDatabase.LoadAssetAtPath<RoomStructureDefinition>(RoomStructureSetup.AssetPath)==null)RoomStructureSetup.CaptureRoom();
            var data=AssetDatabase.LoadAssetAtPath<RoomStructureDefinition>(RoomStructureSetup.AssetPath).CreateGameplayCopy(GameplayScale);data.Validate();
            if(data.mapId!="room")throw new InvalidOperationException("Room house setup only accepts the measured room");
            Directory.CreateDirectory(Art+"Meshes");Directory.CreateDirectory(Art+"Prefabs");AssetDatabase.Refresh();
            var profile=AssetDatabase.LoadAssetAtPath<EnvironmentThemeProfile>(Art+"AbandonedHouse.asset");
            if(profile==null){profile=ScriptableObject.CreateInstance<EnvironmentThemeProfile>();AssetDatabase.CreateAsset(profile,Art+"AbandonedHouse.asset");}
            profile.mapId="room";profile.themeId="AbandonedHouse";profile.allowPlaceholders=false;
            profile.plaster=MakeMaterial("Weathered plaster",new Color(.48f,.49f,.40f),false,.05f);
            profile.wood=MakeMaterial("Aged timber",new Color(.29f,.20f,.115f),true,.14f);
            profile.woodDark=MakeMaterial("Dark stained timber",new Color(.18f,.13f,.08f),true,.08f);
            profile.fabric=MakeMaterial("Old linen",new Color(.43f,.405f,.30f),false,.02f);
            profile.metal=MakeMaterial("Oxidized iron",new Color(.12f,.14f,.13f),false,.22f);
            profile.metal.SetFloat("_Metallic",.5f);
            var temporary=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temporary.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(temporary);
            profile.visuals=data.elements.Where(e=>e.kind!=StructureKind.DoorOpening).Select(e=>new ThemeVisualBinding{elementId=e.id,size=e.size,prefab=MakePrefab(e,profile,data.gameplayScale)}).ToArray();
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            // NewScene unloads unused assets; reacquire saved assets after the scene transition.
            data=AssetDatabase.LoadAssetAtPath<RoomStructureDefinition>(RoomStructureSetup.AssetPath).CreateGameplayCopy(GameplayScale);
            profile=AssetDatabase.LoadAssetAtPath<EnvironmentThemeProfile>(Art+"AbandonedHouse.asset");
            var map=RoomStructureSetup.GenerateStructure(data);
            RoomThemeGenerator.Generate(map,data,profile);
            var surface=map.GetComponent<NavMeshSurface>();surface.BuildNavMesh();
            var navigation=Save(surface.navMeshData,Navigation);surface.RemoveData();surface.navMeshData=navigation;surface.AddData();
            var definition=AssetDatabase.LoadAssetAtPath<MapDefinition>("Assets/VRFPS/Configuration/Maps/Room.asset");
            if(map.GetSpawnPositions(definition).Length!=7)throw new InvalidOperationException("House must preserve seven reachable anchors");
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.38f,.41f,.45f);RenderSettings.ambientIntensity=1;
            var light=new GameObject("Soft room light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(55,-25,0);
            light.color=new Color(1,.80f,.56f);light.intensity=1.1f;light.range=7;light.shadows=LightShadows.None;
            var corridor=new GameObject("Corridor cold light").AddComponent<Light>();corridor.type=LightType.Point;corridor.transform.position=new Vector3(1.1f,2.15f,3.7f)*GameplayScale;
            corridor.color=new Color(.60f,.72f,.84f);corridor.intensity=1.5f;corridor.range=5*GameplayScale;corridor.shadows=LightShadows.None;
            if(!EditorSceneManager.SaveScene(scene,Preview))throw new IOException("Failed to save house preview");AssetDatabase.SaveAssets();
            var dependencies=AssetDatabase.GetDependencies(Preview,true);
            if(dependencies.Any(x=>x.Contains("External/RoomScan")))throw new InvalidOperationException("Private scan dependency in house preview");
            Debug.Log("VRFPS_HOUSE_PREVIEW_CREATED prefabs="+profile.visuals.Length+" renderers="+map.visualRoot.GetComponentsInChildren<Renderer>().Length);
        }

        public static void RenderPreview()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch editor");
            EditorSceneManager.OpenScene(Preview);
            var camera=new GameObject("Preview camera").AddComponent<Camera>();
            camera.fieldOfView=82;camera.nearClipPlane=.04f;camera.farClipPlane=30;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.06f,.07f);
            var target=new RenderTexture(1280,720,24);target.Create();
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                Directory.CreateDirectory("Builds/Previews");
                var positions=new[]{new Vector3(.55f*GameplayScale,1.65f,1.4f*GameplayScale),new Vector3(.15f*GameplayScale,1.6f,-1.25f*GameplayScale)};
                var looks=new[]{new Vector3(-.45f,1.0f,-.5f)*GameplayScale,new Vector3(.8f,1.1f,2.5f)*GameplayScale};
                for(int i=0;i<positions.Length;i++)
                {
                    camera.transform.position=positions[i];camera.transform.LookAt(looks[i]);
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                    File.WriteAllBytes("Builds/Previews/house-"+i+".png",image.EncodeToPNG());
                }
                Debug.Log("VRFPS_HOUSE_PREVIEWS_RENDERED");
            }
            finally {RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
        }

        public static void PromoteAndBuild()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch editor");
            var dependencies=AssetDatabase.GetDependencies(Preview,true);
            if(dependencies.Any(x=>x.Contains("External/RoomScan")))throw new InvalidOperationException("Private scan dependency in house");
            const string attribution="Assets/VRFPS/Documentation/GrannyHouse-Attribution.txt";
            bool usesGranny=dependencies.Any(x=>x.StartsWith(GrannyHouseSetup.Source,StringComparison.Ordinal));
            if(usesGranny && !File.Exists(attribution))throw new FileNotFoundException("Missing Granny asset attribution",attribution);
            // Preserve the original photo reference locally. It is never part of the build list.
            var reference=ScannedRoomSetup.Source+"ReferenceRoom.unity";
            if(File.Exists(ScannedRoomSetup.ScenePath) && AssetDatabase.GetDependencies(ScannedRoomSetup.ScenePath,true).Any(p=>p.StartsWith(ScannedRoomSetup.Source,StringComparison.Ordinal)))
                EnsureReferenceBackup(ScannedRoomSetup.ScenePath,reference);
            var scene=EditorSceneManager.OpenScene(Preview);
            if(!EditorSceneManager.SaveScene(scene,ScannedRoomSetup.ScenePath))throw new IOException("Failed to save themed Room; build stopped");
            AssetDatabase.SaveAssets();
            var scenes=new[]{"Assets/VRFPS/Scenes/Bootstrap.unity","Assets/VRFPS/Scenes/Combat.unity",ScannedRoomSetup.ScenePath};
            EditorBuildSettings.scenes=scenes.Select(x=>new EditorBuildSettingsScene(x,true)).ToArray();
            var previous=PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone);
            var timing=PlayerSettings.enableFrameTimingStats;
            try
            {
                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.enableFrameTimingStats=true;
                var path=Path.GetFullPath("Builds/Windows/AbandonedHouse/AbandonedHouse.exe");Directory.CreateDirectory(Path.GetDirectoryName(path));
                var report=UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions{scenes=scenes,locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("House build failed: "+report.summary.result);
                var scanAssets=report.packedAssets.SelectMany(p=>p.contents).Where(p=>p.sourceAssetPath.Contains("External/RoomScan")).Select(p=>p.sourceAssetPath).ToArray();
                if(scanAssets.Length!=0)throw new InvalidOperationException("Private scan assets leaked into build: "+string.Join(",",scanAssets));
                File.WriteAllText("Builds/Windows/AbandonedHouse/scan-dependency-check.txt","RoomScan packed assets: 0\n"+report.summary.guid+"\n");
                if(usesGranny)File.Copy(attribution,Path.Combine(Path.GetDirectoryName(path),"GrannyHouse-Attribution.txt"),true);
                Debug.Log("VRFPS_HOUSE_BUILD_SUCCEEDED "+path);
            }
            finally {PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,previous);PlayerSettings.enableFrameTimingStats=timing;}
        }

        public static void EnsureReferenceBackup(string source,string destination)
        {
            if(!File.Exists(source))throw new IOException("Missing source scene for backup");
            if(File.Exists(destination))
            {
                if(AssetDatabase.LoadAssetAtPath<SceneAsset>(destination)==null)throw new IOException("Existing reference is not a valid scene asset");
                return;
            }
            var directory=Path.GetDirectoryName(destination);
            if(!Directory.Exists(directory))throw new IOException("Backup folder is not available: "+directory);
            if(!AssetDatabase.CopyAsset(source,destination) || !File.Exists(destination) || AssetDatabase.LoadAssetAtPath<SceneAsset>(destination)==null)
                throw new IOException("Reference backup failed; original Room was not overwritten");
            if(!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(destination)))throw new IOException("Reference backup bytes differ; promotion stopped");
        }

        static GameObject MakePrefab(StructureElement e,EnvironmentThemeProfile p,float gameplayScale)
        {
            var parts=new List<Part>();var s=e.size/gameplayScale;
            void Box(Vector3 center,Vector3 size,Material material){if(size.x>0 && size.y>0 && size.z>0)parts.Add(new Part{center=center,size=size,material=material});}
            void FrameFront(float y,float height)
            {
                var z=s.z*.5f+.002f;float side=Mathf.Min(.045f,s.x*.1f);
                Box(new Vector3(-s.x*.5f+side*.5f,y,z),new Vector3(side,height,.008f),p.woodDark);
                Box(new Vector3(s.x*.5f-side*.5f,y,z),new Vector3(side,height,.008f),p.woodDark);
                Box(new Vector3(0,y+height*.5f-side*.5f,z),new Vector3(s.x,side,.008f),p.woodDark);
                Box(new Vector3(0,y-height*.5f+side*.5f,z),new Vector3(s.x,side,.008f),p.woodDark);
            }
            var material=e.kind==StructureKind.Floor?p.woodDark:e.kind==StructureKind.Wall||e.kind==StructureKind.Ceiling?p.plaster:p.wood;
            if(e.kind!=StructureKind.Bed)Box(Vector3.zero,s,material); // Full solid silhouette matches the existing proxy.
            if(e.kind==StructureKind.Floor)
            {
                int columns=Mathf.CeilToInt(s.x/.19f);float width=s.x/columns;
                for(int x=0;x<columns;x++)
                {
                    int rows=Mathf.CeilToInt(s.z/.8f);float length=s.z/rows;
                    for(int z=0;z<rows;z++)Box(new Vector3(-s.x*.5f+(x+.5f)*width,s.y*.5f+.002f,-s.z*.5f+(z+.5f)*length),new Vector3(width-.006f,.004f,length-.007f),(x+z)%5==0?p.woodDark:p.wood);
                }
            }
            else if(e.kind==StructureKind.Wall && s.y>.3f)
            {
                bool alongX=s.x>s.z;
                foreach(int side in new[]{-1,1})
                {
                    var basePos=new Vector3(0,-s.y*.5f+.075f,0);
                    if(alongX)basePos.z=side*(s.z*.5f+.003f);else basePos.x=side*(s.x*.5f+.003f);
                    Box(basePos,alongX?new Vector3(s.x,.15f,.012f):new Vector3(.012f,.15f,s.z),p.woodDark);
                    var rail=basePos;rail.y=-s.y*.5f+.9f;
                    Box(rail,alongX?new Vector3(s.x,.045f,.01f):new Vector3(.01f,.045f,s.z),p.wood);
                    float length=alongX?s.x:s.z;int count=Mathf.Max(1,Mathf.FloorToInt(length/.65f));
                    for(int i=0;i<count;i++)
                    {
                        var upright=basePos;upright.y=-s.y*.5f+.5f;
                        if(alongX)upright.x=-length*.5f+(i+.5f)*length/count;else upright.z=-length*.5f+(i+.5f)*length/count;
                        Box(upright,alongX?new Vector3(.028f,.68f,.009f):new Vector3(.009f,.68f,.028f),p.woodDark);
                    }
                }
            }
            else if(e.kind==StructureKind.Bed)
            {
                Box(new Vector3(0,-s.y*.225f,0),new Vector3(s.x,s.y*.55f,s.z),p.wood);
                Box(new Vector3(0,s.y*.275f,0),new Vector3(s.x,s.y*.45f,s.z),p.fabric);
                for(int i=0;i<7;i++)Box(new Vector3(0,s.y*.5f+.003f,-s.z*.46f+i*s.z*.15f),new Vector3(s.x-.06f,.006f,.012f),p.woodDark);
                FrameFront(-s.y*.17f,s.y*.55f);
            }
            else if(e.kind==StructureKind.Desk || e.id=="Wardrobe" || e.id=="Keyboard")
            {
                FrameFront(0,s.y);
                int drawers=e.id=="Wardrobe"?2:3;
                for(int i=0;i<drawers;i++)
                {
                    float y=-s.y*.5f+(i+.5f)*s.y/drawers;
                    Box(new Vector3(0,y-s.y/(2*drawers)+.015f,s.z*.5f+.003f),new Vector3(s.x-.05f,.015f,.009f),p.woodDark);
                    Box(new Vector3(0,y,s.z*.5f+.008f),new Vector3(Mathf.Min(.18f,s.x*.4f),.022f,.01f),p.metal);
                }
            }
            else if(e.kind==StructureKind.Chair)
            {
                // Covered seat / solid crate silhouette avoids an invisible solid under open chair legs.
                Box(new Vector3(0,s.y*.5f+.002f,0),new Vector3(s.x-.025f,.004f,s.z-.025f),p.fabric);
                FrameFront(0,s.y);
            }
            var root=new GameObject(e.id);
            try
            {
                int index=0;
                foreach(var group in parts.GroupBy(part=>part.material))
                {
                    var meshes=new List<Mesh>();
                    try
                    {
                        foreach(var part in group)
                        {
                            var mesh=UnityEngine.Object.Instantiate(cube);var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
                            for(int i=0;i<vertices.Length;i++)
                            {
                                var local=Vector3.Scale(vertices[i],part.size);vertices[i]=local+part.center;
                                uv[i]=Mathf.Abs(normals[i].y)>.5f?new Vector2(local.x,local.z):Mathf.Abs(normals[i].x)>.5f?new Vector2(local.z,local.y):new Vector2(local.x,local.y);
                            }
                            mesh.vertices=vertices;mesh.uv=uv;mesh.RecalculateBounds();meshes.Add(mesh);
                        }
                        var combined=new Mesh{name=e.id+" surface "+index};combined.CombineMeshes(meshes.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),true,true);
                        combined.vertices=combined.vertices.Select(v=>v*gameplayScale).ToArray();combined.RecalculateBounds();var saved=Save(combined,Art+"Meshes/"+e.id+"-"+index+".asset");
                        var child=new GameObject(group.Key.name,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(root.transform,false);
                        child.GetComponent<MeshFilter>().sharedMesh=saved;child.GetComponent<MeshRenderer>().sharedMaterial=group.Key;index++;
                    }
                    finally {foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);}
                }
                return PrefabUtility.SaveAsPrefabAsset(root,Art+"Prefabs/"+e.id+".prefab");
            }
            finally {UnityEngine.Object.DestroyImmediate(root);}
        }
        static Material MakeMaterial(string name,Color color,bool grain,float smoothness)
        {
            const int resolution=256;var pixels=new Color[resolution*resolution];
            for(int y=0;y<resolution;y++)for(int x=0;x<resolution;x++)
            {
                float u=x/(float)resolution,v=y/(float)resolution;
                float noise=Mathf.PerlinNoise(u*9+2,v*9+7)*.55f+Mathf.PerlinNoise(u*37+11,v*37+3)*.25f;
                float detail=grain?Mathf.Sin((u*95+Mathf.PerlinNoise(u*3,v*5)*4)*Mathf.PI)*.075f:Mathf.PerlinNoise(u*83,v*83)*.10f;
                float stain=Mathf.SmoothStep(.1f,.8f,Mathf.PerlinNoise(u*3+15,v*3+8));
                pixels[y*resolution+x]=color*Mathf.Clamp(.55f+noise+detail-stain*.22f,.35f,1.3f);pixels[y*resolution+x].a=1;
            }
            var texture=new Texture2D(resolution,resolution,TextureFormat.RGBA32,true){name=name,wrapMode=TextureWrapMode.Repeat};texture.SetPixels(pixels);texture.Apply();
            var saved=Save(texture,Art+name+".asset");var path=Art+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",saved);mat.SetFloat("_Smoothness",smoothness);EditorUtility.SetDirty(mat);return mat;
        }
        static T Save<T>(T value,string path)where T:UnityEngine.Object
        {
            var old=AssetDatabase.LoadAssetAtPath<T>(path);
            if(old==null){AssetDatabase.CreateAsset(value,path);return value;}
            EditorUtility.CopySerialized(value,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(value);return old;
        }
    }
}
