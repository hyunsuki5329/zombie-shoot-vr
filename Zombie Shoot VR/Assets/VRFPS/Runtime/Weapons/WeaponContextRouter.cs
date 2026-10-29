using UnityEngine;
using VRFPS.Player;
namespace VRFPS
{
    public sealed class WeaponContextRouter : MonoBehaviour
    {
        RunController run;K2Controller gun;PlayerRigController rig;AmmoPouch pouch;
        bool ownsIntro,preview,blocked;
        double introTime;
        public WeaponContext Context {get;private set;}=WeaponContext.Disabled;
        public double InputTime=>ownsIntro?introTime:run!=null?run.ActiveTime:0;
        public void Configure(RunController run,K2Controller gun,PlayerRigController rig,AmmoPouch pouch)
        {
            Unsubscribe();this.run=run;this.gun=gun;this.rig=rig;this.pouch=pouch;
            if(gun!=null){var input=gun.GetComponent<K2HandInput>();if(input!=null)input.UseContext(this);}
            if(isActiveAndEnabled)Subscribe();
        }
        void OnEnable()=>Subscribe();
        void Subscribe(){if(run!=null){run.PhaseChanged-=OnPhase;run.PhaseChanged+=OnPhase;}Apply();}
        void Unsubscribe(){if(run!=null)run.PhaseChanged-=OnPhase;}
        void OnDisable()
        {
            Unsubscribe();Context=WeaponContext.Disabled;if(gun!=null)gun.SetContext(Context);
            RequireTriggerRelease();
            if(rig!=null)rig.SetLocomotionEnabled(false);if(pouch!=null)pouch.SetSimulationEnabled(false);
        }
        void RequireTriggerRelease()
        {
            if(gun==null)return;
            var input=gun.GetComponent<K2HandInput>();
            if(input!=null)input.RequireTriggerRelease();
        }
        void OnPhase(RunPhase phase)=>Apply();
        public void SetIntroState(bool preview,bool blocked,double introTime)
        {
            if(double.IsNaN(introTime)||double.IsInfinity(introTime)||introTime<0)
                throw new System.ArgumentOutOfRangeException(nameof(introTime));
            if(!ownsIntro){if(gun!=null)gun.ResetInputClock();RequireTriggerRelease();}
            ownsIntro=true;this.preview=preview;this.blocked=blocked;this.introTime=introTime;Apply();
        }
        public void ReleaseIntro(){ownsIntro=false;introTime=0;Apply();}
        void Apply()
        {
            var phase=run!=null?run.Phase:RunPhase.Dead;
            var next=ownsIntro?(preview&&!blocked?WeaponContext.Intro:WeaponContext.Disabled):
                phase==RunPhase.Active?WeaponContext.Combat:
                phase==RunPhase.Preparing?WeaponContext.Preparing:WeaponContext.Disabled;
            if(next!=Context && (next==WeaponContext.Disabled || Context==WeaponContext.Disabled ||
                next==WeaponContext.Intro || Context==WeaponContext.Intro))
                RequireTriggerRelease();
            Context=next;
            if(gun!=null){if(ownsIntro)gun.SetContext(next);else gun.SetPhase(phase);}
            if(rig!=null)rig.SetLocomotionEnabled(!ownsIntro&&next!=WeaponContext.Disabled);
            if(pouch!=null)pouch.SetSimulationEnabled(next!=WeaponContext.Disabled);
        }
    }
}
