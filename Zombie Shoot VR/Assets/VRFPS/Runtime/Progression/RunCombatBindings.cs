using UnityEngine;
using VRFPS.Player;
namespace VRFPS
{
    public sealed class RunCombatBindings : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] K2Controller gun;
        [SerializeField] PlayerRigController movement;
        [SerializeField] AmmoPouch pouch;
        WeaponContextRouter router;
        public WeaponContextRouter Router=>router;

        public void Configure(RunController run, K2Controller gun, PlayerRigController movement, AmmoPouch pouch)
        {
            this.run = run; this.gun = gun; this.movement = movement; this.pouch = pouch;
            if (isActiveAndEnabled) Subscribe();
        }

        void OnEnable() => Subscribe();
        void Subscribe()
        {
            if (run == null) return;
            router=GetComponent<WeaponContextRouter>()??gameObject.AddComponent<WeaponContextRouter>();
            router.enabled=true;router.Configure(run,gun,movement,pouch);
        }
        void OnDisable()
        {
            if(router!=null)router.enabled=false;
        }
    }
}
