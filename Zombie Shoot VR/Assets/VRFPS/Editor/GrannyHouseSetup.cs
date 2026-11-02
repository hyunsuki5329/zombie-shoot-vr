using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace VRFPS.Editor {
 // Local supplied asset only; original house layout is never instantiated in the game.
 public static class GrannyHouseSetup {
  public const string Source="Assets/VRFPS/External/GrannyHouse/";
  public const string Output=Source+"Generated/";
  struct Piece {public Mesh mesh; public Material material;}
  [Serializable]class MetaEntry{public string path,content;}
  [Serializable]class MetaList{public MetaEntry[] entries;}
  static Dictionary<string,string> metadata;
  public static void Configure(){
   if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch editor");
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"source/untitled.fbx");
   if(model==null)throw new FileNotFoundException("Restore user-supplied Granny house archive first");
   metadata=JsonUtility.FromJson<MetaList>(File.ReadAllText(Source+"GenerationMetadata.json")).entries.ToDictionary(e=>Source+e.path,e=>e.content);
   Directory.CreateDirectory(Output);AssetDatabase.Refresh();
   var data=AssetDatabase.LoadAssetAtPath<RoomStructureDefinition>(RoomStructureSetup.AssetPath).CreateGameplayCopy(2.5f);
   var profile=ScriptableObject.CreateInstance<EnvironmentThemeProfile>();profile.allowPlaceholders=false;
   profile.plaster=Material("Wallpaper","HouseBakedTextureWithTransparentWindows2.png",new Vector2(.248f,.248f),new Vector2(.751f,.751f));
   profile.wood=Material("Floorboards","WoodPlanksFloors0026_1_350.jpg",Vector2.one,Vector2.zero);
   profile.woodDark=Material("OldWood","old wood.png",Vector2.one,Vector2.zero);
   profile.fabric=Material("Bedroom","FurnitureSovrum2.png",Vector2.one,Vector2.zero);
   profile.metal=profile.woodDark;
   var hall=Material("Hall","FurnitureHall2.png",Vector2.one,Vector2.zero);
   var living=Material("Living","Vardagsrum2.png",Vector2.one,Vector2.zero);
   var drawer=Material("DeskDrawers","SkrivbordsLoda2.png",Vector2.one,Vector2.zero);
   var bedroomDrawer=Material("BedroomDrawers","Byroloda2.png",Vector2.one,Vector2.zero);
   var bindings=new List<ThemeVisualBinding>();
   foreach(var e in data.elements){
    var pieces=new List<Piece>();float yaw=0;
    void Extract(string name,Vector3 min,Vector3 max,Material material){
     var f=model.GetComponentsInChildren<MeshFilter>(true).Single(t=>t.name==name);
     pieces.Add(new Piece{mesh=ExtractRegion(f,min,max),material=material});
    }
    switch(e.id){
     case "Bed":Extract("FurnitureSovrum1",new Vector3(-18,-3,26),new Vector3(-12,1,36),profile.fabric);break;
     case "Desk":
      Extract("HallFurniture2",new Vector3(-17,-10,33),new Vector3(-12,-5,36),hall);
      Extract("Loda",Vector3.one*-100,Vector3.one*100,drawer);Extract("Loda_1",Vector3.one*-100,Vector3.one*100,drawer);break;
     case "Chair":Extract("Furniture___FIXAD",new Vector3(12.7f,-9,14.5f),new Vector3(14.9f,-5,16.8f),living);break;
     case "Wardrobe":Extract("HallFurniture2",new Vector3(-5,-9,24),new Vector3(-3.5f,-2,29),hall);yaw=90;break;
     case "Keyboard":
      Extract("FurnitureSovrum1",new Vector3(-12,-2,19.7f),new Vector3(-8,2,21.2f),profile.fabric);
      Extract("ByroLoda_2",Vector3.one*-100,Vector3.one*100,bedroomDrawer);Extract("ByroLoda_3",Vector3.one*-100,Vector3.one*100,bedroomDrawer);yaw=90;break;
    }
    var root=new GameObject(e.id);
    try{
     if(pieces.Count>0){
      Fit(pieces.Select(p=>p.mesh).ToArray(),e.size,Quaternion.Euler(0,yaw,0));
      for(int i=0;i<pieces.Count;i++)Add(root,Save(pieces[i].mesh,Output+e.id+"-"+i+".asset"),pieces[i].material,"Extracted "+i);
     }else{
      // Keep collision shell, replace its appearance with source surface materials.
      var isFloor=e.kind==StructureKind.Floor;
      var mat=isFloor?profile.wood:e.kind==StructureKind.Ceiling?profile.woodDark:profile.plaster;
      var faceSize=e.size;if(e.kind==StructureKind.Wall)faceSize.y-=.14f;
      Add(root,Save(BoxMesh(faceSize,isFloor),Output+e.id+"-surface.asset"),mat,"Source surface");
      if(e.kind==StructureKind.Wall)root.transform.GetChild(0).localPosition=Vector3.up*.07f;
      if(e.kind==StructureKind.Wall){
       var trimSize=e.size;trimSize.y=.14f;
       Add(root,Save(BoxMesh(trimSize,false),Output+e.id+"-trim.asset"),profile.woodDark,"Skirting");
       root.transform.GetChild(1).localPosition=Vector3.down*(e.size.y-.14f)*.5f;
      }
     }
     SeedMeta(Output+e.id+".prefab");
     var prefab=PrefabUtility.SaveAsPrefabAsset(root,Output+e.id+".prefab");
     bindings.Add(new ThemeVisualBinding{elementId=e.id,size=e.size,prefab=prefab});
    }finally{UnityEngine.Object.DestroyImmediate(root);}
   }
   profile.visuals=bindings.ToArray();Save(profile,Output+"GrannyHouse.asset");AssetDatabase.SaveAssets();
   UnityEngine.Object.DestroyImmediate(data);
   var scene=EditorSceneManager.OpenScene(RoomHouseSetup.Preview);
   data=AssetDatabase.LoadAssetAtPath<RoomStructureDefinition>(RoomStructureSetup.AssetPath).CreateGameplayCopy(2.5f);
   profile=AssetDatabase.LoadAssetAtPath<EnvironmentThemeProfile>(Output+"GrannyHouse.asset");
   var map=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapRoot>()).Single();
   RoomThemeGenerator.Generate(map,data,profile);
   if(!EditorSceneManager.SaveScene(scene))throw new IOException("Preview save failed");
   UnityEngine.Object.DestroyImmediate(data);AssetDatabase.SaveAssets();Debug.Log("GRANNY_THEME_CREATED");
  }
  static Mesh ExtractRegion(MeshFilter filter,Vector3 min,Vector3 max){
   var source=filter.sharedMesh;var vertices=source.vertices.Select(filter.transform.TransformPoint).ToArray();var uv=source.uv;var triangles=source.triangles;
   bool Inside(int i){var p=vertices[i];return p.x>=min.x&&p.y>=min.y&&p.z>=min.z&&p.x<=max.x&&p.y<=max.y&&p.z<=max.z;}
   var ids=new Dictionary<int,int>();var points=new List<Vector3>();var coords=new List<Vector2>();var indices=new List<int>();
   for(int i=0;i<triangles.Length;i+=3){if(!Inside(triangles[i])||!Inside(triangles[i+1])||!Inside(triangles[i+2]))continue;
    for(int j=0;j<3;j++){int id=triangles[i+j];if(!ids.TryGetValue(id,out int mapped)){mapped=points.Count;ids[id]=mapped;points.Add(vertices[id]);coords.Add(uv[id]);}indices.Add(mapped);}}
   if(indices.Count<3)throw new InvalidOperationException("Empty furniture extraction: "+filter.name);
   var mesh=new Mesh{name=filter.name+" extracted"};mesh.SetVertices(points);mesh.SetUVs(0,coords);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
  static void Fit(Mesh[] meshes,Vector3 target,Quaternion rotation){
   var points=meshes.SelectMany(m=>m.vertices).Select(v=>rotation*v).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
   if(bounds.size.x<.001f||bounds.size.y<.001f||bounds.size.z<.001f)throw new InvalidOperationException("Flat furniture extraction");
   var scale=new Vector3(target.x/bounds.size.x,target.y/bounds.size.y,target.z/bounds.size.z);
   foreach(var mesh in meshes){mesh.vertices=mesh.vertices.Select(v=>Vector3.Scale(rotation*v-bounds.center,scale)).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();}
  }
  static Mesh BoxMesh(Vector3 size,bool tile){var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);try{var mesh=UnityEngine.Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);mesh.vertices=mesh.vertices.Select(v=>Vector3.Scale(v,size)).ToArray();if(tile)mesh.uv=mesh.uv.Select(v=>Vector2.Scale(v,new Vector2(size.x/2,size.z/2))).ToArray();mesh.RecalculateBounds();return mesh;}finally{UnityEngine.Object.DestroyImmediate(cube);}}
  static void Add(GameObject root,Mesh mesh,Material material,string name){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;}
  static Material Material(string name,string texture,Vector2 scale,Vector2 offset){var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"textures/"+texture);if(t==null)throw new FileNotFoundException(texture);var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetTexture("_BaseMap",t);m.SetTextureScale("_BaseMap",scale);m.SetTextureOffset("_BaseMap",offset);m.SetFloat("_Smoothness",.08f);m.SetFloat("_Cull",0);return Save(m,Output+name+".mat");}
  static void SeedMeta(string path){
   if(!metadata.TryGetValue(path+".meta",out var content))throw new InvalidOperationException("Missing stable metadata: "+path);
   if(!File.Exists(path))File.WriteAllText(path+".meta",content);
   else if(!File.Exists(path+".meta") || !File.ReadAllText(path+".meta").Contains(System.Text.RegularExpressions.Regex.Match(content,@"guid: [a-f0-9]+").Value))
    throw new InvalidOperationException("Existing asset GUID differs from restoration manifest: "+path);
  }
  // Native mesh/material GUIDs are internal to the regenerated prefabs. Only the
  // prefab GUIDs referenced by committed scenes must match across restorations.
  static T Save<T>(T value,string path)where T:UnityEngine.Object{var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old==null){AssetDatabase.CreateAsset(value,path);return value;}EditorUtility.CopySerialized(value,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(value);return old;}
 }
}
