using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InGameTerminal.Elements
{
	[ExecuteAlways]
	public class Box : Element
	{
		private const string RootMenu = "GameObject/InGameTerminal/";

		public bool Solid = false;

		[MenuItem(RootMenu + "Box", false, 10)]
		public static void CreateBox(MenuCommand cmd)
		{
			var go = new GameObject("Box", typeof(Box));
			GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create Box");
			Selection.activeObject = go;
			EditorSceneManager.MarkSceneDirty(go.scene);
		}
	}
}
