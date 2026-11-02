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
                status.text = "WEAPON LAB - PROTOTYPE\nLeft foregrip + right trigger\nROUNDS: " + gun.Magazine.Rounds +
                    " / 30\nReload: release foregrip, grab magazine, drop\nGrab fresh at LEFT WAIST, align and insert\nRelease grip, then grab foregrip again";
            }
        }
    }
}
