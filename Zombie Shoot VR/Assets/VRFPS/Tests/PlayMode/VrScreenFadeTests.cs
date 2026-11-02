using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace VRFPS.Tests
{
    public class VrScreenFadeTests
    {
        GameObject host;VrScreenFade fade;float oldScale;
        [SetUp] public void Setup(){oldScale=Time.timeScale;host=new GameObject("Fade test");fade=host.AddComponent<VrScreenFade>();}
        [TearDown] public void Cleanup(){Time.timeScale=oldScale;Object.DestroyImmediate(host);}
        [UnityTest] public IEnumerator FadeUsesUnscaledTimeAndPauseFreezesAlpha()
        {
            Time.timeScale=0;var paused=true;
            var routine=fade.FadeTo(1,.05f,()=>paused,()=>false);
            Assert.That(routine.MoveNext(),Is.True);yield return null;
            Assert.That(routine.MoveNext(),Is.True);Assert.That(fade.Alpha,Is.Zero);
            paused=false;var deadline=Time.realtimeSinceStartup+3;
            while(routine.MoveNext()){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null;}
            Assert.That(fade.Alpha,Is.EqualTo(1));
            yield return fade.FadeTo(0,.05f,()=>false,()=>false);
            Assert.That(fade.Alpha,Is.Zero);
        }
        [Test] public void ImmediateResetInvalidatesOlderFadeAndCancellationDoesNotJumpToTarget()
        {
            fade.SetImmediate(.4f);var cancel=false;
            var old=fade.FadeTo(1,1,()=>true,()=>cancel);Assert.That(old.MoveNext(),Is.True);
            cancel=true;Assert.That(old.MoveNext(),Is.False);Assert.That(fade.Alpha,Is.EqualTo(.4f));
            var stale=fade.FadeTo(1,1,()=>true,()=>false);Assert.That(stale.MoveNext(),Is.True);
            fade.SetImmediate(0);Assert.That(stale.MoveNext(),Is.False);Assert.That(fade.Alpha,Is.Zero);
            var first=fade.FadeTo(1,1,()=>true,()=>false);Assert.That(first.MoveNext(),Is.True);
            var second=fade.FadeTo(.7f,0,()=>false,()=>false);Assert.That(second.MoveNext(),Is.False);
            Assert.That(first.MoveNext(),Is.False);Assert.That(fade.Alpha,Is.EqualTo(.7f));
        }
        [UnityTest] public IEnumerator MonoRenderCoversBackgroundAndNearGeometryAndReleasesVisual()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Requires graphics; run explicitly without -nographics. Does not validate XR.");
            var camera=host.AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;
            camera.nearClipPlane=.01f;camera.farClipPlane=10;
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.SetParent(host.transform);
            cube.transform.localPosition=new Vector3(0,0,.3f);cube.transform.localScale=Vector3.one*.2f;
            var target=new RenderTexture(128,128,24);target.Create();
            var image=new Texture2D(128,128,TextureFormat.RGB24,false);
            try
            {
                var shader=Shader.Find("VRFPS/IntroScreenFade");Assert.That(shader,Is.Not.Null);
                Assert.That(shader.isSupported,Is.True);
                fade.Configure(camera,shader);yield return null;
                foreach(var alpha in new[]{0f,1f,0f})
                {
                    fade.SetImmediate(alpha);
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                        new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target});
                    var previous=RenderTexture.active;RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=previous;
                    var lit=0;foreach(var pixel in image.GetPixels32())if(pixel.r>3||pixel.g>3||pixel.b>3)lit++;
                    Assert.That(lit,alpha==1?Is.EqualTo(0):Is.GreaterThan(1000));
                }
                Object.Destroy(fade);yield return null;yield return null;
                Assert.That(host.transform.Find("Intro fade shell"),Is.Null);
            }
            finally{target.Release();Object.Destroy(target);Object.Destroy(image);}
        }
#if UNITY_EDITOR
        [UnityTest] public IEnumerator RealMenuRemainsVisibleAboveBlackFade()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Requires graphics; does not validate XR.");
            var camera=host.AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;
            camera.nearClipPlane=.01f;camera.farClipPlane=10;
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/UI/Menus.prefab");
            var menus=Object.Instantiate(prefab,host.transform);
            menus.transform.localPosition=new Vector3(0,0,1.2f);menus.transform.localRotation=Quaternion.identity;
            menus.GetComponent<Canvas>().worldCamera=camera;
            var panel=menus.transform.Find("Panel");panel.gameObject.SetActive(true);
            panel.GetComponent<UnityEngine.UI.Image>().color=Color.green;
            // Remove other bright image surfaces so white pixels prove font rendering.
            foreach(var graphic in menus.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if(graphic.transform!=panel)graphic.enabled=false;
            var target=new RenderTexture(128,128,24);target.Create();
            var image=new Texture2D(128,128,TextureFormat.RGB24,false);
            try
            {
                fade.Configure(camera,Shader.Find("VRFPS/IntroScreenFade"));fade.SetImmediate(1);
                yield return null;Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target});
                var previous=RenderTexture.active;RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=previous;
                var green=0;var black=0;var bright=0;
                foreach(var pixel in image.GetPixels32())
                {
                    if(pixel.g>100&&pixel.r<30&&pixel.b<30)green++;
                    if(pixel.r<4&&pixel.g<4&&pixel.b<4)black++;
                    if(pixel.r>100&&pixel.g>100&&pixel.b>100)bright++;
                }
                Assert.That(green,Is.GreaterThan(500),"Menu panel must remain visible over black.");
                Assert.That(bright,Is.GreaterThan(10),"Menu text must remain visible.");
                Assert.That(black,Is.GreaterThan(500),"World outside the menu remains black.");
            }
            finally{target.Release();Object.Destroy(target);Object.Destroy(image);}
        }
#endif
        [Test] public void RejectsInvalidAlphaAndDuration()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>fade.SetImmediate(float.NaN));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>fade.FadeTo(1,-1,()=>false,()=>false).MoveNext());
        }
    }
}
