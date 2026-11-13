#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRFPS.Player;
namespace VRFPS.Tests
{
    [PrebuildSetup(typeof(MapSceneTestSetup)),PostBuildCleanup(typeof(MapSceneTestSetup))]
    public class CombatSessionTests
    {
        const string Lab="Assets/VRFPS/Scenes/Tests/WaveLab.unity";
        const string Map="Assets/VRFPS/Tests/Fixtures/MapLoading/ValidMap.unity";
        CombatSession session;RunController run;PlayerRigController rig;K2Controller gun;
        MapDefinition map;string directory;Scene lab;RecordStore records;
        [UnitySetUp] public IEnumerator Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"VRFPS-session-"+Guid.NewGuid().ToString("N"));records=new RecordStore(directory);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Lab,new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            lab=SceneManager.GetSceneByPath(Lab);
            rig=UnityEngine.Object.FindFirstObjectByType<PlayerRigController>();run=rig.GetComponent<RunController>();run.enabled=false;rig.enabled=false;
            gun=UnityEngine.Object.FindFirstObjectByType<K2Controller>(FindObjectsInactive.Include);
            UnityEngine.Object.Destroy(rig.GetComponent<WaveLabSession>());UnityEngine.Object.Destroy(GameObject.Find("Wave Runtime"));yield return null;run.EndRun();
            var folder="Assets/VRFPS/Prefabs/Enemies/";
            session=rig.gameObject.AddComponent<CombatSession>();
            session.Configure(run,rig,gun,rig.GetComponentInChildren<LeftHandInteractor>(true),
                AssetDatabase.LoadAssetAtPath<GameObject>(folder+"MeleeZombie.prefab").GetComponent<EnemyActor>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(folder+"RangedZombie.prefab").GetComponent<EnemyActor>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(folder+"AcidProjectile.prefab").GetComponent<AcidProjectile>(),records);
            map=ScriptableObject.CreateInstance<MapDefinition>();map.mapId="room";map.sceneName=Map;map.maxAlive=4;map.maxRangedAlive=1;map.minSpawnDistance=2;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(session!=null)yield return session.Shutdown();
            if(lab.IsValid() && lab.isLoaded)yield return SceneManager.UnloadSceneAsync(lab);
            var scene=SceneManager.GetSceneByPath(Map);if(scene.IsValid() && scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            UnityEngine.Object.DestroyImmediate(map);if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        sealed class FakeReality : IRealityPresentation
        {
            public bool ready,hold,unloaded,loading,failUnload;public int loads;public Transform anchor;public IntroVirusTarget virus;
            public bool IsReady=>ready;public string LastError=>null;
            public Transform ObservationAnchor=>anchor;public IntroVirusTarget Virus=>virus;
            public IEnumerator Load(RealityIntroProfile profile){loading=true;loads++;while(hold)yield return null;ready=true;loading=false;}
            public IEnumerator Unload(){if(failUnload)throw new InvalidOperationException("unload failure probe");ready=false;unloaded=true;yield break;}
        }
        ComparisonIntroController MakeComparison(out FakeReality reality,out RealityIntroProfile profile,out VrScreenFade fade)
        {
            reality=new FakeReality{anchor=new GameObject("Reality anchor").transform,virus=new GameObject("Virus").AddComponent<IntroVirusTarget>()};
            profile=ScriptableObject.CreateInstance<RealityIntroProfile>();profile.mapId=map.mapId;
            profile.realityScenePath="fake";profile.realityAnchorKey="anchor";profile.themeAnchorKey="playerSpawn";profile.dataId="test";
            profile.fadeOutSeconds=.03f;profile.fadeInSeconds=.03f;profile.minBlackSeconds=.03f;
            fade=rig.gameObject.AddComponent<VrScreenFade>();var intro=rig.gameObject.AddComponent<ComparisonIntroController>();
            intro.Configure(reality,session,rig,gun,rig.GetComponent<WeaponContextRouter>(),fade);return intro;
        }
        static void DestroyComparisonData(FakeReality reality,RealityIntroProfile profile)
        {reality.hold=false;UnityEngine.Object.DestroyImmediate(reality.anchor.gameObject);UnityEngine.Object.DestroyImmediate(reality.virus.gameObject);UnityEngine.Object.DestroyImmediate(profile);}
        [UnityTest] public IEnumerator MenuOwnsIntroPauseCancelAndBlocksCombatRetry()
        {
            var intro=MakeComparison(out var reality,out var profile,out var fade);
            var menu=rig.gameObject.AddComponent<MenuController>();menu.Configure(run);menu.BindIntro(intro);
            var panel=new GameObject("Intro menu panel");panel.transform.SetParent(rig.transform);
            UnityEngine.UI.Button Button(string name){var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(panel.transform);return obj.AddComponent<UnityEngine.UI.Button>();}
            UnityEngine.UI.Text Text(string name){var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(panel.transform);return obj.AddComponent<UnityEngine.UI.Text>();}
            var resume=Button("Resume");var retry=Button("Retry");var settings=Button("Settings");var back=Button("Back");var maps=Button("Maps");var save=Button("Save");
            var title=Text("Title");var details=Text("Details");var view=rig.gameObject.AddComponent<MenuView>();
            view.Configure(menu,run,null,panel,title,details,resume,retry,settings,back,maps,save);
            run.StartRun(map);var starts=0;run.RunStarted+=()=>starts++;
            reality.hold=true;
            try
            {
                var request=IntroAsyncOperation.Start(intro.Begin(profile,map));var deadline=Time.realtimeSinceStartup+3;
                while(!reality.loading&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Hidden));
                menu.Pause();Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Pause));
                Assert.That(retry.gameObject.activeSelf,Is.False);Assert.That(details.text,Does.Contain("현실 공간에서 일시정지"));
                reality.hold=false;yield return null;yield return null;
                Assert.That(intro.Phase,Is.EqualTo(IntroPhase.LoadingReality));
                menu.Retry();Assert.That(starts,Is.Zero);
                menu.OpenSettings();Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Settings));
                menu.CloseSettings();Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Pause));
                menu.Resume();
                while(intro.Phase!=IntroPhase.RealityPreview&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(menu.Screen,Is.EqualTo(MenuScreen.Hidden));
                menu.Pause();Assert.That(reality.virus.ApplyDamage(1,gun.GetInstanceID(),0),Is.False);
                menu.ShowMapSelection();Assert.That(menu.Screen,Is.EqualTo(MenuScreen.MapSelection));
                while(!request.IsDone&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(request.IsDone,Is.True);Assert.That(reality.unloaded,Is.True);
                Assert.That(starts,Is.Zero);Assert.That(menu.Screen,Is.EqualTo(MenuScreen.MapSelection));
                reality.hold=true;reality.failUnload=true;
                var failed=IntroAsyncOperation.Start(intro.Begin(profile,map));deadline=Time.realtimeSinceStartup+3;
                while(!reality.loading&&Time.realtimeSinceStartup<deadline)yield return null;
                menu.ShowMapSelection();reality.hold=false;
                while(intro.Phase!=IntroPhase.Failed&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(retry.gameObject.activeSelf,Is.True);Assert.That(details.text,Does.Contain("현실 공간을 정리하지 못했습니다."));
                reality.failUnload=false;retry.onClick.Invoke();
                while(!failed.IsDone&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(failed.IsDone,Is.True);Assert.That(intro.IsBusy,Is.False);Assert.That(menu.Screen,Is.EqualTo(MenuScreen.MapSelection));
            }
            finally{reality.failUnload=false;if(intro!=null)intro.Cancel();DestroyComparisonData(reality,profile);}
        }
        [UnityTest] public IEnumerator ComparisonCleanupFailureCanBeRetried()
        {
            var intro=MakeComparison(out var reality,out var profile,out var fade);reality.hold=true;
            try
            {
                var request=IntroAsyncOperation.Start(intro.Begin(profile,map));var deadline=Time.realtimeSinceStartup+3;
                while(!reality.loading&&Time.realtimeSinceStartup<deadline)yield return null;
                reality.failUnload=true;intro.Cancel();reality.hold=false;
                while(intro.Phase!=IntroPhase.Failed&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(intro.LastError,Does.Contain("unload failure"));Assert.That(intro.IsBusy,Is.True);
                reality.failUnload=false;var retry=IntroAsyncOperation.Start(intro.Shutdown());
                while(!retry.IsDone&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(retry.IsDone,Is.True);Assert.That(intro.IsBusy,Is.False);Assert.That(reality.unloaded,Is.True);
                while(!request.IsDone&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(request.IsDone,Is.True);
            }
            finally{reality.failUnload=false;if(intro!=null)intro.Cancel();DestroyComparisonData(reality,profile);}
        }
        [UnityTest] public IEnumerator ComparisonInterruptedFadeCannotStartCombat()
        {
            var intro=MakeComparison(out var reality,out var profile,out var fade);profile.fadeOutSeconds=.5f;var starts=0;run.RunStarted+=()=>starts++;
            try
            {
                var request=IntroAsyncOperation.Start(intro.Begin(profile,map));var deadline=Time.realtimeSinceStartup+3;
                while(intro.Phase!=IntroPhase.RealityPreview&&Time.realtimeSinceStartup<deadline)yield return null;
                reality.virus.ApplyDamage(1,gun.GetInstanceID(),0);
                while(intro.Phase!=IntroPhase.FadingOut&&Time.realtimeSinceStartup<deadline)yield return null;
                fade.SetImmediate(0);
                while(!request.IsDone&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(request.IsDone,Is.True);Assert.That(starts,Is.Zero);Assert.That(intro.Phase,Is.EqualTo(IntroPhase.Failed));
            }
            finally{if(intro!=null)intro.Cancel();DestroyComparisonData(reality,profile);}
        }
        [UnityTest] public IEnumerator ComparisonOwnerSceneDestructionReleasesPreparedTheme()
        {
            var intro=MakeComparison(out var reality,out var profile,out var fade);var starts=0;run.RunStarted+=()=>starts++;
            intro.PhaseChanged+=phase=>{if(phase==IntroPhase.FadingIn)intro.SetPaused(true);};
            try
            {
                var request=IntroAsyncOperation.Start(intro.Begin(profile,map));var deadline=Time.realtimeSinceStartup+3;
                while(intro.Phase!=IntroPhase.RealityPreview&&Time.realtimeSinceStartup<deadline)yield return null;
                reality.virus.ApplyDamage(1,gun.GetInstanceID(),0);
                while(intro.Phase!=IntroPhase.FadingIn&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(session.HasPreparedMap,Is.True);yield return SceneManager.UnloadSceneAsync(lab);
                while(SceneManager.GetSceneByPath(Map).isLoaded&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(SceneManager.GetSceneByPath(Map).isLoaded,Is.False);Assert.That(starts,Is.Zero);
                while(!request.IsDone&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(request.IsDone,Is.True);
            }
            finally{if(intro!=null)intro.Cancel();DestroyComparisonData(reality,profile);}
        }
        [UnityTest] public IEnumerator IntroStartsOnlyAfterFadeAndCancellationDrainsLateLoad()
        {
            var anchor=new GameObject("Reality anchor");var target=new GameObject("Virus");
            var presentation=new FakeReality{anchor=anchor.transform,virus=target.AddComponent<IntroVirusTarget>()};
            var profile=ScriptableObject.CreateInstance<RealityIntroProfile>();
            profile.mapId=map.mapId;profile.realityScenePath="fake";profile.realityAnchorKey="anchor";
            profile.themeAnchorKey="playerSpawn";profile.dataId="test";
            profile.fadeOutSeconds=.03f;profile.fadeInSeconds=.03f;profile.minBlackSeconds=.03f;
            var fade=rig.gameObject.AddComponent<VrScreenFade>();
            var intro=rig.gameObject.AddComponent<ComparisonIntroController>();
            intro.Configure(presentation,session,rig,gun,rig.GetComponent<WeaponContextRouter>(),fade);
            var starts=0;run.RunStarted+=()=>{starts++;Assert.That(fade.Alpha,Is.Zero);};
            try
            {
                var request=IntroAsyncOperation.Start(intro.Begin(profile,map));
                var deadline=Time.realtimeSinceStartup+5;
                while(intro.Phase!=IntroPhase.RealityPreview && !request.IsDone && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(intro.Phase,Is.EqualTo(IntroPhase.RealityPreview));Assert.That(starts,Is.Zero);
                Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));
                intro.SetPaused(true);var clock=rig.GetComponent<WeaponContextRouter>().InputTime;
                yield return null;yield return null;
                Assert.That(rig.GetComponent<WeaponContextRouter>().InputTime,Is.EqualTo(clock));
                Assert.That(presentation.virus.ApplyDamage(1,gun.GetInstanceID(),0),Is.False);
                intro.SetPaused(false);
                Assert.That(presentation.virus.ApplyDamage(1,gun.GetInstanceID(),0),Is.True);
                Assert.That(presentation.virus.ApplyDamage(1,gun.GetInstanceID(),0),Is.False);
                while(!request.IsDone && Time.realtimeSinceStartup<deadline)
                {if(fade.Alpha>0)Assert.That(starts,Is.Zero);yield return null;}
                Assert.That(request.IsDone,Is.True);Assert.That(request.Error,Is.Null);
                Assert.That(intro.Phase,Is.EqualTo(IntroPhase.Complete));Assert.That(starts,Is.EqualTo(1));
                Assert.That(presentation.unloaded,Is.True);Assert.That(run.PreparationRemaining,Is.EqualTo(8));
                yield return intro.Shutdown();
                presentation.hold=true;presentation.unloaded=false;
                var late=IntroAsyncOperation.Start(intro.Begin(profile,map));
                deadline=Time.realtimeSinceStartup+5;
                while(!presentation.loading&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(presentation.loading,Is.True);
                intro.Cancel();yield return null;
                Assert.That(intro.IsBusy,Is.True);Assert.That(fade.Alpha,Is.Zero);
                presentation.hold=false;
                deadline=Time.realtimeSinceStartup+5;
                while(!late.IsDone && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(late.IsDone,Is.True);Assert.That(starts,Is.EqualTo(1));
                Assert.That(presentation.unloaded,Is.True);Assert.That(intro.IsBusy,Is.False);
                presentation.hold=true;presentation.unloaded=false;profile.loadTimeoutSeconds=.03f;
                var timed=IntroAsyncOperation.Start(intro.Begin(profile,map));
                deadline=Time.realtimeSinceStartup+5;
                while(intro.Phase!=IntroPhase.Failed&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(intro.Phase,Is.EqualTo(IntroPhase.Failed));Assert.That(intro.LastError,Does.Contain("timed out"));
                Assert.That(intro.IsBusy,Is.True);Assert.That(fade.Alpha,Is.Zero);
                var loadCount=presentation.loads;
                yield return intro.Begin(profile,map);Assert.That(presentation.loads,Is.EqualTo(loadCount));
                presentation.hold=false;
                while(!timed.IsDone&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(timed.IsDone,Is.True);Assert.That(presentation.unloaded,Is.True);Assert.That(starts,Is.EqualTo(1));
                presentation.hold=true;presentation.unloaded=false;profile.loadTimeoutSeconds=30;
                var destroyed=IntroAsyncOperation.Start(intro.Begin(profile,map));
                deadline=Time.realtimeSinceStartup+5;
                while(!presentation.loading&&Time.realtimeSinceStartup<deadline)yield return null;
                UnityEngine.Object.Destroy(intro);yield return null;presentation.hold=false;
                while(!destroyed.IsDone&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(destroyed.IsDone,Is.True);Assert.That(presentation.unloaded,Is.True);Assert.That(starts,Is.EqualTo(1));
            }
            finally{presentation.hold=false;if(intro!=null)intro.Cancel();UnityEngine.Object.DestroyImmediate(anchor);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(profile);}
        }
        [UnityTest] public IEnumerator CancelledPreparationUnloadsWithoutMovingOrCreatingRuntime()
        {
            var position=rig.transform.position;var starts=0;run.RunStarted+=()=>starts++;
            yield return session.PrepareMap(map,()=>false);
            Assert.That(session.HasPreparedMap,Is.False);Assert.That(session.Director,Is.Null);
            Assert.That(rig.transform.position,Is.EqualTo(position));Assert.That(starts,Is.Zero);
            Assert.That(SceneManager.GetSceneByPath(Map).isLoaded,Is.False);
        }
        [UnityTest] public IEnumerator PreparedMapDoesNotStartUntilRequestedAndStartsOnlyOnce()
        {
            var starts=0;run.RunStarted+=()=>starts++;
            Assert.That(session.BeginPreparedRun(),Is.False);
            yield return session.PrepareMap(map);
            Assert.That(session.LastError,Is.Null);
            Assert.That(session.HasPreparedMap,Is.True);
            Assert.That(session.IsReady,Is.False);
            Assert.That(starts,Is.Zero);Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));
            Assert.That(run.Alive,Is.Zero);
            Assert.That(session.BeginPreparedRun(),Is.True);
            Assert.That(session.BeginPreparedRun(),Is.False);
            Assert.That(starts,Is.EqualTo(1));Assert.That(session.IsReady,Is.True);
            Assert.That(run.PreparationRemaining,Is.EqualTo(8));
            Assert.That(run.Health,Is.EqualTo(100));Assert.That(gun.Magazine.Rounds,Is.EqualTo(30));
            Assert.That(run.ReachedWave,Is.Zero);Assert.That(run.Kills,Is.Zero);
        }
        [UnityTest] public IEnumerator PreparedPlacementSurvivesFirstStartAndRetryReturnsToDefaultSpawn()
        {
            foreach(var component in rig.Head.GetComponents<Behaviour>())
                if(component.GetType().Name=="TrackedPoseDriver")component.enabled=false;
            rig.Head.localPosition=new Vector3(.3f,1.7f,.2f);
            yield return session.PrepareMap(map);
            var anchor=new GameObject("Intro theme anchor");
            try
            {
                anchor.transform.SetParent(UnityEngine.Object.FindFirstObjectByType<MapRoot>().transform);
                anchor.transform.position=new Vector3(8001,0,1);
                anchor.transform.rotation=Quaternion.Euler(0,40,0);
                session.PlacePreparedPlayer(anchor.transform,25);
                var position=rig.transform.position;var rotation=rig.transform.rotation;
                Assert.That(Vector2.Distance(new Vector2(rig.Head.position.x,rig.Head.position.z),new Vector2(8001,1)),Is.LessThan(.02f));
                Assert.That(Mathf.DeltaAngle(rig.transform.eulerAngles.y,65),Is.EqualTo(0).Within(.01));
                Assert.That(session.BeginPreparedRun(),Is.True);
                Assert.That(rig.transform.position,Is.EqualTo(position));
                Assert.That(Quaternion.Angle(rig.transform.rotation,rotation),Is.LessThan(.001f));
                run.StartRun(map);
                Assert.That(Vector2.Distance(new Vector2(rig.Head.position.x,rig.Head.position.z),new Vector2(8000,0)),Is.LessThan(.02f));
            }
            finally{UnityEngine.Object.DestroyImmediate(anchor);}
        }
        [UnityTest] public IEnumerator PreparedPlacementRejectsForeignOrUnreachableAnchorsWithoutMovingPlayer()
        {
            yield return session.PrepareMap(map);
            var anchor=new GameObject("Invalid intro anchor");var position=rig.transform.position;
            try
            {
                anchor.transform.position=new Vector3(8001,0,1);
                Assert.Throws<InvalidOperationException>(()=>session.PlacePreparedPlayer(anchor.transform,0));
                anchor.transform.SetParent(UnityEngine.Object.FindFirstObjectByType<MapRoot>().transform);
                anchor.transform.position=new Vector3(8001,100,1);
                Assert.Throws<InvalidOperationException>(()=>session.PlacePreparedPlayer(anchor.transform,0));
                anchor.transform.position=new Vector3(8001,0,1);map.minSpawnDistance=100000;
                Assert.Throws<InvalidOperationException>(()=>session.PlacePreparedPlayer(anchor.transform,0));
                Assert.That(rig.transform.position,Is.EqualTo(position));
            }
            finally{UnityEngine.Object.DestroyImmediate(anchor);}
        }
        [UnityTest] public IEnumerator FailedStartReturnsErrorAndLegacyWrapperUnloadsOwnedMap()
        {
            run.RunStarted+=()=>throw new InvalidOperationException("start failure probe");
            yield return session.StartMap(map);
            Assert.That(session.LastError,Is.EqualTo("start failure probe"));
            Assert.That(session.IsReady,Is.False);Assert.That(session.HasPreparedMap,Is.False);
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));Assert.That(session.Director,Is.Null);
            Assert.That(SceneManager.GetSceneByPath(Map).isLoaded,Is.False);
        }
        [UnityTest] public IEnumerator MapStartsAtSpawnAndDeathPersistsSameRun()
        {
            foreach(var component in rig.Head.GetComponents<Behaviour>())
                if(component.GetType().Name=="TrackedPoseDriver")component.enabled=false;
            var trackedPosition=new Vector3(.3f,1.7f,.2f);var trackedRotation=Quaternion.Euler(10,15,0);
            rig.Head.localPosition=trackedPosition;rig.Head.localRotation=trackedRotation;
            yield return session.StartMap(map);
            Assert.That(rig.Head.localPosition,Is.EqualTo(trackedPosition));
            Assert.That(Quaternion.Angle(rig.Head.localRotation,trackedRotation),Is.LessThan(.001f));
            Assert.That(session.IsReady,Is.True);Assert.That(session.LastError,Is.Null);
            Assert.That(Vector2.Distance(new Vector2(rig.Head.position.x,rig.Head.position.z),new Vector2(8000,0)),Is.LessThan(.02f));
            Assert.That(run.Health,Is.EqualTo(100));Assert.That(gun.Magazine.Rounds,Is.EqualTo(30));Assert.That(run.Phase,Is.EqualTo(RunPhase.Preparing));
            session.Director.enabled=false;run.Tick(8);session.Director.Tick(0);session.Director.Tick(1.6f);
            Assert.That(run.Alive,Is.EqualTo(1));
            run.ApplyDamage(100,1,0);Assert.That(session.Results.SaveSucceeded,Is.True);
            Assert.That(new RecordStore(directory).Read("room").reachedWave,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RepeatedMapChangesKeepOneRuntimeAndShutdownUnloadsMap()
        {
            var count=SceneManager.sceneCount;
            for(int i=0;i<10;i++)
            {
                yield return session.StartMap(map);Assert.That(session.IsReady,Is.True);
                Assert.That(UnityEngine.Object.FindObjectsByType<SpawnDirector>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
                Assert.That(SceneManager.sceneCount,Is.EqualTo(count+1));
            }
            yield return session.Shutdown();Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));
            Assert.That(session.IsReady,Is.False);Assert.That(SceneManager.sceneCount,Is.EqualTo(count));
        }
    }
}
#endif
