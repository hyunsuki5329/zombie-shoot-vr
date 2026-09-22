using UnityEngine;

namespace VRFPS
{
    public enum MagazineTokenState { Fresh, Inserted, Discarded }

    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class MagazineToken : MonoBehaviour
    {
        public MagazineTokenState State { get; private set; }
        public Transform HeldBy { get; private set; }
        MagazineSocket mountedIn;

        public bool TryGrab(Transform hand)
        {
            if (hand == null || HeldBy != null || State == MagazineTokenState.Inserted) return false;
            Hold(hand);
            return true;
        }

        public bool Release(Transform hand)
        {
            if (HeldBy == null || HeldBy != hand) return false;
            HeldBy = null;
            transform.SetParent(null, true);
            var body = GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = true;
            body.detectCollisions = true;
            return true;
        }

        void Hold(Transform hand)
        {
            HeldBy = hand;
            SetAttachedPhysics();
            transform.SetParent(hand, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        void SetAttachedPhysics()
        {
            var body = GetComponent<Rigidbody>();
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            body.isKinematic = true;
            body.detectCollisions = false;
        }

        internal void Mount(MagazineSocket socket)
        {
            if (State != MagazineTokenState.Fresh) throw new System.InvalidOperationException("Only a fresh magazine can be mounted.");
            State = MagazineTokenState.Inserted;
            HeldBy = null;
            mountedIn = socket;
            SetAttachedPhysics();
            transform.SetParent(socket.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        internal bool EjectToHand(MagazineSocket socket, Transform hand)
        {
            if (mountedIn != socket || State != MagazineTokenState.Inserted || hand == null) return false;
            mountedIn = null;
            State = MagazineTokenState.Discarded;
            Hold(hand);
            return true;
        }
    }
}
