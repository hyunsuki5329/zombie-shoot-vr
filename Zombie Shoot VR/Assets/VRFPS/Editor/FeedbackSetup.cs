using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
namespace VRFPS.Editor
{
    public static class FeedbackSetup
    {
        const string Folder="Assets/VRFPS/Audio";
        public static void Configure()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batchmode project.");
            var userAudio=UserWeaponAudioSetup.HasAnyOriginal;
            if(userAudio)UserWeaponAudioSetup.ValidateOriginals();
            Directory.CreateDirectory(Folder);
            Wave("K2_Shot",.09,0);Wave("K2_Empty",.045,1);Wave("Magazine_Insert",.09,2);
            AssetDatabase.Refresh();
            const string path="Assets/VRFPS/Prefabs/PlayerRig.prefab";
            var rig=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var gun=rig.GetComponentInChildren<K2Controller>(true);
                var feedback=gun.GetComponent<CombatFeedback>() ?? gun.gameObject.AddComponent<CombatFeedback>();
                var data=new SerializedObject(feedback);
                data.FindProperty("gun").objectReferenceValue=gun;
                data.FindProperty("socket").objectReferenceValue=gun.GetComponentInChildren<MagazineSocket>(true);
                data.ApplyModifiedPropertiesWithoutUndo();
                feedback.shotClip=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/K2_Shot.wav");
                feedback.emptyClip=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/K2_Empty.wav");
                feedback.insertClip=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Magazine_Insert.wav");
                feedback.rightHaptics=Haptic(rig,"Right Controller","RightHand");
                feedback.leftHaptics=Haptic(rig,"Left Controller","LeftHand");
                PrefabUtility.SaveAsPrefabAsset(rig,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(rig);}
            if(userAudio)UserWeaponAudioSetup.Configure();
            AssetDatabase.SaveAssets();Debug.Log("VRFPS_WEAPON_FEEDBACK_CONFIGURED");
        }
        static HapticImpulsePlayer Haptic(GameObject rig,string name,string usage)
        {
            var parent=rig.GetComponentsInChildren<Transform>(true).Single(x=>x.name==name);
            var child=parent.Find("Weapon Haptics");
            if(child==null){child=new GameObject("Weapon Haptics").transform;child.SetParent(parent,false);}
            var player=child.GetComponent<HapticImpulsePlayer>() ?? child.gameObject.AddComponent<HapticImpulsePlayer>();
            player.hapticOutput=new XRInputHapticImpulseProvider("Weapon haptics",false,XRInputHapticImpulseProvider.InputSourceMode.InputAction)
            {inputAction=new InputAction("Weapon haptics",InputActionType.PassThrough,"<XRController>{"+usage+"}/*",expectedControlType:"Haptic")};
            return player;
        }
        // Deterministic original PCM synthesis. No recordings or third-party audio samples.
        static void Wave(string name,double duration,int kind)
        {
            const int rate=44100;var count=(int)(rate*duration);var random=new System.Random(20260928+kind);
            using(var writer=new BinaryWriter(File.Create(Folder+"/"+name+".wav")))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(var i=0;i<count;i++)
                {
                    var t=i/(double)rate;var noise=random.NextDouble()*2-1;
                    var envelope=Math.Min(1,t/.001)*Math.Exp(-t*(kind==1 ? 100 : 45));
                    var tone=Math.Sin(2*Math.PI*(kind==0 ? 110 : kind==1 ? 1500 : 650)*t);
                    var sample=(kind==0 ? .75*noise+.25*tone : .25*noise+.75*tone)*envelope*.7;
                    writer.Write((short)(sample*short.MaxValue));
                }
            }
        }
    }
}
