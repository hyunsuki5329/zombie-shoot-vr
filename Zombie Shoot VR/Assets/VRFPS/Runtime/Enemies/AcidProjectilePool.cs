using UnityEngine;
using System.Collections.Generic;
namespace VRFPS
{
    public sealed class AcidProjectilePool : MonoBehaviour
    {
        readonly List<AcidProjectile> items = new List<AcidProjectile>(12);
        AcidProjectile template;
        LayerMask solidMask, playerMask;
        public int CreatedCount => items.Count;
        public int ActiveCount { get { var count = 0; foreach (var item in items) if (item.Flying) count++; return count; } }
        public void Configure(AcidProjectile template, LayerMask solidMask, LayerMask playerMask)
        { this.template = template; this.solidMask = solidMask; this.playerMask = playerMask; }
        public bool TryLaunch(Vector3 origin, Vector3 direction, int sourceId, int damage)
        {
            if (template == null) return false;
            AcidProjectile available = null;
            foreach (var item in items) if (!item.Flying) { available = item; break; }
            if (available == null && items.Count < 12)
            { available = Instantiate(template, transform); items.Add(available); }
            if (available == null) return false;
            available.Launch(origin, direction, sourceId, damage, solidMask, playerMask);
            return true;
        }
        public void Tick(float deltaTime, double activeTime)
        { foreach (var item in items) if (item.Flying) item.Tick(deltaTime, activeTime); }
        public void RetireAll() { foreach (var item in items) item.Retire(); }
    }
}
