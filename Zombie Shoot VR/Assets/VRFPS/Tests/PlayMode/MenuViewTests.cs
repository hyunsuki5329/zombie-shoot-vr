using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace VRFPS.Tests
{
    public class MenuViewTests
    {
        GameObject root,panel;
        RunController run; MenuController menu; MenuView view; RunRecordBinding result;
        MapDefinition map; Text title,details; Button resume,retry,settings,back,maps,retrySave;
        string directory;
        T Child<T>(string name) where T:Component
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(panel.transform,false);return obj.AddComponent<T>();
        }
        [SetUp] public void SetUp()
        {
            directory=Path.Combine(Path.GetTempPath(),"VRFPS-menu-"+Guid.NewGuid().ToString("N"));
            root=new GameObject("Menu view test");panel=new GameObject("Panel");panel.transform.SetParent(root.transform);
            run=root.AddComponent<RunController>();run.enabled=false;
            menu=root.AddComponent<MenuController>();menu.Configure(run);
            result=root.AddComponent<RunRecordBinding>();result.Configure(run,new RecordStore(directory));
            title=Child<Text>("Title");details=Child<Text>("Details");
            resume=Child<Button>("Resume");retry=Child<Button>("Retry");settings=Child<Button>("Settings");
            back=Child<Button>("Back");maps=Child<Button>("Maps");retrySave=Child<Button>("Retry save");
            view=root.AddComponent<MenuView>();view.Configure(menu,run,result,panel,title,details,resume,retry,settings,back,maps,retrySave);
            map=ScriptableObject.CreateInstance<MapDefinition>();map.mapId="room";map.sceneName="Room";
            map.maxAlive=4;map.maxRangedAlive=1;map.minSpawnDistance=2;
        }
        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(map);
            if(Directory.Exists(directory))Directory.Delete(directory,true);
            if(File.Exists(directory))File.Delete(directory);
        }
        [Test] public void ButtonsDrivePauseSettingsResumeAndResultRetry()
        {
            run.StartRun(map);view.Refresh();Assert.That(panel.activeSelf,Is.False);
            menu.Pause();view.Refresh();Assert.That(panel.activeSelf,Is.True);
            Assert.That(title.text,Is.EqualTo("일시정지"));
            settings.onClick.Invoke();view.Refresh();Assert.That(title.text,Is.EqualTo("조작 방법"));
            back.onClick.Invoke();resume.onClick.Invoke();view.Refresh();Assert.That(panel.activeSelf,Is.False);
            run.Tick(8);run.RegisterSpawn(1);run.ResolveEnemy(1,true);run.ApplyDamage(100,1,0);view.Refresh();
            Assert.That(details.text,Does.Contain("웨이브 1").And.Contain("처치 1").And.Contain("저장 완료"));
            Assert.That(resume.gameObject.activeSelf,Is.False);
            retry.onClick.Invoke();view.Refresh();Assert.That(panel.activeSelf,Is.False);
            Assert.That(run.Health,Is.EqualTo(100));Assert.That(run.Kills,Is.Zero);
        }
        [Test] public void SaveFailureIsVisibleAndButtonRetriesWithoutRestart()
        {
            File.WriteAllText(directory,"block record directory");
            run.StartRun(map);run.Tick(8);run.EndRun();view.Refresh();
            Assert.That(details.text,Does.Contain("저장에 실패"));
            Assert.That(retrySave.gameObject.activeSelf,Is.True);
            File.Delete(directory);retrySave.onClick.Invoke();view.Refresh();
            Assert.That(details.text,Does.Contain("저장 완료"));Assert.That(retrySave.gameObject.activeSelf,Is.False);
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Dead));
            Assert.That(new RecordStore(directory).Read("room").reachedWave,Is.EqualTo(1));
        }
    }
}
