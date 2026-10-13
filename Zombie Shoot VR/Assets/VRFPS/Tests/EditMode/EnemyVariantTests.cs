using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace VRFPS.Tests {
 public sealed class EnemyVariantTests {
  [Test] public void FifthWaveBossAndPostFifthWaveCrawlerSelection() {
   var method=typeof(GameBalance).GetMethod("MeleeVariantForWave");
   Assert.That(method,Is.Not.Null,"Missing wave variant selection");
   Func<int,int,string> get=(wave,index)=>method.Invoke(null,new object[]{wave,index}).ToString();
   Assert.That(get(4,0),Is.EqualTo("Normal"));
   Assert.That(get(5,0),Is.EqualTo("Boss"));
   Assert.That(get(5,1),Is.EqualTo("Normal"));
   Assert.That(get(6,0),Is.EqualTo("Crawler"));
   Assert.That(get(6,1),Is.EqualTo("Normal"));
   Assert.That(get(10,0),Is.EqualTo("Boss"));
   Assert.That(get(10,3),Is.EqualTo("Crawler"));
   for(int wave=1;wave<=100;wave++) {
    int bosses=0,crawlers=0;int count=GameBalance.QuotaForWave(wave)-GameBalance.RangedQuotaForWave(wave);
    for(int i=0;i<count;i++){var v=get(wave,i);if(v=="Boss")bosses++;if(v=="Crawler")crawlers++;}
    Assert.That(bosses,Is.EqualTo(wave%5==0?1:0));
    Assert.That(crawlers>0,Is.EqualTo(wave>5));
   }
  }
  [Test] public void CrawlerFeetStayGroundedAcrossMovingAndAttackPoses() {
   var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/Enemies/CrawlerZombie.prefab"));
   try {
    var ground=instance.GetComponent<GroundedEnemyVisual>();Assert.That(ground,Is.Not.Null);
    var animator=instance.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
    foreach(var state in new[]{"Walk","Attack","Dead"})foreach(var t in new[]{.1f,.3f,.5f,.7f,.9f}) {
     animator.SetFloat("Speed",state=="Walk"?1:0);animator.SetBool("Attack",state=="Attack");
     animator.Play("Base Layer."+state,0,t);animator.Update(0);ground.AlignPose();
     var low=Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
     Assert.That(low,Is.GreaterThanOrEqualTo(.03f),state+" feet below floor");
     if(state!="Attack")Assert.That(animator.GetBoneTransform(HumanBodyBones.Head).position.y,Is.LessThan(.8f),"Crawl must stay prone, not float diagonally");
    }
   }finally{UnityEngine.Object.DestroyImmediate(instance);}
  }
  [TestCase("BossZombie")][TestCase("CrawlerZombie")]
  public void VariantPrefabHasHumanoidVisual(string name) {
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/Enemies/"+name+".prefab");
   Assert.That(prefab,Is.Not.Null,"Missing variant prefab");
   var animator=prefab.GetComponentInChildren<Animator>();
   Assert.That(animator,Is.Not.Null);Assert.That(animator.avatar.isHuman,Is.True);
   Assert.That(animator.runtimeAnimatorController,Is.Not.Null);
   var source=PrefabUtility.GetCorrespondingObjectFromSource(animator.gameObject);
   StringAssert.Contains(name=="BossZombie"?"Skeletonzombie T Avelange":"Zombiegirl W Kurniawan",AssetDatabase.GetAssetPath(source));
   foreach(var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())foreach(var mat in skin.sharedMaterials) {
    Assert.That(mat.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(mat.GetTexture("_BaseMap"),Is.Not.Null);
   }
  }
 }
}
