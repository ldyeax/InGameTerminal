using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
/**
 * Attributes are composited in this order:
 * - Base glyph fetch
 * - Italic shear
 * - Bold OR-shift
 * - Underline row force
 * - Reverse video
 * - Blink mask
 * 
 * Horizontal line at 17,7
 * Vertical line at 24,7
 **/
namespace InGameTerminal.TerminalDefinitions
{
	public sealed class VT320 : TerminalDefinition
	{
		public override int AtlasRows => 9;

		public override int AtlasCols => 32;

		public override int GlyphWidth => 15;

		public override int GlyphHeight => 12;

		public override float PixelHeight => 11.0f / 4.0f;

		// Box drawing characters
		public override int HorizontalLineX => 17;
		public override int HorizontalLineY => 7;

		public override int VerticalLineX => 24;
		public override int VerticalLineY => 7;

		public override int TopLeftCornerX => 12;
		public override int TopLeftCornerY => 7;

		public override int TopRightCornerX => 11;
		public override int TopRightCornerY => 7;

		public override int BottomLeftCornerX => 13;
		public override int BottomLeftCornerY => 7;

		public override int BottomRightCornerX => 10;
		public override int BottomRightCornerY => 7;

		public override int CrossX => 14;
		public override int CrossY => 7;

		public override int LeftTeeX => 21;
		public override int LeftTeeY => 7;

		public override int RightTeeX => 20;
		public override int RightTeeY => 7;

		public override int UpTeeX => 22;
		public override int UpTeeY => 7;

		public override int DownTeeX => 23;
		public override int DownTeeY => 7;

		public override Vector2Int CharToXY(char c)
		{
			if (c == ' ')
			{
				return Vector2Int.zero;
			}
			return base.CharToXY(c);
		}
	}
}
