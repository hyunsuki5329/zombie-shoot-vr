using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRFPS.Player;
namespace VRFPS.Editor
{
    public static class BootstrapSetup
    {
        public static void Configure()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batchmode project.");
            Directory.CreateDirectory("Assets/VRFPS/Configuration/Maps");AssetDatabase.Refresh();
            var room=Map("Room","room",4,1,2);var classroom=Map("Classroom","classroom",6,2,3);
            var scene=EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Combat.unity",OpenSceneMode.Single);
            var rig=UnityEngine.Object.FindFirstObjectByType<PlayerRigController>();
            var run=rig.GetComponent<RunController>() ?? rig.gameObject.AddComponent<RunController>();
            var entry=rig.GetComponent<CombatSceneEntry>() ?? rig.gameObject.AddComponent<CombatSceneEntry>();
            entry.run=run;entry.rig=rig;entry.gun=rig.GetComponentInChildren<K2Controller>(true);entry.hand=rig.GetComponentInChildren<LeftHandInteractor>(true);
            const string enemies="Assets/VRFPS/Prefabs/Enemies/";
            entry.melee=AssetDatabase.LoadAssetAtPath<GameObject>(enemies+"MeleeZombie.prefab").GetComponent<EnemyActor>();
            entry.ranged=AssetDatabase.LoadAssetAtPath<GameObject>(enemies+"RangedZombie.prefab").GetComponent<EnemyActor>();
            entry.projectile=AssetDatabase.LoadAssetAtPath<GameObject>(enemies+"AcidProjectile.prefab").GetComponent<AcidProjectile>();
            entry.room=room;entry.classroom=classroom;
            var old=GameObject.Find("Menus");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            var menus=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/UI/Menus.prefab"));
            menus.name="Menus";entry.view=menus.GetComponent<MenuView>();entry.menu=menus.GetComponent<MenuController>();
            var camera=rig.GetComponentInChildren<Camera>();menus.GetComponent<Canvas>().worldCamera=camera;
            Set(entry.view,"run",run);Set(entry.view,"viewer",camera.transform);
            Set(menus.GetComponent<SettingsView>(),"rig",rig);
            menus.transform.position=camera.transform.position+Vector3.forward*1.2f;
            var panel=menus.transform.Find("Panel");entry.status=panel.Find("Details").GetComponent<Text>();
            var mapButton=panel.Find("Maps unavailable").GetComponent<Button>();mapButton.interactable=true;mapButton.GetComponentInChildren<Text>().text="Maps";
            entry.mapControls=new GameObject("MapChoices",typeof(RectTransform));entry.mapControls.transform.SetParent(panel,false);
            entry.roomButton=Choice(entry.mapControls.transform,"Room",-205,out entry.roomLabel);
            entry.classroomButton=Choice(entry.mapControls.transform,"Classroom",205,out entry.classroomLabel);
            entry.mapControls.SetActive(false);
            var right=rig.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Right Controller");
            var oldHud=right.Find("ForearmHUD");if(oldHud!=null)UnityEngine.Object.DestroyImmediate(oldHud.gameObject);
            var hud=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/UI/ForearmHUD.prefab"),right);
            hud.name="ForearmHUD";hud.transform.localPosition=new Vector3(0,.035f,-.125f);hud.transform.localRotation=Quaternion.Euler(90,0,0);Set(hud.GetComponent<ForearmHUD>(),"run",run);
            if(UnityEngine.Object.FindFirstObjectByType<EventSystem>()==null)new GameObject("VRFPS UI Events",typeof(EventSystem),typeof(XRUIInputModule));
            if(UnityEngine.Object.FindFirstObjectByType<Light>()==null)
            {
                var light=new GameObject("Combat Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(45,-30,0);
            }
            EditorSceneManager.SaveScene(scene);
            scene=EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Bootstrap.unity",OpenSceneMode.Single);
            if(UnityEngine.Object.FindFirstObjectByType<GameBootstrap>()==null)new GameObject("Game Bootstrap").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();MapLoaderFixtureSetup.EnableForTests();Debug.Log("VRFPS_BOOTSTRAP_CONFIGURED");
        }
        public static void Build()
        {
            var output=System.IO.Path.GetFullPath("Builds/Windows/BootstrapPreview/BootstrapPreview.exe");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
            var scenes=new[]{"Assets/VRFPS/Scenes/Bootstrap.unity","Assets/VRFPS/Scenes/Combat.unity","Assets/VRFPS/Scenes/Maps/Room.unity","Assets/VRFPS/Scenes/Maps/Classroom.unity"}.Where(File.Exists).ToArray();
            var previous=PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Bootstrap build failed: "+report.summary.result);
                Debug.Log("VRFPS_BOOTSTRAP_BUILD_SUCCEEDED "+output);
            }
            finally{PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,previous);}
        }        static MapDefinition Map(string name,string id,int alive,int ranged,float distance)
        {
            var path="Assets/VRFPS/Configuration/Maps/"+name+".asset";var map=AssetDatabase.LoadAssetAtPath<MapDefinition>(path);
            if(map!=null)return map;
            map=ScriptableObject.CreateInstance<MapDefinition>();map.mapId=id;map.sceneName="Assets/VRFPS/Scenes/Maps/"+name+".unity";
            map.maxAlive=alive;map.maxRangedAlive=ranged;map.minSpawnDistance=distance;AssetDatabase.CreateAsset(map,path);return map;
        }
        static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
        {
            var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();
        }
        static Button Choice(Transform parent,string name,float x,out Text label)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));root.transform.SetParent(parent,false);
            var rect=root.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(360,110);rect.anchoredPosition=new Vector2(x,10);
            var image=root.GetComponent<Image>();image.color=new Color(.15f,.24f,.34f);var button=root.GetComponent<Button>();button.targetGraphic=image;
            var text=new GameObject("Label",typeof(RectTransform),typeof(Text));text.transform.SetParent(root.transform,false);
            text.GetComponent<RectTransform>().sizeDelta=new Vector2(350,105);label=text.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize=24;label.alignment=TextAnchor.MiddleCenter;label.color=Color.white;label.raycastTarget=false;return button;
        }
    }
}
