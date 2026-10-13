using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace VRFPS.Editor {
 // Extract visual library only. Classroom dimensions and collision belong to its structure data.
 public static class SchoolAssetsSetup {
  public const string Source="Assets/VRFPS/External/AbandonedSchool/";
  public const string Output=Source+"Generated/";
  sealed class Batch {
   public readonly List<Vector3> vertices=new List<Vector3>();
   public readonly List<Vector3> normals=new List<Vector3>();
   public readonly List<Vector2> uv=new List<Vector2>();
   public readonly List<int> triangles=new List<int>();
  }
  public static void Configure(){
   if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch editor");
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"source/model.fbx");
   if(model==null)throw new FileNotFoundException("Restore abandoned-school.zip first");
   Directory.CreateDirectory(Output);AssetDatabase.Refresh();
   var materials=new Dictionary<int,Material>();
   foreach(var spec in new[]{("Desk","table",.75f),("Chair","chair",.9f),("TeacherDesk","teacher_table",.8f),("Board","whitebord/frount_whitebord1",1.2f),("Wall","classrom_frount_wall",3f),("Floor","ground",0f)}){
    var group=model.transform.Find(spec.Item2);
    if(group==null)throw new InvalidDataException("Missing source group "+spec.Item2);
    Extract(group,spec.Item1,spec.Item3,materials);
   }
   AssetDatabase.SaveAssets();Debug.Log("SCHOOL_LIBRARY_CREATED prefabs=6");
  }
  static void Extract(Transform source,string name,float targetHeight,Dictionary<int,Material> materials){
   var batches=new Dictionary<int,Batch>();
   foreach(var f in source.GetComponentsInChildren<MeshFilter>(true)){
    var mesh=f.sharedMesh;var vs=mesh.vertices;var uv=mesh.uv;var ns=mesh.normals;
    var matrix=f.transform.localToWorldMatrix;
    var normalMatrix=matrix.inverse.transpose;
    var triangles=mesh.triangles;
    if(uv.Length!=vs.Length||ns.Length!=vs.Length)throw new InvalidDataException("Missing UV/normals: "+f.name);
    for(int t=0;t<triangles.Length;t+=3){
     var center=(uv[triangles[t]]+uv[triangles[t+1]]+uv[triangles[t+2]])/3f;
     int u=Mathf.FloorToInt(center.x),v=Mathf.FloorToInt(center.y),tile=1001+u+10*v;
     if(tile<1001||tile>1009)throw new InvalidDataException("Unsupported UDIM "+tile);
     if(!batches.TryGetValue(tile,out var batch)){batch=new Batch();batches.Add(tile,batch);}
     for(int k=0;k<3;k++){
      int id=triangles[t+(matrix.determinant<0?2-k:k)];var mapped=uv[id]-new Vector2(u,v);
      if(mapped.x<-.001f||mapped.x>1.001f||mapped.y<-.001f||mapped.y>1.001f)throw new InvalidDataException("Triangle crosses texture tiles: "+f.name);
      batch.triangles.Add(batch.vertices.Count);batch.vertices.Add(matrix.MultiplyPoint3x4(vs[id]));batch.normals.Add(normalMatrix.MultiplyVector(ns[id]).normalized);batch.uv.Add(mapped);
     }
    }
   }
   var all=batches.Values.SelectMany(b=>b.vertices).ToArray();if(all.Length==0)throw new InvalidDataException("Empty selection");
   var bounds=new Bounds(all[0],Vector3.zero);foreach(var p in all)bounds.Encapsulate(p);
   // Preserve proportions. These canonical library sizes are replaced by measured placement later.
   float scale=targetHeight>0?targetHeight/bounds.size.y:10f/Mathf.Max(bounds.size.x,bounds.size.z);
   var origin=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
   var root=new GameObject(name);
   try{
    foreach(var pair in batches){
     var b=pair.Value;var mesh=new Mesh{name=name+"-"+pair.Key,indexFormat=IndexFormat.UInt32};
     mesh.SetVertices(b.vertices.Select(p=>(p-origin)*scale).ToList());mesh.SetNormals(b.normals);mesh.SetUVs(0,b.uv);mesh.SetTriangles(b.triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();
     var path=Output+name+"-"+pair.Key+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
     if(old==null){AssetDatabase.CreateAsset(mesh,path);old=mesh;}else{EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);}
     if(!materials.TryGetValue(pair.Key,out var mat)){mat=MakeMaterial(pair.Key);materials.Add(pair.Key,mat);}
     var child=new GameObject("Tile "+pair.Key,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(root.transform,false);
     child.GetComponent<MeshFilter>().sharedMesh=old;child.GetComponent<MeshRenderer>().sharedMaterial=mat;
    }
    PrefabUtility.SaveAsPrefabAsset(root,Output+name+".prefab");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  static Material MakeMaterial(int tile){
   var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"textures/model_"+tile+"_BaseColor.png");
   if(tex==null)throw new FileNotFoundException("Missing base color tile "+tile);
   var normalPath=Source+"textures/model_"+tile+"_Normal.png";
   var importer=AssetImporter.GetAtPath(normalPath) as TextureImporter;
   if(importer!=null&&importer.textureType!=TextureImporterType.NormalMap){importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}
   var path=Output+"Tile"+tile+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
   mat.SetTexture("_BaseMap",tex);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",1);mat.SetFloat("_Metallic",1);
   mat.SetTexture("_MetallicGlossMap",PackMetalSmoothness(tile));mat.EnableKeyword("_METALLICSPECGLOSSMAP");
   mat.SetFloat("_WorkflowMode",1);mat.SetFloat("_SmoothnessTextureChannel",0);
   mat.DisableKeyword("_SPECULAR_SETUP");mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
   mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));mat.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(mat);return mat;
  }
  static Texture2D PackMetalSmoothness(int tile){
   Texture2D metal=null,rough=null,packed=null;
   try{
    metal=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
    rough=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
    if(!metal.LoadImage(File.ReadAllBytes(Source+"textures/model_"+tile+"_Metalness.png"))||
       !rough.LoadImage(File.ReadAllBytes(Source+"textures/model_"+tile+"_Roughness.png")))throw new InvalidDataException("Unreadable PBR maps "+tile);
    if(metal.width!=rough.width||metal.height!=rough.height)throw new InvalidDataException("PBR map size mismatch "+tile);
    var pixels=metal.GetPixels32();var roughPixels=rough.GetPixels32();
    for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(pixels[i].r,0,0,(byte)(255-roughPixels[i].r));
    packed=new Texture2D(metal.width,metal.height,TextureFormat.RGBA32,false,true);
    packed.SetPixels32(pixels);packed.Apply();
    var path=Output+"Tile"+tile+"-MetalSmooth.png";
    File.WriteAllBytes(path,packed.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
    importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;
    importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
   }finally{
    if(metal!=null)UnityEngine.Object.DestroyImmediate(metal);
    if(rough!=null)UnityEngine.Object.DestroyImmediate(rough);
    if(packed!=null)UnityEngine.Object.DestroyImmediate(packed);
   }
  }

 }
}
