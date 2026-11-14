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
        public Button roomRealityButton,classroomRealityButton;
        public Text roomRealityLabel,classroomRealityLabel;
        RealityIntroProfile roomReality,classroomReality;string comparisonUnavailable;
        public event Action Initialized;
        public bool IsInitialized=>ready;
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
            if(roomRealityButton!=null)roomRealityButton.onClick.AddListener(SelectRoomReality);
            if(classroomRealityButton!=null)classroomRealityButton.onClick.AddListener(SelectClassroomReality);
            menu.ScreenChanged+=OnScreenChanged;menu.IntroStatusChanged+=Refresh;ready=true;
            Initialized?.Invoke();menu.OpenInstructions();Refresh();
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
        void SelectRoomReality()=>SelectRealityChoice(roomReality,room);
        void SelectClassroomReality()=>SelectRealityChoice(classroomReality,classroom);
        void SelectRealityChoice(RealityIntroProfile profile,MapDefinition map)
        {
            var reason=RealityUnavailable(profile,map);
            if(reason!=null){mapError=reason;Refresh();return;}
            StartCoroutine(SelectReality(profile,map));
        }
        public void ConfigureRealityOptions(RealityIntroProfile roomProfile,RealityIntroProfile classroomProfile,string unavailableReason=null)
        {
            roomReality=roomProfile;classroomReality=classroomProfile;comparisonUnavailable=unavailableReason;Refresh();
        }
        string RealityUnavailable(RealityIntroProfile profile,MapDefinition map)
        {
            if(profile==null)return "촬영 자료가 설정되지 않았습니다.";
            if(comparisonUnavailable!=null)return "현실 공간을 사용할 수 없습니다. 실행 환경을 확인하세요.";
            if(intro==null)return "현실 공간 연결이 준비되지 않았습니다.";
            if(!Available(map))return "테마 맵을 사용할 수 없습니다.";
            try{profile.ValidateFor(map);}catch(ArgumentException){return "촬영 자료 설정을 확인해 주세요.";}
            if(!Application.CanStreamedLevelBeLoaded(profile.realityScenePath))return "현실 공간 씬이 빌드에 없습니다.";
            return null;
        }
        public IEnumerator SelectReality(RealityIntroProfile profile,MapDefinition map)
        {
            if(!ready||busy||menu.IntroBusy)yield break;
            if(intro==null||profile==null){mapError="GS 현실 공간 자료가 설정되지 않았습니다.";Refresh();yield break;}
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
            roomLabel.text=Label("방",room,roomReady);classroomLabel.text=Label("강의실",classroom,classroomReady);
            RefreshRealityChoice(roomRealityButton,roomRealityLabel,"방",roomReality,room);
            RefreshRealityChoice(classroomRealityButton,classroomRealityLabel,"강의실",classroomReality,classroom);
            status.text=menu.IntroBusy ? menu.IntroError!=null?"현실 공간을 정리하지 못했습니다. 다시 시작 버튼으로 재시도하세요.":"맵 선택으로 돌아가는 중…" : busy ? "맵을 불러오는 중…" : mapError!=null ? "맵/GS 공간을 불러오지 못했습니다. 다른 맵을 선택하거나 다시 시도하세요." : records.LastError!=null ? "기록을 불러오지 못했습니다. 저장 위치를 확인하세요." :
                (roomReady || classroomReady ? "맵과 시작 방식을 선택하세요." : "사용할 수 있는 맵이 없습니다.");
        }
        static bool Available(MapDefinition map)=>map!=null && Application.CanStreamedLevelBeLoaded(map.sceneName);
        void RefreshRealityChoice(Button button,Text label,string name,RealityIntroProfile profile,MapDefinition map)
        {
            if(button==null||label==null)return;
            var reason=RealityUnavailable(profile,map);button.interactable=!busy&&!menu.IntroBusy&&reason==null;
            label.text=name+"\n현실 공간에서 시작"+(reason==null?"": "\n"+reason);
        }
        string Label(string name,MapDefinition map,bool available)
        {
            var record=records.Read(map.mapId);
            return $"{name}\n바로 전투 시작\n최고 웨이브 {record.reachedWave} / 처치 {record.kills}"+(available ? "" : "\n사용 불가");
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
            if(roomRealityButton!=null)roomRealityButton.onClick.RemoveListener(SelectRoomReality);
            if(classroomRealityButton!=null)classroomRealityButton.onClick.RemoveListener(SelectClassroomReality);
            pause?.Dispose();
        }
    }
}
