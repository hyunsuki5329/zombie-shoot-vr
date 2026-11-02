using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
namespace VRFPS.Editor
{
    public static class WorldFeedbackSetup
    {
        const string Folder="Assets/VRFPS/Audio/";
        public static void Configure()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batchmode project.");
            Wave("Player_Damage",.14,95,.7,1);Wave("Wave_Start",.35,520,0,1);
            Wave("Low_Health",.4,440,0,2);Wave("Enemy_Approach",.5,65,.5,1);
            Wave("Melee_Warning",.25,180,.3,1);Wave("Ranged_Warning",.3,750,.1,1);
            AssetDatabase.Refresh();
            var group=Mixer().FindMatchingGroups("Master").Single(x=>x.name=="Master");
            const string rigPath="Assets/VRFPS/Prefabs/PlayerRig.prefab";
            var rig=PrefabUtility.LoadPrefabContents(rigPath);
            try{rig.GetComponentInChildren<CombatFeedback>(true).output=group;PrefabUtility.SaveAsPrefabAsset(rig,rigPath);}
            finally{PrefabUtility.UnloadPrefabContents(rig);}
            foreach(var name in new[]{"MeleeZombie","RangedZombie"})
            {
                var path="Assets/VRFPS/Prefabs/Enemies/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var feedback=root.GetComponent<EnemyFeedback>() ?? root.AddComponent<EnemyFeedback>();
                    feedback.output=group;feedback.approachClip=Clip("Enemy_Approach");
                    feedback.warningClip=Clip(name=="MeleeZombie" ? "Melee_Warning" : "Ranged_Warning");
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(var path in new[]{"Assets/VRFPS/Scenes/Combat.unity",WaveLabSetup.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                var run=UnityEngine.Object.FindFirstObjectByType<RunController>();
                var feedback=run.GetComponent<RunFeedback>() ?? run.gameObject.AddComponent<RunFeedback>();
                var data=new SerializedObject(feedback);data.FindProperty("run").objectReferenceValue=run;data.ApplyModifiedPropertiesWithoutUndo();
                feedback.output=group;feedback.damageClip=Clip("Player_Damage");feedback.waveClip=Clip("Wave_Start");feedback.lowHealthClip=Clip("Low_Health");
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Cannot save feedback scene.");
            }
            AssetDatabase.SaveAssets();Debug.Log("VRFPS_WORLD_FEEDBACK_CONFIGURED");
        }
        static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+name+".wav");
        static AudioMixer Mixer()
        {
            var path=Folder+"AudioMixer.mixer";var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            if(mixer!=null)return mixer;
            // The installed editor owns native mixer creation; no hand-authored serialized mixer data.
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(x=>x.GetType("UnityEditor.Audio.AudioMixerController")).FirstOrDefault(x=>x!=null);
            var create=type?.GetMethod("CreateMixerControllerAtPath",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(string)},null);
            if(create==null)throw new NotSupportedException("This Unity editor has no mixer creation entry point.");
            create.Invoke(null,new object[]{path});AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(path) ?? throw new IOException("Mixer creation failed.");
        }
        static void Wave(string name,double duration,double frequency,double noise,int pulses)
        {
            const int rate=44100;var count=(int)(duration*rate);var random=new System.Random(20260928+(int)frequency);
            using(var writer=new BinaryWriter(File.Create(Folder+name+".wav")))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(var i=0;i<count;i++)
                {
                    var t=i/(double)rate;var part=(t*pulses/duration)%1;
                    var envelope=Math.Sin(Math.PI*part)*Math.Min(1,(duration-t)/.02);
                    var tone=Math.Sin(2*Math.PI*frequency*(t+.25*t*t/duration));
                    var sample=((1-noise)*tone+noise*(random.NextDouble()*2-1))*envelope*.6;
                    writer.Write((short)(sample*short.MaxValue));
                }
            }
        }
    }
}
