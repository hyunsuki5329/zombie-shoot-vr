using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VRFPS;
using System.Linq;
using UnityEngine;

public static class ClassroomRealitySetup
{
    public const string Folder = "Assets/VRFPS/External/RealityGS/Classroom";
    public const string CaptureSha256 = "f8d591a9f6ddb478f003f46b0ad015751f4925e62938624407581485e93ff9b2";
    public static void Import()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use isolated batch project.");
        var input = Environment.GetEnvironmentVariable("VRFPS_CLASSROOM_PLY");
        if (string.IsNullOrEmpty(input) || !File.Exists(input)) throw new FileNotFoundException("Set VRFPS_CLASSROOM_PLY to the room capture.");
        using (var stream=File.OpenRead(input))
        using (var sha=SHA256.Create())
            if(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant()!=CaptureSha256)
                throw new InvalidOperationException("Classroom capture identity mismatch.");
        Directory.CreateDirectory(Folder);
        var local=Path.GetFullPath(Folder+"/classroom.ply");
        if(Path.GetFullPath(input)!=local) File.Copy(input,local,true);
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var creator=ScriptableObject.CreateInstance<GaussianSplatAssetCreator>();
        try
        {
            var quality=typeof(GaussianSplatAssetCreator).GetField("m_Quality",flags);
            quality.SetValue(creator,Enum.Parse(quality.FieldType,"Medium"));
            typeof(GaussianSplatAssetCreator).GetMethod("ApplyQualityLevel",flags).Invoke(creator,null);
            typeof(GaussianSplatAssetCreator).GetField("m_ImportCameras",flags).SetValue(creator,false);
            typeof(GaussianSplatAssetCreator).GetField("m_InputFile",flags).SetValue(creator,local);
            typeof(GaussianSplatAssetCreator).GetField("m_OutputFolder",flags).SetValue(creator,Folder);
            typeof(GaussianSplatAssetCreator).GetMethod("CreateAsset",flags).Invoke(creator,null);
        }
        finally { UnityEngine.Object.DestroyImmediate(creator); }
        AssetDatabase.SaveAssets();
        var asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(Folder+"/classroom.asset");
        if(!asset || asset.splatCount!=459968) throw new InvalidOperationException("Classroom import count mismatch.");
        Debug.Log("CLASSROOM_GS_IMPORTED: "+asset.splatCount+" sha256="+CaptureSha256);
    }


    public const string ScenePath = Folder+"/ClassroomReality.unity";
    public const string ProfilePath = Folder+"/ClassroomIntro.asset";
    public static void Configure()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch project.");
        if(File.Exists(ScenePath))throw new InvalidOperationException("Existing classroom scene requires an explicit backed-up revision.");
        var asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(Folder+"/classroom.asset");
        var virusPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/External/IntroVirus/IntroVirus.prefab");
        if(!asset||!virusPrefab)throw new InvalidOperationException("Import verified classroom and virus first.");
        var profile=AssetDatabase.LoadAssetAtPath<RealityIntroProfile>(ProfilePath);
        if(!profile){profile=ScriptableObject.CreateInstance<RealityIntroProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);}
        profile.mapId="classroom";profile.dataId="classroom-"+CaptureSha256;
        profile.realityScenePath=ScenePath;profile.realityAnchorKey="ClassroomObservation";profile.themeAnchorKey="playerSpawn";
        profile.metersPerUnit=.83f;profile.Validate();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var host=new GameObject("Classroom Reality");var root=host.AddComponent<RealitySceneRoot>();
        root.mapId=profile.mapId;root.dataId=profile.dataId;
        Transform Child(string name){var go=new GameObject(name);go.transform.SetParent(host.transform,false);return go.transform;}
        root.dataRoot=Child("Positive calibration");root.dataRoot.localScale=Vector3.one*.83f;
        var data=new GameObject("FBX aligned Classroom GS");data.transform.SetParent(root.dataRoot,false);
        // Registration to the preserved FBX-derived Unity reference, not a blind Room reflection.
        data.transform.localPosition=new Vector3(1.070572f,.156495f,1.853815f);
        data.transform.localRotation=Quaternion.Euler(180,92.285f,0);
        data.transform.localScale=new Vector3(-1.013465f,1,1.013465f);
        var renderer=data.AddComponent<GaussianSplatRenderer>();renderer.enabled=false;renderer.m_Asset=asset;
        const string package="Packages/org.nesnausk.gaussian-splatting/";
        renderer.m_ShaderSplats=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/RenderGaussianSplats.shader");
        renderer.m_ShaderComposite=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/GaussianComposite.shader");
        renderer.m_ShaderDebugPoints=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/GaussianDebugRenderPoints.shader");
        renderer.m_ShaderDebugBoxes=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/GaussianDebugRenderBoxes.shader");
        renderer.m_CSSplatUtilities=AssetDatabase.LoadAssetAtPath<ComputeShader>(package+"Shaders/SplatUtilities.compute");
        var source=data.AddComponent<GaussianRealitySource>();source.splats=renderer;source.dataId=profile.dataId;
        var crop=Child("Reversible rectangular boundary");crop.position=new Vector3(0,1.45f,0);crop.localScale=new Vector3(7.08f,1.55f,3.23f);
        var cut=crop.gameObject.AddComponent<GaussianCutout>();cut.m_Type=GaussianCutout.Type.Box;cut.m_Invert=false;renderer.m_Cutouts=new[]{cut};
        root.observationAnchor=Child(profile.realityAnchorKey);root.observationAnchor.position=new Vector3(-3.8f,0,1.65f);root.observationAnchor.rotation=Quaternion.Euler(0,90,0);
        root.collisionRoot=Child("WorldSolid proxies");

        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Boundary.mat");
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,Folder+"/Boundary.mat");}
        material.name="Classroom neutral wall";material.color=new Color(.72f,.72f,.69f);
        void Box(string name,Vector3 position,Vector3 size)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root.collisionRoot,false);go.layer=LayerMask.NameToLayer("WorldSolid");go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        Box("Floor",new Vector3(0,-.08f,0),new Vector3(14.2f,.16f,6.5f));
        Box("Left end",new Vector3(-7.1f,1.5f,0),new Vector3(.2f,3,6.5f));
        Box("Right end",new Vector3(7.1f,1.5f,0),new Vector3(.2f,3,6.5f));
        Box("North wall",new Vector3(0,1.5f,3.25f),new Vector3(14.2f,3,.2f));
        Box("South wall",new Vector3(0,1.5f,-3.25f),new Vector3(14.2f,3,.2f));
        var virus=(GameObject)PrefabUtility.InstantiatePrefab(virusPrefab,host.transform);virus.name="Intro Virus";virus.transform.position=new Vector3(-2.1f,1.35f,1.65f);root.virus=virus.GetComponent<IntroVirusTarget>();
        var light=Child("Classroom light").gameObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-30,0);
        // NewScene unloads unreferenced editor assets; reacquire the persistent profile.
        profile=AssetDatabase.LoadAssetAtPath<RealityIntroProfile>(ProfilePath);
        root.Validate(profile);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        Debug.Log("CLASSROOM_REALITY_PREVIEW_CONFIGURED: FBX reference alignment and reversible rectangle; existing scenes untouched");
    }

    public static void FinishBoundary()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use batch editor.");
        var backup=Path.Combine("Builds/Reproduction/ClassroomGS",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(backup);
        File.Copy(ScenePath,Path.Combine(backup,"ClassroomReality.unity"));File.Copy(ScenePath+".meta",Path.Combine(backup,"ClassroomReality.unity.meta"));
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var root=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<RealitySceneRoot>()).Single();
        var ceiling=root.collisionRoot.Find("Ceiling backing");
        if(!ceiling){ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;ceiling.name="Ceiling backing";ceiling.SetParent(root.collisionRoot,false);}
        ceiling.gameObject.layer=LayerMask.NameToLayer("WorldSolid");ceiling.localPosition=new Vector3(0,3.08f,0);ceiling.localScale=new Vector3(14.2f,.16f,6.5f);
        Material Surface(string name,Color color)
        {
            var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;EditorUtility.SetDirty(m);return m;
        }
        ceiling.GetComponent<MeshRenderer>().sharedMaterial=Surface("Ceiling",new Color(.82f,.81f,.77f));
        root.collisionRoot.Find("Floor").GetComponent<MeshRenderer>().sharedMaterial=Surface("Floor",new Color(.46f,.42f,.35f));
        root.Validate(AssetDatabase.LoadAssetAtPath<RealityIntroProfile>(ProfilePath));EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("CLASSROOM_BOUNDARY_FINISHED: reversible ceiling/floor backing; approximate height 3m");
    }
}
