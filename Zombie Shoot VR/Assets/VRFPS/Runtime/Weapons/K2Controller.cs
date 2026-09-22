using UnityEngine;

namespace VRFPS
{
    public sealed class K2Controller : MonoBehaviour
    {
        [SerializeField] Transform rightAnchor;
        [SerializeField] Transform leftAnchor;
        [SerializeField] Transform muzzle;
        [SerializeField] LayerMask worldSolidMask;
        [SerializeField] LayerMask shotMask;
        bool leftGrip;
        RunPhase phase = RunPhase.Preparing;
        K2FireControl fire;
        public MagazineState Magazine { get; } = new MagazineState(30);
        public RunPhase Phase => phase;
        public event System.Action<Vector3, Vector3> Fired;

        public void Configure(Transform right, Transform left, Transform muzzle, LayerMask worldMask, LayerMask shotMask)
        {
            rightAnchor = right;
            leftAnchor = left;
            this.muzzle = muzzle;
            worldSolidMask = worldMask;
            this.shotMask = shotMask;
        }

        public void SetLeftGrip(bool held) => leftGrip = held;
        public void SetCombatEnabled(bool value) => SetPhase(value ? RunPhase.Active : RunPhase.Preparing);
        public void SetPhase(RunPhase value)
        {
            phase = value;
            if (phase != RunPhase.Active) leftGrip = false;
        }

        public void UpdatePose()
        {
            if (rightAnchor == null) return;
            transform.position = rightAnchor.position;
            if (!leftGrip || leftAnchor == null) { transform.rotation = rightAnchor.rotation; return; }
            var direction = leftAnchor.position - rightAnchor.position;
            if (direction.sqrMagnitude < 0.0001f) return;
            direction.Normalize();
            var up = rightAnchor.up;
            if (Vector3.Cross(direction, up).sqrMagnitude < 0.0001f)
                up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) < 0.99f ? Vector3.up : Vector3.right;
            transform.rotation = Quaternion.LookRotation(direction, up);
        }

        public bool Tick(bool trigger, double activeTime)
        {
            UpdatePose();
            if (rightAnchor == null || leftAnchor == null || muzzle == null || worldSolidMask.value == 0 || shotMask.value == 0) return false;
            if (fire == null) fire = new K2FireControl(Magazine);
            var barrel = muzzle.position - rightAnchor.position;
            const float clearance = 0.01f;
            var blocked = Physics.CheckSphere(rightAnchor.position, clearance, worldSolidMask, QueryTriggerInteraction.Ignore) ||
                Physics.CheckSphere(muzzle.position, clearance, worldSolidMask, QueryTriggerInteraction.Ignore) ||
                (barrel.sqrMagnitude > 0.000001f && Physics.SphereCast(rightAnchor.position, clearance,
                    barrel.normalized, out _, barrel.magnitude, worldSolidMask, QueryTriggerInteraction.Ignore));
            if (!fire.TryFire(trigger, leftGrip, phase, blocked, activeTime)) return false;
            var end = muzzle.position + muzzle.forward * 60f;
            if (Physics.Raycast(muzzle.position, muzzle.forward, out var hit, 60f, shotMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                hit.collider.GetComponentInParent<IDamageable>()?.ApplyDamage(25, GetInstanceID(), activeTime);
            }
            Fired?.Invoke(muzzle.position, end);
            return true;
        }
    }
}
