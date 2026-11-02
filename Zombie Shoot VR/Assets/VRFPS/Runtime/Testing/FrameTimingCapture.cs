using System;
using System.IO;
using UnityEngine;
namespace VRFPS
{
    [Serializable] sealed class FrameTimingMetadata
    {
        public int schemaVersion=1;
        public string source="Unity.FrameTimingManager",units="milliseconds",scenario,mapId,utc,buildGuid,unityVersion,graphicsApi,objectLog;
        public string retrievalPolicy="Latest one per poll; coverage is not guaranteed";
        public string phaseMeaning="Run phase at retrieval, not necessarily at measured frame";
        public string cpuTotalMeaning="Includes waiting and overhead; not CPU active work";
        public string[] phaseNames=Enum.GetNames(typeof(RunPhase));
        public bool featureEnabledAtStart;
    }
    [DefaultExecutionOrder(11010)]
    public sealed class FrameTimingCapture : MonoBehaviour
    {
        readonly FrameTiming[] timing=new FrameTiming[1];
        FrameTimingLog log;
        RunController run;
        double started,nextFlush;
        int attempt;
        public string LastError { get; private set; }
        public bool IsRecording=>log!=null;
        public static void AttachIfRequested(RunController run,SpawnDirector director,string scenario)
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vrfps-frame-timings")<0)return;
            var objects=director.GetComponent<CombatDiagnostics>();
            var objectPath=objects!=null?objects.OutputPath:null;
            var path=string.IsNullOrEmpty(objectPath)
                ?Path.Combine(Application.persistentDataPath,"Diagnostics",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".vrft")
                :Path.ChangeExtension(objectPath,".vrft");
            var capture=director.gameObject.AddComponent<FrameTimingCapture>();
            if(capture.Begin(path,run,scenario,objectPath))Debug.Log("VRFPS_FRAME_TIMINGS "+path);
            else Debug.LogWarning("VRFPS_FRAME_TIMINGS_FAILED "+capture.LastError);
        }
        public bool Begin(string path,RunController run,string scenario,string objectLog=null)
        {
            Close();LastError=null;
            try
            {
                if(run==null||run.Map==null)throw new ArgumentException("Frame capture needs a running map.");
                this.run=run;
                log=new FrameTimingLog(path,JsonUtility.ToJson(new FrameTimingMetadata{scenario=scenario,mapId=run.Map.mapId,
                    utc=DateTime.UtcNow.ToString("O"),buildGuid=Application.buildGUID,unityVersion=Application.unityVersion,
                    graphicsApi=SystemInfo.graphicsDeviceType.ToString(),objectLog=objectLog,
                    featureEnabledAtStart=FrameTimingManager.IsFeatureEnabled()}));
                started=Time.realtimeSinceStartupAsDouble;nextFlush=started+1;attempt=1;run.RunStarted+=OnRetry;
                return true;
            }
            catch(Exception error){LastError=error.Message;Close();return false;}
        }
        void OnRetry()=>attempt++;
        void LateUpdate()=>PollNow();
        public void PollNow()
        {
            if(log==null)return;
            try
            {
                if(!FrameTimingManager.IsFeatureEnabled())log.Unavailable(false);
                else
                {
                    FrameTimingManager.CaptureFrameTimings();
                    if(FrameTimingManager.GetLatestTimings(1,timing)==0)log.Unavailable(true);
                    else log.Append(timing[0],Time.realtimeSinceStartupAsDouble-started,Time.frameCount,attempt,run.Phase);
                }
                if(Time.realtimeSinceStartupAsDouble>=nextFlush){log.Flush();nextFlush=Time.realtimeSinceStartupAsDouble+1;}
            }
            catch(Exception error){LastError=error.Message;Close();Debug.LogWarning("VRFPS_FRAME_TIMINGS_FAILED "+LastError);}
        }
        public void Close()
        {
            if(run!=null)run.RunStarted-=OnRetry;
            var current=log;log=null;
            try{current?.Dispose();}catch(Exception error){LastError=error.Message;}
        }
        void OnDisable()=>Close();
        void OnDestroy()=>Close();
    }
}
