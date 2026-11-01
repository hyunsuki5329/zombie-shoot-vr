using System.Collections;
using UnityEngine;
namespace VRFPS
{
    public interface IRealityPresentation
    {
        IEnumerator Load(RealityIntroProfile profile);
        bool IsReady {get;}
        string LastError {get;}
        Transform ObservationAnchor {get;}
        IntroVirusTarget Virus {get;}
        IEnumerator Unload();
    }
}
