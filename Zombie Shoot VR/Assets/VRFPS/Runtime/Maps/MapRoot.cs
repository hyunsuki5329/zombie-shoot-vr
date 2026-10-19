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
                    !NavMesh.CalculatePath(hit.position,playerHit.position,filter,path) || path.status!=NavMeshPathStatus.PathComplete)
                {RejectedSpawnCount++;continue;}
                if(!valid.Contains(hit.position))valid.Add(hit.position);
                else RejectedSpawnCount++;
            }
            // Preserve near-start anchors for later movement. SpawnDirector checks the
            // distance from the current player before warning and again before spawning.
            if(valid.Count==0 || !valid.Exists(p=>Vector3.Distance(p,playerHit.position)>=map.minSpawnDistance))
                throw new InvalidOperationException("Map has no reachable opening enemy spawn candidates.");
            PlayerPosition=playerHit.position;
            return valid.ToArray();
        }
        bool Owned(Transform value)=>value!=null && value.IsChildOf(transform) && value.gameObject.activeInHierarchy;
        static bool Finite(Vector3 p)=> !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
            !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
    }
}
