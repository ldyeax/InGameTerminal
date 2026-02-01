using InGameTerminal;
using UnityEngine;
namespace InGameTerminal.TerminalDefinitions
{
	[CreateAssetMenu(fileName = "Custom Terminal Definition", menuName = "InGameTerminal/Custom Terminal Definition", order = 1)]
	public class CustomUnityTerminalDefinition : UnityTerminalDefinitionBase, ITerminalDefinition
	{

		[field: SerializeField]
		public override int AtlasRows { get; set; }
		public override RenderTexture GetInstanceAtlas()
		{
			RenderTexture ret = new RenderTexture(Atlas.mainTexture.width, Atlas.mainTexture.height, 0, RenderTextureFormat.ARGB32);
			ret.enableRandomWrite = true;
			ret.Create();
			Graphics.Blit(Atlas.mainTexture, ret);
			Debug.Log("Created atlas instance", ret);
			return ret;
		}

		[field: SerializeField]
		public override int AtlasCols { get; set; }

		[field: SerializeField]
		public override int GlyphWidth { get; set; }

		[field: SerializeField]
		public override int GlyphHeight { get; set; }

		[field: SerializeField]
		public override float PixelHeight { get; set; }

		[field: SerializeField]
		public override int HorizontalLineX { get; set; }

		[field: SerializeField]
		public override int HorizontalLineY { get; set; }

		[field: SerializeField]
		public override int VerticalLineX { get; set; }

		[field: SerializeField]
		public override int VerticalLineY { get; set; }

		[field: SerializeField]
		public override int TopLeftCornerX { get; set; }

		[field: SerializeField]
		public override int TopLeftCornerY { get; set; }

		[field: SerializeField]
		public override int TopRightCornerX { get; set; }

		[field: SerializeField]
		public override int TopRightCornerY { get; set; }

		[field: SerializeField]
		public override int BottomLeftCornerX { get; set; }

		[field: SerializeField]
		public override int BottomLeftCornerY { get; set; }

		[field: SerializeField]
		public override int BottomRightCornerX { get; set; }

		[field: SerializeField]
		public override int BottomRightCornerY { get; set; }

		[field: SerializeField]
		public override int CrossX { get; set; }

		[field: SerializeField]
		public override int CrossY { get; set; }

		[field: SerializeField]
		public override int LeftTeeX { get; set; }

		[field: SerializeField]
		public override int LeftTeeY { get; set; }

		[field: SerializeField]
		public override int RightTeeX { get; set; }

		[field: SerializeField]
		public override int RightTeeY { get; set; }

		[field: SerializeField]
		public override int UpTeeX { get; set; }

		[field: SerializeField]
		public override int UpTeeY { get; set; }

		[field: SerializeField]
		public override int DownTeeX { get; set; }

		[field: SerializeField]
		public override int DownTeeY { get; set; }


	}
}
