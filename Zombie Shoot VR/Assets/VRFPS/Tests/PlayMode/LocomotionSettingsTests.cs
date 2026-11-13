using System;
using NUnit.Framework;
using UnityEngine;
using VRFPS.Player;
using UnityEngine.UI;
namespace VRFPS.Tests
{
    public class LocomotionSettingsTests
    {
        GameObject root; PlayerRigController rig; string key;
        [SetUp] public void SetUp()
        {
            key="VRFPS-Test-"+Guid.NewGuid().ToString("N");
            root=new GameObject("Settings rig");rig=root.AddComponent<PlayerRigController>();rig.enabled=false;
            var head=new GameObject("Head").transform;head.SetParent(root.transform);head.localPosition=Vector3.up*1.7f;rig.Head=head;
        }
        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            PlayerPrefs.DeleteKey(key+".Move");PlayerPrefs.DeleteKey(key+".Turn");PlayerPrefs.Save();
        }
        [Test] public void SelectedSpeedsDriveMovementAndReloadFromPreferences()
        {
            var settings=new LocomotionSettings(rig,key);settings.Set(2,90);settings.Save();
            rig.Tick(Vector2.up,Vector2.zero,.1f);
            Assert.That(root.transform.position.z,Is.EqualTo(.2f).Within(.001f));
            rig.Tick(Vector2.zero,Vector2.right,.1f);
            Assert.That(Quaternion.Angle(Quaternion.identity,root.transform.rotation),Is.EqualTo(9).Within(.01));
            rig.SetSpeeds(1,30);new LocomotionSettings(rig,key);
            Assert.That(rig.MoveSpeed,Is.EqualTo(2));Assert.That(rig.TurnSpeed,Is.EqualTo(90));
        }
        [Test] public void ValuesAreBoundedAndNonFiniteStoredValuesUseDefaults()
        {
            var settings=new LocomotionSettings(rig,key);settings.Set(-5,999);
            Assert.That(rig.MoveSpeed,Is.EqualTo(1));Assert.That(rig.TurnSpeed,Is.EqualTo(90));
            PlayerPrefs.SetFloat(key+".Move",float.NaN);PlayerPrefs.SetFloat(key+".Turn",float.PositiveInfinity);
            new LocomotionSettings(rig,key);
            Assert.That(rig.MoveSpeed,Is.EqualTo(1.6f));Assert.That(rig.TurnSpeed,Is.EqualTo(60));
        }
        [Test] public void SettingsButtonsApplyPersistAndDisableAtLimits()
        {
            T Child<T>() where T:Component
            {
                var o=new GameObject(typeof(T).Name,typeof(RectTransform));o.transform.SetParent(root.transform);return o.AddComponent<T>();
            }
            var move=Child<Text>();var turn=Child<Text>();
            var down=Child<Button>();var up=Child<Button>();var left=Child<Button>();var right=Child<Button>();
            root.AddComponent<SettingsView>().Configure(rig,move,turn,down,up,left,right,key);
            right.onClick.Invoke();right.onClick.Invoke();
            Assert.That(turn.text,Is.EqualTo("회전 90 °/s"));Assert.That(right.interactable,Is.False);
            down.onClick.Invoke();Assert.That(rig.MoveSpeed,Is.EqualTo(1.5f).Within(.001f));
            rig.SetSpeeds(1,30);new LocomotionSettings(rig,key);
            Assert.That(rig.TurnSpeed,Is.EqualTo(90));Assert.That(rig.MoveSpeed,Is.EqualTo(1.5f).Within(.001f));
        }    }
}
