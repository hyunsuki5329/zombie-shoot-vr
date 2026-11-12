using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using VRFPS;

public static class ClassroomHybridRepair
{
    const string Folder = ClassroomRealitySetup.Folder + "/Hybrid";

    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use batch editor.");
        var backup = Path.Combine("Builds/Reproduction/ClassroomHybrid", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(backup);
        File.Copy(ClassroomRealitySetup.ScenePath, Path.Combine(backup, "ClassroomReality.unity"));
        File.Copy(ClassroomRealitySetup.ScenePath + ".meta", Path.Combine(backup, "ClassroomReality.unity.meta"));
        // The scene references these mutable assets, so rollback must include them and their GUIDs.
        var assetBackup = Path.Combine(backup, "Hybrid");
        Directory.CreateDirectory(assetBackup);
        foreach (var path in Directory.GetFiles(Folder))
            File.Copy(path, Path.Combine(assetBackup, Path.GetFileName(path)));
        var scene = EditorSceneManager.OpenScene(ClassroomRealitySetup.ScenePath);
        var root = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<RealitySceneRoot>()).Single();

        var texturePath = Folder + "/ReferenceTexture.jpg";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.maxTextureSize = 8192;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (!texture) throw new InvalidOperationException("Reference texture unavailable.");
        var mesh = new Mesh { name = "FBX missing classroom surfaces", indexFormat = IndexFormat.UInt32 };
        using (var input = new BinaryReader(File.OpenRead(Folder + "/gap-mesh.bytes")))
        {
            int vertexCount = input.ReadInt32();
            int indexCount = input.ReadInt32();
            if (vertexCount <= 0 || indexCount <= 0 || indexCount % 3 != 0) throw new InvalidDataException("Invalid mesh header.");
            var vertices = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];
            var indices = new int[indexCount];
            for (int i = 0; i < vertexCount; i++) vertices[i] = new Vector3(input.ReadSingle(), input.ReadSingle(), input.ReadSingle());
            for (int i = 0; i < vertexCount; i++) uv[i] = new Vector2(input.ReadSingle(), input.ReadSingle());
            for (int i = 0; i < indexCount; i++)
            {
                indices[i] = input.ReadInt32();
                if (indices[i] < 0 || indices[i] >= vertexCount) throw new InvalidDataException("Mesh index out of range.");
            }
            if (input.BaseStream.Position != input.BaseStream.Length) throw new InvalidDataException("Unexpected mesh data.");
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = indices;
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var storedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/GapMesh.asset");
        if (storedMesh) { EditorUtility.CopySerialized(mesh, storedMesh); UnityEngine.Object.DestroyImmediate(mesh); mesh = storedMesh; }
        else AssetDatabase.CreateAsset(mesh, Folder + "/GapMesh.asset");
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.name = "FBX captured classroom texture";
        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Cull", 0);
        var storedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/GapMaterial.mat");
        if (storedMaterial) { EditorUtility.CopySerialized(material, storedMaterial); UnityEngine.Object.DestroyImmediate(material); material = storedMaterial; }
        else AssetDatabase.CreateAsset(material, Folder + "/GapMaterial.mat");
        var existing = root.transform.Find("FBX gap repairs");
        var group = existing ? existing.gameObject : new GameObject("FBX gap repairs");
        group.transform.SetParent(root.transform, false);
        (group.GetComponent<MeshFilter>() ?? group.AddComponent<MeshFilter>()).sharedMesh = mesh;
        (group.GetComponent<MeshRenderer>() ?? group.AddComponent<MeshRenderer>()).sharedMaterial = material;
        root.Validate(AssetDatabase.LoadAssetAtPath<RealityIntroProfile>(ClassroomRealitySetup.ProfilePath));
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("CLASSROOM_HYBRID_APPLIED triangles=" + mesh.triangles.Length / 3);
    }
}
