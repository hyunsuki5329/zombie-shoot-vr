using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace VRFPS.Tests
{
    public class IntroVirusAssetTests
    {
        [Test] public void LocalVirusHasExplicitMaterialsScaleAndNonTriggerHitShape()
        {
            if(!File.Exists(System.Environment.GetEnvironmentVariable("VRFPS_VIRUS_ARCHIVE") ?? "C:/Users/User/Downloads/coronavirus-virus-model.zip"))
                Assert.Ignore("Requires locally supplied virus archive; not bundled with source.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRFPS/External/IntroVirus/IntroVirus.prefab");
            Assert.That(prefab,Is.Not.Null,"Run VRFPS.Editor.IntroVirusSetup.Import in the isolated project.");
            var instance=Object.Instantiate(prefab);
            try
            {
                var renderers=instance.GetComponentsInChildren<MeshRenderer>();Assert.That(renderers.Length,Is.GreaterThan(0));
                var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                Assert.That(Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z),Is.EqualTo(.8f).Within(.001f));
                Assert.That(bounds.center.magnitude,Is.LessThan(.001f));
                foreach(var r in renderers)foreach(var m in r.sharedMaterials)
                {
                    Assert.That(m.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
                    Assert.That(m.GetTexture("_BaseMap"),Is.Not.Null);Assert.That(m.GetTexture("_BumpMap"),Is.Not.Null);
                    Assert.That(m.GetTexture("_MetallicGlossMap"),Is.Not.Null);
                }
                Assert.That(instance.layer,Is.EqualTo(LayerMask.NameToLayer("IntroTarget")));
                var target=instance.GetComponent<IntroVirusTarget>();Assert.That(target,Is.Not.Null);
                var hit=instance.GetComponent<SphereCollider>();Assert.That(hit,Is.Not.Null);
                Assert.That(hit.isTrigger,Is.False);Assert.That(hit.radius,Is.InRange(.25f,.4f));
                Assert.That(instance.GetComponentsInChildren<Collider>().Length,Is.EqualTo(1));
                Assert.That(instance.GetComponentsInChildren<Camera>().Length,Is.Zero);
                Assert.That(instance.GetComponentsInChildren<AudioListener>().Length,Is.Zero);
                Assert.That(instance.GetComponentsInChildren<EnemyActor>().Length,Is.Zero);
            }
            finally{Object.DestroyImmediate(instance);}
        }
    }
}
