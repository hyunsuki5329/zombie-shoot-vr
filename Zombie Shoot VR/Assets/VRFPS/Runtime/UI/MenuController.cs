using System;
using UnityEngine;
namespace VRFPS
{
    public enum MenuScreen { Hidden, MapSelection, Pause, Settings, Result, Instructions }
    public sealed class MenuController : MonoBehaviour
    {
        RunController run;
        MenuScreen settingsReturn;
        MenuScreen instructionsReturn;
        public MenuScreen Screen { get; private set; } = MenuScreen.MapSelection;
        public event Action<MenuScreen> ScreenChanged;
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
            run.PhaseChanged+=OnPhaseChanged;
            OnPhaseChanged(run.Phase);
        }
        void Unsubscribe() { if(run!=null) run.PhaseChanged-=OnPhaseChanged; }
        void OnPhaseChanged(RunPhase phase)
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
        public void Pause() { if(run!=null) run.SetPaused(true); }
        public void Resume()
        {
            if(run!=null && Screen==MenuScreen.Pause) run.SetPaused(false);
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
            if(run==null || run.Map==null || (Screen!=MenuScreen.Result && Screen!=MenuScreen.Pause)) return;
            run.StartRun(run.Map);
        }
        public void ShowMapSelection()
        {
            if(run==null) return;
            run.EndRun();
            SetScreen(MenuScreen.MapSelection);
        }
    }
}
