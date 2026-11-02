using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
    public class RoomThemeTests
    {
        static Type EditorType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("VRFPS.Editor."+name)).FirstOrDefault(t=>t!=null);
        [Test] public void RegenerationPreservesStructureAndUserObjectsAndRejectsInvalidInput()
        {
            var generator=EditorType("RoomThemeGenerator");Assert.That(generator,Is.Not.Null,"Missing safe theme generator");
            var type=typeof(MapRoot).Assembly.GetType("VRFPS.EnvironmentThemeProfile");Assert.That(type,Is.Not.Null);
            var profile=ScriptableObject.CreateInstance(type);
            var data=ScriptableObject.CreateInstance<RoomStructureDefinition>();
            data.measuredRoomSize=new Vector3(4.3f,2.45f,3.5f);data.enemySpawns=new[]{new Vector3(1,0,3.7f)};
            data.elements=new[]{new StructureElement{id="Floor",kind=StructureKind.Floor,size=new Vector3(4.3f,.2f,3.5f),collision=StructureCollision.Solid,navigation=StructureNavigation.WalkableSurface}};
            var map=(MapRoot)EditorType("RoomStructureSetup").GetMethod("GenerateStructure").Invoke(null,new object[]{data});
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            foreach(var field in type.GetFields().Where(f=>f.FieldType==typeof(Material)))field.SetValue(profile,material);
            var user=new GameObject("User decoration");user.transform.SetParent(map.visualRoot,false);
            try
            {
                var method=generator.GetMethod("Generate");Assert.That(method,Is.Not.Null);
                method.Invoke(null,new object[]{map,data,profile});
                var first=map.visualRoot.GetComponentsInChildren<Renderer>().Length;Assert.That(first,Is.GreaterThan(0));
                var floor=map.collisionRoot.Find("Floor").GetComponent<BoxCollider>();var floorId=floor.GetInstanceID();
                method.Invoke(null,new object[]{map,data,profile});
                Assert.That(map.visualRoot.GetComponentsInChildren<Renderer>().Length,Is.EqualTo(first));
                Assert.That(map.visualRoot.Find("User decoration"),Is.EqualTo(user.transform));
                Assert.That(map.visualRoot.GetComponentsInChildren<Collider>(true),Is.Empty);
                Assert.That(map.collisionRoot.Find("Floor").GetComponent<BoxCollider>().GetInstanceID(),Is.EqualTo(floorId));
                Assert.That(floor.size,Is.EqualTo(data.elements[0].size));
                var preserved=map.visualRoot.Find("ThemeGenerated");Assert.That(preserved,Is.Not.Null);
                data.themeId="AbandonedSchool";
                Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{map,data,profile}));
                Assert.That(map.visualRoot.Find("ThemeGenerated"),Is.EqualTo(preserved));
                Assert.That(map.visualRoot.GetComponentsInChildren<Renderer>().Length,Is.EqualTo(first));
                Assert.That(map.visualRoot.childCount,Is.EqualTo(2));
            }
            finally {UnityEngine.Object.DestroyImmediate(map.gameObject);UnityEngine.Object.DestroyImmediate(data);UnityEngine.Object.DestroyImmediate(profile);UnityEngine.Object.DestroyImmediate(material);}
        }
    }
}
