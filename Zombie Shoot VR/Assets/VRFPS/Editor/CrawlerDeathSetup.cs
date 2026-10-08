using System.Collections.Generic;
using UnityEngine;

namespace VRFPS.Editor
{
    public static class CrawlerDeathSetup
    {
        public static void Apply(GameObject prefabRoot)
        {
            var sample = Object.Instantiate(prefabRoot);
            try
            {
                var animator = sample.GetComponentInChildren<Animator>();
                animator.enabled = true; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0); animator.SetFloat("Speed", 1);
                animator.Play("Base Layer.Walk", 0, .35f); animator.Update(0);
                sample.GetComponent<GroundedEnemyVisual>().AlignPose();
                var visual = animator.transform;
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                hips.rotation = Quaternion.AngleAxis(65, visual.forward) * hips.rotation;
                var spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                spine.rotation = Quaternion.AngleAxis(30, visual.right) * spine.rotation;
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                head.rotation = Quaternion.AngleAxis(35, visual.right) * head.rotation;
                RelaxArm(animator, true); RelaxArm(animator, false);
                var poses = new List<CrawlerDeathMotion.BonePose>();
                for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                {
                    var bone = animator.GetBoneTransform((HumanBodyBones)i); if (bone == null) continue;
                    poses.Add(new CrawlerDeathMotion.BonePose { bone = (HumanBodyBones)i, position = bone.localPosition, rotation = bone.localRotation });
                }
                var motion = prefabRoot.GetComponent<CrawlerDeathMotion>() ?? prefabRoot.AddComponent<CrawlerDeathMotion>();
                motion.SetRestingPose(poses.ToArray());
            }
            finally { Object.DestroyImmediate(sample); }
        }
        static void RelaxArm(Animator animator, bool left)
        {
            var upper = animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            var lower = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            float side = left ? -1 : 1;
            var root = animator.transform.parent;
            var elbowTarget = root.TransformPoint(new Vector3(side * .42f, .12f, .35f));
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbowTarget - upper.position) * upper.rotation;
            var handTarget = root.TransformPoint(new Vector3(side * .65f, .08f, .65f));
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, handTarget - lower.position) * lower.rotation;
        }
    }
}
