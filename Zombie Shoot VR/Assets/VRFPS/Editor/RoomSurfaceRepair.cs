using GaussianSplatting.Runtime;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using VRFPS;
using Object=UnityEngine.Object;

// Authored approximation. Original splats remain intact; disable the group to restore their visibility.
public static class RoomSurfaceRepair
{
    public static void Apply()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Close the interactive Editor and use isolated batch authoring.");
        var backup=Path.Combine("Builds/Reproduction/RoomSurfaceRepair",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffff"));
        Directory.CreateDirectory(backup);
        File.Copy(RoomRealitySetup.ScenePath,Path.Combine(backup,"RoomReality.unity"));
        File.Copy(RoomRealitySetup.ScenePath+".meta",Path.Combine(backup,"RoomReality.unity.meta"));
        var scene=EditorSceneManager.OpenScene(RoomRealitySetup.ScenePath,OpenSceneMode.Single);
        var root=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<RealitySceneRoot>()).Single();
        if(root.transform.Find("Room Surface Repairs"))throw new InvalidOperationException("Repairs already exist; edit that group instead of creating duplicates.");
        var renderer=root.GetComponentInChildren<GaussianSplatRenderer>();
        renderer.enabled=false;
        var group=CreateGroup(renderer);group.transform.SetParent(root.transform,true);
        const string folder="Assets/VRFPS/External/RealityGS/Room/SurfaceRepairs";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/VRFPS/External/RealityGS/Room","SurfaceRepairs");
        var materials=group.GetComponentsInChildren<MeshRenderer>().Select(x=>x.sharedMaterial).Distinct().ToArray();
        for(var i=0;i<materials.Length;i++)
        {
            var path=folder+"/"+(i==0?"Wall":"Wood")+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing)
            {
                foreach(var mesh in group.GetComponentsInChildren<MeshRenderer>())if(mesh.sharedMaterial==materials[i])mesh.sharedMaterial=existing;
                Object.DestroyImmediate(materials[i]);
            }
            else AssetDatabase.CreateAsset(materials[i],path);
        }
        root.Validate(AssetDatabase.LoadAssetAtPath<RealityIntroProfile>(RoomRealitySetup.ProfilePath));
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save repaired Room scene.");
        Debug.Log("ROOM_SURFACE_REPAIR_APPLIED backup="+Path.GetFullPath(backup));
    }
    public static GameObject CreateGroup(GaussianSplatRenderer renderer)
    {
        if(Vector3.Distance(renderer.transform.position,new Vector3(0,.85f,0))>.001f || Quaternion.Angle(renderer.transform.rotation,Quaternion.Euler(180,0,0))>.01f || Vector3.Distance(renderer.transform.lossyScale,Vector3.one)>.001f)
            throw new InvalidOperationException("Room calibration differs from reviewed repair coordinates; realign patches before authoring.");
        var group=new GameObject("Room Surface Repairs");
        var wall=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        wall.SetColor("_BaseColor",new Color(.66f,.64f,.60f));
        var wood=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        wood.SetColor("_BaseColor",new Color(.35f,.24f,.14f));
        void Panel(string name,Vector3 center,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
            go.transform.SetParent(group.transform,false);go.transform.position=center;
            go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        Panel("Bed wall backing",new Vector3(-.88f,1.4f,2.48f),new Vector3(2.94f,2.8f,.025f),wall);
        Panel("Above door backing",new Vector3(1.1f,2.5f,2.48f),new Vector3(1.1f,.6f,.025f),wall);
        Panel("Left wall backing",new Vector3(-2.35f,1.4f,1.7f),new Vector3(.025f,2.8f,1.56f),wall);
        Panel("Wardrobe side backing",new Vector3(-2.27f,1.25f,.2f),new Vector3(.025f,2.5f,1.5f),wood);
        var cuts=new List<GaussianCutout>();
        if(renderer.m_Cutouts!=null)cuts.AddRange(renderer.m_Cutouts);
        void Cut(string name,Vector3 center,Vector3 size)
        {
            var go=new GameObject(name);go.transform.SetParent(group.transform,false);
            go.transform.position=center;go.transform.localScale=size*.5f;
            var cut=go.AddComponent<GaussianCutout>();cut.m_Type=GaussianCutout.Type.Box;cut.m_Invert=true;cuts.Add(cut);
        }
        Cut("Bed wall noise",new Vector3(-1.05f,2.65f,2.65f),new Vector3(2.6f,2.9f,2.8f));
        Cut("Above door noise",new Vector3(.95f,3.2f,2.65f),new Vector3(1.4f,2f,2.8f));
        Cut("Left upper wall noise",new Vector3(-2.6f,2.65f,1.7f),new Vector3(1.8f,2.9f,1.5f));
        Cut("Wardrobe side noise",new Vector3(-2.65f,1.4f,.2f),new Vector3(1.4f,2.4f,1.5f));
        renderer.m_Cutouts=cuts.ToArray();
        return group;
    }
}
