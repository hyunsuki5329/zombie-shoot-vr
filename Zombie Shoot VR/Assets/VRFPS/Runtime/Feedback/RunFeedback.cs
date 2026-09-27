using UnityEngine;
using UnityEngine.Audio;
namespace VRFPS
{
    public sealed class RunFeedback : MonoBehaviour
    {
        [SerializeField] RunController run;
        public AudioClip damageClip,waveClip,lowHealthClip;
        public AudioMixerGroup output;
        AudioSource damage,wave,low;
        bool subscribed;
        public int DamageCues {get;private set;}
        public int WaveCues {get;private set;}
        public int LowHealthCues {get;private set;}
        void Awake(){damage=Voice("Damage");wave=Voice("Wave");low=Voice("Low health");}
        AudioSource Voice(string name)
        {
            var child=new GameObject(name+" audio");child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.priority=48;return source;
        }
        public void Configure(RunController run){Unsubscribe();this.run=run;if(isActiveAndEnabled)Subscribe();}
        void OnEnable()=>Subscribe();
        void Subscribe()
        {
            if(subscribed || run==null)return;subscribed=true;
            run.DamageTaken+=OnDamage;run.WaveStarted+=OnWave;run.RunStarted+=Stop;run.PhaseChanged+=OnPhase;
        }
        void Unsubscribe()
        {
            if(!subscribed)return;subscribed=false;
            if(run!=null){run.DamageTaken-=OnDamage;run.WaveStarted-=OnWave;run.RunStarted-=Stop;run.PhaseChanged-=OnPhase;}
        }
        void OnDisable(){Unsubscribe();Stop();}
        void OnPhase(RunPhase phase){if(phase==RunPhase.Paused || phase==RunPhase.Dead)Stop();}
        void Stop(){if(damage!=null)damage.Stop();if(wave!=null)wave.Stop();if(low!=null)low.Stop();}
        void Play(AudioSource source,AudioClip clip)
        {if(source!=null && clip!=null){source.clip=clip;source.outputAudioMixerGroup=output;source.volume=.4f;source.Play();}}
        void OnWave(int number){WaveCues++;Play(wave,waveClip);}
        void OnDamage(int before,int after)
        {
            DamageCues++;Play(damage,damageClip);
            if(before>25 && after>0 && after<=25){LowHealthCues++;Play(low,lowHealthClip);}
        }
    }
}
