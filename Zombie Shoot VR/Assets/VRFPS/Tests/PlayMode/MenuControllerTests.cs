using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
    public class MenuControllerTests
    {
        GameObject root;
        RunController run;
        MenuController menu;
        MapDefinition map;
        [SetUp] public void SetUp()
        {
            root=new GameObject("Menu test"); run=root.AddComponent<RunController>();run.enabled=false;
            menu=root.AddComponent<MenuController>(); menu.Configure(run);
            map=ScriptableObject.CreateInstance<MapDefinition>();map.mapId="room";map.sceneName="Room";
            map.maxAlive=4;map.maxRangedAlive=1;map.minSpawnDistance=2;
        }
        [TearDown] public void TearDown(){Object.DestroyImmediate(root);Object.DestroyImmediate(map);}
        [Test] public void SettingsFromCombatRemainPausedUntilExplicitResume()
        {
            run.StartRun(map);run.Tick(8);menu.OpenSettings();
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Paused));
            Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Settings));
            run.Tick(30);Assert.That(run.ActiveTime,Is.Zero);
            menu.CloseSettings();Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Pause));
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Paused));
            menu.Resume();Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Hidden));
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Active));
        }
        [Test] public void DeathShowsResultAndRetryStartsSameMapWithFreshState()
        {
            run.StartRun(map);run.Tick(8);run.RegisterSpawn(1);run.ResolveEnemy(1,true);
            run.ApplyDamage(100,1,0);
            Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Result));
            menu.Resume();Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));
            Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Result));
            menu.Retry();Assert.That(run.Map,Is.SameAs(map));
            Assert.That(run.Health,Is.EqualTo(100));Assert.That(run.Kills,Is.Zero);
            Assert.That(run.ReachedWave,Is.Zero);Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Hidden));
        }
        [Test] public void MapSelectionEndsRunOnceAndSettingsReturnToSelection()
        {
            Assert.That(menu.Screen,Is.EqualTo(MenuScreen.MapSelection));
            run.StartRun(map);int ended=0;run.RunEnded+=()=>ended++;
            menu.ShowMapSelection();menu.ShowMapSelection();
            Assert.That(ended,Is.EqualTo(1));Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));
            Assert.That(menu.Screen,Is.EqualTo(MenuScreen.MapSelection));
            menu.OpenSettings();menu.CloseSettings();
            Assert.That(menu.Screen,Is.EqualTo(MenuScreen.MapSelection));
            menu.Resume();Assert.That(menu.Screen,Is.EqualTo(MenuScreen.MapSelection));
        }
    }
}
