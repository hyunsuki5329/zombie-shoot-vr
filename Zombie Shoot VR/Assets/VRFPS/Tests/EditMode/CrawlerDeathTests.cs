using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace VRFPS.Tests {
 public class CrawlerDeathTests {
  [Test] public void CrawlerHasAuthoredDeathPose() {
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/Prefabs/Enemies/CrawlerZombie.prefab");
   Assert.That(prefab.GetComponent("CrawlerDeathMotion"),Is.Not.Null,"Crawler needs a moving death transition, not a frozen crawl pose");
  }
 }
}
