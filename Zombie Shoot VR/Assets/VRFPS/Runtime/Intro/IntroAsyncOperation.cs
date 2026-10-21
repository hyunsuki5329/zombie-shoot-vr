using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRFPS
{
    // The drain owns the coroutine independently of the requesting scene object.
    // Cancellation invalidates the request; it must not stop an outstanding Unity load.
    public sealed class IntroAsyncOperation
    {
        public bool IsDone {get;private set;}
        public Exception Error {get;private set;}
        public static IntroAsyncOperation Start(IEnumerator operation)
        {
            if(operation==null)throw new ArgumentNullException(nameof(operation));
            var result=new IntroAsyncOperation();
            var host=new GameObject("Intro operation drain");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<IntroOperationDrain>().Run(result.Drain(operation,host));
            return result;
        }
        IEnumerator Drain(IEnumerator operation,GameObject host)
        {
            var stack=new Stack<IEnumerator>();stack.Push(operation);
            try
            {
                while(stack.Count>0)
                {
                    var current=stack.Peek();bool more=false;object next=null;
                    try{more=current.MoveNext();if(more)next=current.Current;}
                    catch(Exception error){Error=error;break;}
                    if(!more)
                    {
                        stack.Pop();Dispose(current);if(Error!=null)break;
                    }
                    else if(next is IEnumerator nested)stack.Push(nested);
                    else yield return next;
                }
            }
            finally
            {
                while(stack.Count>0)Dispose(stack.Pop());
                IsDone=true;
                if(host!=null)UnityEngine.Object.Destroy(host);
            }
        }
        void Dispose(IEnumerator operation)
        {
            try{(operation as IDisposable)?.Dispose();}
            catch(Exception error){if(Error==null)Error=error;}
        }
    }
}
