using System;
using UnityEngine;
using VRFPS.Player;
namespace VRFPS
{
    public sealed class LocomotionSettings
    {
        readonly PlayerRigController rig;
        readonly string keyPrefix;
        public LocomotionSettings(PlayerRigController rig, string keyPrefix = "VRFPS.Settings")
        {
            this.rig=rig!=null ? rig : throw new ArgumentNullException(nameof(rig));
            if(string.IsNullOrWhiteSpace(keyPrefix))throw new ArgumentException("Preference key is required.",nameof(keyPrefix));
            this.keyPrefix=keyPrefix;
            rig.SetSpeeds(PlayerPrefs.GetFloat(keyPrefix+".Move",1.6f),PlayerPrefs.GetFloat(keyPrefix+".Turn",60));
        }
        public void Set(float move, float turn) => rig.SetSpeeds(move,turn);
        public void Save()
        {
            PlayerPrefs.SetFloat(keyPrefix+".Move",rig.MoveSpeed);
            PlayerPrefs.SetFloat(keyPrefix+".Turn",rig.TurnSpeed);
            PlayerPrefs.Save();
        }
    }
}
