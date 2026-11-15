using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.AI.Navigation;
namespace VRFPS.Editor {
 public static class ClassroomSetup {
  public const string ScenePath="Assets/VRFPS/Scenes/Maps/Classroom.unity";
  public const string StructurePath="Assets/VRFPS/Configuration/Maps/ClassroomStructure.asset";
  const string NavigationPath="Assets/VRFPS/Configuration/Maps/ClassroomNavigation.asset";
  static GameObject Prefab(string name){var p=AssetDatabase.LoadAssetAtPath<GameObject>(SchoolAssetsSetup.Output+name+".prefab");if(p==null)throw new FileNotFoundException(name);return p;}
  static Bounds BoundsOf(GameObject p){var rs=p.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  static T Save<T>(T value,string path)where T:UnityEngine.Object{
   var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old==null){AssetDatabase.CreateAsset(value,path);return value;}
   EditorUtility.CopySerialized(value,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(value);return old;
  }
  public static void Configure(){
   if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch editor");
   var d=ScriptableObject.CreateInstance<RoomStructureDefinition>();d.mapId="classroom";d.themeId="AbandonedSchool";
   d.measuredRoomSize=new Vector3(14,3,6.3f);d.ceilingHeightEstimated=true;d.gameplayScale=1;
   var elements=new List<StructureElement>();var visuals=new Dictionary<string,string>();
   void Add(string id,StructureKind kind,Vector3 center,Vector3 size,string visual,float yaw=0){
    elements.Add(new StructureElement{id=id,kind=kind,origin=kind==StructureKind.Floor?StructureOrigin.Measured:StructureOrigin.ScanEstimated,
     localCenter=center,size=size,localEuler=new Vector3(0,yaw,0),collision=StructureCollision.Solid,
     navigation=kind==StructureKind.Floor?StructureNavigation.WalkableSurface:StructureNavigation.BlockingGeometry});visuals[id]=visual;
   }
   Add("Floor",StructureKind.Floor,new Vector3(0,-.1f,0),new Vector3(14,.2f,6.3f),"Floor");
   Add("Ceiling",StructureKind.Ceiling,new Vector3(0,3.1f,0),new Vector3(14,.2f,6.3f),"Ceiling");
   Add("North wall",StructureKind.Wall,new Vector3(0,1.5f,3.25f),new Vector3(.2f,3,14.4f),"Wall",90);
   Add("South wall",StructureKind.Wall,new Vector3(0,1.5f,-3.25f),new Vector3(.2f,3,14.4f),"Wall",90);
   Add("West wall",StructureKind.Wall,new Vector3(-7.1f,1.5f,0),new Vector3(.2f,3,6.3f),"Wall");
   Add("East wall",StructureKind.Wall,new Vector3(7.1f,1.5f,0),new Vector3(.2f,3,6.3f),"Wall");
   void Furniture(string id,string prefab,float x,float z,float yaw){var b=BoundsOf(Prefab(prefab));Add(id,prefab=="Chair"?StructureKind.Chair:StructureKind.Desk,new Vector3(x,b.size.y/2,z),b.size,prefab,yaw);}
   // Five scan-observed student furniture bands. Individual counts are a documented estimate.
   float[] xs={-3.8f,-1.4f,1f,3.3f,5.6f};float[] zs={-2.25f,-1.35f,-.45f,.45f};
   for(int c=0;c<xs.Length;c++)for(int r=0;r<zs.Length;r++){
    Furniture("Desk "+c+"-"+r,"Desk",xs[c],zs[r],90);
    Furniture("Chair "+c+"-"+r,"Chair",xs[c]+.55f,zs[r],90);
   }
   Furniture("Teacher desk","TeacherDesk",-5.7f,-.9f,0);
   Furniture("Teacher chair","Chair",-6.45f,-.9f,-90);
   var board=BoundsOf(Prefab("Board"));Add("Board",StructureKind.Obstacle,new Vector3(-6.9f,1.65f,0),board.size,"Board",0);
   d.elements=elements.ToArray();d.playerSpawn=new Vector3(-5.8f,0,2);
   d.playerEuler=new Vector3(0,90,0);
   d.enemySpawns=new[]{new Vector3(-6,0,2.4f),new Vector3(-2,0,2.4f),new Vector3(2,0,2.4f),new Vector3(6,0,2.4f),new Vector3(-2.32f,0,-1),new Vector3(.05f,0,-1),new Vector3(2.42f,0,-1),new Vector3(4.72f,0,-1)};
   d.Validate();Save(d,StructurePath);AssetDatabase.SaveAssets();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   d=AssetDatabase.LoadAssetAtPath<RoomStructureDefinition>(StructurePath);var map=RoomStructureSetup.GenerateStructure(d);
   var owned=new GameObject("SchoolThemeGenerated");owned.transform.SetParent(map.visualRoot,false);var marker=owned.AddComponent<GeneratedThemeRoot>();marker.mapId=d.mapId;marker.themeId=d.themeId;
   foreach(var e in d.elements){
    string type=visuals[e.id];GameObject visual;
    var prefab=Prefab(type=="Ceiling"?"Floor":type);var b=BoundsOf(prefab);visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab);visual.transform.SetParent(owned.transform,false);
    Vector3 scale=Vector3.one;
    if(type=="Wall")scale=new Vector3(e.size.x/b.size.x,e.size.y/b.size.y,e.size.z/b.size.z);
    if(type=="Floor"||type=="Ceiling")scale=new Vector3(e.size.x/b.size.x,1,e.size.z/b.size.z);
    visual.transform.localScale=scale;visual.transform.localRotation=type=="Ceiling"?Quaternion.Euler(180,0,0):Quaternion.Euler(e.localEuler);
    visual.transform.localPosition=e.localCenter-visual.transform.localRotation*Vector3.Scale(b.center,scale);
    if(type=="Floor")visual.transform.localPosition+=Vector3.up*.1f;
    if(type=="Ceiling")visual.transform.localPosition-=Vector3.up*.1f;
    visual.name=e.id;
    if(e.kind==StructureKind.Desk||e.kind==StructureKind.Chair||e.id=="Board"){
     var collision=map.collisionRoot.Find(e.id);UnityEngine.Object.DestroyImmediate(collision.GetComponent<BoxCollider>());
     foreach(var f in visual.GetComponentsInChildren<MeshFilter>()){
      var g=new GameObject(f.name);g.transform.SetParent(collision,false);g.layer=LayerMask.NameToLayer("WorldSolid");
      g.transform.position=f.transform.position;g.transform.rotation=f.transform.rotation;g.transform.localScale=f.transform.lossyScale;
      g.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
     }
    }
   }
   Physics.SyncTransforms();var surface=map.GetComponent<NavMeshSurface>();surface.BuildNavMesh();
   var nav=Save(surface.navMeshData,NavigationPath);surface.RemoveData();surface.navMeshData=nav;surface.AddData();
   var definition=AssetDatabase.LoadAssetAtPath<MapDefinition>("Assets/VRFPS/Configuration/Maps/Classroom.asset");
   var anchors=map.GetSpawnPositions(definition);if(anchors.Length!=d.enemySpawns.Length)throw new InvalidOperationException("Rejected classroom anchors: "+map.RejectedSpawnCount);
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.46f,.5f);
   var light=new GameObject("School daylight").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,-35,0);light.color=new Color(.8f,.88f,1);light.intensity=1.2f;
   if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Could not save classroom");AssetDatabase.SaveAssets();
   EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(x=>x.path!=ScenePath).Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
   Debug.Log("CLASSROOM_CREATED 14x6.3 anchors="+anchors.Length+" desks=20 estimated");
  }
  public static void Build(){
   if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch editor");
   var scenes=new[]{"Assets/VRFPS/Scenes/Bootstrap.unity","Assets/VRFPS/Scenes/Combat.unity","Assets/VRFPS/Scenes/Maps/Room.unity",ScenePath};
   foreach(var p in scenes)if(!File.Exists(p))throw new FileNotFoundException(p);
   var notices=new[]{"GrannyHouse-Attribution.txt","AbandonedSchool-Attribution.txt"};
   foreach(var n in notices)if(!File.Exists("Assets/VRFPS/Documentation/"+n))throw new FileNotFoundException(n);
   EditorBuildSettings.scenes=scenes.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
   var previous=PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone);bool timings=PlayerSettings.enableFrameTimingStats;
   try{
    PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.enableFrameTimingStats=true;
    const string dir="Builds/Windows/AbandonedSchool/";Directory.CreateDirectory(dir);
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=dir+PlayerSettings.productName + ".exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
    if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Two-map build failed");
    var bad=report.packedAssets.SelectMany(p=>p.contents).Where(p=>p.sourceAssetPath.Contains("External/RoomScan")||p.sourceAssetPath.Contains("ClassroomScan")||p.sourceAssetPath==SchoolAssetsSetup.Source+"source/model.fbx").ToArray();
    if(bad.Length!=0)throw new InvalidOperationException("Raw scan/source model leaked into build");
    foreach(var n in notices)File.Copy("Assets/VRFPS/Documentation/"+n,dir+n,true);
    File.WriteAllText(dir+"scan-dependency-check.txt","Raw scans / original school model packed: 0\n"+report.summary.guid+"\n");
    Debug.Log("CLASSROOM_BUILD_SUCCEEDED "+report.summary.guid);
   }finally{PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,previous);PlayerSettings.enableFrameTimingStats=timings;}
  }
  public static void ConfigureAndRender(){Configure();Render();}
  public static void Render(){
   EditorSceneManager.OpenScene(ScenePath);var camera=new GameObject("Inspection camera").AddComponent<Camera>();camera.fieldOfView=80;camera.nearClipPlane=.04f;camera.farClipPlane=50;
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;
   var rt=new RenderTexture(1280,720,24);rt.Create();var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
   try{Directory.CreateDirectory("Builds/Previews");
    var positions=new[]{new Vector3(-6,1.65f,2.3f),new Vector3(6.4f,1.65f,2.2f)};
    for(int i=0;i<positions.Length;i++){camera.transform.position=positions[i];camera.transform.LookAt(new Vector3(0,.8f,-.4f));RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Builds/Previews/school-"+i+".png",image.EncodeToPNG());}
   }finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
   Debug.Log("CLASSROOM_RENDERED");
  }
 }
}

