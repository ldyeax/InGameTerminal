using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InGameTerminal.Elements
{
	public class Modifier : Element
	{
		public bool Bold, Italic, Underline, Blink, Invert;
		private const string RootMenu = "GameObject/InGameTerminal/";
		[MenuItem(RootMenu + "Modifier", false, 10)]
		public static void CreateModifier(MenuCommand cmd)
		{
			var go = new GameObject("Modifier", typeof(Modifier));
			GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create Modifier");
			Selection.activeObject = go;
			EditorSceneManager.MarkSceneDirty(go.scene);
		}
	}
}
