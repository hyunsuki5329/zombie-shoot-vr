using System;
using System.Collections.Generic;
using UnityEngine;
namespace VRFPS
{
    public readonly struct PoolCounts
    {
        public readonly int Current, Peak, Created, Capacity;
        public PoolCounts(int current,int peak,int created,int capacity)
        { Current=current;Peak=peak;Created=created;Capacity=capacity; }
    }
    public sealed class PoolRegistry : MonoBehaviour
    {
        sealed class Entry { public GameObject Instance; public bool Leased, Returning; }
        sealed class Pool
        {
            public GameObject Prefab;
            public int Capacity, Current, Peak;
            public Action<GameObject> Reset;
            public readonly List<Entry> Entries=new List<Entry>();
        }
        readonly Dictionary<string,Pool> pools=new Dictionary<string,Pool>(StringComparer.Ordinal);
        Transform storage;
        public void Register(string poolId,GameObject prefab,int capacity,Action<GameObject> reset)
        {
            if(string.IsNullOrEmpty(poolId)||prefab==null||capacity<=0||reset==null)
                throw new ArgumentException("Pool requires an ID, prefab, positive capacity and reset callback.");
            if(pools.ContainsKey(poolId))throw new InvalidOperationException("Pool is already registered: "+poolId);
            if(storage==null)
            {
                var root=new GameObject("Inactive Pool Storage");root.SetActive(false);
                storage=root.transform;storage.SetParent(transform,false);
            }
            pools.Add(poolId,new Pool{Prefab=prefab,Capacity=capacity,Reset=reset});
        }
        public bool TryRent(string poolId,out GameObject instance)
        {
            instance=null;
            if(poolId==null||!pools.TryGetValue(poolId,out var pool))return false;
            RemoveDestroyed(pool);
            Entry available=null;
            foreach(var entry in pool.Entries)if(!entry.Leased&&!entry.Returning){available=entry;break;}
            if(available==null)
            {
                if(pool.Entries.Count>=pool.Capacity)return false;
                var created=Instantiate(pool.Prefab,storage);created.SetActive(false);
                available=new Entry{Instance=created};pool.Entries.Add(available);
            }
            available.Leased=true;pool.Current++;pool.Peak=Math.Max(pool.Peak,pool.Current);
            instance=available.Instance;
            // Caller initializes state and then activates; no premature OnEnable on rent.
            instance.transform.SetParent(transform,false);
            return true;
        }
        public void Return(string poolId,GameObject instance)
        {
            if(poolId==null||instance==null||!pools.TryGetValue(poolId,out var pool))return;
            foreach(var entry in pool.Entries)
            {
                if(entry.Instance!=instance||!entry.Leased)continue;
                entry.Leased=false;entry.Returning=true;pool.Current--;
                try
                {
                    instance.SetActive(false);
                    // The owner resets type-specific velocity, timers, hit history and subscriptions.
                    pool.Reset(instance);
                    instance.transform.SetParent(storage,false);
                    instance.transform.localPosition=Vector3.zero;
                    instance.transform.localRotation=Quaternion.identity;
                    instance.transform.localScale=pool.Prefab.transform.localScale;
                    entry.Returning=false;
                }
                catch
                {
                    // Never rent an object whose reset failed, even before deferred destruction.
                    DestroyOwned(instance);entry.Instance=null;throw;
                }
                return;
            }
        }
        public PoolCounts GetCounts(string poolId)
        {
            if(poolId==null||!pools.TryGetValue(poolId,out var pool))return default;
            RemoveDestroyed(pool);
            return new PoolCounts(pool.Current,pool.Peak,pool.Entries.Count,pool.Capacity);
        }
        static void RemoveDestroyed(Pool pool)
        {
            for(var i=pool.Entries.Count-1;i>=0;i--)
                if(pool.Entries[i].Instance==null)
                { if(pool.Entries[i].Leased)pool.Current--;pool.Entries.RemoveAt(i); }
        }
        static void DestroyOwned(GameObject instance)
        { if(Application.isPlaying)Destroy(instance);else DestroyImmediate(instance); }
        void OnDestroy()
        {
            foreach(var pool in pools.Values)foreach(var entry in pool.Entries)
                if(entry.Instance!=null){entry.Instance.SetActive(false);DestroyOwned(entry.Instance);}
        }
    }
}
