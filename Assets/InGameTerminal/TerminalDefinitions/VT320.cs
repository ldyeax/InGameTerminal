using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace InGameTerminal.TerminalDefinitions
{
	[CreateAssetMenu(fileName = "VT320 Terminal Definition", menuName = "InGameTerminal/VT320 Terminal Definition", order = 1)]
	public sealed class VT320_ScriptableObject : UnityTerminalDefinitionBase, ITerminalDefinition, IChartoXY
	{
		public override int AtlasRows { get; set; } = 9;

		public override int AtlasCols { get; set; } = 32;

		public override int GlyphWidth { get; set; } = 15;

		public override int GlyphHeight { get; set; } = 12;

		public override float PixelHeight { get; set; } = 11.0f / 4.0f;

		// Box drawing characters
		public override int HorizontalLineX { get; set; } = 17;
		public override int HorizontalLineY { get; set; } = 7;

		public override int VerticalLineX { get; set; } = 24;
		public override int VerticalLineY { get; set; } = 7;

		public override int TopLeftCornerX { get; set; } = 12;
		public override int TopLeftCornerY { get; set; } = 7;

		public override int TopRightCornerX { get; set; } = 11;
		public override int TopRightCornerY { get; set; } = 7;

		public override int BottomLeftCornerX { get; set; } = 13;
		public override int BottomLeftCornerY { get; set; } = 7;

		public override int BottomRightCornerX { get; set; } = 10;
		public override int BottomRightCornerY { get; set; } = 7;

		public override int CrossX { get; set; } = 14;
		public override int CrossY { get; set; } = 7;

		public override int LeftTeeX { get; set; } = 21;
		public override int LeftTeeY { get; set; } = 7;

		public override int RightTeeX { get; set; } = 20;
		public override int RightTeeY { get; set; } = 7;

		public override int UpTeeX { get; set; } = 22;
		public override int UpTeeY { get; set; } = 7;

		public override int DownTeeX { get; set; } = 23;
		public override int DownTeeY { get; set; } = 7;

		char IChartoXY.XYToChar(int x, int y)
		{
			if (x == 0 && y == 0)
			{
				return ' ';
			}
			return '\0';
		}

		Vector2Int IChartoXY.CharToXY(char c)
		{
			if (c == ' ')
			{
				return Vector2Int.zero;
			}
			return new Vector2Int(-1, -1);
		}
	}
}
