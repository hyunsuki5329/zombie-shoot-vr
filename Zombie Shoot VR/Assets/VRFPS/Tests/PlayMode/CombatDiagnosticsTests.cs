#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace VRFPS.Tests
{
    public class CombatDiagnosticsTests
    {
        [UnityTest] public IEnumerator CaptureRecordsActualSceneCountsPauseAndRetryWithoutOverwritingFiles()
        {
            const string scenePath="Assets/VRFPS/Scenes/Tests/WaveLab.unity";
            var path=Path.Combine(Application.temporaryCachePath,"vrfps-diagnostics-"+Guid.NewGuid().ToString("N")+".jsonl");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath,new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            var scene=SceneManager.GetSceneByPath(scenePath);
            try
            {
                var run=UnityEngine.Object.FindFirstObjectByType<RunController>();
                var director=UnityEngine.Object.FindFirstObjectByType<SpawnDirector>();
                var pouch=UnityEngine.Object.FindFirstObjectByType<AmmoPouch>(FindObjectsInactive.Include);
                var input=UnityEngine.Object.FindFirstObjectByType<K2HandInput>(FindObjectsInactive.Include);
                run.enabled=false;director.enabled=false;
                var capture=director.gameObject.AddComponent<CombatDiagnostics>();capture.enabled=false;
                Assert.That(capture.Begin(path,run,director,pouch,input,"wave-lab"),Is.True);
                for(var i=0;i<120;i++)Assert.That(capture.SampleNow(),Is.True);
                run.SetPaused(true);Assert.That(capture.SampleNow(),Is.True);
                run.StartRun(run.Map);Assert.That(capture.SampleNow(),Is.True);capture.Close();
                var lines=File.ReadAllLines(path);
                Assert.That(lines.Length,Is.EqualTo(124),"Header,122 samples,footer.");
                var sample=JsonUtility.FromJson<CombatDiagnosticsSample>(lines[1]);
                Assert.That(sample.kind,Is.EqualTo("sample"));Assert.That(sample.mapId,Is.EqualTo(run.Map.mapId));
                Assert.That(sample.magazines,Is.EqualTo(1));Assert.That(sample.alive,Is.Zero);
                Assert.That(sample.projectiles,Is.Zero);Assert.That(sample.effectObjects,Is.EqualTo(2));
                Assert.That(sample.boundsSatisfied,Is.True);
                var paused=JsonUtility.FromJson<CombatDiagnosticsSample>(lines[121]);
                Assert.That(paused.phase,Is.EqualTo("Paused"));
                var retry=JsonUtility.FromJson<CombatDiagnosticsSample>(lines[122]);Assert.That(retry.attempt,Is.EqualTo(2));
                Assert.That(capture.Begin(path,run,director,pouch,input,"wave-lab"),Is.False);
                Assert.That(capture.LastError,Is.Not.Empty);Assert.That(File.ReadAllLines(path).Length,Is.EqualTo(124));
                Assert.That(capture.IsRecording,Is.False);
                Assert.That(capture.Begin(path+".auto",run,director,pouch,input,"wave-lab"),Is.True);
                capture.enabled=true;yield return null;yield return new WaitForSecondsRealtime(1.1f);capture.enabled=false;
                Assert.That(capture.IsRecording,Is.False);
                var automatic=File.ReadAllLines(path+".auto");
                Assert.That(automatic.Length,Is.GreaterThanOrEqualTo(4),"Header,at least two timed samples,footer.");
                Assert.That(automatic[automatic.Length-1],Does.Contain("footer"));
                TestContext.Out.WriteLine(lines[0]);TestContext.Out.WriteLine(lines[1]);TestContext.Out.WriteLine(lines[122]);
                var frames=director.gameObject.AddComponent<FrameTimingCapture>();frames.enabled=false;
                Assert.That(frames.Begin(path+".vrft",run,"wave-lab",path),Is.True);
                frames.PollNow();run.StartRun(run.Map);frames.PollNow();frames.Close();
                Assert.That(frames.LastError,Is.Null);Assert.That(frames.IsRecording,Is.False);
                using(var reader=new BinaryReader(File.OpenRead(path+".vrft")))
                {
                    Assert.That(System.Text.Encoding.ASCII.GetString(reader.ReadBytes(8)),Is.EqualTo("VRFT0001"));
                    var metadata=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                    Assert.That(metadata,Does.Contain("Unity.FrameTimingManager"));TestContext.Out.WriteLine(metadata);
                }
            }
            finally
            {
                if(File.Exists(path))File.Delete(path);if(File.Exists(path+".auto"))File.Delete(path+".auto");
                if(File.Exists(path+".vrft"))File.Delete(path+".vrft");
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
#endif
