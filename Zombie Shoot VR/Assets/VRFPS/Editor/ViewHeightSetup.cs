using System;using System.Linq;using UnityEditor;using UnityEngine;
namespace VRFPS.Editor {public static class ViewHeightSetup {
 public static void Configure(){if(!Application.isBatchMode)throw new InvalidOperationException("Use isolated editor");const string path="Assets/VRFPS/Prefabs/PlayerRig.prefab";var root=PrefabUtility.LoadPrefabContents(path);try {
 var movement=root.GetComponent<Player.PlayerRigController>();var head=movement.Head;var floor=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Camera Offset");var view=floor.Find("View Height Offset");
 if(view==null){var children=floor.Cast<Transform>().ToArray();view=new GameObject("View Height Offset").transform;view.SetParent(floor,false);foreach(var child in children)child.SetParent(view,false);}
 var height=root.GetComponent<Player.ViewHeightAdjustment>()??root.AddComponent<Player.ViewHeightAdjustment>();height.Configure(floor,view,head);PrefabUtility.SaveAsPrefabAsset(root,path);
 }finally{PrefabUtility.UnloadPrefabContents(root);}Debug.Log("VIEW_HEIGHT_115_CONFIGURED");}
}}
