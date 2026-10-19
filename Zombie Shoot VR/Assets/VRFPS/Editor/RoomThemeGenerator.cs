using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRFPS.Editor
{
    public static class RoomThemeGenerator
    {
        public static void Generate(MapRoot map, RoomStructureDefinition data, EnvironmentThemeProfile profile)
        {
            if(map==null || data==null || profile==null)throw new ArgumentNullException("Map, structure and profile are required");
            data.Validate();
            if(profile.mapId!=data.mapId || profile.themeId!=data.themeId)throw new InvalidOperationException("Wrong fixed map/theme pairing");
            if(!IdentityChild(map.visualRoot,map.transform) || !IdentityChild(map.collisionRoot,map.transform) || map.transform.lossyScale!=Vector3.one)
                throw new InvalidOperationException("Visual and collision roots must be direct identity children of a unit-scale map");
            if(profile.plaster==null || profile.wood==null || profile.woodDark==null || profile.fabric==null || profile.metal==null)
                throw new InvalidOperationException("Missing theme materials");
            var solids=data.elements.Where(e=>e.collision==StructureCollision.Solid).ToArray();
            var colliders=map.collisionRoot.GetComponentsInChildren<BoxCollider>(true);
            if(colliders.Length!=solids.Length)throw new InvalidOperationException("Structure/collision count mismatch");
            foreach(var e in solids)
            {
                var matches=colliders.Where(c=>c.name==e.id).ToArray();
                if(matches.Length!=1)throw new InvalidOperationException("Missing or duplicate collision: "+e.id);
                var c=matches[0];
                if(c.transform.parent!=map.collisionRoot || c.transform.localPosition!=e.localCenter || c.size!=e.size ||
                    c.center!=Vector3.zero || c.transform.localScale!=Vector3.one || Quaternion.Angle(c.transform.localRotation,Quaternion.Euler(e.localEuler))>.001f)
                    throw new InvalidOperationException("Structure/collision transform mismatch: "+e.id);
            }
            var bindings=new Dictionary<string,ThemeVisualBinding>(StringComparer.Ordinal);
            foreach(var binding in profile.visuals ?? Array.Empty<ThemeVisualBinding>())
            {
                if(binding==null || string.IsNullOrWhiteSpace(binding.elementId) || bindings.ContainsKey(binding.elementId))
                    throw new InvalidOperationException("Invalid or duplicate visual binding");
                bindings.Add(binding.elementId,binding);
            }
            foreach(var e in data.elements.Where(e=>e.kind!=StructureKind.DoorOpening))
            {
                if(!bindings.TryGetValue(e.id,out var binding))
                {if(!profile.allowPlaceholders)throw new InvalidOperationException("Missing visual prefab: "+e.id);continue;}
                if(binding.prefab==null || (binding.size-e.size).sqrMagnitude>.000001f)throw new InvalidOperationException("Prefab size does not match structure: "+e.id);
                ValidatePrefab(binding.prefab,e.size);
            }
            var owned=map.visualRoot.Cast<Transform>().Select(t=>t.GetComponent<GeneratedThemeRoot>()).Where(m=>m!=null).ToArray();
            if(owned.Length>1 || owned.Any(m=>m.mapId!=data.mapId))throw new InvalidOperationException("Ambiguous generated root ownership");
            var named=map.visualRoot.Find("ThemeGenerated");
            if(named!=null && (owned.Length==0 || named!=owned[0].transform))throw new InvalidOperationException("ThemeGenerated is user owned");
            var staging=new GameObject("ThemeStaging");staging.SetActive(false);staging.transform.SetParent(map.visualRoot,false);
            try
            {
                var marker=staging.AddComponent<GeneratedThemeRoot>();marker.mapId=data.mapId;marker.themeId=data.themeId;
                foreach(var e in data.elements)
                {
                    if(e.kind==StructureKind.DoorOpening)continue;
                    GameObject visual;
                    if(bindings.TryGetValue(e.id,out var binding))visual=(GameObject)PrefabUtility.InstantiatePrefab(binding.prefab,staging.transform);
                    else
                    {
                        visual=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                        visual.transform.SetParent(staging.transform,false);visual.transform.localScale=e.size;
                        visual.GetComponent<Renderer>().sharedMaterial=e.kind==StructureKind.Floor?profile.wood:profile.plaster;
                    }
                    visual.name=e.id;visual.transform.localPosition=e.localCenter;visual.transform.localRotation=Quaternion.Euler(e.localEuler);
                }
                var world=LayerMask.NameToLayer("WorldSolid");
                foreach(var t in staging.GetComponentsInChildren<Transform>(true)) if(t.gameObject.layer==world)throw new InvalidOperationException("Visual layer cannot be WorldSolid");
                if(staging.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Visual must not change collision");
                Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Regenerate fixed map theme");
                Undo.RegisterCreatedObjectUndo(staging,"Generate theme");
                if(owned.Length==1)Undo.DestroyObjectImmediate(owned[0].gameObject);
                staging.name="ThemeGenerated";staging.SetActive(true);Undo.CollapseUndoOperations(group);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
            }
            catch {if(staging!=null)UnityEngine.Object.DestroyImmediate(staging);throw;}
        }

        static bool IdentityChild(Transform child,Transform parent) => child!=null && child.parent==parent && child.localPosition==Vector3.zero && child.localRotation==Quaternion.identity && child.localScale==Vector3.one;

        static void ValidatePrefab(GameObject prefab,Vector3 expectedSize)
        {
            if(!PrefabUtility.IsPartOfPrefabAsset(prefab))throw new InvalidOperationException("Visual binding must be a prefab asset");
            foreach(var component in prefab.GetComponentsInChildren<Component>(true))
                if(component==null || !(component is Transform || component is MeshFilter || component is MeshRenderer))
                    throw new InvalidOperationException("Visual prefab contains a nonvisual component");
            if(prefab.transform.localPosition!=Vector3.zero || prefab.transform.localRotation!=Quaternion.identity || prefab.transform.localScale!=Vector3.one)
                throw new InvalidOperationException("Prefab must have a centered identity root");
            var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
            if(filters.Length==0)throw new InvalidOperationException("Empty visual prefab");
            var bounds=new Bounds();bool first=true;
            foreach(var filter in filters)
            {
                if(filter.sharedMesh==null || filter.GetComponent<MeshRenderer>()==null ||
                    filter.GetComponent<MeshRenderer>().sharedMaterials.Any(m=>m==null))throw new InvalidOperationException("Missing mesh or material");
                var matrix=prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(!RoomStructureDefinition.Finite(p))throw new InvalidOperationException("Nonfinite prefab geometry");
                    if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
                }
            }
            if(bounds.center.magnitude>.025f || (bounds.size-expectedSize).magnitude>.05f)
                throw new InvalidOperationException("Visual bounds do not correspond to solid structure");
        }
    }
}
