using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using GaussianSplatting.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRFPS;
using Object = UnityEngine.Object;

// Private capture authoring only. Does not regenerate or modify either theme map.
public static class RoomMirrorWardrobe
{
    const string Source = "Assets/VRFPS/External/RoomScan/";
    const string Output = RoomRealitySetup.Folder + "/WardrobeHybrid";
    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use closed-editor batch authoring.");
        if (Directory.Exists(Output)) throw new InvalidOperationException("Generated wardrobe already exists; restore or archive it explicitly before authoring.");
        using (var file = File.OpenRead(Source + "Room.fbx"))
        using (var sha = SHA256.Create())
            if (BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant() != "245bbdb7284d6b755125b131be681891173ba791be3cf27b429726a048e1b2e8")
                throw new InvalidOperationException("Unexpected reference FBX; preserve it and recalibrate explicitly.");
        var scene = EditorSceneManager.OpenScene(RoomRealitySetup.ScenePath, OpenSceneMode.Single);
        var root = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<RealitySceneRoot>()).Single();
        var gs = root.GetComponentInChildren<GaussianSplatRenderer>();
        if (root.transform.Find("FBX Wardrobe Repair") || root.dataRoot != gs.transform)
            throw new InvalidOperationException("Room already migrated or hierarchy changed. Restore the backup before applying again.");
        if (Vector3.Distance(gs.transform.position, new Vector3(0, .85f, 0)) > .001f ||
            Quaternion.Angle(gs.transform.rotation, Quaternion.Euler(180, 0, 0)) > .01f ||
            Vector3.Distance(gs.transform.lossyScale, Vector3.one) > .001f)
            throw new InvalidOperationException("Room calibration changed; do not overwrite manual edits.");
        var backup = Path.Combine("Builds/Reproduction/RoomMirrorWardrobe", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffff"));
        Directory.CreateDirectory(backup);
        File.Copy(RoomRealitySetup.ScenePath, Path.Combine(backup, "RoomReality.unity"));
        File.Copy(RoomRealitySetup.ScenePath + ".meta", Path.Combine(backup, "RoomReality.unity.meta"));
        var repairs = root.transform.Find("Room Surface Repairs");
        if (!repairs || repairs.Find("Wardrobe side backing") || !repairs.Find("Wardrobe overhead noise"))
            throw new InvalidOperationException("Apply the current three-wall repair / RestoreWardrobeDetail revision first.");
        if (Vector3.Distance(repairs.position, Vector3.zero) > .001f || Quaternion.Angle(repairs.rotation, Quaternion.identity) > .01f || Vector3.Distance(repairs.lossyScale, Vector3.one) > .001f)
            throw new InvalidOperationException("Wall repair alignment changed; preserve manual edits and realign explicitly.");
        // Keep the calibrated data container and player observation transform positive/unit scale.
        var container = new GameObject("Reflected Room Data").transform;
        container.SetParent(root.transform, false);
        gs.transform.SetParent(container, true);
        gs.transform.localScale = new Vector3(-1, 1, 1);
        root.dataRoot = container;
        if (repairs) repairs.localScale = new Vector3(-1, 1, 1);
        foreach (Transform proxy in root.collisionRoot) proxy.localPosition = Mirror(proxy.localPosition);
        root.observationAnchor.position = Mirror(root.observationAnchor.position);
        root.virus.transform.position = Mirror(root.virus.transform.position);

        var fbx = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Room.fbx"));
        Mesh mesh; Texture2D texture;
        try { mesh = ExtractWardrobe(fbx, out texture); }
        finally { Object.DestroyImmediate(fbx); }
        if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder(RoomRealitySetup.Folder, "WardrobeHybrid");
        if (AssetDatabase.LoadMainAssetAtPath(Output + "/Wardrobe.asset")) throw new InvalidOperationException("Generated wardrobe already exists; preserve and inspect before replacing.");
        AssetDatabase.CreateAsset(mesh, Output + "/Wardrobe.asset");
        AssetDatabase.CreateAsset(texture, Output + "/WardrobeTexture.asset");
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetTexture("_BaseMap", texture);
        if (!material.mainTexture) throw new InvalidOperationException("Reference texture missing.");
        material.SetFloat("_Cull", 0);
        AssetDatabase.CreateAsset(material, Output + "/Wardrobe.mat");
        var patch = new GameObject("FBX Wardrobe Repair"); patch.transform.SetParent(root.transform, false);
        patch.AddComponent<MeshFilter>().sharedMesh = mesh;
        patch.AddComponent<MeshRenderer>().sharedMaterial = material;
        var solid = new GameObject("FBX Wardrobe solid"); solid.transform.SetParent(root.collisionRoot, false);
        solid.layer = LayerMask.NameToLayer("WorldSolid"); solid.transform.position = new Vector3(2.15f, 1.18f, -.35f);
        solid.AddComponent<BoxCollider>().size = new Vector3(.42f, 2.28f, 2.6f);
        var cutObject = new GameObject("Wardrobe replacement mask"); cutObject.transform.SetParent(patch.transform, false);
        cutObject.transform.position = new Vector3(2.35f, 1.25f, -.3f);
        cutObject.transform.localScale = new Vector3(.65f, 1.21f, 1.4f);
        var cut = cutObject.AddComponent<GaussianCutout>(); cut.m_Type = GaussianCutout.Type.Box; cut.m_Invert = true;
        gs.m_Cutouts = (gs.m_Cutouts ?? Array.Empty<GaussianCutout>()).Concat(new[] { cut }).ToArray();
        root.Validate(AssetDatabase.LoadAssetAtPath<RealityIntroProfile>(RoomRealitySetup.ProfilePath));
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save mirrored room.");
        Debug.Log("ROOM_MIRROR_WARDROBE_APPLIED triangles=" + mesh.triangles.Length / 3 + " bounds=" + mesh.bounds + " backup=" + Path.GetFullPath(backup));
    }
    static Vector3 Mirror(Vector3 p) => new Vector3(-p.x, p.y, p.z);
    static Mesh ExtractWardrobe(GameObject source, out Texture2D texture)
    {
        // Reproject the actual FBX atlas onto a complete, straight wardrobe facade.
        // The scan ends below the top: extend its intact door texture vertically rather
        // than retaining floating/jagged triangles or introducing a plain brown slab.
        var original = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "Textures/texture.jpg");
        if (!original) throw new InvalidOperationException("Reference texture missing.");
        var rt = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var old = RenderTexture.active;
        var readable = new Texture2D(original.width, original.height, TextureFormat.RGB24, false);
        try { Graphics.Blit(original, rt); RenderTexture.active = rt; readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); readable.Apply(); }
        finally { RenderTexture.active = old; RenderTexture.ReleaseTemporary(rt); }
        var colliders = source.GetComponentsInChildren<MeshFilter>().Select(f => { var c = f.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = f.sharedMesh; return c; }).ToArray();
        const int width = 1024, height = 1024;
        var colors = new Color[width * height]; var valid = new bool[width * height]; var hits = 0;
        try
        {
            Physics.SyncTransforms();
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var sourceX = Mathf.Lerp(-1.8f, .8f, x / (float)(width - 1));
                // The rightmost scanned pair is warped. Reuse an intact matching pair.
                if (sourceX < -.95f) sourceX += 1.7f;
                var targetY = Mathf.Lerp(.04f, 2.32f, y / (float)(height - 1));
                var sourceY = targetY <= 1.3f ? targetY + .06f : Mathf.Lerp(1.36f, 1.40f, (targetY - 1.3f) / 1.02f);
                // Complete the missing top rail from the original lower rail texture.
                if (targetY > 2.24f) sourceY = .10f + (2.32f - targetY);
                var ray = new Ray(new Vector3(sourceX, sourceY, -1.25f), Vector3.back);
                var distance = float.PositiveInfinity; var sample = Vector2.zero;
                foreach (var collider in colliders)
                    if (collider.Raycast(ray, out var hit, 1f) && hit.distance < distance) { distance = hit.distance; sample = hit.textureCoord; }
                if (float.IsPositiveInfinity(distance)) continue;
                colors[y * width + x] = readable.GetPixelBilinear(sample.x, sample.y); valid[y * width + x] = true; hits++;
            }
            if (hits < width * height * .70f) throw new InvalidOperationException("Insufficient wardrobe texture coverage: " + hits);
            // Fill only missing scan pixels with the nearest valid sample on that row.
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++) if (!valid[y * width + x])
                {
                    var found = false;
                    for (var offset = 1; offset < width; offset++)
                    {
                        var neighbor = x - offset;
                        if (neighbor >= 0 && valid[y * width + neighbor]) { colors[y * width + x] = colors[y * width + neighbor]; found = true; break; }
                        neighbor = x + offset;
                        if (neighbor < width && valid[y * width + neighbor]) { colors[y * width + x] = colors[y * width + neighbor]; found = true; break; }
                    }
                    if (!found) throw new InvalidOperationException("Entire wardrobe texture row missing; use a narrower source region.");
                }
        }
        finally { Object.DestroyImmediate(readable); }
        texture = new Texture2D(width, height, TextureFormat.RGB24, true) { name = "FBX wardrobe projected texture", wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels(colors); texture.Apply(true, true);
        var mesh = new Mesh { name = "Room FBX wardrobe facade" };
        mesh.vertices = new[] { new Vector3(1.94f, .04f, -1.65f), new Vector3(1.94f, .04f, .95f), new Vector3(1.94f, 2.32f, .95f), new Vector3(1.94f, 2.32f, -1.65f) };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        Debug.Log("WARDROBE_TEXTURE_COVERAGE " + hits + "/" + width * height);
        return mesh;
    }
}


