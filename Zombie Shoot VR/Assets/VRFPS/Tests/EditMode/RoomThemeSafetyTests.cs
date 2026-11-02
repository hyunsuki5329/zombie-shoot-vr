using System;
using System.Linq;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
 public class RoomThemeSafetyTests
 {
  static Type Tool(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("VRFPS.Editor."+n)).FirstOrDefault(t=>t!=null);
  [TestCase("collision","position")][TestCase("collision","rotation")][TestCase("collision","scale")]
  [TestCase("visual","parent")][TestCase("collision","parent")]
  public void TransformedRootIsRejectedBeforeExistingVisualChanges(string root,string change)
  {
   var data=ScriptableObject.CreateInstance<RoomStructureDefinition>();data.measuredRoomSize=new Vector3(4.3f,2.45f,3.5f);data.enemySpawns=new[]{Vector3.forward*3};
   data.elements=new[]{new StructureElement{id="Floor",kind=StructureKind.Floor,size=new Vector3(4.3f,.2f,3.5f),collision=StructureCollision.Solid,navigation=StructureNavigation.WalkableSurface}};
   var profile=ScriptableObject.CreateInstance<EnvironmentThemeProfile>();var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));
   profile.plaster=profile.wood=profile.woodDark=profile.fabric=profile.metal=mat;
   var map=(MapRoot)Tool("RoomStructureSetup").GetMethod("GenerateStructure").Invoke(null,new object[]{data});
   try
   {
    var method=Tool("RoomThemeGenerator").GetMethod("Generate");method.Invoke(null,new object[]{map,data,profile});
    var old=map.visualRoot.Find("ThemeGenerated");var target=root=="collision"?map.collisionRoot:map.visualRoot;
    if(change=="position")target.localPosition=Vector3.right;
    if(change=="rotation")target.localRotation=Quaternion.Euler(0,90,0);
    if(change=="scale")target.localScale=Vector3.one*2;
    if(change=="parent"){var parent=new GameObject("Moved intermediate parent");parent.transform.SetParent(map.transform,false);parent.transform.localPosition=Vector3.right;target.SetParent(parent.transform,false);}
    var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{map,data,profile}));
    Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(map.visualRoot.Find("ThemeGenerated"),Is.EqualTo(old));
   }
   finally{UnityEngine.Object.DestroyImmediate(map.gameObject);UnityEngine.Object.DestroyImmediate(data);UnityEngine.Object.DestroyImmediate(profile);UnityEngine.Object.DestroyImmediate(mat);}
  }
  [Test] public void ImpossibleBackupDestinationFailsWithoutChangingOriginalScene()
  {
   const string source="Assets/VRFPS/Scenes/Maps/Room.unity";
   var before=File.ReadAllBytes(source);var method=Tool("RoomHouseSetup").GetMethod("EnsureReferenceBackup");Assert.That(method,Is.Not.Null,"Backup must be a checked operation before promotion");
   var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{source,source+"/Reference.unity"}));
   Assert.That(error.InnerException,Is.TypeOf<IOException>());CollectionAssert.AreEqual(before,File.ReadAllBytes(source));
  }
 }
}
