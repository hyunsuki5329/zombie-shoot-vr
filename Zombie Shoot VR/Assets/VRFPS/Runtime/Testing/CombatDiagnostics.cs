using System;
using System.IO;
using UnityEngine;
namespace VRFPS
{
    [System.Serializable] public sealed class CombatDiagnosticsSample
    {
        public string kind,mapId,phase;
        public int attempt,alive,corpses,projectiles,magazines,looseMagazines,effects,effectObjects;
        public int index,wave,kills,aliveCap,rangedCap,projectileCap=12,magazineCap=10,looseCap=8,corpseCap=4,effectCap=16;
        public int enemyBodies,peakAlive,peakCorpses,projectileBodies,peakProjectiles,magazineBodies,peakMagazines,peakLoose;
        public double elapsedSeconds,activeSeconds;
        public long managedHeapBytes;
        public int rangedAlive,enemyBodyCap=16;
        public bool boundsSatisfied;
    }
    [Serializable] sealed class DiagnosticsHeader
    {
        public string kind="header",scenario,utc,unityVersion,buildGuid,mapId;
        public int schemaVersion=1;
        public double automaticIntervalSeconds=1;
        public bool cpuGpuTimingCaptured=false;
    }
    [Serializable] sealed class DiagnosticsFooter { public string kind="footer";public int samples; }
    [DefaultExecutionOrder(11000)]
    public sealed class CombatDiagnostics : MonoBehaviour
    {
        StreamWriter writer;
        RunController run;SpawnDirector director;AmmoPouch pouch;K2HandInput input;AcidProjectilePool projectiles;
        double started,nextSample;
        int attempt,samples;
        public bool IsRecording=>writer!=null;
        public string LastError { get; private set; }
        public string OutputPath { get; private set; }
        public static void AttachIfRequested(RunController run,SpawnDirector director,AmmoPouch pouch,K2HandInput input,string scenario)
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vrfps-diagnostics")<0)return;
            var capture=director.gameObject.AddComponent<CombatDiagnostics>();
            var path=Path.Combine(Application.persistentDataPath,"Diagnostics",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".jsonl");
            if(capture.Begin(path,run,director,pouch,input,scenario))Debug.Log("VRFPS_DIAGNOSTICS "+path);
            else Debug.LogWarning("VRFPS_DIAGNOSTICS_FAILED "+capture.LastError);
        }
        public bool Begin(string path,RunController run,SpawnDirector director,AmmoPouch pouch,K2HandInput input,string scenario)
        {
            Close();LastError=null;
            try
            {
                if(run==null||run.Map==null||director==null||pouch==null||input==null||string.IsNullOrEmpty(scenario))
                    throw new ArgumentException("Diagnostics requires a running map and combat references.");
                this.run=run;this.director=director;this.pouch=pouch;this.input=input;
                projectiles=director.GetComponentInChildren<AcidProjectilePool>(true);
                if(projectiles==null)throw new ArgumentException("Projectile pool is missing.");
                OutputPath=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                writer=new StreamWriter(new FileStream(OutputPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read));
                writer.WriteLine(JsonUtility.ToJson(new DiagnosticsHeader{scenario=scenario,utc=DateTime.UtcNow.ToString("O"),
                    unityVersion=Application.unityVersion,buildGuid=Application.buildGUID,mapId=run.Map.mapId}));
                writer.Flush();started=Time.realtimeSinceStartupAsDouble;nextSample=started;attempt=1;samples=0;
                run.RunStarted+=OnRetry;return true;
            }
            catch(Exception error){Fail(error);return false;}
        }
        void OnRetry()=>attempt++;
        void LateUpdate()
        {
            if(!IsRecording||Time.realtimeSinceStartupAsDouble<nextSample)return;
            nextSample=Time.realtimeSinceStartupAsDouble+1;
            if(!SampleNow())Debug.LogWarning("VRFPS_DIAGNOSTICS_FAILED "+LastError);
        }
        public bool SampleNow()
        {
            if(writer==null)return false;
            try
            {
                var shots=projectiles.Counts;var tokens=pouch.Counts;
                var sample=new CombatDiagnosticsSample{kind="sample",index=samples,attempt=attempt,mapId=run.Map.mapId,phase=run.Phase.ToString(),
                    elapsedSeconds=Time.realtimeSinceStartupAsDouble-started,activeSeconds=run.ActiveTime,wave=run.ReachedWave,kills=run.Kills,
                    alive=director.AliveCount,rangedAlive=director.RangedAliveCount,corpses=director.CorpseCount,aliveCap=run.Map.maxAlive,rangedCap=run.Map.maxRangedAlive,
                    enemyBodyCap=director.EnemyBodyCapacity,enemyBodies=director.CreatedCount,peakAlive=director.PeakAliveCount,peakCorpses=director.PeakCorpseCount,
                    projectiles=projectiles.ActiveCount,projectileBodies=shots.Created,peakProjectiles=shots.Peak,
                    magazines=tokens.Current,magazineBodies=tokens.Created,peakMagazines=tokens.Peak,
                    looseMagazines=pouch.LooseCount,peakLoose=pouch.PeakLooseCount,
                    effects=input.ActiveTracerCount+(director.WarningActive?1:0),effectObjects=input.TracerObjectCount+1,
                    managedHeapBytes=GC.GetTotalMemory(false)};
                sample.boundsSatisfied=sample.alive<=sample.aliveCap&&sample.rangedAlive<=sample.rangedCap&&sample.corpses<=sample.corpseCap&&
                    sample.enemyBodies<=sample.enemyBodyCap&&sample.projectiles<=sample.projectileCap&&sample.magazines<=sample.magazineCap&&
                    sample.looseMagazines<=sample.looseCap&&sample.effects<=sample.effectCap;
                writer.WriteLine(JsonUtility.ToJson(sample));writer.Flush();samples++;return true;
            }
            catch(Exception error){Fail(error);return false;}
        }
        void Fail(Exception error)
        {
            LastError=error.GetType().Name+": "+error.Message;
            DisposeWriter();
        }
        void DisposeWriter()
        {
            if(run!=null)run.RunStarted-=OnRetry;
            var current=writer;writer=null;
            try{current?.Dispose();}catch(Exception error){LastError=error.GetType().Name+": "+error.Message;}
        }
        public void Close()
        {
            if(writer==null)return;
            try{writer.WriteLine(JsonUtility.ToJson(new DiagnosticsFooter{samples=samples}));}
            catch(Exception error){LastError=error.GetType().Name+": "+error.Message;}
            finally{DisposeWriter();}
        }
        void OnDisable()=>Close();
        void OnDestroy()=>Close();
    }
}
