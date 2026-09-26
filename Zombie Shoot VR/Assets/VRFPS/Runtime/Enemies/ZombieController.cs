using System;
using UnityEngine;
namespace VRFPS
{
    public enum EnemyKind { Melee, Ranged }
    public sealed class ZombieController : MonoBehaviour, IDamageable
    {
        int enemyId, health, attackDamage;
        EnemyKind kind;
        Transform target, origin;
        LayerMask solidMask;
        bool configured, combatEnabled;
        float windupRemaining, cooldownRemaining;
        public bool IsWindingUp { get; private set; }
        public bool IsDead => configured && health == 0;
        public int Health => health;
        public bool HoldsPosition => IsWindingUp || cooldownRemaining > 0 || CanReachTarget();
        public event Action<Vector3, Vector3, int, int> ProjectileRequested;
        public event Action<int, int> Died;
        public void Configure(int id, EnemyKind kind, Transform target, Transform origin, LayerMask solidMask, int wave = 1)
        {
            if (target == null || origin == null || solidMask.value == 0)
                throw new ArgumentException("Enemy requires a target, attack origin and solid mask.");
            if (kind != EnemyKind.Melee && kind != EnemyKind.Ranged) throw new ArgumentOutOfRangeException(nameof(kind));
            enemyId = id; this.kind = kind; this.target = target; this.origin = origin; this.solidMask = solidMask;
            health = GameBalance.EnemyHealth(kind, wave);
            attackDamage = GameBalance.EnemyDamage(kind, wave);
            configured = combatEnabled = true;
            IsWindingUp = false; windupRemaining = cooldownRemaining = 0;
        }

        public static bool IsMeleeHitValid(Vector3 origin, Vector3 target, float range, int solidMask)
        {
            var delta = target - origin;
            var distance = delta.magnitude;
            return distance <= range && !Physics.CheckSphere(origin, 0.01f, solidMask, QueryTriggerInteraction.Ignore) &&
                (distance < 0.001f || !Physics.Raycast(origin, delta / distance, distance, solidMask, QueryTriggerInteraction.Ignore));
        }

        bool CanReachTarget()
        {
            if (target == null || origin == null) return false;
            var range = kind == EnemyKind.Melee ? 1.2f : 7f;
            if (kind == EnemyKind.Ranged && Vector3.Distance(origin.position, target.position) < 2.5f) return false;
            return IsMeleeHitValid(origin.position, target.position, range, solidMask);
        }

        // A scene driver supplies combat-only time; pause never advances attack timers.
        public void Tick(float deltaTime, double activeTime)
        {
            if (!configured || !combatEnabled || IsDead) return;
            deltaTime = Mathf.Max(0, deltaTime);
            if (IsWindingUp)
            {
                windupRemaining -= deltaTime;
                if (windupRemaining > 0) return;
                IsWindingUp = false;
                cooldownRemaining = kind == EnemyKind.Melee ? 0.85f : 2.7f;
                if (!CanReachTarget()) return;
                if (kind == EnemyKind.Melee)
                    target.GetComponentInParent<IDamageable>()?.ApplyDamage(attackDamage, enemyId, activeTime);
                else
                    ProjectileRequested?.Invoke(origin.position, (target.position - origin.position).normalized, enemyId, attackDamage);
                return;
            }
            cooldownRemaining = Mathf.Max(0, cooldownRemaining - deltaTime);
            if (cooldownRemaining > 0 || !CanReachTarget()) return;
            IsWindingUp = true;
            windupRemaining = kind == EnemyKind.Melee ? 0.65f : 0.8f;
        }

        public void SetCombatEnabled(bool enabled) => combatEnabled = enabled;
        public bool ApplyDamage(int amount, int sourceId, double activeTime)
        {
            if (!configured || !combatEnabled || IsDead || amount <= 0) return false;
            health = Math.Max(0, health - amount);
            if (health == 0)
            {
                IsWindingUp = false;
                Died?.Invoke(enemyId, sourceId);
            }
            return true;
        }
    }
}
