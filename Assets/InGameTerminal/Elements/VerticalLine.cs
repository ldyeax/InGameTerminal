using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InGameTerminal.Elements
{
	[ExecuteAlways]
	public class VerticalLine : Element
	{
		private const string RootMenu = "GameObject/InGameTerminal/";
		[MenuItem(RootMenu + "Vertical Line", false, 10)]
		public static void CreateVerticalLine(MenuCommand cmd)
		{
			var go = new GameObject("VerticalLine", typeof(VerticalLine));
			GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create Vertical Line");
			Selection.activeObject = go;
			EditorSceneManager.MarkSceneDirty(go.scene);
		}
	}
}
