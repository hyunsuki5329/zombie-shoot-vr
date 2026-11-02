using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace VRFPS
{
    [RequireComponent(typeof(CombatSceneEntry))]
    public sealed class ComparisonIntroInstaller : MonoBehaviour
    {
        public UniversalRenderPipelineAsset pipeline;
        public Shader fadeShader;
        public RealityIntroProfile roomProfile,classroomProfile;
        public bool IsInstalled {get;private set;}
        public string LastError {get;private set;}
        CombatSceneEntry entry;ComparisonIntroController intro;
        void OnEnable()
        {
            entry=GetComponent<CombatSceneEntry>();entry.Initialized+=Install;
            if(entry.IsInitialized)Install();
        }
        void OnDisable()
        {
            if(entry!=null){entry.Initialized-=Install;if(entry.IsInitialized)entry.ConfigureRealityOptions(roomProfile,classroomProfile,"Comparison disabled");}
            if(intro!=null)intro.Cancel();
        }
        void Install()
        {
            LastError=null;
            try
            {
                if(!IsInstalled)
                {
                    if(!pipeline||!fadeShader)throw new InvalidOperationException("Comparison rendering is not configured");
                    var camera=entry.rig.Head.GetComponent<Camera>();
                    var router=entry.run.GetComponent<WeaponContextRouter>();
                    var fade=GetComponent<VrScreenFade>()??gameObject.AddComponent<VrScreenFade>();fade.Configure(camera,fadeShader);
                    intro=GetComponent<ComparisonIntroController>()??gameObject.AddComponent<ComparisonIntroController>();
                    intro.Configure(new GsPipelinePresentation(new SceneRealityPresentation(),pipeline,camera),entry.Session,entry.rig,entry.gun,router,fade);
                    entry.BindComparison(intro);IsInstalled=true;
                }
                if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Direct3D12&&SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Vulkan)
                    LastError="GS requires a DX12/Vulkan build";
            }
            catch(Exception error){LastError=error.Message;}
            // Invalid/missing GS configuration never disables the existing direct game.
            entry.ConfigureRealityOptions(roomProfile,classroomProfile,LastError);
        }
    }
}
