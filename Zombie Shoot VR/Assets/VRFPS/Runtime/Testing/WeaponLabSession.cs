using UnityEngine;

namespace VRFPS
{
    // Only the hardware lab starts directly in combat; the game will use RunController.
    public sealed class WeaponLabSession : MonoBehaviour
    {
        [SerializeField] K2Controller gun;
        [SerializeField] TextMesh status;
        int displayedRounds = -1;
        void Start() { if (gun != null) gun.SetCombatEnabled(true); }
        void Update()
        {
            if (status != null && gun != null && displayedRounds != gun.Magazine.Rounds)
            {
                displayedRounds = gun.Magazine.Rounds;
                status.text = "WEAPON LAB - PROTOTYPE\nLeft grip at yellow foregrip + right trigger\nROUNDS: " + gun.Magazine.Rounds + " / 30\nReload not connected yet; restart lab to refill";
            }
        }
    }
}
