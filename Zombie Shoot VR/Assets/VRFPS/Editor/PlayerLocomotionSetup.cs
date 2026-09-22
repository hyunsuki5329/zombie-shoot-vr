using System;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using VRFPS.Player;

namespace VRFPS.Editor
{
    public static class PlayerLocomotionSetup
    {
        public static void Configure()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run in a separate batchmode editor.");
            const string path = "Assets/VRFPS/Prefabs/PlayerRig.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var provider in root.GetComponentsInChildren<LocomotionProvider>(true)) provider.enabled = false;
                foreach (var transformer in root.GetComponentsInChildren<XRBodyTransformer>(true)) transformer.enabled = false;
                foreach (var inputManager in root.GetComponentsInChildren<ControllerInputActionManager>(true))
                {
                    var managerSettings = new SerializedObject(inputManager);
                    var teleport = managerSettings.FindProperty("m_TeleportInteractor").objectReferenceValue as Component;
                    if (teleport != null) teleport.gameObject.SetActive(false);
                    inputManager.enabled = false;
                }
                var controller = root.GetComponent<PlayerRigController>();
                if (controller == null) controller = root.AddComponent<PlayerRigController>();
                var settings = new SerializedObject(controller);
                settings.FindProperty("head").objectReferenceValue = root.GetComponent<XROrigin>().Camera.transform;
                var actions = AssetDatabase.LoadAllAssetsAtPath("Assets/VRFPS/Input/VRFPS.inputactions")
                    .OfType<InputActionReference>().Where(r => (r.hideFlags & HideFlags.HideInHierarchy) == 0).ToArray();
                settings.FindProperty("moveAction").objectReferenceValue = actions.Single(r => r.action.actionMap.name == "XRI Left Locomotion" && r.action.name == "Move");
                settings.FindProperty("turnAction").objectReferenceValue = actions.Single(r => r.action.actionMap.name == "XRI Right Locomotion" && r.action.name == "Turn");
                settings.ApplyModifiedPropertiesWithoutUndo();
                var capsule = root.GetComponent<CharacterController>();
                capsule.radius = 0.2f;
                capsule.skinWidth = 0.02f;
                capsule.stepOffset = 0.2f;
                capsule.minMoveDistance = 0;
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new InvalidOperationException("Failed to save locomotion rig.");
                AssetDatabase.SaveAssets();
                Debug.Log("VRFPS_LOCOMOTION_CONFIGURED");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
