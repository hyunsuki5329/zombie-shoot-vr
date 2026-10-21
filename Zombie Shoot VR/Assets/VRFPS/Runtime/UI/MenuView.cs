using UnityEngine;
using UnityEngine.UI;
namespace VRFPS
{
    public sealed class MenuView : MonoBehaviour
    {
        [SerializeField] MenuController menu;
        [SerializeField] RunController run;
        [SerializeField] RunRecordBinding result;
        [SerializeField] GameObject panel;
        [SerializeField] Transform viewer;
        [SerializeField] GameObject settingsControls;
        [SerializeField] Text title, details;
        [SerializeField] Button resume, retry, settings, back, maps, retrySave;
        [SerializeField] Button instructions;
        [SerializeField] GameObject instructionPanel;
        bool subscribed;
        public void Configure(MenuController menu, RunController run, RunRecordBinding result,
            GameObject panel, Text title, Text details, Button resume, Button retry, Button settings, Button back, Button maps, Button retrySave)
        {
            Unsubscribe(); this.menu=menu;this.run=run;this.result=result;this.panel=panel;
            this.title=title;this.details=details;this.resume=resume;this.retry=retry;this.settings=settings;
            this.back=back;this.maps=maps;this.retrySave=retrySave;
            if(isActiveAndEnabled) Subscribe();
        }
        public void BindResult(RunRecordBinding binding)
        {
            Unsubscribe();result=binding;if(isActiveAndEnabled)Subscribe();
        }
        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();
        void Subscribe()
        {
            if(subscribed || menu==null || run==null || panel==null) return;
            subscribed=true;
            menu.Configure(run);
            menu.ScreenChanged+=OnScreenChanged;menu.IntroStatusChanged+=Refresh;
            if(result!=null) result.ResultChanged+=Refresh;
            resume.onClick.AddListener(menu.Resume); retry.onClick.AddListener(Retry);
            settings.onClick.AddListener(menu.OpenSettings); back.onClick.AddListener(menu.CloseAuxiliary);
            if(instructions!=null)instructions.onClick.AddListener(menu.OpenInstructions);
            maps.onClick.AddListener(menu.ShowMapSelection); retrySave.onClick.AddListener(RetrySave);
            Refresh();
        }
        void Unsubscribe()
        {
            if(!subscribed) return; subscribed=false;
            if(menu!=null)
            {
                menu.ScreenChanged-=OnScreenChanged;menu.IntroStatusChanged-=Refresh;
                resume.onClick.RemoveListener(menu.Resume);retry.onClick.RemoveListener(Retry);
                settings.onClick.RemoveListener(menu.OpenSettings);back.onClick.RemoveListener(menu.CloseAuxiliary);
                if(instructions!=null)instructions.onClick.RemoveListener(menu.OpenInstructions);
                maps.onClick.RemoveListener(menu.ShowMapSelection);
            }
            if(result!=null)result.ResultChanged-=Refresh;
            retrySave.onClick.RemoveListener(RetrySave);
        }
        void OnScreenChanged(MenuScreen screen) => Refresh();
        void Retry(){if(menu.IntroOwnsMenu)menu.RetryIntroCleanup();else menu.Retry();}
        void RetrySave() { if(result!=null) result.RetrySave();Refresh(); }
        public void Refresh()
        {
            if(menu==null || panel==null) return;
            var screen=menu.Screen;
            if(screen!=MenuScreen.Hidden && !panel.activeSelf && viewer!=null)
            {
                var forward=Vector3.ProjectOnPlane(viewer.forward,Vector3.up);
                if(forward.sqrMagnitude<.001f) forward=Vector3.forward;
                forward.Normalize();
                transform.position=viewer.position+forward*1.2f;
                transform.rotation=Quaternion.LookRotation(forward,Vector3.up);
            }
            panel.SetActive(screen!=MenuScreen.Hidden);
            if(settingsControls!=null)settingsControls.SetActive(screen==MenuScreen.Settings);
            var showingInstructions=screen==MenuScreen.Instructions;
            if(instructionPanel!=null)instructionPanel.SetActive(showingInstructions);
            details.gameObject.SetActive(!showingInstructions);
            if(instructions!=null)instructions.gameObject.SetActive(!showingInstructions && screen!=MenuScreen.Settings);
            if(screen==MenuScreen.Hidden)return;
            title.text=screen==MenuScreen.MapSelection ? "MAP SELECTION" : screen==MenuScreen.Pause ? "PAUSED" : screen.ToString().ToUpperInvariant();
            resume.gameObject.SetActive(screen==MenuScreen.Pause);
            retry.gameObject.SetActive(menu.IntroOwnsMenu ? screen==MenuScreen.MapSelection&&menu.IntroError!=null : screen==MenuScreen.Pause||screen==MenuScreen.Result);
            settings.gameObject.SetActive(screen!=MenuScreen.Settings && !showingInstructions);
            back.gameObject.SetActive(screen==MenuScreen.Settings || showingInstructions);
            maps.gameObject.SetActive(screen==MenuScreen.Pause || screen==MenuScreen.Result);
            retrySave.gameObject.SetActive(screen==MenuScreen.Result && result!=null && result.HasResult && !result.SaveSucceeded);
            if(screen==MenuScreen.Result)
            {
                var wave=result!=null && result.HasResult ? result.Result.reachedWave : run.ReachedWave;
                var kills=result!=null && result.HasResult ? result.Result.kills : run.Kills;
                var saved=result==null ? "Practice scores are not saved." : result.SaveSucceeded ? "Saved" : "Save failed. Retry saving below.";
                details.text=$"WAVE {wave}   KILLS {kills}\n{saved}";
            }
            else if(screen==MenuScreen.Pause) details.text=menu.IntroOwnsMenu ? "Comparison paused. Resume or return to map selection." : "Combat paused. Move your hands to point at a button.";
            else if(screen==MenuScreen.Settings) details.text="Continuous movement and continuous turning.";
            else if(menu.IntroBusy)details.text=menu.IntroError!=null?menu.IntroError+"\nCleanup pending. Use Retry to retry cleanup.":"Returning to map selection...";
            else details.text=menu.IntroError??"Choose a map to begin.";
        }
    }
}
