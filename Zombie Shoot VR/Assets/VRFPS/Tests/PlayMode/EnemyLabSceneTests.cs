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
                    var animator = actor.GetComponentInChildren<Animator>();
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var hitLayer = animator.GetLayerIndex("Hit Reaction");
                    Assert.That(hitLayer, Is.GreaterThan(0), "Final enemies need the authored hit reaction");
                    Assert.That(actor.Controller.ApplyDamage(1, 8, .1), Is.True);
                    actor.Tick(0, .1);
                    animator.Update(.01f); animator.Update(.08f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(hitLayer).IsName("Hit"), Is.True);
                    Assert.That(animator.GetLayerWeight(hitLayer), Is.GreaterThan(0));
                    var firstHitTime = animator.GetCurrentAnimatorStateInfo(hitLayer).normalizedTime;
                    Assert.That(actor.Controller.ApplyDamage(1, 8, .15), Is.True);
                    actor.Tick(.05f, .15); animator.Update(.05f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(hitLayer).normalizedTime, Is.GreaterThan(firstHitTime), "Automatic fire must not pin the flinch to its first frame");
                    actor.SetCombatEnabled(false);
                    var pausedTime = animator.GetCurrentAnimatorStateInfo(hitLayer).normalizedTime;
                    animator.Update(.2f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(hitLayer).normalizedTime, Is.EqualTo(pausedTime).Within(.001f));
                    actor.SetCombatEnabled(true);
                    actor.Tick(.5f, .8); animator.Update(.5f);
                    Assert.That(animator.GetLayerWeight(hitLayer), Is.Zero, "Hit blend must expire");
                    Assert.That(actor.Controller.ApplyDamage(1, 8, .9), Is.True);
                    actor.Tick(0, .9);
                    Assert.That(animator.GetLayerWeight(hitLayer), Is.GreaterThan(0));
                    Assert.That(actor.Controller.ApplyDamage(100, 8, 1), Is.True);
                    Assert.That(animator.GetBool("Dead"), Is.True);
                    animator.Update(.01f); animator.Update(.3f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Dead"), Is.True);
                    Assert.That(animator.GetLayerWeight(hitLayer), Is.Zero);
                }
                session.Restart(); yield return null;
                var resetActors = Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None);
                Assert.That(resetActors.Length, Is.EqualTo(2));
                CollectionAssert.AreEquivalent(actors, resetActors);
                foreach (var actor in resetActors)
                {
                    Assert.That(actor.Controller.IsDead, Is.False);
                    var animator = actor.GetComponentInChildren<Animator>();
                    Assert.That(animator.GetBool("Dead"), Is.False);
                    Assert.That(animator.speed, Is.EqualTo(1));
                    animator.Update(.01f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Dead"), Is.False, "Reused enemy must leave the corpse animation");
                    Assert.That(animator.GetLayerWeight(animator.GetLayerIndex("Hit Reaction")), Is.Zero);
                }
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
