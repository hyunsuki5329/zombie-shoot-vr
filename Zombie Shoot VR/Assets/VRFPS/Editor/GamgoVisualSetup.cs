using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRFPS.Editor
{
    // Originals and generated materials stay local. Reapply after restoring their archives.
    public static class GamgoVisualSetup
    {
        const string Root = "Assets/VRFPS/External/GAMGO/";
        const string GunPath = "Assets/VRFPS/Prefabs/Weapons/K2.prefab";
        const string MagazinePath = "Assets/VRFPS/Prefabs/Weapons/Magazine.prefab";
        public static void Configure()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batchmode editor.");
            AssetDatabase.Refresh();
            var gunModel = RequireModel("K2/source/k2_orgin.fbx");
            var magazineModel = RequireModel("Magazine/source/magazine556.fbx");
            Directory.CreateDirectory(Root + "Generated");
            AssetDatabase.Refresh();
            var gunMaterials = new[] { GunMaterial("Body_up"), GunMaterial("Plastic_Parts"), GunMaterial("Body_down") };
            var magazineMaterials = new[] { MagazineMaterial("02"), MagazineMaterial("03") };
            Apply(GunPath, gunModel, true, gunMaterials);
            Apply(MagazinePath, magazineModel, false, magazineMaterials);
            AssetDatabase.SaveAssets();
            Debug.Log("VRFPS_GAMGO_VISUALS_CONFIGURED");
        }

        static GameObject RequireModel(string path)
        {
            var importer = AssetImporter.GetAtPath(Root + path) as ModelImporter;
            if (importer != null)
            {
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.SaveAndReimport();
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + path);
            if (model == null) throw new FileNotFoundException("Run scripts/Restore-GamgoAssets.py first: " + path);
            return model;
        }

        static Texture2D Texture(string path, bool normal = false, bool linear = false, bool readable = false)
        {
            path = Root + path;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal && !linear;
            importer.flipGreenChannel = false;
            importer.isReadable = readable;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            importer.textureCompression = readable ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Material(string name)
        {
            var path = Root + "Generated/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_WorkflowMode", 1);
            material.SetFloat("_Metallic", 1);
            // Keep the authored normal strength; preview only after render resources are initialized.
            material.SetFloat("_BumpScale", 1);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            return material;
        }

        static Material GunMaterial(string name)
        {
            var prefix = "K2/textures/" + name;
            var metallic = Texture(prefix + "_metallic.png", linear: true, readable: true);
            var roughness = Texture(prefix + "_roughness.png", linear: true, readable: true);
            if (metallic.width != roughness.width || metallic.height != roughness.height)
                throw new InvalidOperationException("Metal/roughness dimensions differ");
            var metal = metallic.GetPixels32();
            var rough = roughness.GetPixels32();
            for (var i = 0; i < metal.Length; i++) metal[i] = new Color32(metal[i].r, 0, 0, (byte)(255 - rough[i].r));
            var packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            try
            {
                packed.SetPixels32(metal); packed.Apply();
                File.WriteAllBytes(Root + "Generated/" + name + "_MetalSmooth.png", packed.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(packed); }
            AssetDatabase.ImportAsset(Root + "Generated/" + name + "_MetalSmooth.png", ImportAssetOptions.ForceUpdate);
            // Packing buffers are editor-only; keep source textures non-readable afterwards.
            Texture(prefix + "_metallic.png", linear: true);
            Texture(prefix + "_roughness.png", linear: true);
            var material = Material(name);
            material.SetTexture("_BaseMap", Texture(prefix + "_baseColor.png"));
            material.SetTexture("_BumpMap", Texture(prefix + "_normal.png", normal: true));
            material.SetTexture("_MetallicGlossMap", Texture("Generated/" + name + "_MetalSmooth.png", linear: true));
            material.SetFloat("_Smoothness", 1);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material MagazineMaterial(string slot)
        {
            var prefix = "Magazine/textures/magazine556_" + slot + "_-_Default_";
            var material = Material("Magazine" + slot);
            material.SetTexture("_BaseMap", Texture(prefix + "AlbedoTransparenc.jpg"));
            material.SetTexture("_BumpMap", Texture(prefix + "Normal.jpg", normal: true));
            material.SetTexture("_MetallicGlossMap", Texture(prefix + "MetallicSmoothnes.jpg", linear: true));
            // The source JPEG has no alpha. Use an explicit initial smoothness pending headset review.
            material.SetFloat("_Smoothness", .35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void Apply(string prefabPath, GameObject model, bool gun, Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var old = root.transform.Find("GAMGO Visual");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                visual.name = "GAMGO Visual";
                if (gun)
                {
                    visual.transform.localRotation = Quaternion.Euler(0, -90, 0);
                    visual.transform.localScale = Vector3.one * .14f;
                    visual.transform.localPosition = new Vector3(0, -.17f, .079f);
                    foreach (var name in new[] { "Receiver (placeholder)", "Barrel", "Pistol Grip", "Magazine (placeholder)", "Foregrip" })
                    {
                        var part = root.transform.Find(name);
                        if (part == null) throw new InvalidOperationException("Missing legacy anchor: " + name);
                        var renderer = part.GetComponent<Renderer>();
                        if (renderer != null) renderer.enabled = false;
                    }
                    foreach (var renderer in visual.GetComponentsInChildren<MeshRenderer>())
                    {
                        int index = renderer.name == "상부" ? 0 : renderer.name == "플라스틱" ? 1 : renderer.name == "하부" ? 2 : -1;
                        if (index < 0) throw new InvalidOperationException("Unknown K2 mesh " + renderer.name);
                        renderer.sharedMaterials = new[] { materials[index] };
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    }
                }
                else
                {
                    visual.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    visual.transform.localScale = Vector3.one * .14f;
                    visual.transform.localPosition = Vector3.zero;
                    var renderer = visual.GetComponent<MeshRenderer>();
                    if (renderer == null || renderer.sharedMaterials.Length != 2) throw new InvalidOperationException("Unexpected magazine mesh");
                    renderer.sharedMaterials = materials;
                    // Centre the visual around the existing token/socket pivot without changing physics or insertion rules.
                    var centre = root.transform.InverseTransformPoint(renderer.bounds.center);
                    visual.transform.localPosition -= centre;
                    root.transform.Find("Visual").gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                foreach (var t in visual.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = root.layer;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                }
                if (visual.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("Unexpected imported collider");
                PrefabUtility.RecordPrefabInstancePropertyModifications(visual.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                if (PrefabUtility.SaveAsPrefabAsset(root, prefabPath) == null) throw new IOException("Could not save " + prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
