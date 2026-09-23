using UnityEngine;
using UnityEngine.AI;
namespace VRFPS
{
    public sealed class ZombieMotor : MonoBehaviour
    {
        NavMeshAgent agent;
        ZombieController enemy;
        Transform target;
        CharacterController targetBody;
        EnemyKind kind;
        Vector3[] recoveryPoints;
        Vector3 progressPosition;
        float stalledTime, pathTimer;
        bool configured, combatEnabled;
        NavMeshPath recoveryPath;
        public int RecoveryCount { get; private set; }
        Vector3 NavigationPosition => targetBody != null
            ? targetBody.transform.TransformPoint(targetBody.center - Vector3.up * (targetBody.height * 0.5f))
            : target.position;

        public void Configure(Transform target, EnemyKind kind, Vector3[] recoveryPoints)
        {
            agent = GetComponent<NavMeshAgent>(); enemy = GetComponent<ZombieController>();
            if (recoveryPath == null) recoveryPath = new NavMeshPath();
            if (agent == null || enemy == null || target == null)
                throw new System.InvalidOperationException("Motor requires agent, enemy and target.");
            this.target = target; this.kind = kind;
            targetBody = target.GetComponentInParent<CharacterController>();
            this.recoveryPoints = recoveryPoints ?? new Vector3[0];
            agent.speed = kind == EnemyKind.Melee ? 1.1f : 0.85f;
            agent.angularSpeed = 240; agent.acceleration = 6; agent.stoppingDistance = 0.65f;
            configured = combatEnabled = true; stalledTime = pathTimer = 0;
            RecoveryCount = 0; progressPosition = transform.position;
        }

        void Stop() { if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true; }
        public void SetCombatEnabled(bool enabled)
        { combatEnabled = enabled; if (enemy != null) enemy.SetCombatEnabled(enabled); if (!enabled) Stop(); }

        public void Tick(float deltaTime)
        {
            if (!configured) return;
            if (!combatEnabled || enemy.IsDead || target == null) { Stop(); return; }
            if (enemy.HoldsPosition) { Stop(); stalledTime = 0; progressPosition = transform.position; return; }
            deltaTime = Mathf.Max(0, deltaTime);
            pathTimer -= deltaTime;
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                if (pathTimer <= 0)
                {
                    var destination = NavigationPosition;
                    var away = Vector3.ProjectOnPlane(transform.position - destination, Vector3.up);
                    if (kind == EnemyKind.Ranged && away.magnitude < 2.5f)
                        destination += (away.sqrMagnitude > 0.001f ? away.normalized : transform.forward) * 3.5f;
                    if (NavMesh.SamplePosition(destination, out var hit, 2, agent.areaMask)) agent.SetDestination(hit.position);
                    pathTimer = 0.25f;
                }
            }
            stalledTime += deltaTime;
            if (Vector3.Distance(progressPosition, transform.position) >= 0.15f)
            { stalledTime = 0; progressPosition = transform.position; }
            if (stalledTime < 5) return;
            stalledTime = 0;
            if (!agent.enabled || !NavMesh.SamplePosition(NavigationPosition, out var goal, 2, agent.areaMask)) return;
            foreach (var candidate in recoveryPoints)
            {
                if (Vector3.Distance(candidate, NavigationPosition) < 2 ||
                    !NavMesh.SamplePosition(candidate, out var hit, 0.5f, agent.areaMask) ||
                    !NavMesh.CalculatePath(hit.position, goal.position, agent.areaMask, recoveryPath) ||
                    recoveryPath.status != NavMeshPathStatus.PathComplete) continue;
                if (!agent.Warp(hit.position)) continue;
                RecoveryCount++; pathTimer = 0; progressPosition = transform.position; break;
            }
        }
    }
}
