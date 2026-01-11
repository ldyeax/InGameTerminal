using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InGameTerminal.Elements
{
	[ExecuteAlways]
	public class HorizontalLine : Element
	{
		private const string RootMenu = "GameObject/InGameTerminal/";
		[MenuItem(RootMenu + "Horizontal Line", false, 10)]
		public static void CreateHorizontalLine(MenuCommand cmd)
		{
			var go = new GameObject("HorizontalLine", typeof(HorizontalLine));
			GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create Horizontal Line");
			Selection.activeObject = go;
			EditorSceneManager.MarkSceneDirty(go.scene);
		}
	}
}
