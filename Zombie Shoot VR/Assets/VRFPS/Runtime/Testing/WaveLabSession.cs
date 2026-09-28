using UnityEngine;
using UnityEngine.InputSystem;
using VRFPS.Player;
namespace VRFPS
{
    public sealed class WaveLabSession : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] SpawnDirector director;
        [SerializeField] RunCombatBindings bindings;
        [SerializeField] MapDefinition map;
        [SerializeField] Transform target;
        [SerializeField] K2Controller gun;
        [SerializeField] LeftHandInteractor hand;
        [SerializeField] EnemyActor melee, ranged;
        [SerializeField] AcidProjectile projectile;
        [SerializeField] TextMesh status;
        InputAction pause, retry;
        bool ready;

        void Awake()
        {
            pause = new InputAction("WaveLabPause", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
            pause.AddBinding("<Keyboard>/f6");
            retry = new InputAction("WaveLabRetry", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
            retry.AddBinding("<Keyboard>/f7");
        }
        void OnEnable() { pause?.Enable(); retry?.Enable(); }
        void Start()
        {
            if (run == null || director == null || bindings == null || map == null || gun == null || hand == null)
                throw new System.InvalidOperationException("Wave lab scene references are incomplete.");
            director.Configure(run, target, melee, ranged, projectile, new[] {
                new Vector3(-3, 0, 4), new Vector3(3, 0, 4), new Vector3(-3, 0, -3), new Vector3(3, 0, -3)
            }, gun.GetInstanceID());
            bindings.Configure(run, gun, GetComponent<PlayerRigController>(), GetComponentInChildren<AmmoPouch>());
            gun.GetComponent<K2HandInput>().UseRunClock(run);
            run.RunStarted += ResetWeapon;
            ready = true; Restart();
            CombatDiagnostics.AttachIfRequested(run,director,GetComponentInChildren<AmmoPouch>(true),gun.GetComponent<K2HandInput>(),"wave-lab");
        }
        void ResetWeapon() => hand.Restart();
        public void Restart() { if (ready) run.StartRun(map); }
        void Update()
        {
            if (!ready) return;
            if (retry.WasPressedThisFrame()) Restart();
            else if (pause.WasPressedThisFrame())
            {
                var menu=FindFirstObjectByType<MenuController>();
                if(menu!=null && (menu.Screen==MenuScreen.Settings || menu.Screen==MenuScreen.Instructions))menu.CloseAuxiliary();
                else run.SetPaused(run.Phase != RunPhase.Paused);
            }
            if (status != null) status.text = "WAVE LAB - " + run.Phase +
                "\nHP " + run.Health + " / 100   WAVE " + run.ReachedWave +
                "\nKILLS " + run.Kills + "   ROUNDS " + gun.Magazine.Rounds + " / 30" +
                "\nALIVE " + run.Alive + "   PENDING " + run.Pending +
                "\nPREP " + Mathf.CeilToInt((float)run.PreparationRemaining) + " s" +
                "\nX / F6: pause or resume | Y / F7: retry" +
                "\nLeft foregrip + right trigger to fire" +
                "\nReload: eject, drop, left waist, insert, regrip";
        }
        void OnDisable()
        {
            pause?.Disable(); retry?.Disable();
            if (ready && run != null) run.SetPaused(true);
        }
        void OnDestroy()
        {
            if (run != null) run.RunStarted -= ResetWeapon;
            pause?.Dispose(); retry?.Dispose();
        }
    }
}
