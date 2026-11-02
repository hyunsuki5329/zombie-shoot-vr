using System.Collections;
using UnityEngine;
namespace VRFPS
{
    // Implemented by the package-specific bridge; metadata alone is never render readiness.
    public abstract class RealityRenderSource : MonoBehaviour
    {
        public abstract string DataId {get;}
        public abstract bool IsReady {get;}
        public abstract bool HasResources {get;}
        public abstract IEnumerator Initialize();
        // Must be idempotent, including before Initialize and after a partially failed release.
        // HasResources must become false before the owning scene may unload.
        public abstract IEnumerator Release();
    }
}
