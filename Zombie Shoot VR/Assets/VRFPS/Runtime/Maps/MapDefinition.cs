using UnityEngine;
namespace VRFPS
{
    [CreateAssetMenu(menuName = "VRFPS/Map Definition")]
    public sealed class MapDefinition : ScriptableObject
    {
        public string mapId, sceneName;
        public int maxAlive, maxRangedAlive;
        public float minSpawnDistance;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(mapId) || string.IsNullOrWhiteSpace(sceneName) ||
                maxAlive <= 0 || maxRangedAlive <= 0 || maxRangedAlive > maxAlive ||
                minSpawnDistance <= 0 || float.IsNaN(minSpawnDistance) || float.IsInfinity(minSpawnDistance))
                throw new System.ArgumentException("Map identity, scene and spawn limits must be valid.");
        }
    }
}
