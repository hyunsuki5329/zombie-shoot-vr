using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRFPS.Player;

namespace VRFPS.Editor
{
    public static class WaveLabSetup
    {
        public const string ScenePath = "Assets/VRFPS/Scenes/Tests/WaveLab.unity";
        public static void Configure()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batchmode editor.");
            if (!File.Exists(ScenePath) && !AssetDatabase.CopyAsset(ProjectSetup.Lab, ScenePath))
                throw new IOException("Could not copy the existing interaction lab.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var old in UnityEngine.Object.FindObjectsByType<EnemyLabSession>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(old);
            foreach (var old in UnityEngine.Object.FindObjectsByType<WeaponLabSession>(FindObjectsSortMode.None))
            {
                var label = old.transform.Find("Instructions");
                if (label != null) UnityEngine.Object.DestroyImmediate(label.gameObject);
                UnityEngine.Object.DestroyImmediate(old);
            }
            var rig = UnityEngine.Object.FindFirstObjectByType<PlayerRigController>();
            if (rig == null) throw new InvalidOperationException("Player rig missing.");
            var run = rig.GetComponent<RunController>() ?? rig.gameObject.AddComponent<RunController>();
            var bindings = rig.GetComponent<RunCombatBindings>() ?? rig.gameObject.AddComponent<RunCombatBindings>();
            var session = rig.GetComponent<WaveLabSession>() ?? rig.gameObject.AddComponent<WaveLabSession>();
            var runtime = GameObject.Find("Wave Runtime") ?? new GameObject("Wave Runtime");
            var director = runtime.GetComponent<SpawnDirector>() ?? runtime.AddComponent<SpawnDirector>();
            const string mapPath = "Assets/VRFPS/Maps/WaveLab.asset";
            Directory.CreateDirectory("Assets/VRFPS/Maps"); AssetDatabase.Refresh();
            var map = AssetDatabase.LoadAssetAtPath<MapDefinition>(mapPath);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<MapDefinition>();
                map.mapId = "interaction-lab"; map.sceneName = "WaveLab";
                map.maxAlive = 4; map.maxRangedAlive = 1; map.minSpawnDistance = 2;
                AssetDatabase.CreateAsset(map, mapPath);
            }
            var sign = GameObject.Find("Enemy Lab Instructions") ?? GameObject.Find("Wave Lab Status");
            if (sign == null) throw new InvalidOperationException("Lab status sign missing.");
            sign.name = "Wave Lab Status";
            sign.GetComponent<TextMesh>().text = "WAVE LAB\nPreparing 8 seconds\nX: pause/resume | Y: retry";
            var settings = new SerializedObject(session);
            Set(settings, "run", run); Set(settings, "director", director); Set(settings, "bindings", bindings);
            Set(settings, "map", map); Set(settings, "target", rig.GetComponentInChildren<Camera>().transform);
            Set(settings, "gun", UnityEngine.Object.FindFirstObjectByType<K2Controller>());
            Set(settings, "hand", UnityEngine.Object.FindFirstObjectByType<LeftHandInteractor>());
            const string folder = "Assets/VRFPS/Prefabs/Enemies/";
            Set(settings, "melee", AssetDatabase.LoadAssetAtPath<GameObject>(folder + "MeleeZombie.prefab").GetComponent<EnemyActor>());
            Set(settings, "ranged", AssetDatabase.LoadAssetAtPath<GameObject>(folder + "RangedZombie.prefab").GetComponent<EnemyActor>());
            Set(settings, "projectile", AssetDatabase.LoadAssetAtPath<GameObject>(folder + "AcidProjectile.prefab").GetComponent<AcidProjectile>());
            Set(settings, "status", sign.GetComponent<TextMesh>());
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save WaveLab.");
            AssetDatabase.SaveAssets(); Debug.Log("VRFPS_WAVE_LAB_CONFIGURED");
        }
        static void Set(SerializedObject settings, string field, UnityEngine.Object value)
        {
            if (value == null) throw new InvalidOperationException("Missing scene reference: " + field);
            settings.FindProperty(field).objectReferenceValue = value;
        }
        public static void Build()
        {
            var output = Path.GetFullPath("Builds/Windows/WaveLab/WaveLab.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var previous = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            var previousTiming=PlayerSettings.enableFrameTimingStats;
            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                PlayerSettings.enableFrameTimingStats=true;
                Debug.Log("VRFPS_FRAME_TIMING_STATS_ENABLED "+PlayerSettings.enableFrameTimingStats);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { ScenePath }, locationPathName = output,
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("WaveLab build failed: " + report.summary.result);
                Debug.Log("VRFPS_WAVE_LAB_BUILD_SUCCEEDED " + output);
            }
            finally { PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previous);PlayerSettings.enableFrameTimingStats=previousTiming; }
        }
    }
}
