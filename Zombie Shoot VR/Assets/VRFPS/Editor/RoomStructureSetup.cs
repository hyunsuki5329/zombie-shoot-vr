using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.AI.Navigation;

namespace VRFPS.Editor
{
    public static class RoomStructureSetup
    {
        public const string AssetPath = "Assets/VRFPS/Configuration/Maps/RoomStructure.asset";
        public static RoomStructureDefinition Capture(MapRoot map)
        {
            if (map == null || map.collisionRoot == null || map.playerSpawn == null || map.enemySpawns == null)
                throw new InvalidOperationException("Missing source map structure");
            if (map.transform.localScale != Vector3.one || map.collisionRoot.localScale != Vector3.one ||
                map.collisionRoot.localPosition != Vector3.zero || map.collisionRoot.localRotation != Quaternion.identity)
                throw new InvalidOperationException("Structure capture requires identity collision root and unit map scale");
            var data=ScriptableObject.CreateInstance<RoomStructureDefinition>();
            try
            {
                data.measuredRoomSize=new Vector3(4.3f,2.45f,3.5f);
                data.playerSpawn=map.transform.InverseTransformPoint(map.playerSpawn.position);
                data.playerEuler=(Quaternion.Inverse(map.transform.rotation)*map.playerSpawn.rotation).eulerAngles;
                data.enemySpawns=map.enemySpawns.Select(t=>map.transform.InverseTransformPoint(t.position)).ToArray();
                data.elements=map.collisionRoot.GetComponentsInChildren<Collider>(true).Select(c=>
                {
                    if (!(c is BoxCollider box) || c.transform.parent!=map.collisionRoot || c.transform.localScale!=Vector3.one ||
                        box.center!=Vector3.zero || !box.enabled || box.isTrigger || !box.gameObject.activeSelf || c.gameObject.layer!=LayerMask.NameToLayer("WorldSolid"))
                        throw new InvalidOperationException("Unsupported source collider: "+c.name);
                    var modifier=c.GetComponent<NavMeshModifier>();
                    bool floor=c.name=="Floor" || c.name=="Corridor floor";
                    if (!floor && (modifier==null || !modifier.overrideArea || modifier.area!=1))
                        throw new InvalidOperationException("Blocking collider must be navigation area 1: "+c.name);
                    var kind=floor?StructureKind.Floor:c.name.Contains("ceiling")||c.name=="Ceiling"?StructureKind.Ceiling:
                        c.name=="Bed"?StructureKind.Bed:c.name=="Desk"?StructureKind.Desk:c.name=="Chair"?StructureKind.Chair:
                        c.name=="Wardrobe"||c.name=="Keyboard"?StructureKind.Obstacle:StructureKind.Wall;
                    return new StructureElement { id=c.name, kind=kind,
                        origin=c.name.StartsWith("Corridor")||c.name.StartsWith("Door")?StructureOrigin.GameExtension:
                            c.name=="Floor"?StructureOrigin.Measured:StructureOrigin.ScanEstimated,
                        localCenter=c.transform.localPosition, localEuler=c.transform.localEulerAngles, size=box.size,
                        collision=StructureCollision.Solid,
                        navigation=floor?StructureNavigation.WalkableSurface:StructureNavigation.BlockingGeometry };
                }).ToArray();
                var surface=map.GetComponent<NavMeshSurface>();
                if(surface==null || !surface.overrideVoxelSize)throw new InvalidOperationException("Expected explicit navigation settings");
                data.navigationAgentType=surface.agentTypeID;data.navigationVoxelSize=surface.voxelSize;
                data.Validate();return data;
            }
            catch { UnityEngine.Object.DestroyImmediate(data);throw; }
        }

        public static MapRoot GenerateStructure(RoomStructureDefinition data)
        {
            if(data==null)throw new ArgumentNullException(nameof(data));data.Validate();
            var layer=LayerMask.NameToLayer("WorldSolid");
            if(layer<0)throw new InvalidOperationException("WorldSolid layer is required");
            var root=new GameObject(data.mapId+" - "+data.themeId);
            try
            {
                var map=root.AddComponent<MapRoot>();
                Transform Child(string name) {var o=new GameObject(name);o.transform.SetParent(root.transform,false);return o.transform;}
                map.visualRoot=Child("VisualRoot");map.collisionRoot=Child("CollisionRoot");
                foreach(var e in data.elements)
                {
                    if(e.collision==StructureCollision.None)continue;
                    var o=new GameObject(e.id);o.transform.SetParent(map.collisionRoot,false);
                    o.transform.localPosition=e.localCenter;o.transform.localRotation=Quaternion.Euler(e.localEuler);o.layer=layer;
                    o.AddComponent<BoxCollider>().size=e.size;
                    if(e.navigation!=StructureNavigation.WalkableSurface)
                    {
                        var modifier=o.AddComponent<NavMeshModifier>();
                        modifier.ignoreFromBuild=e.navigation==StructureNavigation.Excluded;
                        modifier.overrideArea=true;modifier.area=1;
                    }
                }
                map.playerSpawn=Child("PlayerSpawn");map.playerSpawn.localPosition=data.playerSpawn;map.playerSpawn.localRotation=Quaternion.Euler(data.playerEuler);
                map.enemySpawns=data.enemySpawns.Select((p,i)=>{var t=Child("EnemySpawn "+i);t.localPosition=p;return t;}).ToArray();
                var nav=root.AddComponent<NavMeshSurface>();nav.agentTypeID=data.navigationAgentType;
                nav.collectObjects=CollectObjects.Children;nav.layerMask=LayerMask.GetMask("WorldSolid");
                nav.useGeometry=UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;nav.overrideVoxelSize=true;nav.voxelSize=data.navigationVoxelSize;
                return map;
            }
            catch {UnityEngine.Object.DestroyImmediate(root);throw;}
        }
        public static void CaptureRoom()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Capture in isolated batch editor");
            var scene=EditorSceneManager.OpenScene(ScannedRoomSetup.ScenePath);
            var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapRoot>()).Single();
            var data=Capture(source);var old=AssetDatabase.LoadAssetAtPath<RoomStructureDefinition>(AssetPath);
            if(old==null)AssetDatabase.CreateAsset(data,AssetPath);
            else {EditorUtility.CopySerialized(data,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(data);}
            AssetDatabase.SaveAssets();Debug.Log("VRFPS_ROOM_STRUCTURE_CAPTURED");
        }
    }
}
