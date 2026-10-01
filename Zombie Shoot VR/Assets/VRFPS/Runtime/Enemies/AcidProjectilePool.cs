using UnityEngine;
using System.Collections.Generic;
namespace VRFPS
{
    public sealed class AcidProjectilePool : MonoBehaviour
    {
        readonly List<AcidProjectile> items = new List<AcidProjectile>(12);
        AcidProjectile template;
        PoolRegistry registry;
        LayerMask solidMask, playerMask;
        public PoolCounts Counts => registry!=null ? registry.GetCounts("projectiles") : default;
        public int CreatedCount => Counts.Created;
        public int ActiveCount { get { var count = 0; foreach (var item in items) if (item!=null&&item.Flying) count++; return count; } }
        public void Configure(AcidProjectile template, LayerMask solidMask, LayerMask playerMask)
        {
            if(template==null)throw new System.ArgumentNullException(nameof(template));
            if(registry!=null)throw new System.InvalidOperationException("Projectile pool is already configured.");
            registry=GetComponent<PoolRegistry>();if(registry==null)registry=gameObject.AddComponent<PoolRegistry>();
            registry.Register("projectiles",template.gameObject,12,instance=>instance.GetComponent<AcidProjectile>().ResetForPool());
            this.template = template; this.solidMask = solidMask; this.playerMask = playerMask;
        }
        public bool TryLaunch(Vector3 origin, Vector3 direction, int sourceId, int damage)
        {
            if (template == null) return false;
            for(var i=items.Count-1;i>=0;i--)if(items[i]==null)items.RemoveAt(i);
            foreach(var item in items)if(item!=null&&!item.Flying)registry.Return("projectiles",item.gameObject);
            if(!registry.TryRent("projectiles",out var instance))return false;
            var available=instance.GetComponent<AcidProjectile>();
            if(!items.Contains(available))items.Add(available);
            try{available.Launch(origin, direction, sourceId, damage, solidMask, playerMask);}
            catch{registry.Return("projectiles",instance);throw;}
            return true;
        }
        public void Tick(float deltaTime, double activeTime)
        {
            foreach (var item in items)
            {
                if(item==null)continue;
                if(item.Flying)item.Tick(deltaTime,activeTime);
                if(!item.Flying)registry.Return("projectiles",item.gameObject);
            }
        }
        public void RetireAll() { foreach (var item in items)if(item!=null)registry.Return("projectiles",item.gameObject); }
    }
}
