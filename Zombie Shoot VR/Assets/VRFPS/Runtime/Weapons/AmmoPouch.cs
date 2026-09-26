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
        public int TokenCount => tokens.Count;

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
            if (tokens.Count >= 10) throw new System.InvalidOperationException("Magazine pool is full.");
            tokens.Add(token); ages[token] = 0;
            token.SetSimulationEnabled(simulationEnabled);
        }

        public MagazineToken TrySupply(Transform hand)
        {
            if (!simulationEnabled || hand == null || hand != leftHand || prefab == null || Vector3.Distance(hand.position, transform.position) > 0.18f) return null;
            foreach (var token in tokens) if (token != null && token.HeldBy == hand) return null;
            TickLifetime(0);
            MagazineToken available = null;
            foreach (var token in tokens) if (token != null && !token.gameObject.activeSelf) { available = token; break; }
            if (available == null && tokens.Count < 10)
            {
                available = Instantiate(prefab);
                Register(available);
            }
            if (available == null) available = OldestLoose();
            if (available == null) return null;
            available.ResetFresh();
            available.gameObject.SetActive(true);
            ages[available] = 0;
            return available.TryGrab(hand) ? available : null;
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
            for (var i = tokens.Count - 1; i >= 0; i--)
                if (tokens[i] == null) { ages.Remove(tokens[i]); tokens.RemoveAt(i); }
            var looseCount = 0;
            foreach (var token in tokens)
            {
                if (token == null) continue;
                if (!IsLoose(token)) { ages[token] = 0; continue; }
                ages[token] += Mathf.Max(0, deltaTime);
                if (ages[token] >= 12) token.gameObject.SetActive(false);
                else looseCount++;
            }
            while (looseCount > 8)
            {
                var oldest = OldestLoose();
                if (oldest == null) break;
                oldest.gameObject.SetActive(false); looseCount--;
            }
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

        void OnDestroy()
        {
            foreach (var token in tokens)
                if (token != null)
                {
                    token.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(token.gameObject);
                    else DestroyImmediate(token.gameObject);
                }
        }
    }
}
