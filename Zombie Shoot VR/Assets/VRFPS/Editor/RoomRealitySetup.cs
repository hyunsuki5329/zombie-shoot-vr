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

public static class RoomRealitySetup
{
    public const string Folder = "Assets/VRFPS/External/RealityGS/Room";
    public const string CaptureSha256 = "65f2f8c9705d49a660d8223d9986f3c87cc62085d6bb86daa8a686d09ff75e56";
    public static void Import()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use isolated batch project.");
        var input = Environment.GetEnvironmentVariable("VRFPS_ROOM_PLY");
        if (string.IsNullOrEmpty(input) || !File.Exists(input)) throw new FileNotFoundException("Set VRFPS_ROOM_PLY to the room capture.");
        using (var stream=File.OpenRead(input))
        using (var sha=SHA256.Create())
            if(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant()!=CaptureSha256)
                throw new InvalidOperationException("Room capture identity mismatch.");
        Directory.CreateDirectory(Folder);
        var local=Path.GetFullPath(Folder+"/room.ply");
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
        var asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(Folder+"/room.asset");
        if(!asset || asset.splatCount!=577950) throw new InvalidOperationException("Room import count mismatch.");
        Debug.Log("ROOM_GS_IMPORTED: "+asset.splatCount+" sha256="+CaptureSha256);
    }

    public const string ScenePath = Folder+"/RoomReality.unity";
    public const string ProfilePath = Folder+"/RoomIntro.asset";
    public static void Configure()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch project.");
        var asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(Folder+"/room.asset");
        var virusPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/External/IntroVirus/IntroVirus.prefab");
        if(!asset||!virusPrefab)throw new InvalidOperationException("Import verified room and virus first.");
        var profile=AssetDatabase.LoadAssetAtPath<RealityIntroProfile>(ProfilePath);
        if(!profile){profile=ScriptableObject.CreateInstance<RealityIntroProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);}
        profile.mapId="room";profile.dataId="room-"+CaptureSha256;
        profile.realityScenePath=ScenePath;profile.realityAnchorKey="RoomObservation";profile.themeAnchorKey="playerSpawn";
        profile.metersPerUnit=1;profile.Validate();EditorUtility.SetDirty(profile);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var host=new GameObject("Room Reality");var root=host.AddComponent<RealitySceneRoot>();
        root.mapId=profile.mapId;root.dataId=profile.dataId;
        Transform Child(string name){var go=new GameObject(name);go.transform.SetParent(host.transform,false);return go.transform;}
        root.dataRoot=Child("Room GS");root.dataRoot.SetPositionAndRotation(new Vector3(0,.85f,0),Quaternion.Euler(180,0,0));
        var renderer=root.dataRoot.gameObject.AddComponent<GaussianSplatRenderer>();renderer.enabled=false;renderer.m_Asset=asset;
        const string package="Packages/org.nesnausk.gaussian-splatting/";
        renderer.m_ShaderSplats=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/RenderGaussianSplats.shader");
        renderer.m_ShaderComposite=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/GaussianComposite.shader");
        renderer.m_ShaderDebugPoints=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/GaussianDebugRenderPoints.shader");
        renderer.m_ShaderDebugBoxes=AssetDatabase.LoadAssetAtPath<Shader>(package+"Shaders/GaussianDebugRenderBoxes.shader");
        renderer.m_CSSplatUtilities=AssetDatabase.LoadAssetAtPath<ComputeShader>(package+"Shaders/SplatUtilities.compute");
        var source=root.dataRoot.gameObject.AddComponent<GaussianRealitySource>();source.splats=renderer;source.dataId=profile.dataId;
        root.observationAnchor=Child(profile.realityAnchorKey);root.observationAnchor.position=new Vector3(.55f,0,0);
        root.collisionRoot=Child("WorldSolid proxies");
        void Box(string name,Vector3 position,Vector3 size)
        {
            var go=new GameObject(name);go.transform.SetParent(root.collisionRoot,false);go.layer=LayerMask.NameToLayer("WorldSolid");
            go.transform.localPosition=position;go.AddComponent<BoxCollider>().size=size;
        }
        // Initial fit: right-hand wardrobe wall runs 4.20m, z=-1.8 to +2.4 after X rotation.
        Box("Floor",new Vector3(-.35f,-.06f,.3f),new Vector3(3.7f,.12f,4.2f));
        Box("Wardrobe wall 4.20m",new Vector3(1.55f,1.2f,.3f),new Vector3(.12f,2.4f,4.2f));
        Box("Left wall",new Vector3(-2.25f,1.2f,.3f),new Vector3(.12f,2.4f,4.2f));
        Box("Door wall",new Vector3(-.35f,1.2f,2.45f),new Vector3(3.7f,2.4f,.12f));
        Box("Window wall",new Vector3(-.35f,1.2f,-1.85f),new Vector3(3.7f,2.4f,.12f));
        Box("Bed",new Vector3(-1.1f,.32f,1.7f),new Vector3(2.1f,.64f,1.4f));
        Box("Desk",new Vector3(-1.1f,.38f,-1.35f),new Vector3(2.1f,.76f,.8f));
        var virus=(GameObject)PrefabUtility.InstantiatePrefab(virusPrefab,host.transform);
        virus.name="Intro Virus";virus.transform.position=new Vector3(.55f,1.35f,1.25f);root.virus=virus.GetComponent<IntroVirusTarget>();
        var light=Child("Virus light").gameObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;
        light.transform.rotation=Quaternion.Euler(45,-30,0);
        root.Validate(profile);EditorSceneManager.SaveScene(scene,ScenePath);
        var combat=EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Combat.unity",OpenSceneMode.Single);
        var installer=combat.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ComparisonIntroInstaller>(true)).Single();
        installer.roomProfile=profile;EditorSceneManager.MarkSceneDirty(combat);EditorSceneManager.SaveScene(combat);
        EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(x=>x.path!=ScenePath).Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
        AssetDatabase.SaveAssets();Debug.Log("ROOM_REALITY_CONFIGURED: capture, virus, separate observation anchor; theme maps untouched");
    }
}
