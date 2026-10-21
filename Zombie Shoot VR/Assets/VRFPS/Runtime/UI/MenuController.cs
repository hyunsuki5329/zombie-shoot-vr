using System;
using UnityEngine;
namespace VRFPS
{
    public enum MenuScreen { Hidden, MapSelection, Pause, Settings, Result, Instructions }
    public sealed class MenuController : MonoBehaviour
    {
        RunController run;ComparisonIntroController intro;
        public bool IntroBusy=>intro!=null&&intro.IsBusy;
        public bool IntroOwnsMenu=>IntroBusy&&intro.Phase!=IntroPhase.Complete;
        public string IntroError=>intro!=null?intro.LastError:null;
        public event Action IntroStatusChanged;
        MenuScreen settingsReturn;
        MenuScreen instructionsReturn;
        public MenuScreen Screen { get; private set; } = MenuScreen.MapSelection;
        public event Action<MenuScreen> ScreenChanged;
        public void BindIntro(ComparisonIntroController intro)
        {
            Unsubscribe();this.intro=intro;if(isActiveAndEnabled)Subscribe();
        }
        public void Configure(RunController run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            Unsubscribe(); this.run=run;
            if(isActiveAndEnabled) Subscribe();
        }
        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();
        void Subscribe()
        {
            if(run==null) return;
            run.PhaseChanged-=OnPhaseChanged;run.PhaseChanged+=OnPhaseChanged;
            if(intro!=null)
            {
                intro.PhaseChanged-=OnIntroPhase;intro.PhaseChanged+=OnIntroPhase;
                intro.BusyChanged-=OnIntroBusy;intro.BusyChanged+=OnIntroBusy;
            }
            if(IntroOwnsMenu)OnIntroPhase(intro.Phase);else OnPhaseChanged(run.Phase);
        }
        void Unsubscribe()
        {
            if(run!=null)run.PhaseChanged-=OnPhaseChanged;
            if(intro!=null){intro.PhaseChanged-=OnIntroPhase;intro.BusyChanged-=OnIntroBusy;}
        }
        void OnIntroBusy(bool busy)=>IntroStatusChanged?.Invoke();
        void OnIntroPhase(IntroPhase phase)
        {
            if(phase==IntroPhase.Complete)ApplyRunScreen(run.Phase);
            else if(phase==IntroPhase.Idle||phase==IntroPhase.Returning||phase==IntroPhase.Failed)SetScreen(MenuScreen.MapSelection);
            else if(intro.IsPaused)
            {
                if(Screen!=MenuScreen.Settings&&Screen!=MenuScreen.Instructions)SetScreen(MenuScreen.Pause);
            }
            else SetScreen(MenuScreen.Hidden);
            IntroStatusChanged?.Invoke();
        }
        void OnPhaseChanged(RunPhase phase)
        {
            if(IntroOwnsMenu)return;
            ApplyRunScreen(phase);
        }
        void ApplyRunScreen(RunPhase phase)
        {
            if(phase==RunPhase.Dead) SetScreen(run.Map==null ? MenuScreen.MapSelection : MenuScreen.Result);
            else if(phase==RunPhase.Paused) SetScreen(MenuScreen.Pause);
            else SetScreen(MenuScreen.Hidden);
        }
        void SetScreen(MenuScreen screen)
        {
            if(Screen==screen) return;
            Screen=screen; ScreenChanged?.Invoke(screen);
        }
        public void Pause()
        {
            if(IntroOwnsMenu)
            {
                if(intro.Phase==IntroPhase.Returning||intro.Phase==IntroPhase.Failed)return;
                intro.SetPaused(true);SetScreen(MenuScreen.Pause);
            }
            else if(run!=null)run.SetPaused(true);
        }
        public void Resume()
        {
            if(Screen!=MenuScreen.Pause)return;
            if(IntroOwnsMenu){intro.SetPaused(false);SetScreen(MenuScreen.Hidden);}
            else if(run!=null)run.SetPaused(false);
        }
        public void OpenSettings()
        {
            if(run==null || Screen==MenuScreen.Settings) return;
            if(Screen==MenuScreen.Hidden) Pause();
            settingsReturn=Screen;
            SetScreen(MenuScreen.Settings);
        }
        public void CloseSettings() { if(Screen==MenuScreen.Settings) SetScreen(settingsReturn); }
        public void OpenInstructions()
        {
            if(run==null || Screen==MenuScreen.Instructions || Screen==MenuScreen.Settings)return;
            if(Screen==MenuScreen.Hidden)Pause();
            instructionsReturn=Screen;SetScreen(MenuScreen.Instructions);
        }
        public void CloseInstructions() { if(Screen==MenuScreen.Instructions)SetScreen(instructionsReturn); }
        public void CloseAuxiliary()
        {
            if(Screen==MenuScreen.Instructions)CloseInstructions();else CloseSettings();
        }
        public void Retry()
        {
            if(IntroOwnsMenu || run==null || run.Map==null || (Screen!=MenuScreen.Result && Screen!=MenuScreen.Pause)) return;
            run.StartRun(run.Map);
        }
        public void RetryIntroCleanup()
        {
            if(intro!=null&&IntroOwnsMenu&&intro.LastError!=null)IntroAsyncOperation.Start(intro.Shutdown());
        }
        public void ShowMapSelection()
        {
            if(IntroOwnsMenu){intro.Cancel();SetScreen(MenuScreen.MapSelection);return;}
            if(run==null) return;
            run.EndRun();
            SetScreen(MenuScreen.MapSelection);
        }
    }
}
