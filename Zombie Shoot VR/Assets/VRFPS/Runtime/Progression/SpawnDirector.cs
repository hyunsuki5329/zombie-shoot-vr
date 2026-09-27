using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace VRFPS
{
    public sealed class SpawnDirector : MonoBehaviour
    {
        sealed class Entry
        {
            public EnemyActor Actor;
            public EnemyKind Kind;
            public int Id;
            public bool Registered;
            public long DeathOrder;
        }
        readonly List<Entry> actors = new List<Entry>();
        RunController run;
        Transform target;
        CharacterController playerBody;
        EnemyActor melee, ranged;
        AcidProjectilePool projectiles;
        Vector3[] positions;
        int playerSourceId, solidMask, lastId, pointCursor, wave, meleeSpawned, rangedSpawned;
        long deathOrder;
        float warningRemaining, spawnRemaining, retryRemaining;
        EnemyKind warningKind;
        GameObject warning;
        NavMeshPath path;
        bool configured;
        public bool WarningActive { get; private set; }
        public Vector3 WarningPosition { get; private set; }
        public int CreatedCount => actors.Count;
        Vector3 PlayerFeet => playerBody != null
            ? playerBody.transform.TransformPoint(playerBody.center - Vector3.up * (playerBody.height * 0.5f))
            : target.position;

        public void Configure(RunController run, Transform target, EnemyActor melee, EnemyActor ranged,
            AcidProjectile projectile, Vector3[] positions, int playerSourceId)
        {
            if (configured) throw new InvalidOperationException("Spawn director is already configured.");
            if (run == null || target == null || melee == null || ranged == null || projectile == null ||
                positions == null || positions.Length == 0 || melee.GetComponent<NavMeshAgent>() == null ||
                ranged.GetComponent<NavMeshAgent>() == null)
                throw new ArgumentException("Spawn director requires run, target, prefabs and spawn positions.");
            this.run = run; this.target = target; this.melee = melee; this.ranged = ranged;
            this.positions = (Vector3[])positions.Clone(); this.playerSourceId = playerSourceId;
            playerBody = target.GetComponentInParent<CharacterController>();
            solidMask = LayerMask.GetMask("WorldSolid");
            path = new NavMeshPath();
            var poolObject = new GameObject("Run Projectiles"); poolObject.transform.SetParent(transform, false);
            projectiles = poolObject.AddComponent<AcidProjectilePool>();
            projectiles.Configure(projectile, solidMask, LayerMask.GetMask("PlayerBody"));
            warning = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            warning.name = "Spawn Warning"; warning.transform.SetParent(transform, false);
            warning.transform.localScale = new Vector3(0.9f, 0.015f, 0.9f);
            var collider = warning.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            var renderer = warning.GetComponent<Renderer>();
            renderer.sharedMaterial = melee.GetComponentInChildren<Renderer>().sharedMaterial;
            var tint = new MaterialPropertyBlock(); tint.SetColor("_BaseColor", Color.yellow);
            renderer.SetPropertyBlock(tint); warning.SetActive(false);
            run.RunStarted += ResetRun; run.PhaseChanged += ApplyPhase;
            configured = true;
            ApplyPhase(run.Phase);
        }

        void Update() => Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!configured || run.Phase == RunPhase.Paused || run.Phase == RunPhase.Dead) return;
            foreach (var entry in actors) entry.Actor.Tick(deltaTime, run.ActiveTime);
            if (run.Phase != RunPhase.Active) return;
            projectiles.Tick(deltaTime, run.ActiveTime);
            if (run.Phase != RunPhase.Active) return;
            spawnRemaining = Mathf.Max(0, spawnRemaining - deltaTime);
            retryRemaining = Mathf.Max(0, retryRemaining - deltaTime);
            if (WarningActive) warningRemaining = Mathf.Max(0, warningRemaining - deltaTime);
            var kind = WarningActive ? warningKind : Remaining(EnemyKind.Ranged) > 0 && RangedAlive() < run.Map.maxRangedAlive
                ? EnemyKind.Ranged : EnemyKind.Melee;
            TrySpawn(kind, out _);
        }

        int Remaining(EnemyKind kind) => kind == EnemyKind.Ranged
            ? GameBalance.RangedQuotaForWave(wave) - rangedSpawned
            : GameBalance.QuotaForWave(wave) - GameBalance.RangedQuotaForWave(wave) - meleeSpawned;
        int RangedAlive()
        {
            var count = 0;
            foreach (var entry in actors) if (entry.Registered && entry.Kind == EnemyKind.Ranged) count++;
            return count;
        }

        public bool TrySpawn(EnemyKind kind, out int enemyId)
        {
            enemyId = 0;
            if (kind != EnemyKind.Melee && kind != EnemyKind.Ranged) throw new ArgumentOutOfRangeException(nameof(kind));
            if (!configured || run.Phase != RunPhase.Active || run.Pending == 0 || Remaining(kind) <= 0 ||
                run.Alive >= run.Map.maxAlive || kind == EnemyKind.Ranged && RangedAlive() >= run.Map.maxRangedAlive) return false;
            if (WarningActive)
            {
                if (kind != warningKind || warningRemaining > 0 || spawnRemaining > 0) return false;
                if (!ValidPosition(WarningPosition, kind, out var position))
                { CancelWarning(); retryRemaining = 2; return false; }
                var entry = Acquire(kind, position);
                if (entry == null) { CancelWarning(); retryRemaining = 2; return false; }
                if (entry.Actor.Controller != null) entry.Actor.Controller.Died -= OnDeath;
                var agent = entry.Actor.GetComponent<NavMeshAgent>();
                agent.enabled = false; entry.Actor.transform.position = position;
                var id = checked(++lastId); // Never reused across pooled lives or retries.
                entry.Actor.Initialize(id, target, projectiles, positions, solidMask, wave);
                if (!agent.isOnNavMesh || !agent.Warp(position) || !run.RegisterSpawn(id))
                { entry.Actor.gameObject.SetActive(false); CancelWarning(); retryRemaining = 2; return false; }
                entry.Id = id; entry.Registered = true;
                entry.Actor.Controller.Died += OnDeath;
                if (kind == EnemyKind.Melee) meleeSpawned++; else rangedSpawned++;
                enemyId = id; spawnRemaining = 2; CancelWarning();
                return true;
            }
            if (retryRemaining > 0) return false;
            for (var i = 0; i < positions.Length; i++)
            {
                var index = (pointCursor + i) % positions.Length;
                if (!ValidPosition(positions[index], kind, out var position)) continue;
                pointCursor = (index + 1) % positions.Length;
                warningKind = kind; warningRemaining = 1.5f; WarningPosition = position; WarningActive = true;
                warning.transform.position = position + Vector3.up * 0.03f;
                warning.SetActive(true);
                return false;
            }
            retryRemaining = 2;
            return false;
        }

        bool ValidPosition(Vector3 candidate, EnemyKind kind, out Vector3 position)
        {
            position = candidate;
            if (target == null) return false;
            var prefab = kind == EnemyKind.Melee ? melee : ranged;
            var agent = prefab.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(candidate, out var point, 0.25f, filter) ||
                Vector3.Distance(point.position, PlayerFeet) < run.Map.minSpawnDistance ||
                !NavMesh.SamplePosition(PlayerFeet, out var goal, 0.5f, filter) ||
                !NavMesh.CalculatePath(point.position, goal.position, filter, path) || path.status != NavMeshPathStatus.PathComplete)
                return false;
            position = point.position;
            // A new solid obstruction or another live body may appear after the bake/warning.
            if (Physics.CheckCapsule(position + Vector3.up * (agent.radius + 0.05f),
                position + Vector3.up * (agent.height - agent.radius), agent.radius,
                solidMask, QueryTriggerInteraction.Ignore)) return false;
            foreach (var entry in actors)
                if (entry.Registered && Vector3.Distance(entry.Actor.transform.position, position) < agent.radius * 2 + 0.1f) return false;
            return true;
        }

        Entry Acquire(EnemyKind kind, Vector3 position)
        {
            Entry oldest = null;
            var count = 0;
            foreach (var entry in actors)
            {
                if (entry.Kind != kind) continue;
                count++;
                if (!entry.Actor.gameObject.activeSelf) return entry;
                if (!entry.Registered && (oldest == null || entry.DeathOrder < oldest.DeathOrder)) oldest = entry;
            }
            var limit = (kind == EnemyKind.Melee ? run.Map.maxAlive : run.Map.maxRangedAlive) + 4;
            if (count >= limit || actors.Count >= 16)
            {
                if (oldest != null) oldest.Actor.gameObject.SetActive(false);
                return oldest;
            }
            var actor = Instantiate(kind == EnemyKind.Melee ? melee : ranged, position, Quaternion.identity, transform);
            var created = new Entry { Actor = actor, Kind = kind };
            actors.Add(created); return created;
        }

        void OnDeath(int id, int source)
        {
            foreach (var entry in actors)
            {
                if (entry.Id != id || !entry.Registered) continue;
                entry.Registered = false; entry.DeathOrder = ++deathOrder;
                run.ResolveEnemy(id, source == playerSourceId);
                break;
            }
            while (true)
            {
                var count = 0; Entry oldest = null;
                foreach (var entry in actors)
                    if (!entry.Registered && entry.Actor.gameObject.activeSelf && entry.Actor.Controller.IsDead)
                    { count++; if (oldest == null || entry.DeathOrder < oldest.DeathOrder) oldest = entry; }
                if (count <= 4) break;
                oldest.Actor.gameObject.SetActive(false);
            }
        }
        void CancelWarning() { WarningActive = false; if (warning != null) warning.SetActive(false); }
        void ResetRun()
        {
            CancelWarning(); projectiles.RetireAll();
            foreach (var entry in actors)
            {
                entry.Registered = false;
                entry.Actor.SetCombatEnabled(false); entry.Actor.gameObject.SetActive(false);
            }
            wave = meleeSpawned = rangedSpawned = pointCursor = 0;
            spawnRemaining = retryRemaining = 0;
        }
        void ApplyPhase(RunPhase phase)
        {
            if (phase == RunPhase.Active && wave != run.ReachedWave)
            {
                wave = run.ReachedWave; meleeSpawned = rangedSpawned = 0;
                spawnRemaining = retryRemaining = 0; CancelWarning();
            }
            foreach (var entry in actors)
                entry.Actor.SetCombatEnabled(phase == RunPhase.Active || phase == RunPhase.Preparing && entry.Actor.Controller.IsDead);
            if (phase == RunPhase.Preparing || phase == RunPhase.Dead) { CancelWarning(); projectiles.RetireAll(); }
        }
        void OnDestroy()
        {
            if (run != null) { run.RunStarted -= ResetRun; run.PhaseChanged -= ApplyPhase; }
            foreach (var entry in actors) if (entry.Actor != null && entry.Actor.Controller != null) entry.Actor.Controller.Died -= OnDeath;
        }
    }
}
