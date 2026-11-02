using System;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;

namespace VRFPS.Editor
{
    public static class ManualReloadSetup
    {
        const string MagazinePath = "Assets/VRFPS/Prefabs/Weapons/Magazine.prefab";
        public static void Configure()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batchmode editor.");
            var layer = EnsureMagazineLayer();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MagazinePath);
            if (prefab == null)
            {
                var model = new GameObject("Magazine");
                try
                {
                    model.layer = layer;
                    model.AddComponent<MagazineToken>();
                    model.GetComponent<BoxCollider>().size = new Vector3(0.06f, 0.16f, 0.09f);
                    var body = model.GetComponent<Rigidbody>();
                    body.mass = 0.12f; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.name = "Visual"; visual.layer = layer;
                    UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                    visual.transform.SetParent(model.transform, false);
                    visual.transform.localScale = new Vector3(0.06f, 0.16f, 0.09f);
                    visual.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/VRFPS/Materials/PrototypeGun.mat");
                    prefab = PrefabUtility.SaveAsPrefabAsset(model, MagazinePath);
                }
                finally { UnityEngine.Object.DestroyImmediate(model); }
            }
            const string rigPath = "Assets/VRFPS/Prefabs/PlayerRig.prefab";
            var rig = PrefabUtility.LoadPrefabContents(rigPath);
            try
            {
                var gun = rig.GetComponentInChildren<K2Controller>(true);
                var left = rig.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Left Controller");
                var socket = gun.GetComponentInChildren<MagazineSocket>(true);
                if (socket == null)
                {
                    socket = new GameObject("Magazine Socket").AddComponent<MagazineSocket>();
                    socket.transform.SetParent(gun.transform, false);
                    socket.transform.localPosition = new Vector3(0, -0.105f, 0.17f);
                }
                var initial = socket.GetComponentInChildren<MagazineToken>(true);
                if (initial == null)
                    initial = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, socket.transform)).GetComponent<MagazineToken>();
                var oldVisual = gun.transform.Find("Magazine (placeholder)");
                if (oldVisual != null) { oldVisual.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(oldVisual.gameObject); }
                var pouch = rig.GetComponentInChildren<AmmoPouch>(true);
                if (pouch == null)
                {
                    pouch = new GameObject("Left Waist Ammo Pouch").AddComponent<AmmoPouch>();
                    pouch.transform.SetParent(rig.transform, false);
                    pouch.transform.localPosition = new Vector3(-0.25f, 1, -0.05f);
                    var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.name = "Pouch Marker";
                    UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                    visual.transform.SetParent(pouch.transform, false);
                    visual.transform.localScale = new Vector3(0.14f, 0.12f, 0.12f);
                    visual.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/VRFPS/Materials/LabTarget.mat");
                }
                var pouchSettings = new SerializedObject(pouch);
                pouchSettings.FindProperty("prefab").objectReferenceValue = prefab.GetComponent<MagazineToken>();
                pouchSettings.FindProperty("leftHand").objectReferenceValue = left;
                pouchSettings.FindProperty("head").objectReferenceValue = rig.GetComponent<XROrigin>().Camera.transform;
                pouchSettings.ApplyModifiedPropertiesWithoutUndo();
                var owner = rig.GetComponent<LeftHandInteractor>();
                if (owner == null) owner = rig.AddComponent<LeftHandInteractor>();
                var settings = new SerializedObject(owner);
                settings.FindProperty("gun").objectReferenceValue = gun;
                settings.FindProperty("left").objectReferenceValue = left;
                settings.FindProperty("foregrip").objectReferenceValue = gun.transform.Find("Foregrip");
                settings.FindProperty("socket").objectReferenceValue = socket;
                settings.FindProperty("pouch").objectReferenceValue = pouch;
                settings.FindProperty("initial").objectReferenceValue = initial;
                settings.ApplyModifiedPropertiesWithoutUndo();
                var input = gun.GetComponent<K2HandInput>();
                var inputSettings = new SerializedObject(input);
                inputSettings.FindProperty("leftHandOwnership").objectReferenceValue = owner;
                inputSettings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(input);
                if (PrefabUtility.SaveAsPrefabAsset(rig, rigPath) == null) throw new InvalidOperationException("Failed to save reload rig.");
                AssetDatabase.SaveAssets();
                Debug.Log("VRFPS_MANUAL_RELOAD_CONFIGURED");
            }
            finally { PrefabUtility.UnloadPrefabContents(rig); }
        }

        static int EnsureMagazineLayer()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = settings.FindProperty("layers");
            for (var i = 8; i < layers.arraySize; i++) if (layers.GetArrayElementAtIndex(i).stringValue == "Magazine") return i;
            for (var i = 8; i < layers.arraySize; i++) if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            { layers.GetArrayElementAtIndex(i).stringValue = "Magazine"; settings.ApplyModifiedPropertiesWithoutUndo(); return i; }
            throw new InvalidOperationException("No layer available for magazines.");
        }
    }
}
