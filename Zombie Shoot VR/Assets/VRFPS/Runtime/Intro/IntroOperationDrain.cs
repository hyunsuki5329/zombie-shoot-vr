using System.Collections;
using UnityEngine;
namespace VRFPS
{
    // Transient host; destroyed only after the operation and its finally blocks finish.
    public sealed class IntroOperationDrain : MonoBehaviour
    {
        internal void Run(IEnumerator operation)=>StartCoroutine(operation);
    }
}
