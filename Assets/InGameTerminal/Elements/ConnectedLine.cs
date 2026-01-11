using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InGameTerminal.Elements
{
	/// <summary>
	/// A line (or can be dragged into a larger rectangle that will get filled with connected lines)
	/// that will connect to other lines in its ConnectedLinesGroup
	/// </summary>
	[ExecuteAlways]
	public class ConnectedLine : Element
	{
		private const string RootMenu = "GameObject/InGameTerminal/";
		[MenuItem(RootMenu + "Connected Line", false, 10)]
		public static void CreateConnectedLine(MenuCommand cmd)
		{
			var go = new GameObject("ConnectedLine", typeof(ConnectedLine));
			GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create Connected Line");
			Selection.activeObject = go;
			EditorSceneManager.MarkSceneDirty(go.scene);
		}
	}
}
