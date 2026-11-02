using System;
using NUnit.Framework;
using UnityEngine;
namespace VRFPS.Tests
{
    public class RoomStructureTests
    {
        [Test] public void StructureDataExistsWithoutReferencingScanAssets()
        {
            var type = typeof(MapRoot).Assembly.GetType("VRFPS.RoomStructureDefinition");
            Assert.That(type, Is.Not.Null, "Method A needs an independent structure asset");
            Assert.That(typeof(ScriptableObject).IsAssignableFrom(type), Is.True);
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType), Is.False, field.Name + " must not retain the original scan");
        }
        static ScriptableObject Data()
        {
            var type = typeof(MapRoot).Assembly.GetType("VRFPS.RoomStructureDefinition");
            Assert.That(type, Is.Not.Null);
            var data = ScriptableObject.CreateInstance(type);
            JsonUtility.FromJsonOverwrite("{\"mapId\":\"room\",\"themeId\":\"AbandonedHouse\",\"measuredRoomSize\":{\"x\":4.3,\"y\":2.45,\"z\":3.5},\"elements\":[{\"id\":\"Floor\",\"size\":{\"x\":4.3,\"y\":0.2,\"z\":3.5}}],\"enemySpawns\":[{\"x\":1,\"y\":0,\"z\":3.7}]}", data);
            return data;
        }
        [TestCase("{\"themeId\":\"AbandonedSchool\"}")]
        [TestCase("{\"mapId\":\"other\"}")]
        [TestCase("{\"elements\":[]}")]
        [TestCase("{\"elements\":[{\"id\":\"bad\",\"size\":{\"x\":-1,\"y\":1,\"z\":1}}]}")]
        [TestCase("{\"elements\":[{\"id\":\"same\",\"size\":{\"x\":1,\"y\":1,\"z\":1}},{\"id\":\"same\",\"size\":{\"x\":1,\"y\":1,\"z\":1}}]}")]
        public void InvalidStructureIsRejected(string patch)
        {
            var data = Data();
            try
            {
                JsonUtility.FromJsonOverwrite(patch, data);
                var method = data.GetType().GetMethod("Validate");
                Assert.That(method, Is.Not.Null);
                var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(data, null));
                Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }
        [Test] public void ValidRoomContractIsAccepted()
        {
            var data = Data();
            try { var method=data.GetType().GetMethod("Validate"); Assert.That(method,Is.Not.Null); Assert.DoesNotThrow(()=>method.Invoke(data,null)); }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }
    }
}
