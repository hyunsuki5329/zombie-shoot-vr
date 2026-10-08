using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
namespace VRFPS.Editor {
 public static class EnemyVariantSetup {
  const string Root="Assets/VRFPS/External/Mixamo/";
  const string Prefabs="Assets/VRFPS/Prefabs/Enemies/";
  static AnimationClip Motion(string file,bool loop) {
   var path=Root+file+".fbx";var imp=AssetImporter.GetAtPath(path) as ModelImporter;
   if(imp==null)throw new InvalidOperationException(path);
   imp.animationType=ModelImporterAnimationType.Human;imp.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
   imp.sourceAvatar=AssetDatabase.LoadAllAssetsAtPath(Root+"Skeletonzombie T Avelange.fbx").OfType<Avatar>().First();imp.SaveAndReimport();
   var defs=imp.defaultClipAnimations;
   foreach(var d in defs){d.loopTime=loop;d.lockRootRotation=true;d.keepOriginalOrientation=true;d.lockRootPositionXZ=false;d.lockRootHeightY=false;d.heightFromFeet=true;}
   imp.clipAnimations=defs;imp.SaveAndReimport();
   var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
   if(!clip.humanMotion)throw new InvalidOperationException("Not humanoid: "+path);return clip;
  }
  public static void Configure() {
   if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch project");
   foreach(var f in new[]{"Skeletonzombie T Avelange","Zombiegirl W Kurniawan","Skeletonzombie T Avelange@Zombie Crawl","Skeletonzombie T Avelange@Zombie Biting"})
    if(!File.Exists(Root+f+".fbx"))throw new FileNotFoundException(f);
   AssetDatabase.Refresh();
   var model="Skeletonzombie T Avelange";
   foreach(var modelName in new[]{model,"Zombiegirl W Kurniawan"}) {
    var imp=(ModelImporter)AssetImporter.GetAtPath(Root+modelName+".fbx");
    imp.animationType=ModelImporterAnimationType.Human;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;imp.SaveAndReimport();
    Directory.CreateDirectory(Root+modelName+"-Textures");imp.ExtractTextures(Root+modelName+"-Textures");AssetDatabase.Refresh();imp.SaveAndReimport();
   }
   var clips=new Dictionary<string,AnimationClip>();
   foreach(var n in new[]{"zombie idle","zombie walk","zombie attack","zombie death","zombie scream","zombie reaction hit"})
    clips[n]=AssetDatabase.LoadAllAssetsAtPath(Root+"Motions/"+n+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
   var crawl=Motion(model+"@Zombie Crawl",true);var bite=Motion(model+"@Zombie Biting",false);
   foreach(var name in new[]{"BossZombie","CrawlerZombie"}) {
    if(!File.Exists(Prefabs+name+".prefab"))AssetDatabase.CopyAsset(Prefabs+"MeleeZombie.prefab",Prefabs+name+".prefab");
    MixamoVisualSetup.Apply(name,name=="BossZombie"?model:"Zombiegirl W Kurniawan",false,clips);
    var root=PrefabUtility.LoadPrefabContents(Prefabs+name+".prefab");
    try {
     var boss=name=="BossZombie";var actor=new SerializedObject(root.GetComponent<EnemyActor>());
     actor.FindProperty("variant").enumValueIndex=boss?2:1;actor.FindProperty("crawlerPrefab").objectReferenceValue=null;actor.FindProperty("bossPrefab").objectReferenceValue=null;actor.ApplyModifiedPropertiesWithoutUndo();
     var agent=root.GetComponent<NavMeshAgent>();agent.height=boss?2.2f:.8f;agent.radius=boss?.45f:.4f;
     var hit=root.GetComponent<CapsuleCollider>();hit.direction=boss?1:2;hit.height=boss?2.2f:1.5f;hit.radius=boss?.4f:.35f;hit.center=boss?Vector3.up*1.1f:new Vector3(0,.4f,0);
     var origin=(Transform)actor.FindProperty("attackOrigin").objectReferenceValue;origin.localPosition=new Vector3(0,boss?1.5f:.65f,.4f);
     var animator=root.GetComponentInChildren<Animator>();
     if(!boss && root.GetComponent<GroundedEnemyVisual>()==null)root.AddComponent<GroundedEnemyVisual>();
     var head=animator.GetBoneTransform(HumanBodyBones.Head);
     var hitHead=new GameObject("Variant Head Hitbox");hitHead.layer=root.layer;hitHead.transform.SetParent(head,false);
     hitHead.AddComponent<SphereCollider>().radius=boss?.2f:.16f;
     var controller=(AnimatorController)animator.runtimeAnimatorController;
     var sm=controller.layers[0].stateMachine;
     if(boss){root.transform.Find("Mixamo Visual").localScale=Vector3.one*1.15f;}
     else {
      while(controller.layers.Length>1)controller.RemoveLayer(controller.layers.Length-1);
      foreach(var state in sm.states) {
       if(state.state.name=="Walk"){state.state.motion=crawl;state.state.speed=2.2f;}
       if(state.state.name=="Idle"){state.state.motion=crawl;state.state.speed=0;}
       if(state.state.name=="Attack")state.state.motion=bite;
       // Fallback only: CrawlerDeathMotion owns the actual current-pose collapse.
       if(state.state.name=="Dead"){state.state.motion=crawl;state.state.speed=0;}
      }
     }
     var attack=sm.states.First(x=>x.state.name=="Attack").state;
     attack.speed=((AnimationClip)attack.motion).length*.4f/GameBalance.AttackWindup(EnemyKind.Melee,boss?EnemyVariant.Boss:EnemyVariant.Crawler);
     if(!boss)CrawlerDeathSetup.Apply(root);
     EditorUtility.SetDirty(controller);PrefabUtility.SaveAsPrefabAsset(root,Prefabs+name+".prefab");
    } finally {PrefabUtility.UnloadPrefabContents(root);}
   }
   var melee=PrefabUtility.LoadPrefabContents(Prefabs+"MeleeZombie.prefab");
   try {
    var so=new SerializedObject(melee.GetComponent<EnemyActor>());
    so.FindProperty("crawlerPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"CrawlerZombie.prefab").GetComponent<EnemyActor>();
    so.FindProperty("bossPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"BossZombie.prefab").GetComponent<EnemyActor>();
    so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(melee,Prefabs+"MeleeZombie.prefab");
   } finally {PrefabUtility.UnloadPrefabContents(melee);}
   AssetDatabase.SaveAssets();Debug.Log("VRFPS_ENEMY_VARIANTS_CONFIGURED");
  }
 }
}
