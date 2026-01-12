using UnityEngine;

namespace InGameTerminal
{
	public struct TerminalBufferValue
	{
		private ITerminalDefinition _terminalDefinition;
		public TerminalBufferValue(ITerminalDefinition terminalDefinition)
		{
			_terminalDefinition = terminalDefinition;
			AtlasX = 0;
			AtlasY = 0;
			ConnectorID = 0;
			Italic = false;
			Bold = false;
			Underline = false;
			Inverted = false;
			Blink = false;
		}
		public readonly char GetChar(ITerminalDefinition terminalDefinition)
		{
			return terminalDefinition.XYToChar(AtlasX, AtlasY);
		}
		public void SetChar(ITerminalDefinition terminalDefinition, char c)
		{
			Vector2Int charXY = terminalDefinition.CharToXY(c);
			AtlasX = charXY.x;
			AtlasY = charXY.y;
		}
		public int AtlasX;
		public int AtlasY;
		public int ConnectorID;
		public bool Italic;
		public bool Bold;
		public bool Underline;
		public bool Inverted;
		public bool Blink;
		public static bool operator ==(TerminalBufferValue a, TerminalBufferValue b)
		{
			return
				a.AtlasX == b.AtlasX &&
				a.AtlasY == b.AtlasY &&
				a.ConnectorID == b.ConnectorID &&
				a.Italic == b.Italic &&
				a.Bold == b.Bold &&
				a.Underline == b.Underline &&
				a.Inverted == b.Inverted &&
				a.Blink == b.Blink;
		}
		public static bool operator !=(TerminalBufferValue a, TerminalBufferValue b)
		{
			return !(a == b);
		}
		public override readonly bool Equals(object obj)
		{
			if (obj is TerminalBufferValue other)
			{
				return this == other;
			}
			return false;
		}
		public override readonly int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}