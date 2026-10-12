using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRFPS.Tests
{
    public class RoomStructureGenerationTests
    {
        static Type Setup()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("VRFPS.Editor.RoomStructureSetup")).FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, "Missing structure capture/generation tool");
            return type;
        }
        [Test] public void CapturedStructureRecreatesEveryColliderAndAuthoredAnchor()
        {
            var scene=EditorSceneManager.OpenScene("Assets/VRFPS/Scenes/Maps/Room.unity",OpenSceneMode.Additive);
            RoomStructureDefinition data=null; MapRoot generated=null;
            try
            {
                var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapRoot>()).Single();
                var setup=Setup();
                data=(RoomStructureDefinition)setup.GetMethod("Capture").Invoke(null,new object[]{source});
                data.Validate();
                Assert.That(data.measuredRoomSize,Is.EqualTo(new Vector3(4.3f,2.45f,3.5f)));
                Assert.That(data.enemySpawns.Length,Is.EqualTo(7));
                generated=(MapRoot)setup.GetMethod("GenerateStructure").Invoke(null,new object[]{data});
                var before=source.collisionRoot.GetComponentsInChildren<BoxCollider>();
                var after=generated.collisionRoot.GetComponentsInChildren<BoxCollider>();
                Assert.That(after.Length,Is.EqualTo(before.Length));
                foreach(var collider in before)
                {
                    var copy=after.Single(c=>c.name==collider.name);
                    Assert.That(copy.transform.localPosition,Is.EqualTo(collider.transform.localPosition));
                    Assert.That(copy.transform.localRotation,Is.EqualTo(collider.transform.localRotation));
                    Assert.That(copy.size,Is.EqualTo(collider.size));
                    Assert.That(copy.center,Is.EqualTo(collider.center));
                    Assert.That(copy.gameObject.layer,Is.EqualTo(collider.gameObject.layer));
                    Assert.That(copy.isTrigger,Is.False); Assert.That(copy.enabled,Is.True);
                }
                Assert.That(generated.playerSpawn.localPosition,Is.EqualTo(source.playerSpawn.localPosition));
                Assert.That(Quaternion.Angle(generated.playerSpawn.localRotation,source.playerSpawn.localRotation),Is.LessThan(.001f));
                CollectionAssert.AreEqual(source.enemySpawns.Select(t=>t.localPosition),generated.enemySpawns.Select(t=>t.localPosition));
                Assert.That(data.elements.Single(e=>e.id=="Corridor floor").origin,Is.EqualTo(StructureOrigin.GameExtension));
                Assert.That(data.elements.Single(e=>e.id=="Desk").navigation,Is.EqualTo(StructureNavigation.BlockingGeometry));
                Assert.That(data.elements.Single(e=>e.id=="Floor").navigation,Is.EqualTo(StructureNavigation.WalkableSurface));
                Assert.That(data.elements.Single(e=>e.id=="Wardrobe").kind,Is.EqualTo(StructureKind.Obstacle),"Unknown storage classification must remain uncertain");
            }
            finally
            {
                if(generated!=null)UnityEngine.Object.DestroyImmediate(generated.gameObject);
                if(data!=null)UnityEngine.Object.DestroyImmediate(data);
                EditorSceneManager.CloseScene(scene,true);
            }
        }
    }
}
