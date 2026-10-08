using System;
using UnityEngine;

namespace VRFPS
{
    // A short authored collapse blended from the exact pose at the lethal hit.
    // Combat time owns playback; the animator and navigation never move the corpse.
    [DefaultExecutionOrder(-20)]
    public sealed class CrawlerDeathMotion : MonoBehaviour
    {
        [Serializable] public struct BonePose
        {
            public HumanBodyBones bone;
            public Vector3 position;
            public Quaternion rotation;
        }
        [SerializeField] BonePose[] restingPose = Array.Empty<BonePose>();
        Animator animator;
        Transform[] bones;
        Vector3[] startPositions;
        Quaternion[] startRotations;
        public bool Playing { get; private set; }
        public float Elapsed { get; private set; }
        public const float Duration = 1.05f;
        public int PoseBoneCount => restingPose.Length;

        public void SetRestingPose(BonePose[] pose) { restingPose = pose; bones = null; }
        void Cache()
        {
            if (bones != null) return;
            animator = GetComponentInChildren<Animator>(true);
            bones = new Transform[restingPose.Length];
            startPositions = new Vector3[bones.Length]; startRotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) bones[i] = animator.GetBoneTransform(restingPose[i].bone);
        }
        public void Begin()
        {
            if (Playing) return;
            Cache(); if (bones.Length == 0) return;
            for (int i = 0; i < bones.Length; i++)
            {
                startPositions[i] = bones[i].localPosition; startRotations[i] = bones[i].localRotation;
            }
            Elapsed = 0; Playing = true; animator.enabled = false;
        }
        public void Tick(float deltaTime)
        {
            if (Playing) Elapsed = Mathf.Min(Duration, Elapsed + Mathf.Max(0, deltaTime));
        }
        void LateUpdate() => ApplyPose();
        public void ApplyPose()
        {
            if (!Playing) return;
            for (int i = 0; i < bones.Length; i++)
            {
                // Torso gives way first; wrists/head settle slightly later.
                var id = restingPose[i].bone;
                float delay = id == HumanBodyBones.Head || id == HumanBodyBones.LeftHand || id == HumanBodyBones.RightHand ? .10f : 0;
                float t = Mathf.Clamp01((Elapsed - delay) / (Duration - delay));
                float ease = t * t * (3 - 2 * t);
                bones[i].localPosition = Vector3.Lerp(startPositions[i], restingPose[i].position, ease);
                bones[i].localRotation = Quaternion.Slerp(startRotations[i], restingPose[i].rotation, ease);
            }
        }
        public void ResetMotion()
        {
            Playing = false; Elapsed = 0;
            if (animator != null) animator.enabled = true;
        }
    }
}
