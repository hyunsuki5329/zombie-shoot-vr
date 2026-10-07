using UnityEngine;
using System.Collections.Generic;

namespace VRFPS
{
    public sealed class AmmoPouch : MonoBehaviour
    {
        [SerializeField] MagazineToken prefab;
        [SerializeField] Transform leftHand;
        [SerializeField] Transform head;
        readonly List<MagazineToken> tokens = new List<MagazineToken>();
        readonly Dictionary<MagazineToken, float> ages = new Dictionary<MagazineToken, float>();
        bool simulationEnabled = true;
        PoolRegistry registry;
        public int TokenCount => tokens.Count;
        public PoolCounts Counts => registry!=null ? registry.GetCounts("magazines") : default;
        public int LooseCount { get { var count=0;foreach(var token in tokens)if(IsLoose(token))count++;return count; } }
        public int PeakLooseCount { get; private set; }

        void EnsurePool(MagazineToken fallback=null)
        {
            if(registry!=null)return;
            var source=prefab!=null?prefab:fallback;
            if(source==null)throw new System.InvalidOperationException("Magazine prefab is missing.");
            registry=GetComponent<PoolRegistry>();if(registry==null)registry=gameObject.AddComponent<PoolRegistry>();
            registry.Register("magazines",source.gameObject,10,instance=>instance.GetComponent<MagazineToken>().ResetForRun());
        }

        void RemoveDestroyed()
        {
            for(var i=tokens.Count-1;i>=0;i--)
                if(tokens[i]==null){ages.Remove(tokens[i]);tokens.RemoveAt(i);}
        }

        MagazineToken Rent()
        {
            EnsurePool();RemoveDestroyed();
            if(!registry.TryRent("magazines",out var instance))return null;
            var token=instance.GetComponent<MagazineToken>();
            if(!tokens.Contains(token))tokens.Add(token);
            ages[token]=0;token.SetSimulationEnabled(simulationEnabled);
            return token;
        }

        internal MagazineToken ResetForRun()
        {
            EnsurePool();RemoveDestroyed();
            foreach (var token in tokens)
            {
                registry.Return("magazines",token.gameObject);ages[token]=0;
            }
            var mounted=Rent();
            if(mounted==null)throw new System.InvalidOperationException("No magazine slot after reset.");
            mounted.ResetForRun();mounted.gameObject.SetActive(true);return mounted;
        }

        public void SetSimulationEnabled(bool enabled)
        {
            simulationEnabled = enabled;
            foreach (var token in tokens) if (token != null) token.SetSimulationEnabled(enabled);
        }

        void Awake()
        {
            var magazineLayer = LayerMask.NameToLayer("Magazine");
            var playerLayer = LayerMask.NameToLayer("PlayerBody");
            if (magazineLayer >= 0 && playerLayer >= 0) Physics.IgnoreLayerCollision(magazineLayer, playerLayer);
        }

        public void Configure(MagazineToken prefab, Transform leftHand)
        { this.prefab = prefab; this.leftHand = leftHand; }

        public void Register(MagazineToken token)
        {
            if (token == null || tokens.Contains(token)) return;
            EnsurePool(token);RemoveDestroyed();
            if(!registry.Adopt("magazines",token.gameObject))throw new System.InvalidOperationException("Magazine pool is full or token is already owned.");
            tokens.Add(token); ages[token] = 0;
            token.SetSimulationEnabled(simulationEnabled);
        }

        public MagazineToken TrySupply(Transform hand)
        {
            if (!simulationEnabled || hand == null || hand != leftHand || prefab == null || Vector3.Distance(hand.position, transform.position) > 0.18f) return null;
            foreach (var token in tokens) if (token != null && token.HeldBy == hand) return null;
            TickLifetime(0);
            var available=Rent();
            if(available==null)
            {
                var oldest=OldestLoose();
                if(oldest!=null){registry.Return("magazines",oldest.gameObject);available=Rent();}
            }
            if (available == null) return null;
            available.ResetFresh();
            available.gameObject.SetActive(true);
            ages[available] = 0;
            if(available.TryGrab(hand))return available;
            registry.Return("magazines",available.gameObject);return null;
        }

        public MagazineToken FindNearbyLoose(Transform hand)
        {
            if (hand == null || hand != leftHand) return null;
            MagazineToken closest = null;
            var distance = 0.12f;
            foreach (var token in tokens)
            {
                if (!IsLoose(token)) continue;
                var candidate = Vector3.Distance(hand.position, token.transform.position);
                if (candidate <= distance) { closest = token; distance = candidate; }
            }
            return closest;
        }

        static bool IsLoose(MagazineToken token) => token != null && token.gameObject.activeSelf &&
            token.HeldBy == null && token.State != MagazineTokenState.Inserted;

        MagazineToken OldestLoose()
        {
            MagazineToken oldest = null;
            foreach (var token in tokens)
                if (IsLoose(token) && (oldest == null || ages[token] > ages[oldest])) oldest = token;
            return oldest;
        }

        public void TickLifetime(float deltaTime)
        {
            if (!simulationEnabled) return;
            RemoveDestroyed();
            var looseCount = 0;
            foreach (var token in tokens)
            {
                if (token == null) continue;
                if (!IsLoose(token)) { ages[token] = 0; continue; }
                ages[token] += Mathf.Max(0, deltaTime);
                if (ages[token] >= 10) registry.Return("magazines",token.gameObject);
                else looseCount++;
            }
            while (looseCount > 8)
            {
                var oldest = OldestLoose();
                if (oldest == null) break;
                registry.Return("magazines",oldest.gameObject);looseCount--;
            }
            PeakLooseCount=Mathf.Max(PeakLooseCount,looseCount);
        }

        void Update()
        {
            if (head != null)
            {
                var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                if (forward.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward), 5 * Time.deltaTime);
                transform.position = head.position + transform.rotation * new Vector3(-0.25f, -0.65f, -0.05f);
            }
            TickLifetime(Time.deltaTime);
        }

    }
}
