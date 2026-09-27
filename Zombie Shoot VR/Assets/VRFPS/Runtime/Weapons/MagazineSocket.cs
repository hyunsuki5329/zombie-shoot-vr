using UnityEngine;

namespace VRFPS
{
    public sealed class MagazineSocket : MonoBehaviour
    {
        public MagazineToken Installed { get; private set; }
        MagazineState ammo;
        Transform leftHand;

        internal void ResetForRun(MagazineToken token)
        {
            if (ammo == null || !ammo.Inserted || ammo.Rounds != ammo.Capacity || token == null ||
                token.State != MagazineTokenState.Fresh || token.HeldBy != null)
                throw new System.InvalidOperationException("Restart requires reset ammo and an unowned fresh token.");
            token.Mount(this); Installed = token;
        }

        public void Initialize(MagazineState ammo, Transform leftHand, MagazineToken initial)
        {
            if (this.ammo != null) throw new System.InvalidOperationException("Socket is already initialized.");
            if (ammo == null) throw new System.ArgumentNullException(nameof(ammo));
            if (leftHand == null) throw new System.ArgumentNullException(nameof(leftHand));
            if (initial == null || !ammo.Inserted || ammo.Rounds != ammo.Capacity ||
                initial.State != MagazineTokenState.Fresh || initial.HeldBy != null)
                throw new System.ArgumentException("A new run must start with one full, unheld magazine.");
            this.ammo = ammo;
            this.leftHand = leftHand;
            initial.Mount(this);
            Installed = initial;
        }

        public MagazineToken TryEject(Transform hand)
        {
            if (ammo == null || Installed == null || hand != leftHand) return null;
            if (!(Vector3.Distance(hand.position, transform.position) <= 0.12f)) return null;
            var token = Installed;
            if (!token.EjectToHand(this, hand)) return null;
            ammo.Eject();
            Installed = null;
            return token;
        }

        public bool TryInsert(MagazineToken token)
        {
            if (ammo == null || Installed != null || ammo.Inserted || token == null ||
                token.State != MagazineTokenState.Fresh || token.HeldBy != leftHand) return false;
            if (!(Vector3.Distance(token.transform.position, transform.position) <= 0.12f &&
                  Quaternion.Angle(token.transform.rotation, transform.rotation) <= 35f)) return false;
            if (!ammo.InsertFresh()) return false;
            token.Mount(this);
            Installed = token;
            return true;
        }
    }
}
