using System;
using System.Collections;
using UnityEngine;
using VRFPS.Player;
namespace VRFPS
{
    public sealed class ComparisonIntroController : MonoBehaviour
    {
        IRealityPresentation reality;CombatSession combat;PlayerRigController rig;K2Controller gun;
        WeaponContextRouter router;VrScreenFade fade;IntroVirusTarget target;
        IntroAsyncOperation pending;
        int generation;bool configured,paused,hit,cleanupFailed;double introTime;
        public IntroPhase Phase {get;private set;}
        public bool IsBusy {get;private set;}
        public string LastError {get;private set;}
        public event Action<IntroPhase> PhaseChanged;
        public void Configure(IRealityPresentation reality,CombatSession combat,PlayerRigController rig,K2Controller gun,WeaponContextRouter router,VrScreenFade fade)
        {
            if(IsBusy)throw new InvalidOperationException("Transition is busy.");
            if(reality==null||combat==null||rig==null||rig.Head==null||gun==null||router==null||fade==null)
                throw new ArgumentException("Comparison references are incomplete.");
            this.reality=reality;this.combat=combat;this.rig=rig;this.gun=gun;this.router=router;this.fade=fade;configured=true;
        }
        public IEnumerator Begin(RealityIntroProfile profile,MapDefinition map)
        {
            if(IsBusy)yield break;
            if(!configured)throw new InvalidOperationException("Configure the comparison first.");
            LastError=null;
            try{if(profile==null)throw new ArgumentNullException(nameof(profile));profile.ValidateFor(map);map.Validate();}
            catch(Exception error){LastError=error.Message;}
            if(LastError!=null){SetPhase(IntroPhase.Failed);yield break;}
            IsBusy=true;cleanupFailed=false;paused=false;hit=false;introTime=0;var request=++generation;
            IntroAsyncOperation.Start(RunRequest(request,profile,map));
            while(IsBusy)yield return null;
        }
        IEnumerator RunRequest(int request,RealityIntroProfile profile,MapDefinition map)
        {
            var flow=IntroAsyncOperation.Start(Transition(request,profile,map));
            while(!flow.IsDone)yield return null;
            if(flow.Error!=null)Fail(flow.Error.Message);
            if(Cancelled(request)||Phase!=IntroPhase.Complete)
            {
                var cleanup=IntroAsyncOperation.Start(Cleanup());
                while(!cleanup.IsDone)yield return null;
                if(cleanup.Error!=null){cleanupFailed=true;Fail("Cleanup failed: "+cleanup.Error.Message);yield break;}
                if(LastError==null)SetPhase(IntroPhase.Idle);
            }
            IsBusy=false;
        }
        IEnumerator Transition(int request,RealityIntroProfile profile,MapDefinition map)
        {
            SetPhase(IntroPhase.LoadingReality);RefreshInput();fade.SetImmediate(1);
            yield return Stage(combat.Shutdown(),profile.loadTimeoutSeconds,request);
            if(Cancelled(request))yield break;
            yield return Stage(reality.Load(profile),profile.loadTimeoutSeconds,request);
            if(Cancelled(request))yield break;
            if(!reality.IsReady || reality.ObservationAnchor==null || reality.Virus==null)
                throw new InvalidOperationException(reality.LastError??"Reality presentation is incomplete.");
            yield return WaitForResume(request);if(Cancelled(request))yield break;
            PlaceReality(reality.ObservationAnchor);
            var hand=rig.GetComponentInChildren<LeftHandInteractor>(true);
            if(hand!=null)hand.Restart();else gun.Restart();
            target=reality.Virus;target.Configure(gun.GetInstanceID(),()=>!Cancelled(request)&&!paused&&Phase==IntroPhase.RealityPreview);
            target.Triggered+=OnHit;
            SetPhase(IntroPhase.RealityPreview);fade.SetImmediate(0);RefreshInput();
            while(!hit && !Cancelled(request))
            {
                yield return null;
                if(!paused)introTime+=Time.unscaledDeltaTime;
                RefreshInput();
            }
            if(Cancelled(request))yield break;
            var relativeYaw=Mathf.DeltaAngle(reality.ObservationAnchor.eulerAngles.y,rig.transform.eulerAngles.y);
            UnsubscribeTarget();SetPhase(IntroPhase.FadingOut);RefreshInput();
            yield return fade.FadeTo(1,profile.fadeOutSeconds,()=>paused,()=>Cancelled(request));
            if(Cancelled(request))yield break;
            RequireFade(1);SetPhase(IntroPhase.Switching);
            var black=0f;
            while(black<profile.minBlackSeconds&&!Cancelled(request))
            {yield return null;if(!Cancelled(request))RequireFade(1);if(!paused)black+=Time.unscaledDeltaTime;}
            if(Cancelled(request))yield break;
            yield return Stage(reality.Unload(),profile.loadTimeoutSeconds,request);
            if(Cancelled(request))yield break;
            yield return Stage(combat.PrepareMap(map,()=>!Cancelled(request)&&fade!=null&&fade.isActiveAndEnabled&&fade.Alpha==1),profile.loadTimeoutSeconds,request);
            if(Cancelled(request))yield break;
            if(!combat.HasPreparedMap)throw new InvalidOperationException(combat.LastError??"Theme map did not prepare.");
            yield return WaitForResume(request);if(Cancelled(request))yield break;
            RequireFade(1);
            combat.PlacePreparedPlayer(combat.FindPreparedAnchor(profile.themeAnchorKey),relativeYaw);
            SetPhase(IntroPhase.FadingIn);
            yield return fade.FadeTo(0,profile.fadeInSeconds,()=>paused,()=>Cancelled(request));
            yield return WaitForResume(request);if(Cancelled(request))yield break;
            RequireFade(0);router.ReleaseIntro();
            if(!combat.BeginPreparedRun())throw new InvalidOperationException(combat.LastError??"Combat did not start.");
            SetPhase(IntroPhase.Complete);
        }
        IEnumerator Stage(IEnumerator operation,float timeout,int request)
        {
            if(Phase==IntroPhase.Switching)RequireFade(1);
            pending=IntroAsyncOperation.Start(operation);var started=Time.realtimeSinceStartupAsDouble;
            while(!pending.IsDone)
            {
                if(Cancelled(request))yield break;
                if(Phase==IntroPhase.Switching)RequireFade(1);
                if(Time.realtimeSinceStartupAsDouble-started>=timeout){Fail("Comparison loading timed out.");yield break;}
                yield return null;
            }
            var error=pending.Error;pending=null;
            if(error!=null)throw error;
            if(!Cancelled(request)&&Phase==IntroPhase.Switching)RequireFade(1);
        }
        void RequireFade(float alpha)
        {
            if(fade==null || !fade.isActiveAndEnabled || fade.Alpha!=alpha)
                throw new InvalidOperationException("Comparison fade was interrupted.");
        }
        IEnumerator WaitForResume(int request){while(paused&&!Cancelled(request))yield return null;}
        IEnumerator Cleanup()
        {
            UnsubscribeTarget();if(fade!=null)fade.SetImmediate(0);RefreshInput();
            if(pending!=null){while(!pending.IsDone)yield return null;pending=null;}
            // Do not overlap unload with the previous non-cancellable operation.
            var unload=IntroAsyncOperation.Start(reality.Unload());while(!unload.IsDone)yield return null;
            var shutdown=combat!=null?IntroAsyncOperation.Start(combat.Shutdown()):null;
            if(shutdown!=null)while(!shutdown.IsDone)yield return null;
            if(unload.Error!=null)throw unload.Error;
            if(shutdown!=null&&shutdown.Error!=null)throw shutdown.Error;
            if(router!=null)router.ReleaseIntro();
        }
        void PlaceReality(Transform anchor)
        {
            var body=rig.GetComponent<CharacterController>();var enabled=body.enabled;body.enabled=false;
            try
            {
                var head=rig.transform.InverseTransformPoint(rig.Head.position);
                rig.transform.rotation=Quaternion.Euler(0,anchor.eulerAngles.y,0);
                rig.transform.position=anchor.position-rig.transform.TransformVector(new Vector3(head.x,0,head.z));
            }
            finally{body.enabled=enabled;}
        }
        void OnHit(){hit=true;RefreshInput();}
        void UnsubscribeTarget(){if(target!=null)target.Triggered-=OnHit;target=null;}
        bool Cancelled(int request)=>this==null||request!=generation;
        void RefreshInput(){if(router!=null)router.SetIntroState(Phase==IntroPhase.RealityPreview&&!hit,paused||Phase==IntroPhase.Returning||Phase==IntroPhase.Failed,introTime);}
        void SetPhase(IntroPhase phase){Phase=phase;if(this!=null)PhaseChanged?.Invoke(phase);}
        void Fail(string error){LastError=error;generation++;SetPhase(IntroPhase.Failed);if(fade!=null)fade.SetImmediate(0);UnsubscribeTarget();RefreshInput();}
        public void SetPaused(bool value){paused=value;if(IsBusy)RefreshInput();}
        public void Cancel()
        {
            if(!IsBusy)return;
            generation++;SetPhase(IntroPhase.Returning);UnsubscribeTarget();
            if(fade!=null)fade.SetImmediate(0);RefreshInput();
        }
        public IEnumerator Shutdown()
        {
            if(IsBusy&&!cleanupFailed)
            {Cancel();while(IsBusy&&!cleanupFailed)yield return null;yield break;}
            if(!configured)yield break;
            IsBusy=true;cleanupFailed=false;SetPhase(IntroPhase.Returning);
            IntroAsyncOperation.Start(FinishShutdown());
            while(IsBusy&&!cleanupFailed)yield return null;
        }
        IEnumerator FinishShutdown()
        {
            var cleanup=IntroAsyncOperation.Start(Cleanup());while(!cleanup.IsDone)yield return null;
            if(cleanup.Error!=null){cleanupFailed=true;Fail(cleanup.Error.Message);yield break;}
            LastError=null;SetPhase(IntroPhase.Idle);IsBusy=false;
        }
        void OnDisable()=>Cancel();
        void OnDestroy()=>Cancel();
    }
}
