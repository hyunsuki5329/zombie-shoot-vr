#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace VRFPS.Tests
{
    [PrebuildSetup(typeof(MapSceneTestSetup)),PostBuildCleanup(typeof(MapSceneTestSetup))]
    public class IntroAsyncOperationTests
    {
        [UnityTest] public IEnumerator RealSceneLoadCompletesAndUnloadsAfterRequestOwnerDestruction()
        {
            const string path="Assets/VRFPS/Tests/Fixtures/MapLoading/ValidMap.unity";
            var owner=new GameObject("Scene request owner");var loaded=false;
            IEnumerator LoadAndRelease()
            {
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(path,UnityEngine.SceneManagement.LoadSceneMode.Additive);
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);loaded=scene.isLoaded;
                if(scene.isLoaded)yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }
            var job=IntroAsyncOperation.Start(LoadAndRelease());UnityEngine.Object.Destroy(owner);
            var deadline=Time.realtimeSinceStartup+10;
            while(!job.IsDone && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(job.IsDone,Is.True);Assert.That(job.Error,Is.Null);Assert.That(loaded,Is.True);
            Assert.That(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path).isLoaded,Is.False);
        }
        [UnityTest] public IEnumerator NestedFailureIsCapturedAndEnumeratorsAreDisposed()
        {
            var disposed=false;
            IEnumerator Child(){yield return null;throw new InvalidOperationException("nested load failure");}
            IEnumerator Parent(){try{yield return Child();}finally{disposed=true;}}
            var job=IntroAsyncOperation.Start(Parent());
            for(var i=0;i<10&&!job.IsDone;i++)yield return null;
            Assert.That(job.IsDone,Is.True);Assert.That(job.Error?.Message,Is.EqualTo("nested load failure"));
            Assert.That(disposed,Is.True);
        }
        [UnityTest] public IEnumerator OperationFinishesAfterRequestOwnerIsDestroyed()
        {
            var owner=new GameObject("Request owner");var release=false;var finished=false;
            IEnumerator Load(){while(!release)yield return null;finished=true;}
            var job=IntroAsyncOperation.Start(Load());
            UnityEngine.Object.Destroy(owner);yield return null;
            Assert.That(job.IsDone,Is.False);
            release=true;
            for(var i=0;i<10&&!job.IsDone;i++)yield return null;
            Assert.That(job.IsDone,Is.True);Assert.That(job.Error,Is.Null);Assert.That(finished,Is.True);
            yield return null;
            Assert.That(GameObject.Find("Intro operation drain"),Is.Null);
        }
    }
}
#endif
