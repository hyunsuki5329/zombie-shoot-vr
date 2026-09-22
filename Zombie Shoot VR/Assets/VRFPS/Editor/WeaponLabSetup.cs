using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRFPS.Editor
{
    public static class WeaponLabSetup
    {
        const string WeaponPath = "Assets/VRFPS/Prefabs/Weapons/K2.prefab";
        const string RigPath = "Assets/VRFPS/Prefabs/PlayerRig.prefab";

        public static void Configure()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batchmode editor.");
            Directory.CreateDirectory("Assets/VRFPS/Prefabs/Weapons");
            Directory.CreateDirectory("Assets/VRFPS/Materials");
            AssetDatabase.Refresh();
            var world = EnsureLayer("WorldSolid");
            var enemy = EnsureLayer("Enemy");
            var weapon = EnsureLayer("Weapon");
            var body = EnsureLayer("PlayerBody");
            var dark = Material("PrototypeGun", new Color(0.12f, 0.15f, 0.14f));
            var yellow = Material("Foregrip", new Color(1, 0.75f, 0.1f));
            var targetMaterial = Material("LabTarget", new Color(0.1f, 0.65f, 0.3f));
            if (!File.Exists(WeaponPath))
            {
                var model = new GameObject("K2");
                try
                {
                    model.layer = weapon;
                    Part(model.transform, "Receiver (placeholder)", new Vector3(0, 0, 0.2f), new Vector3(0.08f, 0.1f, 0.4f), dark, weapon);
                    Part(model.transform, "Barrel", new Vector3(0, 0, 0.55f), new Vector3(0.035f, 0.035f, 0.3f), dark, weapon);
                    Part(model.transform, "Pistol Grip", new Vector3(0, -0.08f, 0.02f), new Vector3(0.055f, 0.15f, 0.06f), dark, weapon);
                    Part(model.transform, "Magazine (placeholder)", new Vector3(0, -0.11f, 0.15f), new Vector3(0.06f, 0.16f, 0.09f), dark, weapon);
                    var grip = Part(model.transform, "Foregrip", new Vector3(0, 0, 0.3f), new Vector3(0.09f, 0.11f, 0.08f), yellow, weapon);
                    var muzzle = new GameObject("Muzzle").transform;
                    muzzle.SetParent(model.transform, false);
                    muzzle.localPosition = Vector3.forward * 0.72f;
                    var controller = model.AddComponent<K2Controller>();
                    controller.Configure(null, null, muzzle, 1 << world, (1 << world) | (1 << enemy));
                    var input = model.AddComponent<K2HandInput>();
                    input.Configure(controller, null, grip);
                    var tracer = model.AddComponent<LineRenderer>();
                    tracer.positionCount = 2; tracer.useWorldSpace = true; tracer.widthMultiplier = 0.008f;
                    tracer.sharedMaterial = yellow; tracer.enabled = false;
                    var settings = new SerializedObject(input);
                    settings.FindProperty("tracer").objectReferenceValue = tracer;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(model, WeaponPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(model); }
            }

            var rig = PrefabUtility.LoadPrefabContents(RigPath);
            try
            {
                rig.layer = body;
                var transforms = rig.GetComponentsInChildren<Transform>(true);
                var right = transforms.Single(t => t.name == "Right Controller");
                var left = transforms.Single(t => t.name == "Left Controller");
                var gun = rig.GetComponentInChildren<K2Controller>(true);
                if (gun == null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPath), right);
                    gun = instance.GetComponent<K2Controller>();
                }
                gun.Configure(right, left, gun.transform.Find("Muzzle"), 1 << world, (1 << world) | (1 << enemy));
                var input = gun.GetComponent<K2HandInput>();
                input.Configure(gun, left, gun.transform.Find("Foregrip"));
                var actions = AssetDatabase.LoadAllAssetsAtPath("Assets/VRFPS/Input/VRFPS.inputactions")
                    .OfType<InputActionReference>().Where(r => (r.hideFlags & HideFlags.HideInHierarchy) == 0).ToArray();
                var settings = new SerializedObject(input);
                settings.FindProperty("gripAction").objectReferenceValue = actions.Single(r => r.action.actionMap.name == "XRI Left Interaction" && r.action.name == "Select");
                settings.FindProperty("triggerAction").objectReferenceValue = actions.Single(r => r.action.actionMap.name == "XRI Right Interaction" && r.action.name == "Activate");
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(gun);
                PrefabUtility.RecordPrefabInstancePropertyModifications(input);
                PrefabUtility.SaveAsPrefabAsset(rig, RigPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(rig); }

            var scene = EditorSceneManager.OpenScene(ProjectSetup.Lab, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
                if (root.name == "Floor" || root.name.EndsWith("Wall") || root.name == "One Metre Cube") root.layer = world;
            if (!roots.Any(r => r.name == "Weapon Lab"))
            {
                var session = new GameObject("Weapon Lab").AddComponent<WeaponLabSession>();
                var gun = roots.SelectMany(r => r.GetComponentsInChildren<K2Controller>(true)).Single();
                var status = Label("Instructions", session.transform, new Vector3(0, 2.2f, 5.7f), "WEAPON LAB");
                var settings = new SerializedObject(session);
                settings.FindProperty("gun").objectReferenceValue = gun;
                settings.FindProperty("status").objectReferenceValue = status;
                settings.ApplyModifiedPropertiesWithoutUndo();
                var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = "Weapon Lab Target"; target.layer = enemy;
                target.transform.position = new Vector3(2, 1.4f, 4);
                target.transform.localScale = new Vector3(0.7f, 1.2f, 0.2f);
                target.GetComponent<Renderer>().sharedMaterial = targetMaterial;
                var label = Label("Hit Counter", null, new Vector3(2, 2.2f, 3.8f), "HITS: 0");
                label.transform.SetParent(session.transform, true);
                var receiver = target.AddComponent<WeaponLabTarget>();
                var receiverSettings = new SerializedObject(receiver);
                receiverSettings.FindProperty("label").objectReferenceValue = label;
                receiverSettings.ApplyModifiedPropertiesWithoutUndo();
            }
            if (!EditorSceneManager.SaveScene(scene, ProjectSetup.Lab)) throw new IOException("Could not save weapon lab.");
            AssetDatabase.SaveAssets();
            Debug.Log("VRFPS_WEAPON_LAB_CONFIGURED");
        }

        static int EnsureLayer(string name)
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = settings.FindProperty("layers");
            for (var i = 0; i < layers.arraySize; i++) if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
            for (var i = 8; i < layers.arraySize; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                { layers.GetArrayElementAtIndex(i).stringValue = name; settings.ApplyModifiedPropertiesWithoutUndo(); return i; }
            throw new InvalidOperationException("No available layer for " + name);
        }

        static Material Material(string name, Color color)
        {
            var path = "Assets/VRFPS/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Transform Part(Transform parent, string name, Vector3 position, Vector3 size, Material material, int layer)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name; part.layer = layer;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }

        static TextMesh Label(string name, Transform parent, Vector3 position, string text)
        {
            var label = new GameObject(name).AddComponent<TextMesh>();
            label.transform.SetParent(parent, false); label.transform.position = position;
            label.text = text; label.characterSize = 0.035f; label.fontSize = 48;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            return label;
        }
    }
}
