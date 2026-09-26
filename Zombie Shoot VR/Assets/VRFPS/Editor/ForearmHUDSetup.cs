using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace VRFPS.Editor
{
    public static class ForearmHUDSetup
    {
        const string Folder = "Assets/VRFPS/Prefabs/UI";
        public static void Configure()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batchmode editor.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var root = new GameObject("Forearm HUD");
            try
            {
                var hp = Label(root.transform, "Health", "HP 100 / 100", new Vector3(0, .015f, -.001f), .011f);
                var progress = Label(root.transform, "Progress", "WAVE 999   KILLS 99999\nPREPARE 8s", new Vector3(0, -.012f, -.001f), .007f);
                var warning = Label(root.transform, "Low Health Warning", "!", new Vector3(-.052f, .015f, -.002f), .012f);
                warning.color = Color.red;
                var panel = GameObject.CreatePrimitive(PrimitiveType.Quad); panel.name="Panel";
                panel.transform.SetParent(root.transform, false); panel.transform.localScale=new Vector3(.12f,.07f,1);
                UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
                var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/HUDPanel.mat");
                if(material==null)
                {
                    material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    material.SetColor("_BaseColor",new Color(.015f,.025f,.04f));
                    AssetDatabase.CreateAsset(material,Folder+"/HUDPanel.mat");
                }
                panel.GetComponent<MeshRenderer>().sharedMaterial=material;
                root.AddComponent<ForearmHUD>().Configure(null,hp,progress,warning.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/ForearmHUD.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            var scene=EditorSceneManager.OpenScene(WaveLabSetup.ScenePath,OpenSceneMode.Single);
            var run=UnityEngine.Object.FindFirstObjectByType<RunController>();
            if(run==null) throw new InvalidOperationException("WaveLab run missing.");
            var right=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(x=>x.name=="Right Controller");
            var old=right.Find("ForearmHUD"); if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/ForearmHUD.prefab"),right);
            instance.name="ForearmHUD";
            instance.transform.localPosition=new Vector3(0,.035f,-.125f);
            instance.transform.localRotation=Quaternion.Euler(90,0,0);
            instance.GetComponent<ForearmHUD>().Configure(run,instance.transform.Find("Health").GetComponent<TextMesh>(),instance.transform.Find("Progress").GetComponent<TextMesh>(),instance.transform.Find("Low Health Warning").gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<ForearmHUD>());
            if(!EditorSceneManager.SaveScene(scene)) throw new IOException("HUD scene save failed.");
            AssetDatabase.SaveAssets(); Debug.Log("VRFPS_FOREARM_HUD_CONFIGURED");
        }
        static TextMesh Label(Transform parent,string name,string value,Vector3 position,float height)
        {
            var label=new GameObject(name).AddComponent<TextMesh>(); label.transform.SetParent(parent,false);
            label.transform.localPosition=position; label.text=value; label.fontSize=64;
            label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center;
            var bounds=label.GetComponent<MeshRenderer>().localBounds.size;
            label.characterSize=height/Mathf.Max(bounds.y/value.Split('\n').Length,.001f);
            return label;
        }
    }
}

