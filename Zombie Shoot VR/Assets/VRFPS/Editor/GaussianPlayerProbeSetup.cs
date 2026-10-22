using System;
using System.IO;
using GaussianSplatting.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace VRFPS.Editor
{
    public static class GaussianPlayerProbeSetup
    {
        const string Folder="Assets/VRFPS/External/RealityGS/PlayerProbe";
        const string Package="Packages/org.nesnausk.gaussian-splatting/";
        public static void Build()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch project");
            const string attribution="Assets/VRFPS/Documentation/GaussianSplatting-Attribution.txt";
            if(!File.Exists(attribution))throw new InvalidOperationException("GS attribution is missing");
            var asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>("Assets/VRFPS/External/RealityGS/SyntheticFixture/synthetic.asset");
            if(!asset)throw new InvalidOperationException("Generate analytical GS fixture first");
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/VRFPS/Configuration/Intro/GsPipeline.asset");
            if(!pipeline)throw new InvalidOperationException("Production GS pipeline missing");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var host=new GameObject("Analytical GS Player probe - not a capture");host.SetActive(false);
            var renderer=host.AddComponent<GaussianSplatRenderer>();renderer.enabled=false;renderer.m_Asset=asset;
            renderer.m_ShaderSplats=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/RenderGaussianSplats.shader");
            renderer.m_ShaderComposite=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianComposite.shader");
            renderer.m_ShaderDebugPoints=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianDebugRenderPoints.shader");
            renderer.m_ShaderDebugBoxes=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Shaders/GaussianDebugRenderBoxes.shader");
            renderer.m_CSSplatUtilities=AssetDatabase.LoadAssetAtPath<ComputeShader>(Package+"Shaders/SplatUtilities.compute");
            var source=host.AddComponent<GaussianRealitySource>();source.splats=renderer;source.dataId="synthetic-256";
            var camera=new GameObject("Mono probe camera").AddComponent<Camera>();camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,0,-2);camera.nearClipPlane=.05f;camera.farClipPlane=10;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.stereoTargetEye=StereoTargetEyeMask.None;
            camera.GetUniversalAdditionalCameraData();
            var probe=host.AddComponent<GaussianPlayerProbe>();probe.source=source;probe.viewCamera=camera;
            probe.pipeline=pipeline;probe.fadeShader=Shader.Find("VRFPS/IntroScreenFade");host.SetActive(true);
            var scenePath=Folder+"/Probe.unity";EditorSceneManager.SaveScene(scene,scenePath);AssetDatabase.SaveAssets();
            var target=BuildTarget.StandaloneWindows64;var named=NamedBuildTarget.Standalone;
            var automatic=PlayerSettings.GetUseDefaultGraphicsAPIs(target);var apis=PlayerSettings.GetGraphicsAPIs(target);
            var backend=PlayerSettings.GetScriptingBackend(named);var output=Path.GetFullPath("Builds/Windows/GsCompatibilityProbe/GsCompatibilityProbe.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            try
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(target,false);PlayerSettings.SetGraphicsAPIs(target,new[]{GraphicsDeviceType.Direct3D12});
                PlayerSettings.SetScriptingBackend(named,ScriptingImplementation.Mono2x);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName=output,target=target,options=BuildOptions.Development});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("GS probe build failed: "+report.summary.result);
                File.Copy(attribution,Path.Combine(Path.GetDirectoryName(output),"GaussianSplatting-Attribution.txt"),true);
                Debug.Log("GS_PLAYER_PROBE_BUILD_SUCCEEDED "+report.summary.guid+" "+output+" stripping="+PlayerSettings.GetManagedStrippingLevel(named));
            }
            finally
            {
                PlayerSettings.SetGraphicsAPIs(target,apis);PlayerSettings.SetUseDefaultGraphicsAPIs(target,automatic);
                PlayerSettings.SetScriptingBackend(named,backend);
            }
        }
    }
}
