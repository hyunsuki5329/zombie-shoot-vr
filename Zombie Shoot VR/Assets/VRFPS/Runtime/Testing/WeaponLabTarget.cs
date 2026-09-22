using UnityEngine;

namespace VRFPS
{
    // A visible hit counter for hardware acceptance, not a gameplay enemy.
    public sealed class WeaponLabTarget : MonoBehaviour, IDamageable
    {
        [SerializeField] TextMesh label;
        int hits;
        public bool ApplyDamage(int amount, int sourceId, double activeTime)
        {
            if (amount <= 0) return false;
            hits++;
            if (label != null) label.text = "HITS: " + hits;
            return true;
        }
    }
}
