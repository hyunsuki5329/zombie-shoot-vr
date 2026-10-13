using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRFPS
{
    public enum StructureKind { Floor, Ceiling, Wall, DoorOpening, Bed, Desk, Chair, Storage, Obstacle }
    public enum StructureOrigin { Measured, ScanEstimated, GameExtension }
    public enum StructureCollision { None, Solid }
    public enum StructureNavigation { Excluded, WalkableSurface, BlockingGeometry }

    [Serializable]
    public sealed class StructureElement
    {
        public string id;
        public StructureKind kind;
        public StructureOrigin origin;
        public Vector3 localCenter, localEuler, size;
        public StructureCollision collision;
        public StructureNavigation navigation;
    }

    // Pure structure: deliberately no reference to scans, textures, prefabs or baked navigation.
    [CreateAssetMenu(menuName = "VRFPS/Room Structure")]
    public sealed class RoomStructureDefinition : ScriptableObject
    {
        public int schemaVersion = 1;
        public string mapId = "room", themeId = "AbandonedHouse";
        public Vector3 measuredRoomSize;
        public float gameplayScale = 1f;
        public bool ceilingHeightEstimated = true;
        public StructureElement[] elements = Array.Empty<StructureElement>();
        public Vector3 playerSpawn, playerEuler;
        public Vector3[] enemySpawns = Array.Empty<Vector3>();
        public int navigationAgentType;
        public float navigationVoxelSize = .04f;

        public void Validate()
        {
            if (!Finite(gameplayScale) || gameplayScale <= 0) Fail("Invalid gameplay scale");
            if (schemaVersion != 1) Fail("Unsupported structure schema");
            if (!((mapId == "room" && themeId == "AbandonedHouse") ||
                  (mapId == "classroom" && themeId == "AbandonedSchool")))
                Fail("Map theme must be Room/AbandonedHouse or Classroom/AbandonedSchool");
            if (!Positive(measuredRoomSize)) Fail("Invalid measured size");
            if (elements == null || elements.Length == 0) Fail("Missing structure elements");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var element in elements)
            {
                if (element == null || string.IsNullOrWhiteSpace(element.id) || !ids.Add(element.id)) Fail("Missing or duplicate element ID");
                if (!Finite(element.localCenter) || !Finite(element.localEuler) || !Positive(element.size)) Fail("Invalid element transform: " + element.id);
                if (!Enum.IsDefined(typeof(StructureKind), element.kind) || !Enum.IsDefined(typeof(StructureOrigin), element.origin) ||
                    !Enum.IsDefined(typeof(StructureCollision), element.collision) || !Enum.IsDefined(typeof(StructureNavigation), element.navigation)) Fail("Invalid element role");
                if (element.kind == StructureKind.DoorOpening && (element.collision != StructureCollision.None || element.navigation != StructureNavigation.Excluded)) Fail("Door opening must be empty");
                if (element.navigation != StructureNavigation.Excluded && element.collision != StructureCollision.Solid) Fail("Navigation needs solid source geometry");
            }
            if (!Finite(playerSpawn) || !Finite(playerEuler)) Fail("Invalid player spawn");
            if (enemySpawns == null || enemySpawns.Length == 0) Fail("Missing enemy spawns");
            var spawns = new HashSet<Vector3>();
            foreach (var spawn in enemySpawns) if (!Finite(spawn) || !spawns.Add(spawn)) Fail("Invalid or duplicate enemy spawn");
            if (!Finite(navigationVoxelSize) || navigationVoxelSize <= 0 || navigationAgentType < 0) Fail("Invalid navigation settings");
        }
        // Absolute scale, not an incremental multiplier: repeated regeneration is idempotent.
        public RoomStructureDefinition CreateGameplayCopy(float targetScale)
        {
            if (!Finite(targetScale) || targetScale <= 0) throw new ArgumentOutOfRangeException(nameof(targetScale));
            Validate();
            var copy = CreateInstance<RoomStructureDefinition>();
            try
            {
                float ratio = targetScale / gameplayScale;
                copy.schemaVersion=schemaVersion;copy.mapId=mapId;copy.themeId=themeId;
                copy.measuredRoomSize=measuredRoomSize;copy.ceilingHeightEstimated=ceilingHeightEstimated;copy.gameplayScale=targetScale;
                copy.elements=new StructureElement[elements.Length];
                for(int i=0;i<elements.Length;i++)
                {
                    var e=elements[i];copy.elements[i]=new StructureElement{id=e.id,kind=e.kind,origin=e.origin,
                        localCenter=e.localCenter*ratio,localEuler=e.localEuler,size=e.size*ratio,collision=e.collision,navigation=e.navigation};
                }
                copy.playerSpawn=playerSpawn*ratio;copy.playerEuler=playerEuler;
                copy.enemySpawns=new Vector3[enemySpawns.Length];
                for(int i=0;i<enemySpawns.Length;i++)copy.enemySpawns[i]=enemySpawns[i]*ratio;
                copy.navigationAgentType=navigationAgentType;copy.navigationVoxelSize=navigationVoxelSize;
                copy.Validate();return copy;
            }
            catch {DestroyImmediate(copy);throw;}
        }

        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        static bool Positive(Vector3 value) => Finite(value) && value.x > 0 && value.y > 0 && value.z > 0;
        static void Fail(string message) => throw new InvalidOperationException(message);
    }
}
