using InGameTerminal;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace InGameTerminal
{

	[ExecuteAlways]
	public sealed class Terminal : MonoBehaviour
	{
		private const string RootMenu = "GameObject/InGameTerminal/";
		[MenuItem(RootMenu + "Terminal Screen", false, 10)]
		public static void CreateScreenCanvas(MenuCommand cmd)
		{
			var go = new GameObject("Terminal Screen", typeof(Terminal));
			GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create Terminal Screen");
			Selection.activeObject = go;
			EditorSceneManager.MarkSceneDirty(go.scene);
		}

		[SerializeField]
		public int Width = 80;
		[SerializeField]
		public int Height = 24;
		[SerializeField]
		public UnityTerminalDefinitionBase TerminalDefinition;

		[SerializeField]
		private Canvas _unityCanvas;
		public Canvas GetCanvas()
		{
			EnsureSetup();
			return _unityCanvas;
		}
		[SerializeField]
		private RectTransform _rectTransform;
		public RectTransform RectTransform => _rectTransform;
		private void EnsureSetup()
		{
			_unityCanvas = Util.GetOrCreateComponent<Canvas>(gameObject);
			_unityCanvas.renderMode = RenderMode.WorldSpace;
			_unityCanvas.pixelPerfect = true;

			CanvasScaler scaler = Util.GetOrCreateComponent<CanvasScaler>(gameObject);
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

			var raycaster = Util.GetOrCreateComponent<GraphicRaycaster>(gameObject);

			_rectTransform = Util.GetOrCreateComponent<RectTransform>(gameObject);
		}

		private void Reset() => EnsureSetup();
		private void OnValidate() => EnsureSetup();
		private void Awake() { if (!Application.isPlaying) EnsureSetup(); }
		public Vector2Int GetTerminalPosition(Element element)
		{
			float x = element.RectTransform.offsetMin.x;
			float y = -element.RectTransform.offsetMax.y;
			Vector2 fRet = new Vector2(x, y);
			fRet.x /= TerminalDefinition.GlyphWidth;
			fRet.y /= TerminalDefinition.GlyphHeight;
			var ret = new Vector2Int((int)fRet.x, (int)fRet.y);
			return ret;
		}

		void Start()
		{

		}
		private List<Element> elementPool = new();
		// Update is called once per frame
		void Update()
		{
			if (TerminalDefinition == null)
			{
				return;
			}
			_rectTransform.offsetMin = Vector3.zero;
			_rectTransform.offsetMax = new Vector3(
				Width * TerminalDefinition.GlyphWidth,
				Height * TerminalDefinition.GlyphHeight,
				0
			);
			_rectTransform.localScale = new Vector3(
				1,
				TerminalDefinition.PixelHeight,
				0
			);

			elementPool.Clear();
			GetComponentsInChildren<Element>(elementPool);
			foreach (var element in elementPool)
			{
				element.Align(TerminalDefinition);
			}
		}
	}
}