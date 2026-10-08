using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace VRFPS.Tests
{
 public class UserWeaponAudioAssetTests
 {
  [Test] public void RigUsesPreparedUserClipsWithPromptOnsetsAndBoundedShotTail()
  {
   var rig=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/PlayerRig.prefab");
   var feedback=rig.GetComponentInChildren<CombatFeedback>(true);
   Assert.That(feedback.shotClip,Is.Not.Null,"Restore originals with scripts/Restore-UserAudio.py and run UserWeaponAudioSetup.Configure before final-asset verification");
   Assert.That(feedback.shotClip.length,Is.LessThan(4*K2FireControl.ShotInterval),"Tail must finish before its voice is reused");
   foreach(var clip in new[]{feedback.shotClip,feedback.emptyClip,feedback.insertClip})
   {
    Assert.That(AssetDatabase.GetAssetPath(clip),Does.StartWith("Assets/VRFPS/External/UserAudio/"));
    Assert.That(clip.channels,Is.EqualTo(1));
    var settings=((AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip))).defaultSampleSettings;
    Assert.That(settings.preloadAudioData,Is.True);Assert.That(settings.loadType,Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
    clip.LoadAudioData();var data=new float[clip.samples];Assert.That(clip.GetData(data,0),Is.True);
    float onset=0,peak=0;for(var i=0;i<data.Length;i++){peak=Mathf.Max(peak,Mathf.Abs(data[i]));if(i<clip.frequency*.02)onset=Mathf.Max(onset,Mathf.Abs(data[i]));}
    Assert.That(onset,Is.GreaterThan(.05f),"Do not retain leading MP3 silence");
    Assert.That(peak,Is.InRange(.1f,.7f));Assert.That(Mathf.Abs(data[0])+Mathf.Abs(data[data.Length-1]),Is.LessThan(.0001f),"Faded boundaries avoid discontinuities");
   }
  }
 }
}
