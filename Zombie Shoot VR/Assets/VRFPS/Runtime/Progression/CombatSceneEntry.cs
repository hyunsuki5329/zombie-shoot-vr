using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VRFPS.Player;
namespace VRFPS
{
    public sealed class CombatSceneEntry : MonoBehaviour
    {
        public RunController run;public PlayerRigController rig;public K2Controller gun;public LeftHandInteractor hand;
        public EnemyActor melee,ranged;public AcidProjectile projectile;
        public MenuView view;public MenuController menu;public GameObject mapControls;
        public Button roomButton,classroomButton;public Text roomLabel,classroomLabel,status;
        public MapDefinition room,classroom;
        public CombatSession Session {get;private set;}
        RecordStore records;ComparisonIntroController intro;
        InputAction pause;
        string mapError;
        bool ready,busy;
        void Awake()
        {
            pause=new InputAction("Pause",InputActionType.Button,"<XRController>{LeftHand}/primaryButton");
            pause.AddBinding("<Keyboard>/f6");
        }
        void OnEnable()=>pause?.Enable();
        void OnDisable()=>pause?.Disable();
        public void Initialize(RecordStore records)
        {
            if(ready)throw new InvalidOperationException("Combat scene is already initialized.");
            this.records=records ?? throw new ArgumentNullException(nameof(records));
            Session=gameObject.AddComponent<CombatSession>();
            Session.Configure(run,rig,gun,hand,melee,ranged,projectile,records);
            menu.Configure(run);view.BindResult(Session.Results);
            roomButton.onClick.AddListener(SelectRoom);classroomButton.onClick.AddListener(SelectClassroom);
            menu.ScreenChanged+=OnScreenChanged;menu.IntroStatusChanged+=Refresh;ready=true;menu.OpenInstructions();Refresh();
        }
        void Update()
        {
            if(!ready || (busy&&!menu.IntroOwnsMenu) || !pause.WasPressedThisFrame())return;
            if(menu.Screen==MenuScreen.Hidden)menu.Pause();
            else if(menu.Screen==MenuScreen.Pause)menu.Resume();
            else if(menu.Screen==MenuScreen.Settings || menu.Screen==MenuScreen.Instructions)menu.CloseAuxiliary();
        }
        void SelectRoom()=>StartCoroutine(SelectMap(room));
        void SelectClassroom()=>StartCoroutine(SelectMap(classroom));
        public IEnumerator SelectReality(RealityIntroProfile profile,MapDefinition map)
        {
            if(!ready||busy||menu.IntroBusy)yield break;
            if(intro==null||profile==null){mapError="GS comparison is unavailable: reality data is not configured.";Refresh();yield break;}
            busy=true;mapError=null;Refresh();
            try{yield return intro.Begin(profile,map);mapError=intro.LastError;}
            finally{busy=false;Refresh();}
        }
        public void BindComparison(ComparisonIntroController intro)
        {
            if(busy||menu.IntroBusy)throw new InvalidOperationException("Cannot replace a busy comparison.");
            this.intro=intro;menu.BindIntro(intro);Refresh();
        }
        public IEnumerator SelectMap(MapDefinition map)
        {
            if(!ready || busy || menu.IntroBusy)yield break;
            if(intro!=null)intro.ClearError();
            busy=true;mapError=null;Refresh();
            try
            {
                yield return Session.StartMap(map);
                if(Session.LastError!=null){mapError=Session.LastError;menu.ShowMapSelection();}
            }
            finally{busy=false;Refresh();}
        }
        void OnScreenChanged(MenuScreen screen)=>Refresh();
        void Refresh()
        {
            if(!ready)return;
            var selecting=menu.Screen==MenuScreen.MapSelection;mapControls.SetActive(selecting&&!menu.IntroBusy);
            if(!selecting)return;
            var roomReady=Available(room);var classroomReady=Available(classroom);
            roomButton.interactable=!busy&&!menu.IntroBusy&&roomReady;classroomButton.interactable=!busy&&!menu.IntroBusy&&classroomReady;
            roomLabel.text=Label("Room",room,roomReady);classroomLabel.text=Label("Classroom",classroom,classroomReady);
            status.text=menu.IntroBusy ? menu.IntroError!=null?menu.IntroError+"\nCleanup pending. Use Retry to retry cleanup.":"Returning to map selection..." : busy ? "Loading map..." : mapError ?? records.LastError ??
                (roomReady || classroomReady ? "Choose a map to begin." : "Maps are not available in this build.");
        }
        static bool Available(MapDefinition map)=>map!=null && Application.CanStreamedLevelBeLoaded(map.sceneName);
        string Label(string name,MapDefinition map,bool available)
        {
            var record=records.Read(map.mapId);
            return $"{name}\nBest WAVE {record.reachedWave} / KILLS {record.kills}"+(available ? "" : "\nUnavailable");
        }
        public IEnumerator Shutdown()
        {
            ready=false;
            if(intro!=null)
            {
                yield return intro.Shutdown();
                if(intro.IsBusy)throw new InvalidOperationException(intro.LastError??"Comparison cleanup is incomplete.");
            }
            while(busy || (Session!=null && Session.IsSwitching))yield return null;
            if(Session!=null)yield return Session.Shutdown();
        }
        void OnDestroy()
        {
            if(menu!=null){menu.ScreenChanged-=OnScreenChanged;menu.IntroStatusChanged-=Refresh;}
            if(intro!=null)intro.Cancel();
            if(roomButton!=null)roomButton.onClick.RemoveListener(SelectRoom);
            if(classroomButton!=null)classroomButton.onClick.RemoveListener(SelectClassroom);
            pause?.Dispose();
        }
    }
}
