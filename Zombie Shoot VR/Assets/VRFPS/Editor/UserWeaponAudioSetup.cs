using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace VRFPS.Editor
{
    public static class UserWeaponAudioSetup
    {
        public const string Folder="Assets/VRFPS/External/UserAudio";
        static readonly string[] Names={"shoot sound.mp3","empty shoot.mp3","reload.mp3"};
        static readonly string[] Hashes={"b12aee9041391b0214bfdd67fc50d783ec66fad3d4cc89eae77ac900ea601e88","a15f0ce8251274aa4d33733fe94f408f681ee232c47598df257eba3e6b328ccc","78067b1634a13133a218c1826769bfc90ff14c5efeb777d391013cad9b9ce950"};
        public static bool HasAnyOriginal
        {
            get { foreach(var name in Names)if(File.Exists(Folder+"/"+name))return true;return false; }
        }
        public static void ValidateOriginals()
        {
            // Validate every original before touching generated output.
            for(var i=0;i<Names.Length;i++)using(var sha=SHA256.Create())
            {
                var hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Folder+"/"+Names[i]))).Replace("-","").ToLowerInvariant();
                if(hash!=Hashes[i])throw new InvalidDataException("Unexpected audio original: "+Names[i]);
            }
        }
        public static void Configure()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batchmode project.");
            ValidateOriginals();
            AssetDatabase.Refresh();
            // Last discharge of the shortest burst: one attack plus decay, no later shots.
            var shot=Extract(Names[0],"Shot",.322, .702, .001, .05, .65f);
            var empty=Extract(Names[1],"Empty",.207, .57, .001, .025, .55f);
            var insert=Extract(Names[2],"Reload",.063, .84, .001, .025, .65f);
            const string path="Assets/VRFPS/Prefabs/PlayerRig.prefab";
            var rig=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var feedback=rig.GetComponentInChildren<CombatFeedback>(true);
                if(feedback==null)throw new InvalidOperationException("Configure weapon feedback first.");
                feedback.shotClip=shot;feedback.emptyClip=empty;feedback.insertClip=insert;
                PrefabUtility.RecordPrefabInstancePropertyModifications(feedback);
                if(PrefabUtility.SaveAsPrefabAsset(rig,path)==null)throw new IOException("Cannot save audio rig");
            }
            finally{PrefabUtility.UnloadPrefabContents(rig);}
            AssetDatabase.SaveAssets();Debug.Log("VRFPS_USER_WEAPON_AUDIO_CONFIGURED");
        }
        static AudioClip Extract(string source,string name,double start,double end,double fadeIn,double fadeOut,float peak)
        {
            var sourcePath=Folder+"/"+source;
            var importer=(AudioImporter)AssetImporter.GetAtPath(sourcePath);
            SetPcm(importer);
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(sourcePath);clip.LoadAudioData();
            if(end>clip.length)throw new InvalidDataException("Audio shorter than selected segment");
            var input=new float[clip.samples*clip.channels];if(!clip.GetData(input,0))throw new IOException("Cannot decode "+source);
            var begin=(int)Math.Round(start*clip.frequency);var count=(int)Math.Round((end-start)*clip.frequency);
            var mono=new float[count];float maximum=0;
            for(var i=0;i<count;i++)
            {
                float value=0;for(var c=0;c<clip.channels;c++)value+=input[(begin+i)*clip.channels+c]/clip.channels;
                var envelope=Math.Min(1,Math.Min(i/(fadeIn*clip.frequency),(count-1-i)/(fadeOut*clip.frequency)));
                mono[i]=value*(float)envelope;maximum=Mathf.Max(maximum,Mathf.Abs(mono[i]));
            }
            if(maximum<.001f)throw new InvalidDataException("Selected audio is silent");
            var path=Folder+"/"+name+".wav";
            using(var w=new BinaryWriter(File.Create(path)))
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(clip.frequency);w.Write(clip.frequency*2);w.Write((short)2);w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
                foreach(var value in mono)w.Write((short)Math.Round(value/maximum*peak*short.MaxValue));
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);SetPcm((AudioImporter)AssetImporter.GetAtPath(path));
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static void SetPcm(AudioImporter importer)
        {
            var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.loadInBackground=false;importer.SaveAndReimport();
        }
    }
}
