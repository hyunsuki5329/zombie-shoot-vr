using UnityEngine;
namespace VRFPS
{
    public sealed class AcidProjectile : MonoBehaviour
    {
        const float Speed = 3, Lifetime = 4, Radius = 0.05f;
        Vector3 direction;
        int sourceId, damage, solidMask, collisionMask;
        readonly Collider[] overlap = new Collider[1];
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
            // This single-player game needs one hit; never allocate an overlap array per tick.
            if(Physics.OverlapSphereNonAlloc(transform.position,Radius,overlap,collisionMask,QueryTriggerInteraction.Ignore)>0)
            {
                var collider=overlap[0];overlap[0]=null;
                // A full buffer may hide a wall behind a player collider. Solids always win.
                if((solidMask & (1<<collider.gameObject.layer))!=0 ||
                    Physics.CheckSphere(transform.position,Radius,solidMask,QueryTriggerInteraction.Ignore)) { Retire(); return; }
                Impact(collider,activeTime);return;
            }
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
