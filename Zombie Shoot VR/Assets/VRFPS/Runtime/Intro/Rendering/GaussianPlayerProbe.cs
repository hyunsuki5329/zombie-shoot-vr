using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace VRFPS
{
    [Serializable] public sealed class GaussianPlayerProbeReport
    {
        public bool success,resourcesReleased,pipelineRestored;
        public int completedCycles,blackFrames,minVisiblePixels;
        public string error,api,buildGuid,unityVersion;
        public string scope="Analytical GS, mono GPU lifecycle only; not real capture, XR or performance acceptance";
    }
    // Included only in the separate diagnostic scene. No player records/settings or timing capture.
    public sealed class GaussianPlayerProbe : MonoBehaviour
    {
        public GaussianRealitySource source;
        public Camera viewCamera;
        public UniversalRenderPipelineAsset pipeline;
        public Shader fadeShader;
        public GaussianPlayerProbeReport Report {get;private set;}
        bool running;VrScreenFade fade;
        sealed class Presentation : IRealityPresentation
        {
            readonly GaussianRealitySource source;
            public Presentation(GaussianRealitySource source){this.source=source;}
            public bool IsReady=>source.IsReady;public string LastError=>null;
            public Transform ObservationAnchor=>null;public IntroVirusTarget Virus=>null;
            public IEnumerator Load(RealityIntroProfile profile)=>source.Initialize();
            public IEnumerator Unload()=>source.Release();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-vrfps-gs-probe")<0)yield break;
            var index=Array.IndexOf(args,"-vrfps-gs-probe-result");
            if(index<0||index+1>=args.Length||!Path.IsPathRooted(args[index+1]))
            {Debug.LogError("GS_PLAYER_PROBE requires an absolute -vrfps-gs-probe-result path");Application.Quit(1);yield break;}
            yield return RunProbe();
            try
            {
                var path=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path,JsonUtility.ToJson(Report,true));Debug.Log("GS_PLAYER_PROBE "+JsonUtility.ToJson(Report));
            }
            catch(Exception error){Debug.LogError("GS_PLAYER_PROBE result write failed: "+error.Message);Application.Quit(1);yield break;}
            Application.Quit(Report.success?0:1);
        }
        public IEnumerator RunProbe()
        {
            // Includes final inventory/reflection observations: a broken Player linker
            // must produce failure JSON/exit1 instead of abandoning the startup coroutine.
            var operation=IntroAsyncOperation.Start(RunUnchecked());
            while(!operation.IsDone)yield return null;
            if(operation.Error!=null)
            {
                if(Report==null)Report=new GaussianPlayerProbeReport();
                Report.success=false;Report.error=(Report.error??"")+" Probe: "+operation.Error.Message;
            }
        }
        IEnumerator RunUnchecked()
        {
            if(running)throw new InvalidOperationException("GS probe already running");
            running=true;
            Report=new GaussianPlayerProbeReport{api=SystemInfo.graphicsDeviceType.ToString(),buildGuid=Application.buildGUID,
                unityVersion=Application.unityVersion,minVisiblePixels=int.MaxValue};
            var original=QualitySettings.renderPipeline;
            try
            {
                if(!source||!viewCamera||!pipeline||!fadeShader){Report.error="Probe references are incomplete";yield break;}
                var scoped=new GsPipelinePresentation(new Presentation(source),pipeline,viewCamera);
                var work=IntroAsyncOperation.Start(Core(scoped));while(!work.IsDone)yield return null;
                if(work.Error!=null)Report.error=work.Error.Message;
                var cleanup=IntroAsyncOperation.Start(scoped.Unload());while(!cleanup.IsDone)yield return null;
                if(cleanup.Error!=null)Report.error=(Report.error??"")+" Cleanup: "+cleanup.Error.Message;
                Report.resourcesReleased=!source.HasResources;Report.pipelineRestored=QualitySettings.renderPipeline==original;
                Report.success=Report.error==null&&Report.completedCycles==10&&Report.blackFrames==10&&Report.resourcesReleased&&Report.pipelineRestored;
            }
            finally
            {
                if(fade){fade.SetImmediate(0);Destroy(fade);fade=null;}
                if(Report.minVisiblePixels==int.MaxValue)Report.minVisiblePixels=0;
                running=false;
            }
        }
        IEnumerator Core(GsPipelinePresentation scoped)
        {
            fade=gameObject.AddComponent<VrScreenFade>();fade.Configure(viewCamera,fadeShader);
            for(var cycle=0;cycle<10;cycle++)
            {
                yield return scoped.Load(null);yield return null;
                if(!scoped.IsReady||!source.HasResources)throw new InvalidOperationException("GS was not ready");
                var visible=ReadPixels().Count(p=>p.r>60&&p.r>p.g*2&&p.r>p.b*2);
                Report.minVisiblePixels=Math.Min(Report.minVisiblePixels,visible);
                if(visible<=100)throw new InvalidOperationException("Insufficient actual GS pixels: "+visible);
                fade.SetImmediate(1);
                if(!ReadPixels().All(p=>p.r<=2&&p.g<=2&&p.b<=2))throw new InvalidOperationException("Mono blackout leaked pixels");
                Report.blackFrames++;fade.SetImmediate(0);
                yield return scoped.Unload();
                if(source.HasResources)throw new InvalidOperationException("GS resources remain after release");
                Report.completedCycles++;
            }
        }
        Color32[] ReadPixels()
        {
            var target=new RenderTexture(256,256,24);target.Create();var image=new Texture2D(256,256,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(viewCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();return image.GetPixels32();
            }
            finally {RenderTexture.active=previous;target.Release();Destroy(target);Destroy(image);}
        }
    }
}
