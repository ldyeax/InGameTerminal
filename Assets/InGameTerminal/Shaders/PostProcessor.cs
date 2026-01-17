using InGameTerminal;
using UnityEngine;

namespace InGameTermina.Shaders
{
	[ExecuteAlways]
	public class PostProcessor : MonoBehaviour
	{
		[SerializeField]
		new private Camera camera;
		[SerializeField]
		private Terminal terminal;
		[SerializeField]
		private Transform preview;
		[SerializeField]
		private RenderTexture renderTexture;
		private void OnEnable()
		{
			camera = Util.GetOrCreateComponent<Camera>(gameObject);
			camera.orthographic = true;
		}
		private void Update()
		{
			float screenGlyphWidth = terminal.TerminalDefinition.GlyphWidth;
			float screenGlyphHeight = terminal.TerminalDefinition.GlyphHeight * terminal.TerminalDefinition.PixelHeight;
			float aspectWidth = terminal.Width * screenGlyphWidth;
			float aspectHeight = terminal.Height * screenGlyphHeight;
			camera.aspect = aspectWidth / aspectHeight;
			camera.orthographicSize = terminal.Height * terminal.TerminalDefinition.GlyphHeight * terminal.TerminalDefinition.PixelHeight * 0.5f;
			if (preview)
			{
				preview.localScale = new Vector3(aspectWidth, aspectHeight, 1);
			}
			int desiredWidth = Mathf.CeilToInt(aspectWidth);
			int desiredHeight = Mathf.CeilToInt(aspectHeight);
			if (renderTexture.width != desiredWidth || renderTexture.height != desiredHeight)
			{
				Debug.LogWarning($"RenderTexture should be {desiredWidth}x{desiredHeight}", this);
			}
		}
	}
}
