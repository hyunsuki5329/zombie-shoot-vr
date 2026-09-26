using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
    public sealed class ForearmHUDTests
    {
        GameObject root;
        RunController run;
        MapDefinition map;
        ForearmHUD hud;
        TextMesh health, progress;
        GameObject warning;
        [SetUp] public void SetUp()
        {
            root = new GameObject("HUD test");
            run = root.AddComponent<RunController>(); run.enabled = false;
            hud = root.AddComponent<ForearmHUD>();
            health = new GameObject("Health").AddComponent<TextMesh>(); health.transform.SetParent(root.transform);
            progress = new GameObject("Progress").AddComponent<TextMesh>(); progress.transform.SetParent(root.transform);
            warning = new GameObject("Warning"); warning.transform.SetParent(root.transform);
            map = ScriptableObject.CreateInstance<MapDefinition>();
            map.mapId="room"; map.sceneName="Room"; map.maxAlive=4; map.maxRangedAlive=1; map.minSpawnDistance=2;
            hud.Configure(run,health,progress,warning);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(root); Object.DestroyImmediate(map); }
        [Test] public void PreparationAndClearKeepActualReachedWaveAndKills()
        {
            run.StartRun(map); hud.Refresh();
            Assert.That(health.text, Is.EqualTo("HP 100 / 100"));
            Assert.That(progress.text, Does.Contain("WAVE 0").And.Contain("PREPARE 8s"));
            run.Tick(8);
            for(int i=0;i<6;i++) { Assert.That(run.RegisterSpawn(i),Is.True); run.ResolveEnemy(i,true); }
            run.Tick(0); hud.Refresh();
            Assert.That(progress.text, Does.Contain("WAVE 1").And.Contain("KILLS 6").And.Contain("PREPARE"));
            run.Tick(8); hud.Refresh();
            Assert.That(progress.text, Does.Contain("WAVE 2"));
        }
        [Test] public void LowHealthUsesWarningAndRestoresAfterRestart()
        {
            run.StartRun(map); run.Tick(8); run.ApplyDamage(75,1,0); hud.Refresh();
            Assert.That(health.text,Is.EqualTo("HP 25 / 100"));
            Assert.That(warning.activeSelf,Is.True);
            Assert.That(health.color,Is.EqualTo(Color.red));
            run.SetPaused(true); hud.Refresh();
            Assert.That(progress.text,Does.Contain("PAUSED"));
            run.SetPaused(false); run.Tick(0.5f); run.ApplyDamage(25,1,0); hud.Refresh();
            Assert.That(progress.text,Does.Contain("DEAD"));
            run.StartRun(map); hud.Refresh();
            Assert.That(warning.activeSelf,Is.False);
            Assert.That(health.color,Is.EqualTo(Color.white));
            Assert.That(health.text,Is.EqualTo("HP 100 / 100"));
        }
    }
}

