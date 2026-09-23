using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace VRFPS
{
    // A manually started test scenario, not the game's wave/health controller.
    public sealed class EnemyLabSession : MonoBehaviour, IDamageable
    {
        [SerializeField] EnemyActor meleePrefab, rangedPrefab;
        [SerializeField] AcidProjectile projectilePrefab;
        [SerializeField] Transform target;
        [SerializeField] TextMesh status;
        readonly EnemyActor[] actors = new EnemyActor[2];
        readonly Vector3[] spawns = { new Vector3(-3, 0, 4), new Vector3(3, 0, 4) };
        GameObject actorRoot;
        AcidProjectilePool pool;
        InputAction toggle, reset;
        bool running;
        double activeTime;
        int hits, kills;
        void Awake()
        {
            toggle = new InputAction("EnemyLabToggle", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
            toggle.AddBinding("<Keyboard>/f6");
            reset = new InputAction("EnemyLabReset", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
            reset.AddBinding("<Keyboard>/f7");
            actorRoot = new GameObject("Enemy Lab Runtime");
            pool = actorRoot.AddComponent<AcidProjectilePool>();
            pool.Configure(projectilePrefab, LayerMask.GetMask("WorldSolid"), LayerMask.GetMask("PlayerBody"));
        }
        void OnEnable() { toggle?.Enable(); reset?.Enable(); }
        void Start() { ShowStatus(); }
        void Update()
        {
            if (reset.WasPressedThisFrame()) Restart();
            else if (toggle.WasPressedThisFrame())
            {
                if (actors[0] == null) Restart();
                else
                {
                    running = !running;
                    foreach (var actor in actors) if (actor != null) actor.SetCombatEnabled(running);
                }
            }
            if (!running) { ShowStatus(); return; }
            activeTime += Time.deltaTime;
            foreach (var actor in actors) if (actor != null) actor.Tick(Time.deltaTime, activeTime);
            pool.Tick(Time.deltaTime, activeTime);
            ShowStatus();
        }
        public void Restart()
        {
            if (meleePrefab == null || rangedPrefab == null || target == null) return;
            pool.RetireAll(); hits = kills = 0; activeTime = 0;
            for (var i = 0; i < actors.Length; i++)
            {
                if (actors[i] == null) actors[i] = Instantiate(i == 0 ? meleePrefab : rangedPrefab, actorRoot.transform);
                else actors[i].Controller.Died -= CountDeath;
                var agent = actors[i].GetComponent<NavMeshAgent>(); agent.enabled = false;
                actors[i].transform.position = spawns[i];
                actors[i].Initialize(i + 1, target, pool, spawns, LayerMask.GetMask("WorldSolid"));
                actors[i].Controller.Died += CountDeath;
            }
            running = true;
        }
        void CountDeath(int id, int source) { kills++; }
        public bool ApplyDamage(int amount, int sourceId, double time)
        { if (!running || amount <= 0) return false; hits++; return true; }
        void ShowStatus()
        {
            if (status != null) status.text = "ENEMY LAB - " + (running ? "ON" : "OFF") +
                "\nX / F6: start or freeze enemies\nY / F7: reset enemies\nGreen: melee | Purple: ranged\nYellow: attack warning\nHITS TAKEN: " + hits + "   KILLS: " + kills;
        }
        void OnDisable()
        {
            toggle?.Disable(); reset?.Disable(); running = false;
            foreach (var actor in actors) if (actor != null) actor.SetCombatEnabled(false);
        }
        void OnDestroy()
        {
            toggle?.Dispose(); reset?.Dispose();
            if (actorRoot != null) Destroy(actorRoot);
        }
    }
}
