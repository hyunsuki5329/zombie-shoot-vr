using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace VRFPS.Tests
{
    public class EnemyVisualAssetTests
    {
        [TestCase("MeleeZombie", .65f, .85f)]
        [TestCase("RangedZombie", .8f, 2.7f)]
        public void AttackRecoversBeforeNextWindup(string name, float windup, float cooldown)
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/Enemies/" + name + ".prefab"));
            try
            {
                var animator = instance.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0);
                for (var cycle = 0; cycle < 3; cycle++)
                {
                    animator.SetBool("Attack", true);
                    for (var elapsed = 0f; elapsed < windup; elapsed += .01f) animator.Update(.01f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"), Is.True);
                    animator.SetBool("Attack", false);
                    for (var elapsed = 0f; elapsed < cooldown - .05f; elapsed += .01f) animator.Update(.01f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") && !animator.IsInTransition(0), Is.True, "Recovery must finish before a new windup, or the next visual attack is skipped");
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [TestCase("MeleeZombie")]
        [TestCase("RangedZombie")]
        public void WalkStaysNearCollisionBodyAndPauseFreezesPose(string name)
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/Enemies/" + name + ".prefab"));
            try
            {
                var animator = instance.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0);
                animator.SetFloat("Speed", 1);
                animator.Play("Base Layer.Walk", 0, .1f); animator.Update(.001f);
                var hip = animator.GetBoneTransform(HumanBodyBones.Hips);
                var first = hip.position;
                animator.Play("Base Layer.Walk", 0, .9f); animator.Update(.001f);
                Assert.That(Vector3.ProjectOnPlane(hip.position - first, Vector3.up).magnitude, Is.LessThan(.35f), "Walking visual must not leave the stationary collision body");
                Assert.That(instance.transform.position, Is.EqualTo(Vector3.zero));
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var pose = hand.position;
                animator.speed = 0; animator.Update(.5f);
                Assert.That(Vector3.Distance(hand.position, pose), Is.LessThan(.001f));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [TestCase("MeleeZombie", "Yaku J Ignite")]
        [TestCase("RangedZombie", "Parasite L Starkie")]
        public void HumanoidVisualPreservesCombatBodyAndResolvesAllDependencies(string name, string model)
        {
            var path = "Assets/VRFPS/Prefabs/Enemies/" + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var visual = prefab.transform.Find("Mixamo Visual");
            Assert.That(visual, Is.Not.Null, "User-selected Mixamo visual missing");
            foreach (Match reference in Regex.Matches(File.ReadAllText(path), @"m_SourcePrefab: \{fileID: [^,]+, guid: ([0-9a-f]+)"))
                Assert.That(AssetDatabase.GUIDToAssetPath(reference.Groups[1].Value), Is.Not.Empty);
            Assert.That(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject)), Does.EndWith(model + ".fbx"));
            Assert.That(prefab.GetComponent<EnemyActor>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<NavMeshAgent>().height, Is.EqualTo(1.7f));
            Assert.That(prefab.GetComponent<CapsuleCollider>().height, Is.EqualTo(1.7f));
            Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(prefab.transform.Find("Visual").gameObject.activeSelf, Is.False);
            var animators = prefab.GetComponentsInChildren<Animator>(true);
            Assert.That(animators.Length, Is.EqualTo(1), "EnemyActor must select the new animator even including inactive children");
            var animator = animators[0];
            Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            var clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
            Assert.That(clips.Length, Is.EqualTo(4));
            foreach (var clip in clips)
            {
                Assert.That(clip.humanMotion, Is.True);
                Assert.That(clip.length, Is.GreaterThan(.1f), "A bind-pose clip is not gameplay animation");
            }
            var renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.That(renderer.sharedMesh.triangles.Length / 3, Is.InRange(1000, 15000));
            Assert.That(renderer.sharedMaterials.Length, Is.EqualTo(renderer.sharedMesh.subMeshCount));
            foreach (var material in renderer.sharedMaterials)
            {
                Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null);
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")));
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.EqualTo(importer.DoesSourceTextureHaveAlpha()), "Preserve torn clothing alpha");
            }
            foreach (var bone in visual.GetComponentsInChildren<Transform>(true))
                Assert.That(bone.gameObject.layer, Is.EqualTo(prefab.layer));
        }
    }
}
