using System;
using System.IO;
using System.Reflection;
using System.Text;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Deterministic analytical data for GPU lifecycle tests. Never a user's room capture.
public static class GaussianRealityFixtureSetup
{
    public const string Folder = "Assets/VRFPS/External/RealityGS/SyntheticFixture";
    public static void Create()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run in the isolated batch test project.");
        Directory.CreateDirectory(Folder);
        var input = Path.GetFullPath(Folder + "/synthetic.ply");
        var header = new StringBuilder("ply\nformat binary_little_endian 1.0\ncomment Analytical test data, not a scan\nelement vertex 256\n");
        foreach (var name in new[]{"x","y","z","nx","ny","nz","f_dc_0","f_dc_1","f_dc_2"}) header.Append("property float " + name + "\n");
        for (var i=0;i<45;i++) header.Append("property float f_rest_" + i + "\n");
        foreach (var name in new[]{"opacity","scale_0","scale_1","scale_2","rot_0","rot_1","rot_2","rot_3"}) header.Append("property float " + name + "\n");
        header.Append("end_header\n");
        using (var writer = new BinaryWriter(File.Create(input)))
        {
            writer.Write(Encoding.ASCII.GetBytes(header.ToString()));
            for (var i=0;i<256;i++)
            {
                writer.Write((i%16-7.5f)*.08f);writer.Write((i/16-7.5f)*.08f);writer.Write(0f);
                for(var n=0;n<3;n++)writer.Write(0f);
                writer.Write(1.77f);writer.Write(-1.77f);writer.Write(-1.77f);
                for(var n=0;n<45;n++)writer.Write(0f);
                writer.Write(4f);for(var n=0;n<3;n++)writer.Write(-3.3f);
                writer.Write(1f);for(var n=0;n<3;n++)writer.Write(0f);
            }
        }
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var creator=ScriptableObject.CreateInstance<GaussianSplatAssetCreator>();
        try
        {
            typeof(GaussianSplatAssetCreator).GetField("m_InputFile",flags).SetValue(creator,input);
            typeof(GaussianSplatAssetCreator).GetField("m_OutputFolder",flags).SetValue(creator,Folder);
            typeof(GaussianSplatAssetCreator).GetMethod("CreateAsset",flags).Invoke(creator,null);
        }
        finally { UnityEngine.Object.DestroyImmediate(creator); }
        var rd=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Folder+"/Renderer.asset");
        if (!rd)
        {
            rd=ScriptableObject.CreateInstance<UniversalRendererData>();
            var type=typeof(GaussianSplatRenderer).Assembly.GetType("GaussianSplatting.Runtime.GaussianSplatURPFeature",true);
            var feature=(ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
            rd.rendererFeatures.Add(feature);
            AssetDatabase.CreateAsset(rd,Folder+"/Renderer.asset");AssetDatabase.AddObjectToAsset(feature,rd);
            var pipeline=UniversalRenderPipelineAsset.Create(rd);pipeline.msaaSampleCount=1;pipeline.supportsHDR=true;
            AssetDatabase.CreateAsset(pipeline,Folder+"/Pipeline.asset");
        }
        AssetDatabase.SaveAssets();
        Debug.Log("GS_FIXTURE_CREATED: 256 analytical Gaussians; project render settings unchanged");
    }
}
