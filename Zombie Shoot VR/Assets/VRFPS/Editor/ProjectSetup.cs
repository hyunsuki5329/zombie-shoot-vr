using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRFPS.Editor
{
    public static class ProjectSetup
    {
        public const string Lab = "Assets/VRFPS/Scenes/Tests/InteractionLab.unity";
        const string Rig = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        public static void CreateFoundationScenes()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Run scene creation in a separate batchmode editor to preserve open scene edits.");
            Directory.CreateDirectory("Assets/VRFPS/Scenes/Tests");
            AssetDatabase.Refresh();
            CreateScene("Assets/VRFPS/Scenes/Bootstrap.unity", false, false);
            CreateScene("Assets/VRFPS/Scenes/Combat.unity", true, false);
            CreateScene(Lab, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log("VRFPS foundation scenes ready. Hardware acceptance remains pending.");
        }

        static void CreateScene(string path, bool includeRig, bool includeLab)
        {
            // Existing scenes may contain authored work; this command only fills missing scenes.
            if (File.Exists(path)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            try
            {
                if (includeRig)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Rig);
                    if (prefab == null) throw new InvalidOperationException("XRI 3.5.1 Starter Assets rig is missing.");
                    PrefabUtility.InstantiatePrefab(prefab, scene);
                    new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
                }
                if (includeLab)
                {
                    Cube("Floor", new Vector3(0, -0.1f, 0), new Vector3(12, 0.2f, 12));
                    Cube("One Metre Cube", new Vector3(0, 0.5f, 3), Vector3.one);
                    Cube("Back Wall", new Vector3(0, 1.5f, 6), new Vector3(12, 3, 0.2f));
                    Cube("Left Wall", new Vector3(-6, 1.5f, 0), new Vector3(0.2f, 3, 12));
                    var light = new GameObject("Directional Light").AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.intensity = 1.2f;
                    light.transform.rotation = Quaternion.Euler(45, -30, 0);
                    RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
                }
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new IOException("Could not save " + path);
            }
            finally { AssetDatabase.SaveAssets(); }
        }

        static void Cube(string name, Vector3 position, Vector3 scale)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
        }

        [MenuItem("VRFPS/Build Windows Interaction Lab")]
        public static void BuildInteractionLab()
        {
            if (!File.Exists(Lab)) throw new InvalidOperationException("Create the foundation scenes first.");
            var output = Path.GetFullPath("Builds/Windows/InteractionLab/InteractionLab.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            try
            {
                // The lab can be built with the installed Windows Mono module.
                // Do not persist a backend change to the eventual game build settings.
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { Lab },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("InteractionLab build failed: " + report.summary.result);
                Debug.Log("VRFPS_BUILD_SUCCEEDED " + output);
            }
            finally { PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previousBackend); }
        }
    }
}
