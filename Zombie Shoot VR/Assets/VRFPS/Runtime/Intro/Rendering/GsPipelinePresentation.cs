using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace VRFPS
{
    public sealed class GsPipelinePresentation : IRealityPresentation
    {
        static GsPipelinePresentation owner;
        static readonly FieldInfo RendererIndex=typeof(UniversalAdditionalCameraData).GetField("m_RendererIndex",BindingFlags.Instance|BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Pinned URP camera renderer index missing.");
        readonly IRealityPresentation inner;
        readonly UniversalRenderPipelineAsset pipeline;
        readonly Camera camera;
        UniversalAdditionalCameraData cameraData;
        RenderPipelineAsset originalQuality;
        int originalLevel,originalRenderer;
        bool held,starting,ready;
        IntroAsyncOperation current;
        public GsPipelinePresentation(IRealityPresentation inner,UniversalRenderPipelineAsset pipeline,Camera camera)
        {
            this.inner=inner??throw new ArgumentNullException(nameof(inner));
            this.pipeline=pipeline;this.camera=camera;
        }
        public bool IsReady=>ready&&held&&owner==this&&QualitySettings.renderPipeline==pipeline&&
            cameraData&&(int)RendererIndex.GetValue(cameraData)==0&&inner.IsReady;
        public string LastError=>current?.Error?.Message??inner.LastError;
        public Transform ObservationAnchor=>inner.ObservationAnchor;
        public IntroVirusTarget Virus=>inner.Virus;
        public IEnumerator Load(RealityIntroProfile profile)=>Run(LoadCore(profile));
        public IEnumerator Unload()=>Run(UnloadCore());
        IEnumerator Run(IEnumerator operation)
        {
            if(starting||(current!=null&&!current.IsDone))throw new InvalidOperationException("GS pipeline operation is already running.");
            IntroAsyncOperation job;starting=true;
            try{job=IntroAsyncOperation.Start(operation);current=job;}finally{starting=false;}
            while(!job.IsDone)yield return null;
            if(job.Error!=null)throw new InvalidOperationException(job.Error.Message,job.Error);
        }
        IEnumerator LoadCore(RealityIntroProfile profile)
        {
            if(held||owner!=null)throw new InvalidOperationException("The GS pipeline is already owned by a presentation.");
            if(!pipeline||pipeline.msaaSampleCount!=1)throw new InvalidOperationException("GS pipeline must disable MSAA.");
            if(!camera||camera!=Camera.main||!camera.TryGetComponent(out cameraData))
                throw new InvalidOperationException("GS requires the main rig camera with URP camera data.");
            var features=GaussianResourceInventory.RendererFeatures.GetValue(pipeline.GetRenderer(0)) as List<ScriptableRendererFeature>;
            if(features==null||!features.Any(f=>f&&f.isActive&&f.GetType().FullName=="GaussianSplatting.Runtime.GaussianSplatURPFeature"))
                throw new InvalidOperationException("GS pipeline renderer zero requires an active Gaussian Splat feature.");
            originalLevel=QualitySettings.GetQualityLevel();originalQuality=QualitySettings.renderPipeline;
            originalRenderer=(int)RendererIndex.GetValue(cameraData);owner=this;held=true;
            // Use a runtime quality override, leaving the project's default pipeline and
            // pipeline assets themselves untouched. Retain ownership on load failure.
            QualitySettings.renderPipeline=pipeline;cameraData.SetRenderer(0);
            yield return null;
            yield return inner.Load(profile);
            ready=true;
        }
        IEnumerator UnloadCore()
        {
            ready=false;
            if(!held)yield break;
            // A release failure keeps the GS configuration and reservation for cleanup retry.
            yield return inner.Unload();
            Restore();
            yield return null;
        }
        void Restore()
        {
            var selectedLevel=QualitySettings.GetQualityLevel();
            try
            {
                // Restore the slot we changed, even if another system selected another
                // quality level meanwhile. Never overwrite a newer external override.
                if(selectedLevel!=originalLevel)QualitySettings.SetQualityLevel(originalLevel,false);
                if(QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=originalQuality;
            }
            finally {if(QualitySettings.GetQualityLevel()!=selectedLevel)QualitySettings.SetQualityLevel(selectedLevel,false);}
            if(cameraData&&(int)RendererIndex.GetValue(cameraData)==0)cameraData.SetRenderer(originalRenderer);
            held=false;if(owner==this)owner=null;
        }
    }
}
