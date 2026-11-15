#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace VRFPS.Tests
{
 public class KoreanMenuPreviewTests
 {
  GameBootstrap bootstrap;
  string records;
  bool ownsScenes;
  readonly List<(Camera camera,bool enabled)> cameras=new List<(Camera,bool)>();
  [UnityTearDown] public IEnumerator Cleanup()
  {
   foreach(var saved in cameras)if(saved.camera!=null)saved.camera.enabled=saved.enabled;
   cameras.Clear();
   if(bootstrap!=null)yield return bootstrap.Shutdown();
   if(ownsScenes)
    foreach(var path in new[]{"Assets/VRFPS/Scenes/Combat.unity","Assets/VRFPS/Scenes/Bootstrap.unity"})
    {var scene=SceneManager.GetSceneByPath(path);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);}
   if(records!=null&&Directory.Exists(records))Directory.Delete(records,true);
   bootstrap=null;records=null;ownsScenes=false;
  }
  [UnityTest] public IEnumerator CaptureLocalizedMenuScreens()
  {
   const string boot="Assets/VRFPS/Scenes/Bootstrap.unity";
   records=Path.Combine(Path.GetTempPath(),"VRFPS-korean-preview-"+Guid.NewGuid().ToString("N"));
   void Inject(Scene s,LoadSceneMode m){if(s.path==boot)foreach(var root in s.GetRootGameObjects()){var b=root.GetComponent<GameBootstrap>();if(b)b.RecordDirectoryOverride=records;}}
   SceneManager.sceneLoaded+=Inject;
   GameObject cameraObject=null;
   var rt=new RenderTexture(1440,960,24);var image=new Texture2D(1440,960,TextureFormat.RGB24,false);
   try
   {
    Assert.That(SceneManager.GetSceneByPath(boot).isLoaded,Is.False);
    Assert.That(SceneManager.GetSceneByPath("Assets/VRFPS/Scenes/Combat.unity").isLoaded,Is.False);
    ownsScenes=true;
    yield return EditorSceneManager.LoadSceneAsyncInPlayMode(boot,new LoadSceneParameters(LoadSceneMode.Additive));
    bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();
    var end=Time.realtimeSinceStartup+30;
    while(!bootstrap.IsReady&&bootstrap.LastError==null&&Time.realtimeSinceStartup<end)yield return null;
    Assert.That(bootstrap.IsReady,Is.True,bootstrap.LastError);
    var entry=bootstrap.Entry;
    foreach(var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){cameras.Add((cam,cam.enabled));cam.enabled=false;}
    cameraObject=new GameObject("Menu review camera");var camera=cameraObject.AddComponent<Camera>();camera.GetUniversalAdditionalCameraData();
    camera.transform.position=new Vector3(0,0,-2);camera.orthographic=true;camera.orthographicSize=.37f;camera.aspect=1.5f;
    camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.06f,.09f);rt.Create();camera.targetTexture=rt;entry.view.GetComponent<Canvas>().worldCamera=camera;
    var output=Path.GetFullPath("../tmp/korean-menu");Directory.CreateDirectory(output);
    for(int iteration=0;iteration<15;iteration++)
    {
     int i=iteration%5;
     if(i==0&&iteration>0){entry.menu.ShowMapSelection();entry.menu.OpenInstructions();}
     if(i==1)entry.menu.CloseInstructions();
     if(i==2)
     {
      var left=entry.view.transform.Find("Panel/Settings").GetComponent<Button>();
      var right=entry.view.transform.Find("Panel/Controls").GetComponent<Button>();
      Assert.That(left.GetComponentInChildren<Text>().text,Is.EqualTo("조작 방법"));
      Assert.That(right.GetComponentInChildren<Text>().text,Is.EqualTo("게임 종료"));
      left.onClick.Invoke();
     }
     if(i==3){entry.menu.CloseInstructions();entry.run.StartRun(entry.room);entry.menu.Pause();}
     if(i==4){entry.menu.Resume();entry.run.EndRun();}
     entry.view.Refresh();entry.view.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);entry.view.transform.localScale=Vector3.one*.001f;
     Canvas.ForceUpdateCanvases();yield return null;yield return null;Canvas.ForceUpdateCanvases();

     Canvas.ForceUpdateCanvases();yield return null;
     foreach(var label in entry.view.GetComponentsInChildren<Text>(true))
      foreach(char c in label.text)if(c>=0xAC00&&c<=0xD7A3)Assert.That(label.font.HasCharacter(c),Is.True,"Missing Korean glyph "+c);
     Assert.That(entry.view.transform.Find("Panel/Title").GetComponent<Text>().text,Is.EqualTo(new[]{"조작 방법","맵 선택","조작 방법","일시정지","전투 결과"}[i]));
     yield return new WaitForSecondsRealtime(.2f);

     yield return null;yield return null;
     var previous=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1440,960),0,0);image.Apply();RenderTexture.active=previous;
     foreach(var label in entry.view.GetComponentsInChildren<Text>())
     {
      if(string.IsNullOrWhiteSpace(label.text))continue;
      var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);
      var lo=camera.WorldToViewportPoint(corners[0]);var hi=camera.WorldToViewportPoint(corners[2]);int white=0;
      for(int y=Mathf.Clamp((int)(lo.y*960),0,959);y<Mathf.Clamp((int)(hi.y*960),0,960);y++)
       for(int x=Mathf.Clamp((int)(lo.x*1440),0,1439);x<Mathf.Clamp((int)(hi.x*1440),0,1440);x++)
       {var color=image.GetPixel(x,y);if(color.r>.65f&&color.g>.65f&&color.b>.65f)white++;}
      Assert.That(white,Is.GreaterThan(10),"No readable text pixels: "+iteration+" "+label.name+" "+label.text);
     }
     File.WriteAllBytes(Path.Combine(output,new[]{"instructions","maps","guide-from-menu","pause","result"}[i]+".png"),image.EncodeToPNG());
    }
   }
   finally{SceneManager.sceneLoaded-=Inject;UnityEngine.Object.DestroyImmediate(cameraObject);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}

  }
 }
}
#endif
