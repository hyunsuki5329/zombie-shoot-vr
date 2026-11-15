using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
namespace VRFPS.Editor
{
    public static class MenuSetup
    {
        const string Path="Assets/VRFPS/Prefabs/UI/Menus.prefab";
        public static void Configure()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use batchmode.");
            var root=new GameObject("Menus",typeof(RectTransform),typeof(Canvas),typeof(TrackedDeviceGraphicRaycaster));
            try
            {
                root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
                root.GetComponent<RectTransform>().sizeDelta=new Vector2(960,640);
                root.transform.localScale=Vector3.one*.001f;
                var panel=Rect("Panel",root.transform,Vector2.zero,new Vector2(960,640));
                panel.AddComponent<Image>().color=new Color(.025f,.04f,.065f,.98f);
                var title=Label("Title",panel.transform,new Vector2(0,245),new Vector2(860,75),48);
                var details=Label("Details",panel.transform,new Vector2(0,140),new Vector2(860,130),28);
                var resume=Button("Resume",panel.transform,new Vector2(-205,20));
                var retry=Button("Retry",panel.transform,new Vector2(205,20));
                var settings=Button("Settings",panel.transform,new Vector2(-205,-75));
                var maps=Button("Maps unavailable",panel.transform,new Vector2(205,-75));maps.interactable=false;
                var back=Button("Back",panel.transform,new Vector2(-205,-170));
                var save=Button("Retry save",panel.transform,new Vector2(205,-170));
                var help=Button("Controls",panel.transform,new Vector2(205,-265));
                root.AddComponent<MenuView>().Configure(root.AddComponent<MenuController>(),null,null,panel,title,details,resume,retry,settings,back,maps,save);
                Directory.CreateDirectory("Assets/VRFPS/UI");AssetDatabase.Refresh();
                var instruction=Label("InstructionPanel",panel.transform,new Vector2(0,40),new Vector2(860,300),24);
                instruction.alignment=TextAnchor.MiddleLeft;
                instruction.text="MOVE: left stick     TURN: right stick\n"+
                    "FIRE: hold the foregrip with LEFT grip, then RIGHT trigger.\n"+
                    "RELOAD (left hand):\n"+
                    "1. Release the foregrip. Grip the magazine and pull it out.\n"+
                    "2. Release grip to discard it. Old rounds are lost.\n"+
                    "3. Grip near your LEFT waist for a fresh 30-round magazine.\n"+
                    "4. Move it into the socket, then grip the foregrip again.\n"+
                    "HEALTH / WAVE / KILLS: look at your right forearm.\n"+
                    "PAUSE: X (left controller). Point at a button and select it.";
                var instructionPrefab=PrefabUtility.SaveAsPrefabAsset(instruction.gameObject,"Assets/VRFPS/UI/InstructionPanel.prefab");
                UnityEngine.Object.DestroyImmediate(instruction.gameObject);
                var instructionInstance=(GameObject)PrefabUtility.InstantiatePrefab(instructionPrefab,panel.transform);
                instructionInstance.name="InstructionPanel";instructionInstance.SetActive(false);
                var controls=Rect("SettingsControls",panel.transform,Vector2.zero,new Vector2(900,220));
                var moveText=Label("Move speed",controls.transform,new Vector2(0,20),new Vector2(300,70),28);
                var turnText=Label("Turn speed",controls.transform,new Vector2(0,-75),new Vector2(300,70),28);
                var moveDown=Button("Move -",controls.transform,new Vector2(-300,20));
                var moveUp=Button("Move +",controls.transform,new Vector2(300,20));
                var turnDown=Button("Turn -",controls.transform,new Vector2(-300,-75));
                var turnUp=Button("Turn +",controls.transform,new Vector2(300,-75));
                foreach(var b in new[]{moveDown,moveUp,turnDown,turnUp})b.GetComponent<RectTransform>().sizeDelta=new Vector2(180,75);
                root.AddComponent<SettingsView>().Configure(null,moveText,turnText,moveDown,moveUp,turnDown,turnUp);
                var viewData=new SerializedObject(root.GetComponent<MenuView>());
                viewData.FindProperty("overlayMaterial").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/VRFPS/UI/MenuOverlay.mat");
                viewData.FindProperty("quitButton").objectReferenceValue=help;
                viewData.FindProperty("instructionPanel").objectReferenceValue=instructionInstance;
                viewData.FindProperty("settingsControls").objectReferenceValue=controls;viewData.ApplyModifiedPropertiesWithoutUndo();
                controls.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root,Path);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            var scene=EditorSceneManager.OpenScene(WaveLabSetup.ScenePath,OpenSceneMode.Single);
            var old=GameObject.Find("Menus");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
            var run=UnityEngine.Object.FindFirstObjectByType<RunController>();
            var camera=run.GetComponentInChildren<Camera>();
            var data=new SerializedObject(instance.GetComponent<MenuView>());
            data.FindProperty("run").objectReferenceValue=run;
            data.FindProperty("viewer").objectReferenceValue=camera.transform;
            data.ApplyModifiedPropertiesWithoutUndo();
            var settingsData=new SerializedObject(instance.GetComponent<SettingsView>());
            settingsData.FindProperty("rig").objectReferenceValue=run.GetComponent<VRFPS.Player.PlayerRigController>();
            settingsData.ApplyModifiedPropertiesWithoutUndo();
            instance.GetComponent<Canvas>().worldCamera=camera;
            instance.transform.position=camera.transform.position+camera.transform.forward*1.2f;
            instance.transform.rotation=camera.transform.rotation;
            if(UnityEngine.Object.FindFirstObjectByType<EventSystem>()==null)
                new GameObject("VRFPS UI Events",typeof(EventSystem),typeof(XRUIInputModule));
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Menu scene save failed.");
            AssetDatabase.SaveAssets();Debug.Log("VRFPS_MENU_CONFIGURED");
        }
        static GameObject Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);
            var r=o.GetComponent<RectTransform>();r.sizeDelta=size;r.anchoredPosition=position;return o;
        }
        static Text Label(string name,Transform parent,Vector2 position,Vector2 size,int fontSize)
        {
            var text=Rect(name,parent,position,size).AddComponent<Text>();
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;
            text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;return text;
        }
        static Button Button(string name,Transform parent,Vector2 position)
        {
            var o=Rect(name,parent,position,new Vector2(360,75));var image=o.AddComponent<Image>();image.color=new Color(.15f,.24f,.34f);
            var button=o.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(.5f,.8f,1);colors.disabledColor=new Color(.35f,.35f,.35f);button.colors=colors;
            Label("Label",o.transform,Vector2.zero,new Vector2(350,70),28).text=name;return button;
        }
    }
}
