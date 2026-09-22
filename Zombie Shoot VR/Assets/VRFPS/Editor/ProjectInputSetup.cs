using System;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace VRFPS.Editor
{
    public static class ProjectInputSetup
    {
        const string SampleRoot = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/";
        const string SourceActions = SampleRoot + "XRI Default Input Actions.inputactions";
        const string SourceRig = SampleRoot + "Prefabs/XR Origin (XR Rig).prefab";
        const string TargetActions = "Assets/VRFPS/Input/VRFPS.inputactions";
        const string TargetRig = "Assets/VRFPS/Prefabs/PlayerRig.prefab";

        public static void Configure()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Run input setup in a separate batchmode editor.");
            Directory.CreateDirectory("Assets/VRFPS/Input");
            Directory.CreateDirectory("Assets/VRFPS/Prefabs");
            AssetDatabase.Refresh();
            if (!File.Exists(TargetActions) && !AssetDatabase.CopyAsset(SourceActions, TargetActions))
                throw new IOException("Could not copy the installed XRI action asset.");
            AssetDatabase.ImportAsset(TargetActions, ImportAssetOptions.ForceSynchronousImport);
            if (!File.Exists(TargetRig)) CreateRig();
            ConnectScene("Assets/VRFPS/Scenes/Combat.unity");
            ConnectScene(ProjectSetup.Lab);
            AssetDatabase.SaveAssets();
            Debug.Log("VRFPS_PROJECT_INPUT_CONNECTED");
        }

        static void CreateRig()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(TargetActions);
            var references = AssetDatabase.LoadAllAssetsAtPath(TargetActions)
                .OfType<InputActionReference>()
                // Input System also imports hidden name-based compatibility references.
                // Use the canonical GUID-based reference for each action.
                .Where(reference => (reference.hideFlags & HideFlags.HideInHierarchy) == 0)
                .ToDictionary(reference => reference.action.id);
            var root = PrefabUtility.LoadPrefabContents(SourceRig);
            try
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) throw new InvalidOperationException("Missing script in the source rig.");
                    var serialized = new SerializedObject(component);
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        var value = property.objectReferenceValue;
                        if (value == null || AssetDatabase.GetAssetPath(value) != SourceActions) continue;
                        if (value is InputActionAsset) property.objectReferenceValue = actions;
                        else if (value is InputActionReference sourceReference)
                        {
                            if (sourceReference.action == null || !references.TryGetValue(sourceReference.action.id, out var target))
                                throw new InvalidOperationException("An XRI action reference cannot be remapped.");
                            property.objectReferenceValue = target;
                        }
                        else throw new InvalidOperationException("Unexpected input subasset: " + value.name);
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if (PrefabUtility.SaveAsPrefabAsset(root, TargetRig) == null)
                    throw new IOException("Could not save project PlayerRig.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void ConnectScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var origins = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<XROrigin>(true)).ToArray();
            if (origins.Length != 1) throw new InvalidOperationException(path + " must contain exactly one XR Origin.");
            var oldRig = origins[0].gameObject;
            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(oldRig);
            if (prefabPath == TargetRig) return;
            if (prefabPath != SourceRig || PrefabUtility.HasPrefabInstanceAnyOverrides(oldRig, false))
                throw new InvalidOperationException("The scene rig has authored changes. Migrate it explicitly: " + path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetRig);
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            replacement.transform.SetPositionAndRotation(oldRig.transform.position, oldRig.transform.rotation);
            replacement.transform.localScale = oldRig.transform.localScale;
            UnityEngine.Object.DestroyImmediate(oldRig);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save " + path);
        }
    }
}
