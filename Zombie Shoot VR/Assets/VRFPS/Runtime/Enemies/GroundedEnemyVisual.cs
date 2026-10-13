using UnityEngine;
namespace VRFPS {
 // Retargeted prone motions use different leg proportions. Align contact bones to
 // the actor's NavMesh floor after animation, without moving the navigation root.
 public sealed class GroundedEnemyVisual : MonoBehaviour {
  Animator animator;
  Transform visual;
  Transform[] contacts;
  void Awake()=>Cache();
  void Cache(){
   if(animator!=null)return;
   animator=GetComponentInChildren<Animator>();if(animator==null || !animator.isHuman)return;
   visual=animator.transform;
   contacts=new[]{animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot),animator.GetBoneTransform(HumanBodyBones.LeftToes),animator.GetBoneTransform(HumanBodyBones.RightToes),animator.GetBoneTransform(HumanBodyBones.LeftHand),animator.GetBoneTransform(HumanBodyBones.RightHand)};
  }
  void LateUpdate()=>AlignPose();
  public void AlignPose(){
   Cache();if(visual==null || contacts==null)return;
   float lowest=float.PositiveInfinity;
   foreach(var bone in contacts)if(bone!=null)lowest=Mathf.Min(lowest,transform.InverseTransformPoint(bone.position).y);
   if(float.IsInfinity(lowest))return;
   var position=visual.localPosition;position.y+=.08f-lowest;visual.localPosition=position;
  }
 }
}
