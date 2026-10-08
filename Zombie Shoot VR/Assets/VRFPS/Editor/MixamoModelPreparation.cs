using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VRFPS.Editor
{
    // Preparation only: combat prefabs remain unchanged until motion clips are available.
    public static class MixamoModelPreparation
    {
        const string Root = "Assets/VRFPS/External/Mixamo/";
        public static void Prepare()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batchmode editor.");
            var names = new[] { "Yaku J Ignite", "Parasite L Starkie" };
            foreach (var name in names)
                if (!File.Exists(Root + name + ".fbx")) throw new FileNotFoundException("Restore both Mixamo originals first.", name);
            AssetDatabase.Refresh();
            foreach (var name in names)
            {
                var path = Root + name + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.SaveAndReimport();
                var folder = Root + name + "-Textures";
                Directory.CreateDirectory(folder);
                importer.ExtractTextures(folder);
                AssetDatabase.Refresh();
                importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var animator = model.GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.isHuman)
                    throw new InvalidOperationException("Invalid humanoid: " + name);
                Debug.Log("VRFPS_MIXAMO_PREPARED " + name);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
