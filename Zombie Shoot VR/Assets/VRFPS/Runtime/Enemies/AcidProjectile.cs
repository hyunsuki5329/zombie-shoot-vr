using UnityEngine;
namespace VRFPS
{
    public sealed class AcidProjectile : MonoBehaviour
    {
        const float Speed = 3, Lifetime = 4, Radius = 0.05f;
        Vector3 direction;
        int sourceId, damage, solidMask, collisionMask;
        float remaining;
        public bool Flying { get; private set; }

        public void Launch(Vector3 origin, Vector3 direction, int sourceId, int damage, LayerMask solidMask, LayerMask playerMask)
        {
            if (direction.sqrMagnitude < 0.000001f || damage <= 0 || solidMask.value == 0 || playerMask.value == 0)
                throw new System.ArgumentException("Projectile requires direction, positive damage and collision masks.");
            transform.position = origin;
            this.direction = direction.normalized;
            transform.rotation = Quaternion.LookRotation(this.direction);
            this.sourceId = sourceId; this.damage = damage;
            this.solidMask = solidMask; collisionMask = solidMask | playerMask;
            remaining = Lifetime; Flying = true;
            gameObject.SetActive(true);
        }

        public void Tick(float deltaTime, double activeTime)
        {
            if (!Flying || deltaTime <= 0) return;
            // SphereCast does not report colliders that already contain the origin.
            var overlaps = Physics.OverlapSphere(transform.position, Radius, collisionMask, QueryTriggerInteraction.Ignore);
            foreach (var collider in overlaps)
                if ((solidMask & (1 << collider.gameObject.layer)) != 0) { Retire(); return; }
            if (overlaps.Length > 0) { Impact(overlaps[0], activeTime); return; }
            var elapsed = Mathf.Min(deltaTime, remaining);
            var distance = Speed * elapsed;
            if (Physics.SphereCast(transform.position, Radius, direction, out var hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point;
                Impact(hit.collider, activeTime);
                return;
            }
            transform.position += direction * distance;
            remaining -= elapsed;
            if (remaining <= 0) Retire();
        }

        void Impact(Collider collider, double activeTime)
        {
            Retire(); // Resolve before notifying damage receivers to prevent reentrant hits.
            if ((solidMask & (1 << collider.gameObject.layer)) == 0)
                collider.GetComponentInParent<IDamageable>()?.ApplyDamage(damage, sourceId, activeTime);
        }

        public void Retire() { Flying = false; gameObject.SetActive(false); }
    }
}
