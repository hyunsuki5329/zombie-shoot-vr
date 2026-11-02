using UnityEngine;
using UnityEngine.AI;
namespace VRFPS
{
    public sealed class EnemyActor : MonoBehaviour
    {
        [SerializeField] EnemyKind kind;
        [SerializeField] EnemyVariant variant;
        [SerializeField] EnemyActor crawlerPrefab,bossPrefab;
        public EnemyVariant Variant=>variant;
        public EnemyActor CrawlerPrefab=>crawlerPrefab;
        public EnemyActor BossPrefab=>bossPrefab;
        [SerializeField] Transform attackOrigin;
        ZombieController enemy;
        ZombieMotor motor;
        NavMeshAgent agent;
        AcidProjectilePool pool;
        Collider[] hitboxes;
        Animator animator;
        float corpseAge;
        const float HitDuration = .4f;
        int hitLayer = -1, presentedHealth;
        float hitRemaining;
        bool configured, combatEnabled;
        EnemyFeedback feedback;
        CrawlerDeathMotion crawlerDeath;
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
            animator = GetComponentInChildren<Animator>(true);
            crawlerDeath = GetComponent<CrawlerDeathMotion>(); crawlerDeath?.ResetMotion();
            foreach (var hitbox in hitboxes) hitbox.enabled = true;
            agent.enabled = true;
            if (animator != null) { animator.applyRootMotion = false; animator.Rebind(); animator.speed = 1; }
            enemy.Configure(id, kind, target, origin, solidMask, wave,variant);
            hitLayer = animator != null && animator.runtimeAnimatorController != null ? animator.GetLayerIndex("Hit Reaction") : -1;
            presentedHealth = enemy.Health; hitRemaining = 0;
            if (hitLayer > 0) animator.SetLayerWeight(hitLayer, 0);
            feedback=GetComponent<EnemyFeedback>();feedback?.Configure(enemy,target);
            motor.Configure(target, kind, recovery,variant);
            enemy.Died += HandleDeath; enemy.ProjectileRequested += Launch;
            corpseAge = 0; configured = combatEnabled = true;
            UpdatePresentation();
        }
        void Launch(Vector3 position, Vector3 direction, int source, int damage)
        { if (pool != null) pool.TryLaunch(position, direction, source, damage); }
        void HandleDeath(int id, int source)
        {
            crawlerDeath?.Begin();
            foreach (var hitbox in hitboxes) hitbox.enabled = false;
            motor.Tick(0);
            UpdatePresentation();
        }
        public void SetCombatEnabled(bool enabled)
        {
            combatEnabled = enabled;
            feedback?.SetCombatEnabled(enabled);
            if (motor != null) motor.SetCombatEnabled(enabled);
            if (animator != null) animator.speed = enabled ? 1 : 0;
        }
        public void Tick(float deltaTime, double activeTime)
        {
            if (!configured || !combatEnabled || !gameObject.activeSelf) return;
            hitRemaining = Mathf.Max(0, hitRemaining - Mathf.Max(0, deltaTime));
            enemy.Tick(deltaTime, activeTime); motor.Tick(deltaTime);
            feedback?.Tick(deltaTime);
            UpdatePresentation();
            if (enemy.IsDead)
            {
                crawlerDeath?.Tick(deltaTime);
                corpseAge += Mathf.Max(0, deltaTime);
                if (corpseAge >= 5) gameObject.SetActive(false);
            }
        }
        void UpdatePresentation()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                if (hitLayer > 0)
                {
                    // Only presentation reacts: attack timers and navigation remain authoritative.
                    // Do not restart an active flinch on every automatic-rifle bullet.
                    if (enemy.IsDead) hitRemaining = 0;
                    else if (enemy.Health < presentedHealth && hitRemaining <= 0)
                    {
                        hitRemaining = HitDuration;
                        animator.Play("Hit", hitLayer, 0);
                    }
                    animator.SetLayerWeight(hitLayer, .45f * hitRemaining / HitDuration);
                }
                animator.SetBool("Attack", enemy.IsWindingUp); animator.SetBool("Dead", enemy.IsDead);
                animator.SetFloat("Speed", agent.enabled && agent.isOnNavMesh ? agent.velocity.magnitude : 0);
            }
            presentedHealth = enemy.Health;
        }
        void OnDestroy()
        { if (enemy != null) { enemy.Died -= HandleDeath; enemy.ProjectileRequested -= Launch; } }
        internal void ResetForPool()
        {
            configured=combatEnabled=false;corpseAge=0;pool=null;
            hitRemaining=0;presentedHealth=0;
            crawlerDeath?.ResetMotion();
            feedback?.Configure(null,null);
            motor?.ResetForPool();enemy?.ResetForPool();
            if(hitboxes!=null)foreach(var hitbox in hitboxes)if(hitbox!=null)hitbox.enabled=false;
            if(animator!=null)
            {
                if(animator.runtimeAnimatorController!=null)animator.Rebind();
                animator.speed=0;
                if(hitLayer>0)animator.SetLayerWeight(hitLayer,0);
            }
        }
    }
}
