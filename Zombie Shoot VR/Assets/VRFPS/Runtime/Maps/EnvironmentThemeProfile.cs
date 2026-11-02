using System;
using UnityEngine;
namespace VRFPS
{
    [Serializable] public sealed class ThemeVisualBinding
    {
        public string elementId;
        public Vector3 size;
        public GameObject prefab;
    }
    [CreateAssetMenu(menuName="VRFPS/Environment Theme")]
    public sealed class EnvironmentThemeProfile : ScriptableObject
    {
        public string mapId="room", themeId="AbandonedHouse";
        public bool allowPlaceholders=true;
        public Material plaster, wood, woodDark, fabric, metal;
        public ThemeVisualBinding[] visuals=Array.Empty<ThemeVisualBinding>();
    }
}
