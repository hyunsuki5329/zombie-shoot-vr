using UnityEngine;
using UnityEngine.InputSystem;

namespace VRFPS
{
    public sealed class K2HandInput : MonoBehaviour
    {
        [SerializeField] K2Controller gun;
        [SerializeField] Transform left;
        [SerializeField] Transform foregrip;
        [SerializeField] InputActionReference gripAction;
        [SerializeField] InputActionReference triggerAction;
        [SerializeField] LineRenderer tracer;
        double activeTime;
        float tracerUntil;

        public void Configure(K2Controller gun, Transform left, Transform foregrip)
        {
            this.gun = gun; this.left = left; this.foregrip = foregrip;
        }

        void OnEnable() { if (gun != null) gun.Fired += ShowTracer; }
        void OnDisable()
        {
            if (gun != null) { gun.Fired -= ShowTracer; gun.SetLeftGrip(false); }
            if (tracer != null) tracer.enabled = false;
        }

        void LateUpdate()
        {
            if (gun == null) return;
            if (gun.Phase == RunPhase.Active) activeTime += Time.deltaTime;
            TickInput(gripAction != null && gripAction.action.IsPressed(),
                triggerAction != null && triggerAction.action.IsPressed(), activeTime);
            if (tracer != null && Time.unscaledTime >= tracerUntil) tracer.enabled = false;
        }

        public bool TickInput(bool grip, bool trigger, double time)
        {
            if (gun == null || left == null || foregrip == null) return false;
            gun.UpdatePose();
            gun.SetLeftGrip(grip && Vector3.Distance(left.position, foregrip.position) <= 0.18f);
            return gun.Tick(trigger, time);
        }

        void ShowTracer(Vector3 start, Vector3 end)
        {
            if (tracer == null) return;
            tracer.SetPosition(0, start); tracer.SetPosition(1, end);
            tracer.enabled = true;
            tracerUntil = Time.unscaledTime + 0.05f;
        }
    }
}
