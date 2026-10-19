using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
 public class RoomGameplayScaleTests
 {
  static RoomStructureDefinition Source()
  {
   var d=ScriptableObject.CreateInstance<RoomStructureDefinition>();d.measuredRoomSize=new Vector3(4.3f,2.45f,3.5f);
   d.playerSpawn=new Vector3(.9f,0,1.1f);d.playerEuler=new Vector3(0,180,0);d.enemySpawns=new[]{new Vector3(.65f,0,3.7f)};
   d.elements=new[]{new StructureElement{id="Desk",kind=StructureKind.Desk,localCenter=new Vector3(-1.41f,.42f,.05f),localEuler=new Vector3(0,90,0),size=new Vector3(1.48f,.84f,1.22f),collision=StructureCollision.Solid,navigation=StructureNavigation.BlockingGeometry}};return d;
  }
  static RoomStructureDefinition Scale(RoomStructureDefinition source,float factor)
  {
   var method=typeof(RoomStructureDefinition).GetMethod("CreateGameplayCopy");Assert.That(method,Is.Not.Null,"Gameplay scaling needs an independent copy and absolute scale");
   return (RoomStructureDefinition)method.Invoke(source,new object[]{factor});
  }
  [Test] public void UniformScalePreservesLayoutAndMeasurementsWithoutCompounding()
  {
   var source=Source();RoomStructureDefinition scaled=null,again=null;
   try
   {
    scaled=Scale(source,2.5f);scaled.Validate();again=Scale(scaled,2.5f);
    Assert.That(scaled.measuredRoomSize,Is.EqualTo(source.measuredRoomSize),"Measured original dimensions remain provenance");
    Assert.That(scaled.elements[0].localCenter,Is.EqualTo(source.elements[0].localCenter*2.5f));
    Assert.That(scaled.elements[0].size,Is.EqualTo(source.elements[0].size*2.5f));
    Assert.That(scaled.elements[0].localEuler,Is.EqualTo(source.elements[0].localEuler));
    Assert.That(scaled.playerSpawn,Is.EqualTo(new Vector3(2.25f,0,2.75f)));
    Assert.That(scaled.enemySpawns[0],Is.EqualTo(source.enemySpawns[0]*2.5f));
    Assert.That(scaled.playerEuler,Is.EqualTo(source.playerEuler));
    Assert.That(again.elements[0].size,Is.EqualTo(scaled.elements[0].size));Assert.That(again.playerSpawn,Is.EqualTo(scaled.playerSpawn));
    scaled.elements[0].localCenter=Vector3.zero;scaled.enemySpawns[0]=Vector3.zero;
    Assert.That(source.elements[0].localCenter,Is.EqualTo(new Vector3(-1.41f,.42f,.05f)));Assert.That(source.enemySpawns[0],Is.EqualTo(new Vector3(.65f,0,3.7f)));
   }
   finally{if(again!=null)UnityEngine.Object.DestroyImmediate(again);if(scaled!=null)UnityEngine.Object.DestroyImmediate(scaled);UnityEngine.Object.DestroyImmediate(source);}
  }
  [TestCase(0f)][TestCase(-1f)][TestCase(float.NaN)][TestCase(float.PositiveInfinity)]
  public void InvalidScaleIsRejected(float factor)
  {
   var source=Source();try{var method=typeof(RoomStructureDefinition).GetMethod("CreateGameplayCopy");Assert.That(method,Is.Not.Null);var ex=Assert.Throws<TargetInvocationException>(()=>method.Invoke(source,new object[]{factor}));Assert.That(ex.InnerException,Is.TypeOf<ArgumentOutOfRangeException>());}
   finally{UnityEngine.Object.DestroyImmediate(source);}
  }
 }
}
