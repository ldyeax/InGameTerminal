using UnityEngine;

namespace InGameTerminal
{
	public enum TerminalCharacterBank
	{
		ASCII = 0,
		G1,

	}
	public struct TerminalBufferValue
	{
		private ITerminalDefinition _terminalDefinition;
		public TerminalBufferValue(ITerminalDefinition terminalDefinition)
		{
			_terminalDefinition = terminalDefinition;
			AtlasX = 0;
			AtlasY = 0;
			ConnectorID = 0;
			TextAttributes = new TextAttributes();
			CharacterBank = TerminalCharacterBank.ASCII;
		}
		public readonly char GetChar(ITerminalDefinition terminalDefinition)
		{
			return terminalDefinition.XYToChar(AtlasX, AtlasY);
		}
		public readonly byte GetByte(ITerminalDefinition terminalDefinition)
		{
			return (byte)terminalDefinition.XYToChar(AtlasX, AtlasY);
		}
		public void SetChar(ITerminalDefinition terminalDefinition, char c)
		{
			Vector2Int charXY = terminalDefinition.CharToXY(c);
			AtlasX = charXY.x;
			AtlasY = charXY.y;
		}
		public void SetByte(ITerminalDefinition terminalDefinition, byte b)
		{
			Vector2Int charXY = terminalDefinition.ByteToXY(b);
			AtlasX = charXY.x;
			AtlasY = charXY.y;
		}
		public TerminalCharacterBank CharacterBank;
		public int AtlasX;
		public int AtlasY;
		public int ConnectorID;
		public TextAttributes TextAttributes;


		public readonly Color AttributesToVertexColor(bool isCursor = false)
		{
			TextAttributes textAttributes = TextAttributes;
			textAttributes.IsCursor = isCursor;
			return textAttributes.AttributesToVertexColor();
		}

		public static bool operator ==(TerminalBufferValue a, TerminalBufferValue b)
		{
			return
				a.AtlasX == b.AtlasX &&
				a.AtlasY == b.AtlasY &&
				a.ConnectorID == b.ConnectorID &&
				a.TextAttributes == b.TextAttributes &&
				a.CharacterBank == b.CharacterBank;
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
			string s = $"{AtlasX},{AtlasY},{ConnectorID},{TextAttributes.GetHashCode()},{CharacterBank}";
			return s.GetHashCode();
		}

		public readonly bool IsSpace(ITerminalDefinition terminalDefinition = null)
		{
			if (terminalDefinition == null)
			{
				terminalDefinition = _terminalDefinition;
			}
			if (terminalDefinition == null)
			{
				throw new System.Exception("TerminalDefinition is null in TerminalBufferValue.IsSpace");
			}
			return GetChar(terminalDefinition) == ' ' && CharacterBank == TerminalCharacterBank.ASCII;
		}
	}
}
