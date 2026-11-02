using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
    public class InstructionMenuTests
    {
        [Test] public void InstructionsReturnToSelectionAndPauseCombatUntilExplicitResume()
        {
            var root=new GameObject("Instruction test");
            var map=ScriptableObject.CreateInstance<MapDefinition>();
            try
            {
                var run=root.AddComponent<RunController>();run.enabled=false;
                var menu=root.AddComponent<MenuController>();menu.Configure(run);
                var open=typeof(MenuController).GetMethod("OpenInstructions");
                var close=typeof(MenuController).GetMethod("CloseInstructions");
                Assert.That(open,Is.Not.Null,"The control guide must be accessible from the menu.");
                Assert.That(close,Is.Not.Null);
                open.Invoke(menu,null);Assert.That(menu.Screen.ToString(),Is.EqualTo("Instructions"));
                close.Invoke(menu,null);Assert.That(menu.Screen,Is.EqualTo(MenuScreen.MapSelection));
                map.mapId="room";map.sceneName="Room";map.maxAlive=4;map.maxRangedAlive=1;map.minSpawnDistance=2;
                run.StartRun(map);run.Tick(8);open.Invoke(menu,null);
                Assert.That(run.Phase,Is.EqualTo(RunPhase.Paused));
                run.Tick(30);Assert.That(run.ActiveTime,Is.Zero);
                open.Invoke(menu,null);close.Invoke(menu,null);
                Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Pause));
                Assert.That(run.Phase,Is.EqualTo(RunPhase.Paused));
                menu.Resume();Assert.That(run.Phase,Is.EqualTo(RunPhase.Active));
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(map);}
        }
    }
}
