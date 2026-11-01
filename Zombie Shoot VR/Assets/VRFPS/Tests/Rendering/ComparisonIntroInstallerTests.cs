#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace VRFPS.Tests
{
    public class ComparisonIntroInstallerTests
    {
        const string Boot="Assets/VRFPS/Scenes/Bootstrap.unity";
        GameBootstrap bootstrap;string directory;
        void Inject(Scene scene,LoadSceneMode mode)
        {
            if(scene.path!=Boot)return;
            bootstrap=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameBootstrap>()).Single();
            bootstrap.RecordDirectoryOverride=directory;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded-=Inject;
            if(bootstrap)yield return bootstrap.Shutdown();
            foreach(var path in new[]{"Assets/VRFPS/Scenes/Combat.unity",Boot})
            {var scene=SceneManager.GetSceneByPath(path);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);}
            if(directory!=null&&Directory.Exists(directory))Directory.Delete(directory,true);
        }
        [UnityTest] public IEnumerator LiveBootstrapInstallsComparisonAndKeepsDirectCombatAvailable()
        {
            directory=Path.Combine(Path.GetTempPath(),"VRFPS-intro-bootstrap-"+Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded+=Inject;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Boot,new LoadSceneParameters(LoadSceneMode.Additive));
            var deadline=Time.realtimeSinceStartup+30;
            while(!bootstrap.IsReady&&bootstrap.LastError==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(bootstrap.IsReady,Is.True,bootstrap.LastError);
            var entry=bootstrap.Entry;var installer=entry.GetComponent<ComparisonIntroInstaller>();
            Assert.That(installer,Is.Not.Null,"Production Combat scene must install the comparison.");
            Assert.That(installer.IsInstalled,Is.True);
            Assert.That(entry.GetComponents<ComparisonIntroController>().Length,Is.EqualTo(1));
            Assert.That(entry.GetComponents<WeaponContextRouter>().Length,Is.EqualTo(1));
            Assert.That(entry.rig.GetComponentsInChildren<Camera>(true).Length,Is.EqualTo(1));
            Assert.That(entry.menu.Screen,Is.EqualTo(MenuScreen.Instructions));
            // Keep missing-capture recovery coverage independent of locally installed captures.
            entry.ConfigureRealityOptions(null,null);
            entry.menu.ShowMapSelection();
            var room=entry.mapControls.transform.Find("Room Reality").GetComponent<Button>();
            var classroom=entry.mapControls.transform.Find("Classroom Reality").GetComponent<Button>();
            Assert.That(room.interactable,Is.False);Assert.That(classroom.interactable,Is.False);
            Assert.That(room.GetComponentInChildren<Text>().text,Does.Contain("Capture not configured"));
            Assert.That(classroom.GetComponentInChildren<Text>().text,Does.Contain("Capture not configured"));
            Assert.That(entry.roomButton.interactable,Is.True);Assert.That(entry.classroomButton.interactable,Is.True);
            Assert.That(entry.roomLabel.text,Does.Contain("Direct Combat"));
            Assert.That(entry.classroomLabel.text,Does.Contain("Direct Combat"));
            var fade=entry.GetComponent<VrScreenFade>();Assert.That(fade,Is.Not.Null);Assert.That(fade.Alpha,Is.Zero);
            Assert.That(entry.run.Phase,Is.Not.EqualTo(RunPhase.Active));
            CaptureMenu(entry);
            var profile=ScriptableObject.CreateInstance<RealityIntroProfile>();
            try
            {
                profile.mapId="wrong";profile.dataId="test";profile.realityScenePath="Assets/VRFPS/Scenes/Combat.unity";
                profile.realityAnchorKey="anchor";profile.themeAnchorKey="playerSpawn";
                entry.ConfigureRealityOptions(profile,null);
                Assert.That(room.interactable,Is.False);Assert.That(room.GetComponentInChildren<Text>().text,Does.Contain("Invalid capture"));
                room.onClick.Invoke();Assert.That(entry.GetComponent<ComparisonIntroController>().IsBusy,Is.False);
                // A valid-looking profile that targets the already owned Combat scene must
                // reach the real loader through the button, fail safely, and retain direct play.
                profile.mapId="room";entry.ConfigureRealityOptions(profile,null);Assert.That(room.interactable,Is.True);
                var originalPipeline=QualitySettings.renderPipeline;room.onClick.Invoke();
                var intro=entry.GetComponent<ComparisonIntroController>();deadline=Time.realtimeSinceStartup+10;
                while(intro.IsBusy&&Time.realtimeSinceStartup<deadline)yield return null;
                yield return null;
                Assert.That(intro.IsBusy,Is.False);Assert.That(intro.LastError,Does.Contain("already owned"));
                Assert.That(entry.menu.Screen,Is.EqualTo(MenuScreen.MapSelection));Assert.That(entry.Session.IsReady,Is.False);
                Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalPipeline));
                Assert.That(SceneManager.GetSceneByPath("Assets/VRFPS/Scenes/Combat.unity").isLoaded,Is.True);
                Assert.That(entry.roomButton.interactable,Is.True);Assert.That(entry.classroomButton.interactable,Is.True);
            }
            finally {entry.ConfigureRealityOptions(null,null);UnityEngine.Object.DestroyImmediate(profile);}
        }
        static void CaptureMenu(CombatSceneEntry entry)
        {
            var camera=entry.rig.Head.GetComponent<Camera>();var fov=camera.fieldOfView;var aspect=camera.aspect;
            var target=new RenderTexture(1280,850,24);target.Create();var image=new Texture2D(1280,850,TextureFormat.RGB24,false);
            try
            {
                camera.fieldOfView=40;camera.aspect=1280f/850;
                entry.view.transform.position=camera.transform.position+camera.transform.forward*1.2f;
                entry.view.transform.rotation=camera.transform.rotation;Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                var old=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,850),0,0);image.Apply();RenderTexture.active=old;
                var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../tmp/p13/intro-map-menu.png"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally {camera.fieldOfView=fov;camera.aspect=aspect;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
#endif
