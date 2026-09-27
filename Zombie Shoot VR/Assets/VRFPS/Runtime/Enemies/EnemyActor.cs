using UnityEngine;
using UnityEngine.AI;
namespace VRFPS
{
    public sealed class EnemyActor : MonoBehaviour
    {
        [SerializeField] EnemyKind kind;
        [SerializeField] Transform attackOrigin;
        ZombieController enemy;
        ZombieMotor motor;
        NavMeshAgent agent;
        AcidProjectilePool pool;
        Collider[] hitboxes;
        Renderer[] renderers;
        Animator animator;
        MaterialPropertyBlock tint;
        float corpseAge;
        bool configured, combatEnabled;
        public ZombieController Controller => enemy;

        public void Initialize(int id, Transform target, AcidProjectilePool pool, Vector3[] recovery, LayerMask solidMask, int wave = 1)
            => Configure(id, kind, target, attackOrigin, pool, recovery, solidMask, wave);

        public void Configure(int id, EnemyKind kind, Transform target, Transform origin, AcidProjectilePool pool, Vector3[] recovery, LayerMask solidMask, int wave = 1)
        {
            if (enemy != null) { enemy.Died -= HandleDeath; enemy.ProjectileRequested -= Launch; }
            enemy = GetComponent<ZombieController>(); motor = GetComponent<ZombieMotor>(); agent = GetComponent<NavMeshAgent>();
            if (enemy == null || motor == null || agent == null) throw new System.InvalidOperationException("Enemy actor components are missing.");
            this.kind = kind; this.pool = pool;
            gameObject.SetActive(true);
            hitboxes = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            animator = GetComponentInChildren<Animator>(true);
            if (tint == null) tint = new MaterialPropertyBlock();
            foreach (var hitbox in hitboxes) hitbox.enabled = true;
            agent.enabled = true;
            if (animator != null) { animator.applyRootMotion = false; animator.Rebind(); animator.speed = 1; }
            enemy.Configure(id, kind, target, origin, solidMask, wave);
            motor.Configure(target, kind, recovery);
            enemy.Died += HandleDeath; enemy.ProjectileRequested += Launch;
            corpseAge = 0; configured = combatEnabled = true;
            UpdatePresentation();
        }
        void Launch(Vector3 position, Vector3 direction, int source, int damage)
        { if (pool != null) pool.TryLaunch(position, direction, source, damage); }
        void HandleDeath(int id, int source)
        {
            foreach (var hitbox in hitboxes) hitbox.enabled = false;
            motor.Tick(0);
            UpdatePresentation();
        }
        public void SetCombatEnabled(bool enabled)
        {
            combatEnabled = enabled;
            if (motor != null) motor.SetCombatEnabled(enabled);
            if (animator != null) animator.speed = enabled ? 1 : 0;
        }
        public void Tick(float deltaTime, double activeTime)
        {
            if (!configured || !combatEnabled || !gameObject.activeSelf) return;
            enemy.Tick(deltaTime, activeTime); motor.Tick(deltaTime);
            UpdatePresentation();
            if (enemy.IsDead)
            {
                corpseAge += Mathf.Max(0, deltaTime);
                if (corpseAge >= 5) gameObject.SetActive(false);
            }
        }
        void UpdatePresentation()
        {
            var color = enemy.IsDead ? Color.gray : enemy.IsWindingUp ? Color.yellow :
                kind == EnemyKind.Melee ? new Color(0.35f, 0.65f, 0.3f) : new Color(0.7f, 0.25f, 0.65f);
            tint.SetColor("_BaseColor", color);
            foreach (var renderer in renderers) renderer.SetPropertyBlock(tint);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("Attack", enemy.IsWindingUp); animator.SetBool("Dead", enemy.IsDead);
                animator.SetFloat("Speed", agent.enabled && agent.isOnNavMesh ? agent.velocity.magnitude : 0);
            }
        }
        void OnDestroy()
        { if (enemy != null) { enemy.Died -= HandleDeath; enemy.ProjectileRequested -= Launch; } }
    }
}
