using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InGameTerminal.Elements
{
	[ExecuteAlways]
	public class Text : Element
	{
		private const string RootMenu = "GameObject/InGameTerminal/";
		[MenuItem(RootMenu + "Text", false, 10)]
		public static void CreateText(MenuCommand cmd)
		{
			var go = new GameObject("Text", typeof(Text));
			GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create Text");
			Selection.activeObject = go;
			EditorSceneManager.MarkSceneDirty(go.scene);
		}

		[SerializeField]
		[InspectorLabel("Text")]
		public string Contents = "Hello, World!";
	}
}
