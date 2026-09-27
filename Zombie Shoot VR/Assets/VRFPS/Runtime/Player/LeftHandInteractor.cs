using UnityEngine;

namespace VRFPS
{
    public enum LeftHandOwner { None, Foregrip, InsertedMagazine, LooseMagazine }
    public sealed class LeftHandInteractor : MonoBehaviour
    {
        [SerializeField] K2Controller gun;
        [SerializeField] Transform left;
        [SerializeField] Transform foregrip;
        [SerializeField] MagazineSocket socket;
        [SerializeField] AmmoPouch pouch;
        [SerializeField] MagazineToken initial;
        bool configured, previousGrip;
        public LeftHandOwner Owner { get; private set; }
        public MagazineToken HeldToken { get; private set; }
        public void Restart()
        {
            if (!configured) throw new System.InvalidOperationException("Left hand must be configured before restarting.");
            ReleaseAll();
            gun.Restart();
            initial = pouch.ResetForRun();
            socket.ResetForRun(initial);
            previousGrip = true; // A grip held through retry must be released before a new grab.
        }

        void Awake()
        {
            if (gun != null && left != null && socket != null && pouch != null && initial != null)
                Configure(gun, left, foregrip, socket, pouch, initial);
        }

        public void Configure(K2Controller gun, Transform left, Transform foregrip, MagazineSocket socket, AmmoPouch pouch, MagazineToken initial)
        {
            if (configured) throw new System.InvalidOperationException("Left hand is already configured.");
            this.gun = gun; this.left = left; this.foregrip = foregrip;
            this.socket = socket; this.pouch = pouch; this.initial = initial;
            socket.Initialize(gun.Magazine, left, initial);
            pouch.Register(initial);
            configured = true;
        }

        public void ReleaseAll()
        {
            if (HeldToken != null) HeldToken.Release(left);
            HeldToken = null; Owner = LeftHandOwner.None;
            if (gun != null) gun.SetLeftGrip(false);
            if (pouch != null) pouch.TickLifetime(0);
        }

        public void Tick(bool grip, bool interactionEnabled)
        {
            if (!configured) return;
            var pressed = grip && !previousGrip;
            previousGrip = grip;
            gun.SetLeftGrip(false);
            if (!interactionEnabled) return;
            if (!grip) { ReleaseAll(); return; }
            if (Owner == LeftHandOwner.Foregrip && Vector3.Distance(left.position, foregrip.position) > 0.25f)
                Owner = LeftHandOwner.None;
            if (Owner == LeftHandOwner.LooseMagazine && HeldToken == null) Owner = LeftHandOwner.None;
            if (Owner == LeftHandOwner.None && pressed)
            {
                HeldToken = socket.TryEject(left);
                if (HeldToken == null) HeldToken = pouch.TrySupply(left);
                if (HeldToken == null)
                {
                    var nearby = pouch.FindNearbyLoose(left);
                    if (nearby != null && nearby.TryGrab(left)) HeldToken = nearby;
                }
                if (HeldToken != null) Owner = LeftHandOwner.LooseMagazine;
                else if (Vector3.Distance(left.position, foregrip.position) <= 0.18f) Owner = LeftHandOwner.Foregrip;
            }
            if (Owner == LeftHandOwner.LooseMagazine && socket.TryInsert(HeldToken))
            { HeldToken = null; Owner = LeftHandOwner.None; }
            gun.SetLeftGrip(Owner == LeftHandOwner.Foregrip);
        }
    }
}
