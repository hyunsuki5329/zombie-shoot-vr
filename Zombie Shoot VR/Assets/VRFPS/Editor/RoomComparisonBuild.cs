using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class RoomComparisonBuild
{
    public static void Build() => BuildTo("GSComparison");
    public static void BuildMovementRevision() => BuildTo("GSComparison-Walk-20261101");
    static void BuildTo(string directory)
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch project.");
        var scenes=new[]{"Assets/VRFPS/Scenes/Bootstrap.unity","Assets/VRFPS/Scenes/Combat.unity","Assets/VRFPS/Scenes/Maps/Room.unity","Assets/VRFPS/Scenes/Maps/Classroom.unity",RoomRealitySetup.ScenePath};
        foreach(var scene in scenes)if(!File.Exists(scene))throw new FileNotFoundException("Required build scene missing",scene);
        var profile=AssetDatabase.LoadAssetAtPath<VRFPS.RealityIntroProfile>(RoomRealitySetup.ProfilePath);
        if(!profile||profile.dataId!="room-"+RoomRealitySetup.CaptureSha256)throw new InvalidOperationException("Verified Room profile required.");
        var target=BuildTarget.StandaloneWindows64;var named=NamedBuildTarget.Standalone;
        var automatic=PlayerSettings.GetUseDefaultGraphicsAPIs(target);var apis=PlayerSettings.GetGraphicsAPIs(target);
        var backend=PlayerSettings.GetScriptingBackend(named);
        var output=Path.GetFullPath(Path.Combine("Builds/Windows",directory,"AbandonedSchool.exe"));Directory.CreateDirectory(Path.GetDirectoryName(output));
        try
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(target,false);PlayerSettings.SetGraphicsAPIs(target,new[]{GraphicsDeviceType.Direct3D12});
            PlayerSettings.SetScriptingBackend(named,ScriptingImplementation.Mono2x);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=output,target=target,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("GSComparison build failed: "+report.summary.result);
            var packed=report.packedAssets.SelectMany(p=>p.contents).Select(c=>c.sourceAssetPath).Distinct().OrderBy(p=>p).ToArray();
            File.WriteAllLines(Path.Combine(Path.GetDirectoryName(output),"packed-assets.txt"),packed);
            if(packed.Any(p=>p.EndsWith(".ply",StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Raw PLY unexpectedly included in player.");
            File.Copy("Assets/VRFPS/Documentation/GaussianSplatting-Attribution.txt",Path.Combine(Path.GetDirectoryName(output),"GaussianSplatting-Attribution.txt"),true);
            Debug.Log("ROOM_COMPARISON_BUILD_SUCCEEDED guid="+report.summary.guid+" path="+output+" capture="+RoomRealitySetup.CaptureSha256);
        }
        finally
        {
            PlayerSettings.SetGraphicsAPIs(target,apis);PlayerSettings.SetUseDefaultGraphicsAPIs(target,automatic);
            PlayerSettings.SetScriptingBackend(named,backend);
        }
    }
}
