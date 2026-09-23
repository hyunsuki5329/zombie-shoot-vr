#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VRFPS.Tests
{
    public class EnemyLabSceneTests
    {
        [UnityTest]
        public IEnumerator AuthoredLabStartsAndResetsTwoEnemiesWithoutDuplicateObjects()
        {
            const string path = "Assets/VRFPS/Scenes/Tests/InteractionLab.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            var scene = SceneManager.GetSceneByPath(path);
            try
            {
                var session = Object.FindFirstObjectByType<EnemyLabSession>();
                Assert.That(session, Is.Not.Null);
                session.Restart(); yield return null;
                var actors = Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None);
                Assert.That(actors.Length, Is.EqualTo(2));
                foreach (var actor in actors)
                {
                    Assert.That(actor.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);
                    Assert.That(actor.GetComponentInChildren<Animator>().applyRootMotion, Is.False);
                    Assert.That(actor.Controller.ApplyDamage(100, 8, 1), Is.True);
                }
                session.Restart(); yield return null;
                var resetActors = Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None);
                Assert.That(resetActors.Length, Is.EqualTo(2));
                CollectionAssert.AreEquivalent(actors, resetActors);
                foreach (var actor in resetActors) Assert.That(actor.Controller.IsDead, Is.False);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
            }
            while (scene.IsValid() && scene.isLoaded) yield return null;
            yield return null;
            Assert.That(Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).Length, Is.Zero);
        }
    }
}
#endif
