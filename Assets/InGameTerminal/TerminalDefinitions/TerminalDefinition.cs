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
		int AtlasRows { get; }
		int AtlasCols { get; }
		int GlyphWidth { get; }
		int GlyphHeight { get; }
		/// <summary>
		/// Height of a pixel, for example the VT320 has a rougly 4:11 ratio so the pixel height is 2.75
		/// </summary>
		float PixelHeight { get; }

		int HorizontalLineX { get; }
		int HorizontalLineY { get; }

		int VerticalLineX { get; }
		int VerticalLineY { get; }

		int TopLeftCornerX { get; }
		int TopLeftCornerY { get; }

		int TopRightCornerX { get; }
		int TopRightCornerY { get; }

		int BottomLeftCornerX { get; }
		int BottomLeftCornerY { get; }

		int BottomRightCornerX { get; }
		int BottomRightCornerY { get; }

		int CrossX { get; }
		int CrossY { get; }

		int LeftTeeX { get; }
		int LeftTeeY { get; }

		int RightTeeX { get; }
		int RightTeeY { get; }

		int UpTeeX { get; }
		int UpTeeY { get; }

		int DownTeeX { get; }
		int DownTeeY { get; }

		Vector2Int CharToXY(char c);
	}
	public abstract class TerminalDefinition : MonoBehaviour, ITerminalDefinition
	{
		public Material Atlas;
		public abstract int AtlasRows { get; }
		public abstract int AtlasCols { get; }
		public abstract int GlyphWidth { get; }
		public abstract int GlyphHeight { get; }
		public abstract float PixelHeight { get; }
		public abstract int HorizontalLineX { get; }
		public abstract int HorizontalLineY { get; }
		public abstract int VerticalLineX { get; }
		public abstract int VerticalLineY { get; }
		public abstract int TopLeftCornerX { get; }
		public abstract int TopLeftCornerY { get; }
		public abstract int TopRightCornerX { get; }
		public abstract int TopRightCornerY { get; }
		public abstract int BottomLeftCornerX { get; }
		public abstract int BottomLeftCornerY { get; }
		public abstract int BottomRightCornerX { get; }
		public abstract int BottomRightCornerY { get; }
		public abstract int CrossX { get; }
		public abstract int CrossY { get; }
		public abstract int LeftTeeX { get; }
		public abstract int LeftTeeY { get; }
		public abstract int RightTeeX { get; }
		public abstract int RightTeeY { get; }
		public abstract int UpTeeX { get; }
		public abstract int UpTeeY { get; }
		public abstract int DownTeeX { get; }
		public abstract int DownTeeY { get; }

		public virtual Vector2Int CharToXY(char c)
		{
			int index = c;
			int x = index % AtlasCols;
			int y = index / AtlasCols;
			return new Vector2Int(x, y);
		}
	}
	public class CustomTerminalDefinition : TerminalDefinition, ITerminalDefinition
	{
		[field: SerializeField]
		public override int AtlasRows { get;}

		[field: SerializeField]
		public override int AtlasCols { get;}

		[field: SerializeField]
		public override int GlyphWidth { get;}

		[field: SerializeField]
		public override int GlyphHeight { get;}

		[field: SerializeField]
		public override float PixelHeight { get;}

		[field: SerializeField]
		public override int HorizontalLineX { get;}

		[field: SerializeField]
		public override int HorizontalLineY { get;}

		[field: SerializeField]
		public override int VerticalLineX { get;}

		[field: SerializeField]
		public override int VerticalLineY { get;}

		[field: SerializeField]
		public override int TopLeftCornerX { get;}

		[field: SerializeField]
		public override int TopLeftCornerY { get;}

		[field: SerializeField]
		public override int TopRightCornerX { get;}

		[field: SerializeField]
		public override int TopRightCornerY { get;}

		[field: SerializeField]
		public override int BottomLeftCornerX { get;}

		[field: SerializeField]
		public override int BottomLeftCornerY { get;}

		[field: SerializeField]
		public override int BottomRightCornerX { get;}

		[field: SerializeField]
		public override int BottomRightCornerY { get;}

		[field: SerializeField]
		public override int CrossX { get;}

		[field: SerializeField]
		public override int CrossY { get;}

		[field: SerializeField]
		public override int LeftTeeX { get;}

		[field: SerializeField]
		public override int LeftTeeY { get;}

		[field: SerializeField]
		public override int RightTeeX { get;}

		[field: SerializeField]
		public override int RightTeeY { get;}

		[field: SerializeField]
		public override int UpTeeX { get;}

		[field: SerializeField]
		public override int UpTeeY { get;}

		[field: SerializeField]
		public override int DownTeeX { get;}

		[field: SerializeField]
		public override int DownTeeY { get;}


	}
}
