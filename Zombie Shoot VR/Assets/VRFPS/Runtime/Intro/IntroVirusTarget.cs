using System;
using UnityEngine;
namespace VRFPS
{
 public sealed class IntroVirusTarget : MonoBehaviour, IDamageable
 {
  public event Action Triggered;
  int gunId;
  Func<bool> canTrigger;
  bool consumed;
  public void Configure(int playerGunId,Func<bool> canTrigger)
  {
   this.canTrigger=canTrigger??throw new ArgumentNullException(nameof(canTrigger));
   gunId=playerGunId;ResetTarget();
  }
  public void ResetTarget()=>consumed=false;
  public bool ApplyDamage(int amount,int sourceId,double activeTime)
  {
   if(!isActiveAndEnabled||consumed||amount<=0||sourceId!=gunId||canTrigger==null||
      double.IsNaN(activeTime)||double.IsInfinity(activeTime)||activeTime<0||!canTrigger())return false;
   // Consume before invoking subscribers: repeated/reentrant hits cannot start another transition.
   consumed=true;Triggered?.Invoke();return true;
  }
 }
}
