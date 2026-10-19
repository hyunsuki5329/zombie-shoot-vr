using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
namespace VRFPS
{
    // Four bounded shot voices preserve the 0.38-second tail at the 0.1-second fire interval.
    public sealed class CombatFeedback : MonoBehaviour
    {
        [SerializeField] K2Controller gun;
        [SerializeField] MagazineSocket socket;
        public AudioClip shotClip,emptyClip,insertClip;
        public UnityEngine.Audio.AudioMixerGroup output;
        public HapticImpulsePlayer rightHaptics,leftHaptics;
        [Range(0,1)] public float volume=.35f;
        [Range(0,1)] public float shotHaptic=.25f,insertHaptic=.2f;
        readonly AudioSource[] shots=new AudioSource[4];
        int nextShot;
        AudioSource empty,insert;
        bool subscribed;
        public int CueCount { get; private set; }
        void Awake()
        {
            for(var i=0;i<shots.Length;i++)shots[i]=Voice("Shot audio "+i);
            empty=Voice("Empty audio");insert=Voice("Insert audio");
        }
        AudioSource Voice(string name)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;
            source.spatialBlend=1;source.minDistance=.3f;source.maxDistance=15;source.dopplerLevel=0;
            return source;
        }
        public void Configure(K2Controller gun,MagazineSocket socket)
        {
            Unsubscribe();this.gun=gun;this.socket=socket;if(isActiveAndEnabled)Subscribe();
        }
        void OnEnable()=>Subscribe();
        void Subscribe()
        {
            if(subscribed || gun==null)return;
            subscribed=true;gun.Fired+=OnShot;gun.EmptyTriggered+=OnEmpty;gun.PhaseChanged+=OnPhase;
            if(socket!=null)socket.Inserted+=OnInsert;
        }
        void Unsubscribe()
        {
            if(!subscribed)return;subscribed=false;
            if(gun!=null){gun.Fired-=OnShot;gun.EmptyTriggered-=OnEmpty;gun.PhaseChanged-=OnPhase;}
            if(socket!=null)socket.Inserted-=OnInsert;
        }
        void OnDisable(){Unsubscribe();StopVoices();}
        void OnPhase(RunPhase phase){if(phase==RunPhase.Paused || phase==RunPhase.Dead)StopVoices();}
        void StopVoices(){foreach(var voice in shots)if(voice!=null)voice.Stop();nextShot=0;if(empty!=null)empty.Stop();if(insert!=null)insert.Stop();}
        void Play(AudioSource source,AudioClip clip,HapticImpulsePlayer haptics,float amplitude,float duration)
        {
            CueCount++;
            if(source!=null && clip!=null){source.clip=clip;source.outputAudioMixerGroup=output;source.volume=Mathf.Clamp01(volume);source.Play();}
            if(haptics!=null)haptics.SendHapticImpulse(Mathf.Clamp01(amplitude),duration);
        }
        void OnShot(Vector3 start,Vector3 end)
        {
            Play(shots[nextShot],shotClip,rightHaptics,shotHaptic,.035f);
            nextShot=(nextShot+1)%shots.Length;
        }
        public void OnFreshMagazineSupplied()
        {
            if(isActiveAndEnabled && gun!=null && (gun.Phase==RunPhase.Active || gun.Phase==RunPhase.Preparing))
                Play(null,null,leftHaptics,.3f,.07f);
        }
        void OnEmpty()=>Play(empty,emptyClip,null,0,0);
        void OnInsert()
        {
            if(gun.Phase==RunPhase.Active || gun.Phase==RunPhase.Preparing)
                Play(insert,insertClip,leftHaptics,insertHaptic,.055f);
        }
    }
}
