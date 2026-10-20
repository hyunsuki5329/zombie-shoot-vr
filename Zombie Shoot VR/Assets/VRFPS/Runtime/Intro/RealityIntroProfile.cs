using System;
using UnityEngine;
namespace VRFPS
{
 [CreateAssetMenu(menuName="VRFPS/Reality Intro Profile")]
 public sealed class RealityIntroProfile : ScriptableObject
 {
  public string mapId,realityScenePath,realityAnchorKey,themeAnchorKey,dataId;
  public float metersPerUnit=1,fadeOutSeconds=1.2f,minBlackSeconds=.25f,fadeInSeconds=1.2f,loadTimeoutSeconds=30;
  public void Validate()
  {
   if(string.IsNullOrWhiteSpace(mapId)||string.IsNullOrWhiteSpace(realityScenePath)||
      string.IsNullOrWhiteSpace(realityAnchorKey)||string.IsNullOrWhiteSpace(themeAnchorKey)||
      string.IsNullOrWhiteSpace(dataId))
    throw new ArgumentException("Reality data, scene, map and anchor identifiers are required.");
   if(!Positive(metersPerUnit)||!Positive(fadeOutSeconds)||!Positive(fadeInSeconds)||
      !Positive(minBlackSeconds)||!Positive(loadTimeoutSeconds))
    throw new ArgumentException("Reality scale and transition timings must be finite and positive.");
  }
  public void ValidateFor(MapDefinition map)
  {
   Validate();
   if(map==null||map.mapId!=mapId)throw new ArgumentException("Reality profile does not match the selected map.");
  }
  static bool Positive(float value)=>value>0&&!float.IsNaN(value)&&!float.IsInfinity(value);
 }
}
