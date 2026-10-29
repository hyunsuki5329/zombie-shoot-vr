using System;
using System.Linq;
using UnityEngine;
using VRFPS.Player;
namespace VRFPS
{
    // Authoring contract only. The presentation adapter must separately prove GS readiness/release.
    public sealed class RealitySceneRoot : MonoBehaviour
    {
        public string mapId,dataId;
        public Transform dataRoot,observationAnchor,collisionRoot;
        public IntroVirusTarget virus;
        public void Validate(RealityIntroProfile profile)
        {
            if(profile==null)throw new ArgumentNullException(nameof(profile));
            profile.Validate();
            if(!isActiveAndEnabled||mapId!=profile.mapId||dataId!=profile.dataId)
                throw new InvalidOperationException("Reality scene does not match the selected map/data.");
            if(!Owned(dataRoot)||!Owned(observationAnchor)||!Owned(collisionRoot)||virus==null||!Owned(virus.transform)||!virus.isActiveAndEnabled)
                throw new InvalidOperationException("Reality data, anchor, collision and virus references must be active and owned by this root.");
            if(!Near(transform.lossyScale,Vector3.one)||!Near(dataRoot.lossyScale,Vector3.one*profile.metersPerUnit)||!Near(observationAnchor.lossyScale,Vector3.one))
                throw new InvalidOperationException("Reality root/anchor must have unit scale; data scale must match the uniform calibration.");
            if(observationAnchor.name!=profile.realityAnchorKey || GetComponentsInChildren<Transform>(true).Count(t=>t.name==profile.realityAnchorKey)!=1)
                throw new InvalidOperationException("Reality observation anchor key must identify exactly one owned transform.");
            var world=LayerMask.NameToLayer("WorldSolid");var target=LayerMask.NameToLayer("IntroTarget");
            if(world<0||target<0)throw new InvalidOperationException("Reality collision layers are not configured.");
            var worldColliders=collisionRoot.GetComponentsInChildren<Collider>(true);
            var targetColliders=virus.GetComponentsInChildren<Collider>(true);
            if(worldColliders.Length==0||worldColliders.Any(c=>!ValidCollider(c,world)))
                throw new InvalidOperationException("Reality collision proxies must be active non-trigger WorldSolid colliders.");
            if(targetColliders.Length==0||targetColliders.Any(c=>!ValidCollider(c,target)))
                throw new InvalidOperationException("Virus hit shapes must be active non-trigger IntroTarget colliders.");
            var roots=gameObject.scene.GetRootGameObjects();var rootCount=0;var virusCount=0;
            foreach(var sceneRoot in roots)
            {
                rootCount+=sceneRoot.GetComponentsInChildren<RealitySceneRoot>(true).Length;
                virusCount+=sceneRoot.GetComponentsInChildren<IntroVirusTarget>(true).Length;
                if(sceneRoot.GetComponentsInChildren<Camera>(true).Length>0||sceneRoot.GetComponentsInChildren<AudioListener>(true).Length>0||
                    sceneRoot.GetComponentsInChildren<PlayerRigController>(true).Length>0||sceneRoot.GetComponentsInChildren<RunController>(true).Length>0||
                    sceneRoot.GetComponentsInChildren<EnemyActor>(true).Length>0)
                    throw new InvalidOperationException("Reality scene must not contain its own camera, listener, player rig or combat actors.");
                foreach(var collider in sceneRoot.GetComponentsInChildren<Collider>(true))
                    if(!collider.transform.IsChildOf(collisionRoot)&&!collider.transform.IsChildOf(virus.transform))
                        throw new InvalidOperationException("Reality scene contains a collider outside its declared collision/virus roots.");
            }
            if(rootCount!=1||virusCount!=1)throw new InvalidOperationException("Reality scene must contain exactly one root and virus target.");
        }
        bool Owned(Transform value)=>value!=null&&value!=transform&&value.IsChildOf(transform)&&value.gameObject.activeInHierarchy&&Finite(value.position);
        static bool ValidCollider(Collider value,int layer)=>value.enabled&&value.gameObject.activeInHierarchy&&!value.isTrigger&&value.gameObject.layer==layer;
        static bool Near(Vector3 a,Vector3 b)=>Finite(a)&&Finite(b)&&AxisNear(a.x,b.x)&&AxisNear(a.y,b.y)&&AxisNear(a.z,b.z);
        static bool AxisNear(float value,float expected)=>value>0&&expected>0&&Mathf.Abs(value-expected)<=expected*.001f;
        static bool Finite(Vector3 value)=>!float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&!float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&!float.IsNaN(value.z)&&!float.IsInfinity(value.z);
    }
}
