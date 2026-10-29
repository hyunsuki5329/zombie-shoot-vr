using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace VRFPS.Tests
{
    public class RealitySceneRootTests
    {
        Scene scene;RealitySceneRoot root;RealityIntroProfile profile;
        Transform Child(string name){var go=new GameObject(name);go.transform.SetParent(root.transform,false);return go.transform;}
        [SetUp] public void Setup()
        {
            scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("Reality root");SceneManager.MoveGameObjectToScene(go,scene);root=go.AddComponent<RealitySceneRoot>();
            profile=ScriptableObject.CreateInstance<RealityIntroProfile>();profile.mapId="room";profile.dataId="scan-hash";
            profile.realityScenePath="test";profile.realityAnchorKey="observation";profile.themeAnchorKey="playerSpawn";
            root.mapId=profile.mapId;root.dataId=profile.dataId;root.dataRoot=Child("Data");root.observationAnchor=Child("observation");root.collisionRoot=Child("Collision");
            var floor=root.collisionRoot.gameObject.AddComponent<BoxCollider>();floor.gameObject.layer=LayerMask.NameToLayer("WorldSolid");
            root.virus=Child("Virus").gameObject.AddComponent<IntroVirusTarget>();root.virus.gameObject.layer=LayerMask.NameToLayer("IntroTarget");root.virus.gameObject.AddComponent<SphereCollider>().radius=.32f;
        }
        [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(profile);EditorSceneManager.ClosePreviewScene(scene);}
        [Test] public void ValidOwnedSceneContractPasses(){Assert.DoesNotThrow(()=>root.Validate(profile));}
        [Test] public void WrongDataOrMapCannotBeUsed()
        {
            root.dataId="other";Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));root.dataId=profile.dataId;
            root.mapId="classroom";Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
        }
        [Test] public void MissingForeignInactiveOrAmbiguousAnchorsAreRejected()
        {
            var anchor=root.observationAnchor;root.observationAnchor=null;Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
            root.observationAnchor=anchor;anchor.gameObject.SetActive(false);Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));anchor.gameObject.SetActive(true);
            anchor.name="wrong";Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));anchor.name=profile.realityAnchorKey;
            Child(profile.realityAnchorKey);Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
        }
        [Test] public void ForeignOwnedReferenceIsRejected()
        {
            root.dataRoot.SetParent(null);Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
        }
        [Test] public void NonUniformOrDoubleAppliedScaleIsRejected()
        {
            root.dataRoot.localScale=new Vector3(1,2,1);Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
            root.dataRoot.localScale=Vector3.one*2;Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
            profile.metersPerUnit=2;Assert.DoesNotThrow(()=>root.Validate(profile));
            root.transform.localScale=Vector3.one*2;Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
        }
        [TestCase(0f,.0002f)][TestCase(.0001f,.0002f)][TestCase(-.0001f,.0001f)]
        public void SmallCalibrationRejectsCollapsedNonUniformAndNegativeAxes(float x,float y)
        {
            profile.metersPerUnit=.0001f;root.dataRoot.localScale=Vector3.one*.0001f;
            Assert.DoesNotThrow(()=>root.Validate(profile));
            root.dataRoot.localScale=new Vector3(x,y,.0001f);Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
        }
        [Test] public void WrongCollisionLayerTriggerAndMissingCollisionAreRejected()
        {
            var collider=root.collisionRoot.GetComponent<BoxCollider>();collider.isTrigger=true;Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
            collider.isTrigger=false;collider.gameObject.layer=0;Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
            UnityEngine.Object.DestroyImmediate(collider);Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
        }
        [Test] public void VirusMustOwnActiveNonTriggerIntroCollider()
        {
            var collider=root.virus.GetComponent<Collider>();collider.isTrigger=true;Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
            collider.isTrigger=false;root.virus.gameObject.layer=0;Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
            root.virus.gameObject.layer=LayerMask.NameToLayer("IntroTarget");root.virus.enabled=false;Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
        }
        [Test] public void ExtraInactiveCameraOrListenerAnywhereInSceneIsRejected()
        {
            var extra=new GameObject("Unexpected camera");SceneManager.MoveGameObjectToScene(extra,scene);var camera=extra.AddComponent<Camera>();extra.SetActive(false);
            Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));UnityEngine.Object.DestroyImmediate(camera);
            extra.AddComponent<AudioListener>();Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));
        }
        [Test] public void DuplicateRootIsRejected(){Child("duplicate").gameObject.AddComponent<RealitySceneRoot>();Assert.Throws<InvalidOperationException>(()=>root.Validate(profile));}
    }
}
