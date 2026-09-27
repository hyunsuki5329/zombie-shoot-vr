using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace VRFPS
{
    public sealed class MapRoot : MonoBehaviour
    {
        public Transform playerSpawn, visualRoot, collisionRoot;
        public Transform[] enemySpawns;
        public int RejectedSpawnCount { get; private set; }
        public Vector3 PlayerPosition { get; private set; }
        public Vector3[] GetSpawnPositions(MapDefinition map,int agentTypeId=0)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            map.Validate();RejectedSpawnCount=0;
            if(!Owned(playerSpawn) || !Owned(visualRoot) || !Owned(collisionRoot))
                throw new InvalidOperationException("Map needs local player, visual and collision roots.");
            var hasCollision=false;
            foreach(var collider in collisionRoot.GetComponentsInChildren<Collider>())
                if(collider.enabled && !collider.isTrigger && collider.gameObject.layer==LayerMask.NameToLayer("WorldSolid")) {hasCollision=true;break;}
            if(!hasCollision)throw new InvalidOperationException("Map has no active solid collision geometry.");
            var filter=new NavMeshQueryFilter{agentTypeID=agentTypeId,areaMask=NavMesh.AllAreas};
            if(!Finite(playerSpawn.position) || !NavMesh.SamplePosition(playerSpawn.position,out var playerHit,.25f,filter))
                throw new InvalidOperationException("Player spawn is not on the navigation mesh.");
            var valid=new List<Vector3>();var path=new NavMeshPath();
            foreach(var spawn in enemySpawns ?? Array.Empty<Transform>())
            {
                if(!Owned(spawn) || !Finite(spawn.position) ||
                    !NavMesh.SamplePosition(spawn.position,out var hit,.25f,filter) ||
                    Vector3.Distance(hit.position,playerHit.position)<map.minSpawnDistance ||
                    !NavMesh.CalculatePath(hit.position,playerHit.position,filter,path) || path.status!=NavMeshPathStatus.PathComplete)
                {RejectedSpawnCount++;continue;}
                if(!valid.Contains(hit.position))valid.Add(hit.position);
                else RejectedSpawnCount++;
            }
            if(valid.Count==0)throw new InvalidOperationException("Map has no reachable enemy spawn candidates.");
            PlayerPosition=playerHit.position;
            return valid.ToArray();
        }
        bool Owned(Transform value)=>value!=null && value.IsChildOf(transform) && value.gameObject.activeInHierarchy;
        static bool Finite(Vector3 p)=> !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
            !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
    }
}
