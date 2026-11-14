using UnityEngine;
using UnityEngine.UI;
using VRFPS.Player;
namespace VRFPS
{
    public sealed class SettingsView : MonoBehaviour
    {
        [SerializeField] PlayerRigController rig;
        [SerializeField] Text moveLabel,turnLabel;
        [SerializeField] Button moveDown,moveUp,turnDown,turnUp;
        LocomotionSettings settings;
        string preferenceKey="VRFPS.Settings";
        public void Configure(PlayerRigController rig,Text moveLabel,Text turnLabel,Button moveDown,Button moveUp,Button turnDown,Button turnUp,string preferenceKey="VRFPS.Settings")
        {
            Unsubscribe();this.preferenceKey=preferenceKey;this.rig=rig;this.moveLabel=moveLabel;this.turnLabel=turnLabel;
            this.moveDown=moveDown;this.moveUp=moveUp;this.turnDown=turnDown;this.turnUp=turnUp;
            if(isActiveAndEnabled)Subscribe();
        }
        void OnEnable()=>Subscribe();
        void OnDisable()=>Unsubscribe();
        void Subscribe()
        {
            if(rig==null || settings!=null)return;
            settings=new LocomotionSettings(rig,preferenceKey);
            moveDown.onClick.AddListener(MoveDown);moveUp.onClick.AddListener(MoveUp);
            turnDown.onClick.AddListener(TurnDown);turnUp.onClick.AddListener(TurnUp);Refresh();
        }
        void Unsubscribe()
        {
            if(settings==null)return;
            moveDown.onClick.RemoveListener(MoveDown);moveUp.onClick.RemoveListener(MoveUp);
            turnDown.onClick.RemoveListener(TurnDown);turnUp.onClick.RemoveListener(TurnUp);settings=null;
        }
        void MoveDown()=>Change(-.1f,0);
        void MoveUp()=>Change(.1f,0);
        void TurnDown()=>Change(0,-15);
        void TurnUp()=>Change(0,15);
        void Change(float move,float turn)
        {
            settings.Set(rig.MoveSpeed+move,rig.TurnSpeed+turn);settings.Save();Refresh();
        }
        void Refresh()
        {
            moveLabel.text=$"이동 {rig.MoveSpeed:0.0} m/s";turnLabel.text=$"회전 {rig.TurnSpeed:0} °/s";
            moveDown.interactable=rig.MoveSpeed>1.001f;moveUp.interactable=rig.MoveSpeed<1.999f;
            turnDown.interactable=rig.TurnSpeed>30;turnUp.interactable=rig.TurnSpeed<90;
        }
    }
}
