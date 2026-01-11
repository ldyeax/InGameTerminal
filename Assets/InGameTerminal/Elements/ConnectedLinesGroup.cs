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
	/// Groups ConnectedLine children so they share a common ConnectorID and connect to each other
	/// </summary>
	[ExecuteAlways]
	public class ConnectedLinesGroup : Element
	{
		private const string RootMenu = "GameObject/InGameTerminal/";
		[MenuItem(RootMenu + "Connected Lines Group", false, 10)]
		public static void CreateConnectedLinesGroup(MenuCommand cmd)
		{
			var go = new GameObject("ConnectedLinesGroup", typeof(ConnectedLinesGroup));
			GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create Connected Lines Group");
			Selection.activeObject = go;
			EditorSceneManager.MarkSceneDirty(go.scene);
		}

		private List<ConnectedLine> _childLines = new();

		public IReadOnlyList<ConnectedLine> GetChildLines()
		{
			GetComponentsInChildren(_childLines);
			return _childLines;
		}
	}
}
