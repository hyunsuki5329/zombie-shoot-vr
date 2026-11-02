using System;
using NUnit.Framework;
using UnityEngine;

namespace VRFPS.Tests
{
    public class RealityIntroAssetTests
    {
        RealityIntroProfile profile;
        [SetUp] public void Setup()
        {
            var type=typeof(MapDefinition).Assembly.GetType("VRFPS.RealityIntroProfile");
            Assert.That(type,Is.Not.Null,"Reality intro profile is not implemented.");
            profile=(RealityIntroProfile)ScriptableObject.CreateInstance(type);
            profile.mapId="room";profile.realityScenePath="Assets/VRFPS/Scenes/Reality/RoomReality.unity";
            profile.realityAnchorKey="observation";profile.themeAnchorKey="playerSpawn";
            profile.dataId="sha256:test";
        }
        [TearDown] public void Cleanup(){if(profile!=null)UnityEngine.Object.DestroyImmediate(profile);}
        [Test] public void ValidDefaultsPreserveTransitionContract()
        {
            Assert.DoesNotThrow(()=>profile.Validate());
            Assert.That(profile.fadeOutSeconds,Is.EqualTo(1.2f));
            Assert.That(profile.fadeInSeconds,Is.EqualTo(1.2f));
            Assert.That(profile.minBlackSeconds,Is.EqualTo(.25f));
            Assert.That(profile.loadTimeoutSeconds,Is.EqualTo(30f));
        }
        [Test] public void MissingDataOrAnchorsCannotBeReady()
        {
            profile.dataId="";Assert.Throws<ArgumentException>(()=>profile.Validate());
            profile.dataId="hash";profile.realityAnchorKey="";
            Assert.Throws<ArgumentException>(()=>profile.Validate());
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(0f)][TestCase(-1f)]
        public void InvalidScaleIsRejected(float value)
        {
            profile.metersPerUnit=value;Assert.Throws<ArgumentException>(()=>profile.Validate());
        }
        [Test] public void DifferentMapIsRejected()
        {
            var map=ScriptableObject.CreateInstance<MapDefinition>();map.mapId="classroom";
            try{Assert.Throws<ArgumentException>(()=>profile.ValidateFor(map));}
            finally{UnityEngine.Object.DestroyImmediate(map);}
        }
        [Test] public void InvalidTimingIsRejected()
        {
            profile.fadeInSeconds=float.NaN;Assert.Throws<ArgumentException>(()=>profile.Validate());
            profile.fadeInSeconds=1.2f;profile.loadTimeoutSeconds=0;
            Assert.Throws<ArgumentException>(()=>profile.Validate());
        }
    }
}
