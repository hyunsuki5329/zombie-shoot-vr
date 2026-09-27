using UnityEngine;
using UnityEngine.Audio;
namespace VRFPS
{
    public sealed class EnemyFeedback : MonoBehaviour
    {
        public AudioClip approachClip,warningClip;
        public AudioMixerGroup output;
        ZombieController enemy;Transform target;
        AudioSource approach,warning;
        bool subscribed,combatEnabled;
        float nextApproach;
        public int WarningCues {get;private set;}
        public int ApproachCues {get;private set;}
        void Awake(){approach=Voice("Approach");warning=Voice("Attack warning");}
        AudioSource Voice(string name)
        {
            var child=new GameObject(name+" audio");child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;
            source.spatialBlend=1;source.minDistance=1;source.maxDistance=12;source.dopplerLevel=0;return source;
        }
        public void Configure(ZombieController enemy,Transform target)
        {
            Unsubscribe();Stop();this.enemy=enemy;this.target=target;combatEnabled=true;nextApproach=0;
            if(isActiveAndEnabled)Subscribe();
        }
        void OnEnable()=>Subscribe();
        void Subscribe()
        {if(subscribed || enemy==null)return;subscribed=true;enemy.AttackStarted+=OnWarning;enemy.Died+=OnDeath;}
        void Unsubscribe()
        {if(!subscribed)return;subscribed=false;if(enemy!=null){enemy.AttackStarted-=OnWarning;enemy.Died-=OnDeath;}}
        void OnDisable(){Unsubscribe();Stop();}
        public void SetCombatEnabled(bool value){combatEnabled=value;if(!value)Stop();}
        public void Tick(float deltaTime)
        {
            if(!isActiveAndEnabled || !combatEnabled || enemy==null || enemy.IsDead || target==null)return;
            nextApproach=Mathf.Max(0,nextApproach-Mathf.Max(0,deltaTime));
            if(nextApproach>0 || enemy.IsWindingUp || Vector3.Distance(transform.position,target.position)>6)return;
            nextApproach=3;ApproachCues++;Play(approach,approachClip);
        }
        void OnWarning()
        {
            if(!combatEnabled)return;
            if(approach!=null)approach.Stop();WarningCues++;Play(warning,warningClip);
        }
        void OnDeath(int id,int source)=>Stop();
        void Stop(){if(approach!=null)approach.Stop();if(warning!=null)warning.Stop();}
        void Play(AudioSource source,AudioClip clip)
        {if(source!=null && clip!=null){source.clip=clip;source.outputAudioMixerGroup=output;source.volume=.35f;source.Play();}}
    }
}
