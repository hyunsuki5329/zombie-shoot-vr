#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace VRFPS.Tests
{
    public class WaveLabSceneTests
    {
        [UnityTest] public IEnumerator AuthoredWaveLabStartsCombatAndRetriesTenTimesWithoutOrphans()
        {
            const string path = "Assets/VRFPS/Scenes/Tests/WaveLab.unity";
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null, "P07 needs its authored wave scene.");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            // Loading completes before the new scene's Start callbacks run.
            yield return null;
            var scene = SceneManager.GetSceneByPath(path);
            try
            {
                var session = Object.FindFirstObjectByType<WaveLabSession>();
                var run = Object.FindFirstObjectByType<RunController>();
                var director = Object.FindFirstObjectByType<SpawnDirector>();
                // XRI can hide the untracked controller hierarchy in a headless run.
                var gun = Object.FindFirstObjectByType<K2Controller>(FindObjectsInactive.Include);
                Assert.That(gun, Is.Not.Null);
                Assert.That(session, Is.Not.Null);
                Assert.That(run, Is.Not.Null); Assert.That(director, Is.Not.Null);
                Assert.That(Object.FindFirstObjectByType<EnemyLabSession>(), Is.Null);
                Assert.That(Object.FindFirstObjectByType<WeaponLabSession>(), Is.Null);
                Assert.That(run.Phase, Is.EqualTo(RunPhase.Preparing));
                run.enabled = false; director.enabled = false;
                run.Tick(8); director.Tick(0); director.Tick(1.6f);
                Assert.That(run.Alive, Is.EqualTo(1));
                var actor = director.GetComponentInChildren<EnemyActor>();
                Assert.That(actor.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh, Is.True);
                actor.Controller.ApplyDamage(1000, gun.GetInstanceID(), run.ActiveTime);
                Assert.That(run.Kills, Is.EqualTo(1));
                run.SetPaused(true);
                Assert.That(gun.Phase, Is.EqualTo(RunPhase.Paused));
                var menuView = Object.FindFirstObjectByType<MenuView>();
                Assert.That(menuView, Is.Not.Null, "WaveLab must include the authored menu.");
                var menuPanel = menuView.transform.Find("Panel");
                Assert.That(menuPanel.gameObject.activeSelf, Is.True);
                Assert.That(menuPanel.Find("Title").GetComponent<Text>().text, Is.EqualTo("PAUSED"));
                menuPanel.Find("Resume").GetComponent<Button>().onClick.Invoke();
                Assert.That(run.Phase, Is.EqualTo(RunPhase.Active));
                Assert.That(menuPanel.gameObject.activeSelf, Is.False);
                run.SetPaused(true);
                for (var i = 0; i < 10; i++)
                {
                    gun.Magazine.TryConsume(); session.Restart();
                    Assert.That(run.Health, Is.EqualTo(100)); Assert.That(run.ReachedWave, Is.Zero);
                    Assert.That(run.Kills, Is.Zero); Assert.That(run.Alive, Is.Zero);
                    Assert.That(gun.Magazine.Rounds, Is.EqualTo(30));
                    Assert.That(gun.Phase, Is.EqualTo(RunPhase.Preparing));
                    Assert.That(director.CreatedCount, Is.EqualTo(1));
                    var visibleTokens = 0;
                    foreach (var token in Object.FindObjectsByType<MagazineToken>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (token.gameObject.activeSelf) visibleTokens++;
                    Assert.That(visibleTokens, Is.EqualTo(1));
                }
            }
            finally { if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene); }
            while (scene.IsValid() && scene.isLoaded) yield return null;
            yield return null;
            Assert.That(Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.Zero);
            Assert.That(Object.FindObjectsByType<MagazineToken>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.Zero);
        }
    }
}
#endif
