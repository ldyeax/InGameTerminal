using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace InGameTerminal
{
	public interface ITerminalDefinition
	{
		Material Atlas { get; set; }
		/// <summary>
		/// Get an instance atlas, which can be modified at runtime for e.g. user defined characters
		/// </summary>
		/// <returns></returns>
		RenderTexture GetInstanceAtlas();
		int AtlasRows { get; set; }
		int AtlasCols { get; set; }
		int GlyphWidth { get; set; }
		int GlyphHeight { get; set; }
		/// <summary>
		/// Height of a pixel, for example the VT320 has a rougly 4:11 ratio so the pixel height is 2.75
		/// </summary>
		float PixelHeight { get; set; }

		int HorizontalLineX { get; set; }
		int HorizontalLineY { get; set; }

		int VerticalLineX { get; set; }
		int VerticalLineY { get; set; }

		int TopLeftCornerX { get; set; }
		int TopLeftCornerY { get; set; }

		int TopRightCornerX { get; set; }
		int TopRightCornerY { get; set; }

		int BottomLeftCornerX { get; set; }
		int BottomLeftCornerY { get; set; }

		int BottomRightCornerX { get; set; }
		int BottomRightCornerY { get; set; }

		int CrossX { get; set; }
		int CrossY { get; set; }

		int LeftTeeX { get; set; }
		int LeftTeeY { get; set; }

		int RightTeeX { get; set; }
		int RightTeeY { get; set; }

		int UpTeeX { get; set; }
		int UpTeeY { get; set; }

		int DownTeeX { get; set; }
		int DownTeeY { get; set; }
	}
	public interface IChartoXY
	{
		Vector2Int CharToXY(char c);
		char XYToChar(int x, int y);
	}
	public abstract class UnityTerminalDefinitionBase : ScriptableObject, ITerminalDefinition
	{
		[field: SerializeField]
		public virtual Material Atlas { get; set; }
		public abstract RenderTexture GetInstanceAtlas();
		public abstract int AtlasRows { get; set; }
		public abstract int AtlasCols { get; set; }
		public abstract int GlyphWidth { get; set; }
		public abstract int GlyphHeight { get; set; }
		public abstract float PixelHeight { get; set; }
		public abstract int HorizontalLineX { get; set; }
		public abstract int HorizontalLineY { get; set; }
		public abstract int VerticalLineX { get; set; }
		public abstract int VerticalLineY { get; set; }
		public abstract int TopLeftCornerX { get; set; }
		public abstract int TopLeftCornerY { get; set; }
		public abstract int TopRightCornerX { get; set; }
		public abstract int TopRightCornerY { get; set; }
		public abstract int BottomLeftCornerX { get; set; }
		public abstract int BottomLeftCornerY { get; set; }
		public abstract int BottomRightCornerX { get; set; }
		public abstract int BottomRightCornerY { get; set; }
		public abstract int CrossX { get; set; }
		public abstract int CrossY { get; set; }
		public abstract int LeftTeeX { get; set; }
		public abstract int LeftTeeY { get; set; }
		public abstract int RightTeeX { get; set; }
		public abstract int RightTeeY { get; set; }
		public abstract int UpTeeX { get; set; }
		public abstract int UpTeeY { get; set; }
		public abstract int DownTeeX { get; set; }
		public abstract int DownTeeY { get; set; }
	}
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
		public override int AtlasCols { get;  set; }

		[field: SerializeField]
		public override int GlyphWidth { get;  set; }

		[field: SerializeField]
		public override int GlyphHeight { get;  set; }

		[field: SerializeField]
		public override float PixelHeight { get;  set; }

		[field: SerializeField]
		public override int HorizontalLineX { get;  set; }

		[field: SerializeField]
		public override int HorizontalLineY { get;  set; }

		[field: SerializeField]
		public override int VerticalLineX { get;  set; }

		[field: SerializeField]
		public override int VerticalLineY { get;  set; }

		[field: SerializeField]
		public override int TopLeftCornerX { get;  set; }

		[field: SerializeField]
		public override int TopLeftCornerY { get;  set; }

		[field: SerializeField]
		public override int TopRightCornerX { get;  set; }

		[field: SerializeField]
		public override int TopRightCornerY { get;  set; }

		[field: SerializeField]
		public override int BottomLeftCornerX { get;  set; }

		[field: SerializeField]
		public override int BottomLeftCornerY { get;  set; }

		[field: SerializeField]
		public override int BottomRightCornerX { get;  set; }

		[field: SerializeField]
		public override int BottomRightCornerY { get;  set; }

		[field: SerializeField]
		public override int CrossX { get;  set; }

		[field: SerializeField]
		public override int CrossY { get;  set; }

		[field: SerializeField]
		public override int LeftTeeX { get;  set; }

		[field: SerializeField]
		public override int LeftTeeY { get;  set; }

		[field: SerializeField]
		public override int RightTeeX { get;  set; }

		[field: SerializeField]
		public override int RightTeeY { get;  set; }

		[field: SerializeField]
		public override int UpTeeX { get;  set; }

		[field: SerializeField]
		public override int UpTeeY { get;  set; }

		[field: SerializeField]
		public override int DownTeeX { get;  set; }

		[field: SerializeField]
		public override int DownTeeY { get;  set; }


	}
}
