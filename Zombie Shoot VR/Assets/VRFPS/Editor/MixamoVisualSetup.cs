using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace VRFPS.Editor
{
 public static class MixamoVisualSetup
 {
  const string Root="Assets/VRFPS/External/Mixamo/";
  public static void Configure()
  {
   MixamoModelPreparation.Prepare();
   Directory.CreateDirectory(Root+"Generated"); AssetDatabase.Refresh();
   var clips=new System.Collections.Generic.Dictionary<string,AnimationClip>();
   foreach(var n in new[]{"zombie idle","zombie walk","zombie attack","zombie death","zombie scream","zombie reaction hit"})
   {
    var path=Root+"Motions/"+n+".fbx";
    var imp=AssetImporter.GetAtPath(path) as ModelImporter;
    if(imp==null) throw new InvalidOperationException("Missing motion: "+path);
    imp.animationType=ModelImporterAnimationType.Human; imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
    imp.SaveAndReimport();
    var defs=imp.defaultClipAnimations;
    // Extract planar motion instead of baking travel into the skeleton. NavMesh owns movement.
    foreach(var d in defs){d.loopTime=n=="zombie idle"||n=="zombie walk";d.lockRootRotation=true;d.lockRootPositionXZ=false;d.lockRootHeightY=true;}
    imp.clipAnimations=defs; imp.SaveAndReimport();
    var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
    if(!clip.humanMotion||clip.length<.1f)throw new InvalidOperationException("Invalid motion: "+n);
    clips.Add(n,clip);
   }
   Apply("MeleeZombie","Yaku J Ignite",false,clips);
   Apply("RangedZombie","Parasite L Starkie",true,clips);
   AssetDatabase.SaveAssets();Debug.Log("VRFPS_MIXAMO_VISUALS_CONFIGURED");
  }
  static Material Mat(string model,string name,string diffuse,string normal)
  {
   var folder=Root+model+"-Textures/";
   var normalPath=normal==null?null:folder+normal;
   if(normalPath!=null){
    var ti=AssetImporter.GetAtPath(normalPath) as TextureImporter;
    if(ti==null)throw new InvalidOperationException(normalPath);
    ti.textureType=TextureImporterType.NormalMap;ti.maxTextureSize=2048;ti.SaveAndReimport();
   }
   var colorPath=folder+diffuse;var ci=(TextureImporter)AssetImporter.GetAtPath(colorPath);
   ci.sRGBTexture=true;ci.maxTextureSize=2048;
   ci.alphaSource=TextureImporterAlphaSource.FromInput;ci.alphaIsTransparency=true;ci.SaveAndReimport();
   var path=Root+"Generated/"+name+".mat";
   var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath));m.SetColor("_BaseColor",Color.white);
   m.SetTexture("_BumpMap",normalPath==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
   if(normalPath==null)m.DisableKeyword("_NORMALMAP");else m.EnableKeyword("_NORMALMAP");
   var cutout=ci.DoesSourceTextureHaveAlpha();
   m.SetFloat("_AlphaClip",cutout?1:0);m.SetFloat("_Cutoff",.5f);
   if(cutout)m.EnableKeyword("_ALPHATEST_ON");else m.DisableKeyword("_ALPHATEST_ON");
   m.SetOverrideTag("RenderType",cutout?"TransparentCutout":"Opaque");m.renderQueue=cutout?2450:2000;
   m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.25f);EditorUtility.SetDirty(m);return m;
  }
  static AnimatorController Controller(string name,bool ranged,System.Collections.Generic.Dictionary<string,AnimationClip> clips)
  {
   var path=Root+"Generated/"+name+".controller";
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
   if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
   // Rebuild this generated state machine in place, retaining its asset GUID.
   var sm=controller.layers[0].stateMachine;
   foreach(var t in sm.anyStateTransitions)sm.RemoveAnyStateTransition(t);
   foreach(var s in sm.states)sm.RemoveState(s.state);
   controller.parameters=new AnimatorControllerParameter[0];
   controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
   controller.AddParameter("Attack",AnimatorControllerParameterType.Bool);
   controller.AddParameter("Dead",AnimatorControllerParameterType.Bool);
   var idle=sm.AddState("Idle");idle.motion=clips["zombie idle"];sm.defaultState=idle;
   var walk=sm.AddState("Walk");walk.motion=clips["zombie walk"];
   var attack=sm.AddState("Attack");attack.motion=clips[ranged?"zombie scream":"zombie attack"];
   // Contact/roar pose near 40% meets the existing windup; allow the recovery to finish.
   // Damage/projectile authority remains in ZombieController, never animation events.
   attack.speed=((AnimationClip)attack.motion).length*.4f/(ranged?.8f:.65f);
   var dead=sm.AddState("Dead");dead.motion=clips["zombie death"];
   var death=sm.AddAnyStateTransition(dead);death.hasExitTime=false;death.duration=.05f;death.canTransitionToSelf=false;death.AddCondition(AnimatorConditionMode.If,0,"Dead");
   var hit=sm.AddAnyStateTransition(attack);hit.hasExitTime=false;hit.duration=0;hit.canTransitionToSelf=false;hit.AddCondition(AnimatorConditionMode.If,0,"Attack");hit.AddCondition(AnimatorConditionMode.IfNot,0,"Dead");
   var finish=attack.AddTransition(idle);finish.hasExitTime=true;finish.exitTime=.8f;finish.hasFixedDuration=true;finish.duration=.05f;finish.AddCondition(AnimatorConditionMode.IfNot,0,"Attack");finish.AddCondition(AnimatorConditionMode.IfNot,0,"Dead");
   var move=idle.AddTransition(walk);move.hasExitTime=false;move.duration=.15f;move.AddCondition(AnimatorConditionMode.Greater,.05f,"Speed");
   var stop=walk.AddTransition(idle);stop.hasExitTime=false;stop.duration=.15f;stop.AddCondition(AnimatorConditionMode.Less,.05f,"Speed");
   while(controller.layers.Length>1)controller.RemoveLayer(controller.layers.Length-1);
   var maskPath=Root+"Generated/HitUpperBody.mask";
   var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
   if(mask==null){mask=new AvatarMask();AssetDatabase.CreateAsset(mask,maskPath);}
   for(var i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
   foreach(var part in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})mask.SetHumanoidBodyPartActive(part,true);
   EditorUtility.SetDirty(mask);
   var reaction=new AnimatorStateMachine{name="Hit Reaction"};AssetDatabase.AddObjectToAsset(reaction,controller);
   var flinch=reaction.AddState("Hit");flinch.motion=clips["zombie reaction hit"];flinch.speed=clips["zombie reaction hit"].length/.4f;reaction.defaultState=flinch;
   controller.AddLayer(new AnimatorControllerLayer{name="Hit Reaction",stateMachine=reaction,avatarMask=mask,defaultWeight=0,blendingMode=AnimatorLayerBlendingMode.Override});
   EditorUtility.SetDirty(controller);return controller;
  }
  static void Apply(string prefab,string model,bool ranged,System.Collections.Generic.Dictionary<string,AnimationClip> clips)
  {
   var path="Assets/VRFPS/Prefabs/Enemies/"+prefab+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
   try{
    foreach(Transform child in root.transform.Cast<Transform>().ToArray())
     if(child.name=="Mixamo Visual"||child.name.StartsWith("Mixamo Visual (Missing Prefab",StringComparison.Ordinal)) UnityEngine.Object.DestroyImmediate(child.gameObject);
    var old=root.transform.Find("Visual");if(old!=null){old.gameObject.SetActive(false);foreach(var a in old.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);}
    var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+model+".fbx"),root.transform);
    visual.name="Mixamo Visual";visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
    foreach(var t in visual.GetComponentsInChildren<Transform>(true))t.gameObject.layer=root.layer;
    var animator=visual.GetComponent<Animator>();animator.applyRootMotion=false;animator.runtimeAnimatorController=Controller(prefab,ranged,clips);
    var renderer=visual.GetComponentInChildren<SkinnedMeshRenderer>();
    // Use authored RGBA atlases for every slot; embedded RGB copies flatten torn clothing to white.
    renderer.sharedMaterials=ranged?new[]{Mat(model,"ParasiteBody","parasiteZombie_diffuse.png",null),Mat(model,"Parasite","parasiteZombie_diffuse.png","parasiteZombie_normal.png")}:new[]{Mat(model,"Yaku","Yakuzombie_diffuse.png","Yakuzombie_normal.png"),Mat(model,"YakuClothes","Yakuzombie_diffuse.png",null)};
    PrefabUtility.SaveAsPrefabAsset(root,path);
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
 }
}
