using System;
using System.IO;
using System.Text;
using GaussianSplatting.Editor.Utils;
using Unity.Collections;
using UnityEngine;

// Batch-only import regression. Run with -quit and inspect the exit leak report too.
public static class GaussianImportLifetimeCheck
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode required.");
        NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
        var folder = Path.Combine(Application.temporaryCachePath, "VRFPS-GaussianImportLifetime");
        Directory.CreateDirectory(folder);
        var valid = Path.Combine(folder, "valid.ply");
        var missing = Path.Combine(folder, "missing.ply");
        var truncated = Path.Combine(folder, "truncated.ply");
        var properties = new[]{"x","y","z","f_dc_0","f_dc_1","f_dc_2","opacity","scale_0","scale_1","scale_2","rot_0","rot_1","rot_2","rot_3"};
        Write(valid, properties, properties.Length);
        Write(missing, new[]{"x","y","z"}, 3);
        Write(truncated, properties, 1);
        try
        {
            for (var i=0; i<10; i++)
            {
                GaussianFileReader.ReadFile(valid, out var data);
                try { if (data.Length != 1) throw new InvalidOperationException("Wrong splat count."); }
                finally { data.Dispose(); }
                ExpectFailure(missing);
                ExpectFailure(truncated);
            }
            Debug.Log("GS_IMPORT_LIFETIME_CHECK: valid=10 missing-properties=10 truncated=10; inspect shutdown leak report");
        }
        finally { File.Delete(valid); File.Delete(missing); File.Delete(truncated); }
    }

    static void ExpectFailure(string path)
    {
        try
        {
            GaussianFileReader.ReadFile(path, out var data);
            if (data.IsCreated) data.Dispose();
        }
        catch (IOException) { return; }
        throw new InvalidOperationException("Malformed PLY unexpectedly imported: " + path);
    }

    static void Write(string path, string[] properties, int valueCount)
    {
        var header = new StringBuilder("ply\nformat binary_little_endian 1.0\nelement vertex 1\n");
        foreach (var property in properties) header.Append("property float " + property + "\n");
        header.Append("end_header\n");
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(Encoding.ASCII.GetBytes(header.ToString()));
        for (var i=0;i<valueCount;i++) writer.Write(i==10 ? 1f : 0f);
    }
}
