using UnityEngine;
namespace VRFPS.Player {
 [DefaultExecutionOrder(-10)]
 public sealed class ViewHeightAdjustment:MonoBehaviour {
  [SerializeField] Transform floorOffset,viewOffset,head;
  [SerializeField] float heightMultiplier=1.15f;
  public void Configure(Transform floor,Transform view,Transform trackedHead){floorOffset=floor;viewOffset=view;head=trackedHead;}
  void OnEnable(){Application.onBeforeRender+=Apply;}
  void OnDisable(){Application.onBeforeRender-=Apply;if(viewOffset!=null)viewOffset.localPosition=Vector3.zero;}
  void Update()=>Apply();
  public void Apply(){
   if(floorOffset==null||viewOffset==null||head==null)return;
   // Read the raw tracked pose, not world height: jump, crouch and repeated frames cannot accumulate an offset.
   float height=Mathf.Max(0,floorOffset.localPosition.y+head.localPosition.y);
   viewOffset.localPosition=Vector3.up*(height*(heightMultiplier-1f));
  }
 }
}
