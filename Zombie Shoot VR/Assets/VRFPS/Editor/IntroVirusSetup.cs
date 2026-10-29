using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace VRFPS.Editor
{
    // Derived external assets stay local. Rerun after restoring the verified original archive.
    public static class IntroVirusSetup
    {
        const string Folder="Assets/VRFPS/External/IntroVirus";
        const string ArchiveHash="d3e8d8817c2e7051ed12e2fb0ca9601a1fada27b9903190dac43ab553b32b8c3";
        public static void Import()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batchmode project.");
            var targetLayer=LayerMask.NameToLayer("IntroTarget");
            if(targetLayer<0)throw new InvalidOperationException("IntroTarget layer is required.");
            var archive=Environment.GetEnvironmentVariable("VRFPS_VIRUS_ARCHIVE")??"C:/Users/User/Downloads/coronavirus-virus-model.zip";
            using(var sha=SHA256.Create())using(var stream=File.OpenRead(archive))
                if(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=ArchiveHash)
                    throw new InvalidDataException("Virus source archive hash does not match the recorded original.");
            Directory.CreateDirectory(Folder);
            using(var outer=ZipFile.OpenRead(archive))
            using(var memory=new MemoryStream())
            {
                using(var source=outer.GetEntry("source/model.zip").Open())source.CopyTo(memory);
                memory.Position=0;
                using(var inner=new ZipArchive(memory,ZipArchiveMode.Read))
                    foreach(var name in new[]{"model.obj","model.mtl","color.png","color11.png","coron_initialShadingGroup_rough.png"})
                        using(var input=inner.GetEntry(name).Open())using(var output=File.Create(Folder+"/"+name))input.CopyTo(output);
            }
            AssetDatabase.Refresh();
            var modelImporter=(ModelImporter)AssetImporter.GetAtPath(Folder+"/model.obj");
            modelImporter.materialImportMode=ModelImporterMaterialImportMode.None;
            modelImporter.addCollider=false;modelImporter.isReadable=false;modelImporter.SaveAndReimport();
            var color=Texture("color.png",false,false);var normal=Texture("color11.png",true,false);
            var rough=Texture("coron_initialShadingGroup_rough.png",false,true);
            var pixels=rough.GetPixels32();
            for(var i=0;i<pixels.Length;i++)pixels[i]=new Color32(0,0,0,(byte)(255-pixels[i].r));
            var packed=new Texture2D(rough.width,rough.height,TextureFormat.RGBA32,false,true);
            packed.SetPixels32(pixels);packed.Apply();File.WriteAllBytes(Folder+"/MetalSmooth.png",packed.EncodeToPNG());UnityEngine.Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(Folder+"/MetalSmooth.png");
            var smooth=Texture("MetalSmooth.png",false,true);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Virus.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,Folder+"/Virus.mat");}
            material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",color);
            material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",1);material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",smooth);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",1);material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            var root=new GameObject("IntroVirus"){layer=targetLayer};
            try
            {
                var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/model.obj"),root.transform);
                model.name="Visual";
                var renderers=model.GetComponentsInChildren<MeshRenderer>();
                if(renderers.Length==0)throw new InvalidDataException("Virus source has no mesh renderers.");
                var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                var diameter=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
                if(diameter<=0||float.IsInfinity(diameter)||float.IsNaN(diameter))throw new InvalidDataException("Invalid virus bounds.");
                model.transform.localScale*=.8f/diameter;model.transform.localPosition=-bounds.center*(.8f/diameter);
                foreach(var r in renderers){r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;}
                root.AddComponent<IntroVirusTarget>();root.AddComponent<SphereCollider>().radius=.32f;
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/IntroVirus.prefab");
                Debug.Log("VRFPS_VIRUS_IMPORTED: diameter=0.8m, collider=0.32m; IntroTarget configured; observation placement remains integration work.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            AssetDatabase.SaveAssets();
        }
        static Texture2D Texture(string name,bool normal,bool linear)
        {
            var path=Folder+"/"+name;var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!normal&&!linear;importer.isReadable=name=="coron_initialShadingGroup_rough.png";
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        public static void Preview()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batchmode project.");
            var scene=EditorSceneManager.NewPreviewScene();
            var target=new RenderTexture(768,768,24);target.Create();var image=new Texture2D(768,768,TextureFormat.RGB24,false);
            try
            {
                var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/IntroVirus.prefab"));
                SceneManager.MoveGameObjectToScene(model,scene);model.transform.rotation=Quaternion.Euler(15,30,0);
                var light=new GameObject("Preview key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=3;light.transform.rotation=Quaternion.Euler(35,-35,0);
                var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-1.4f);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.09f,.12f);camera.nearClipPlane=.01f;
                SceneManager.MoveGameObjectToScene(light.gameObject,scene);SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                var old=RenderTexture.active;RenderTexture.active=target;
                try{image.ReadPixels(new Rect(0,0,768,768),0,0);image.Apply();}finally{RenderTexture.active=old;}
                var output=Path.GetFullPath("../tmp/p13/virus-preview.png");Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllBytes(output,image.EncodeToPNG());
                Debug.Log("VRFPS_VIRUS_PREVIEW: "+output);
            }
            finally{target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
