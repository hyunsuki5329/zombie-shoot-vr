using System;
using System.IO;
using GaussianSplatting.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
namespace VRFPS.Editor
{
    public static class RealityIntroSetup
    {
        const string Folder="Assets/VRFPS/Configuration/Intro";
        public static void Configure()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use the isolated batch project.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder+"/GsPipeline.asset");
            if(!pipeline)
            {
                var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();
                var type=typeof(GaussianSplatRenderer).Assembly.GetType("GaussianSplatting.Runtime.GaussianSplatURPFeature",true);
                var feature=(ScriptableRendererFeature)ScriptableObject.CreateInstance(type);renderer.rendererFeatures.Add(feature);
                AssetDatabase.CreateAsset(renderer,Folder+"/GsRenderer.asset");AssetDatabase.AddObjectToAsset(feature,renderer);
                pipeline=UniversalRenderPipelineAsset.Create(renderer);pipeline.msaaSampleCount=1;pipeline.supportsHDR=true;
                AssetDatabase.CreateAsset(pipeline,Folder+"/GsPipeline.asset");
            }
            var scene=EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Combat.unity");
            var entry=UnityEngine.Object.FindFirstObjectByType<CombatSceneEntry>();
            if(!entry||!entry.mapControls)throw new InvalidOperationException("Existing combat entry is missing; do not regenerate maps.");
            var installer=entry.GetComponent<ComparisonIntroInstaller>()??entry.gameObject.AddComponent<ComparisonIntroInstaller>();
            installer.pipeline=pipeline;installer.fadeShader=Shader.Find("VRFPS/IntroScreenFade");
            // Preserve any later user-supplied capture profile references.
            entry.roomRealityButton=RealityButton(entry,"Room Reality",entry.roomButton,15,out entry.roomRealityLabel);
            entry.classroomRealityButton=RealityButton(entry,"Classroom Reality",entry.classroomButton,15,out entry.classroomRealityLabel);
            foreach(var button in new[]{entry.roomButton,entry.classroomButton})
            {
                var rect=(RectTransform)button.transform;rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-105);rect.sizeDelta=new Vector2(360,100);
                button.GetComponentInChildren<Text>().fontSize=20;
            }
            var settings=entry.view.transform.Find("Panel/Settings").GetComponent<RectTransform>();
            settings.anchoredPosition=new Vector2(-205,-265);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("VRFPS_REALITY_ENTRY_CONFIGURED: direct combat retained; capture profiles not fabricated");
        }
        static Button RealityButton(CombatSceneEntry entry,string name,Button template,float y,out Text label)
        {
            var existing=entry.mapControls.transform.Find(name);
            var button=existing?existing.GetComponent<Button>():UnityEngine.Object.Instantiate(template,entry.mapControls.transform);
            button.name=name;button.onClick=new Button.ButtonClickedEvent();
            var rect=(RectTransform)button.transform;rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,y);rect.sizeDelta=new Vector2(360,100);
            label=button.GetComponentInChildren<Text>();label.fontSize=20;
            label.text=name.StartsWith("Room")?"Room\nStart in Reality\nCapture not configured":"Classroom\nStart in Reality\nCapture not configured";
            return button;
        }
    }
}
