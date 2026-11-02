using System;
using UnityEditor;
using UnityEngine;
using GaussianSplatting.Runtime;
public static class RoomImportReproCheck
{
    public static void Run()
    {
        const string key="nesnausk.GaussianSplatting.CreatorQuality";
        var existed=EditorPrefs.HasKey(key);var previous=EditorPrefs.GetInt(key);
        try
        {
            EditorPrefs.SetInt(key,0); // VeryHigh: deliberately differs from production Medium.
            RoomRealitySetup.Import();
            var asset=AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(RoomRealitySetup.Folder+"/room.asset");
            if(asset.posFormat!=GaussianSplatAsset.VectorFormat.Norm11 || asset.shFormat!=GaussianSplatAsset.SHFormat.Norm6)
                throw new InvalidOperationException("Room import quality depends on EditorPrefs: "+asset.posFormat+"/"+asset.shFormat);
            Debug.Log("ROOM_IMPORT_REPRO_PASS: hostile VeryHigh preference ignored; production Medium retained");
        }
        finally { if(existed)EditorPrefs.SetInt(key,previous);else EditorPrefs.DeleteKey(key); }
    }
}
