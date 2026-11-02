using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

namespace VRFPS.Editor
{
    public static class EnemyLabSetup
    {
        const string Folder = "Assets/VRFPS/Prefabs/Enemies";
        const string AnimationFolder = "Assets/VRFPS/Animators";
        public static void Configure()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run in a separate batchmode editor.");
            Directory.CreateDirectory(Folder); Directory.CreateDirectory(AnimationFolder);
            Directory.CreateDirectory("Assets/VRFPS/Navigation"); AssetDatabase.Refresh();
            var controller = CreateAnimator();
            var melee = CreateEnemy("MeleeZombie", EnemyKind.Melee, controller);
            var ranged = CreateEnemy("RangedZombie", EnemyKind.Ranged, controller);
            var projectile = CreateProjectile();
            var scene = EditorSceneManager.OpenScene(ProjectSetup.Lab, OpenSceneMode.Single);
            // Opening a scene can unload freshly imported prefab objects; resolve persistent references afterwards.
            melee = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/MeleeZombie.prefab").GetComponent<EnemyActor>();
            ranged = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/RangedZombie.prefab").GetComponent<EnemyActor>();
            projectile = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/AcidProjectile.prefab").GetComponent<AcidProjectile>();
            if (melee == null || ranged == null || projectile == null || !EditorUtility.IsPersistent(melee) ||
                !EditorUtility.IsPersistent(ranged) || !EditorUtility.IsPersistent(projectile))
                throw new InvalidOperationException("Enemy prefab references must be persistent assets.");
            var rig = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (rig == null) throw new InvalidOperationException("Player rig missing.");
            var session = rig.GetComponent<EnemyLabSession>();
            if (session == null) session = rig.gameObject.AddComponent<EnemyLabSession>();
            var sign = GameObject.Find("Enemy Lab Instructions");
            if (sign == null)
            {
                sign = new GameObject("Enemy Lab Instructions");
                sign.transform.position = new Vector3(-2.7f, 2.2f, 4.8f);
                var text = sign.AddComponent<TextMesh>(); text.characterSize = 0.035f; text.fontSize = 48;
                text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
                text.text = "ENEMY LAB\nX: start/freeze | Y: reset";
            }
            var settings = new SerializedObject(session);
            settings.FindProperty("meleePrefab").objectReferenceValue = melee;
            settings.FindProperty("rangedPrefab").objectReferenceValue = ranged;
            settings.FindProperty("projectilePrefab").objectReferenceValue = projectile;
            settings.FindProperty("target").objectReferenceValue = rig.GetComponentInChildren<Camera>().transform;
            settings.FindProperty("status").objectReferenceValue = sign.GetComponent<TextMesh>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            var surface = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            if (surface == null) surface = new GameObject("Lab Navigation").AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.layerMask = LayerMask.GetMask("WorldSolid");
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            const string navPath = "Assets/VRFPS/Navigation/InteractionLab.asset";
            var oldData = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if (oldData == null) AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            else
            {
                var generated = surface.navMeshData;
                surface.RemoveData(); EditorUtility.CopySerialized(generated, oldData);
                surface.navMeshData = oldData; UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(oldData); surface.AddData();
            }
            if (!EditorSceneManager.SaveScene(scene, ProjectSetup.Lab)) throw new IOException("Could not save enemy lab.");
            AssetDatabase.SaveAssets(); Debug.Log("VRFPS_ENEMY_LAB_CONFIGURED");
        }

        static EnemyActor CreateEnemy(string name, EnemyKind kind, RuntimeAnimatorController controller)
        {
            var path = Folder + "/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<EnemyActor>();
            var root = new GameObject(name); root.layer = LayerMask.NameToLayer("Enemy");
            try
            {
                var agent = root.AddComponent<NavMeshAgent>(); agent.radius = 0.3f; agent.height = 1.7f;
                var capsule = root.AddComponent<CapsuleCollider>(); capsule.radius = 0.3f; capsule.height = 1.7f; capsule.center = Vector3.up * 0.85f;
                root.AddComponent<ZombieController>(); root.AddComponent<ZombieMotor>();
                var actor = root.AddComponent<EnemyActor>();
                var origin = new GameObject("Attack Origin").transform; origin.SetParent(root.transform, false); origin.localPosition = new Vector3(0, 1.3f, 0.35f);
                var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform, false);
                var animator = visual.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                Part(visual.transform, "Torso", new Vector3(0, 1, 0), new Vector3(0.5f, 0.6f, 0.3f), PrimitiveType.Cube);
                Part(visual.transform, "Head", new Vector3(0, 1.5f, 0), Vector3.one * (kind == EnemyKind.Melee ? 0.34f : 0.44f), PrimitiveType.Sphere);
                Part(visual.transform, "Left Arm", new Vector3(-0.35f, 1, 0.12f), new Vector3(0.16f, 0.65f, 0.16f), PrimitiveType.Cube);
                Part(visual.transform, "Right Arm", new Vector3(0.35f, 1, 0.12f), new Vector3(0.16f, 0.65f, 0.16f), PrimitiveType.Cube);
                Part(visual.transform, "Left Leg", new Vector3(-0.15f, 0.35f, 0), new Vector3(0.2f, 0.7f, 0.22f), PrimitiveType.Cube);
                Part(visual.transform, "Right Leg", new Vector3(0.15f, 0.35f, 0), new Vector3(0.2f, 0.7f, 0.22f), PrimitiveType.Cube);
                var settings = new SerializedObject(actor);
                settings.FindProperty("kind").enumValueIndex = (int)kind; settings.FindProperty("attackOrigin").objectReferenceValue = origin;
                settings.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<EnemyActor>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static void Part(Transform parent, string name, Vector3 position, Vector3 scale, PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.layer = LayerMask.NameToLayer("Enemy");
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/VRFPS/Materials/LabTarget.mat");
        }
        static AcidProjectile CreateProjectile()
        {
            var path = Folder + "/AcidProjectile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<AcidProjectile>();
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                go.name = "Acid Projectile"; go.transform.localScale = Vector3.one * 0.1f;
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.AddComponent<AcidProjectile>();
                const string materialPath = "Assets/VRFPS/Materials/Acid.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.color = new Color(0.4f, 1, 0.05f);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                go.GetComponent<Renderer>().sharedMaterial = material;
                go.SetActive(false);
                return PrefabUtility.SaveAsPrefabAsset(go, path).GetComponent<AcidProjectile>();
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        static RuntimeAnimatorController CreateAnimator()
        {
            var path = AnimationFolder + "/Zombie.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle"); var walk = machine.AddState("Walk");
            var attack = machine.AddState("Attack"); var dead = machine.AddState("Dead"); machine.defaultState = idle;
            idle.motion = Clip("Idle", "Torso", "localEulerAnglesRaw.x", new[] { 0f, 0f, 0f }, true);
            walk.motion = Clip("Walk", "Left Leg", "localEulerAnglesRaw.x", new[] { -22f, 22f, -22f }, true);
            var walkClip = (AnimationClip)walk.motion;
            walkClip.SetCurve("Right Leg", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(new Keyframe(0, 22), new Keyframe(0.4f, -22), new Keyframe(0.8f, 22)));
            attack.motion = Clip("Attack", "Torso", "localEulerAnglesRaw.x", new[] { 0f, -18f, 0f }, false);
            dead.motion = Clip("Dead", "Torso", "localEulerAnglesRaw.z", new[] { 0f, 70f, 90f }, false);
            var death = machine.AddAnyStateTransition(dead); death.hasExitTime = false; death.canTransitionToSelf = false;
            death.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            var windup = machine.AddAnyStateTransition(attack); windup.hasExitTime = false; windup.canTransitionToSelf = false;
            windup.AddCondition(AnimatorConditionMode.If, 0, "Attack"); windup.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            var finish = attack.AddTransition(idle); finish.hasExitTime = false; finish.AddCondition(AnimatorConditionMode.IfNot, 0, "Attack"); finish.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            var move = idle.AddTransition(walk); move.hasExitTime = false; move.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
            var stop = walk.AddTransition(idle); stop.hasExitTime = false; stop.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");
            EditorUtility.SetDirty(walkClip); return controller;
        }
        static AnimationClip Clip(string name, string target, string property, float[] values, bool loop)
        {
            var clip = new AnimationClip { name = name };
            clip.SetCurve(target, typeof(Transform), property, new AnimationCurve(new Keyframe(0, values[0]), new Keyframe(0.4f, values[1]), new Keyframe(0.8f, values[2])));
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop; AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, AnimationFolder + "/Zombie" + name + ".anim"); return clip;
        }
    }
}
