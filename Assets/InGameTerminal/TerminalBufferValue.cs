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
			DeviceBytes = 0;
			DeviceByte1 = 0;
			DeviceByte2 = 0;
			DeviceByte3 = 0;
			DeviceByte4 = 0;
			CharacterBank = TerminalCharacterBank.ASCII;
			HasTerminalCommand = false;
			TerminalCommandType = TerminalCommandType.Char;
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
		public int DeviceBytes;
		public byte DeviceByte1;
		public byte DeviceByte2;
		public byte DeviceByte3;
		public byte DeviceByte4;
		public int AtlasX;
		public int AtlasY;
		public int ConnectorID;
		public TextAttributes TextAttributes;
		public bool HasTerminalCommand;
		public TerminalCommandType TerminalCommandType;

		public readonly Color AttributesToVertexColor()
		{
			return TextAttributes.AttributesToVertexColor();
		}

		public static bool operator ==(TerminalBufferValue a, TerminalBufferValue b)
		{
			return
				a.AtlasX == b.AtlasX &&
				a.AtlasY == b.AtlasY &&
				a.ConnectorID == b.ConnectorID &&
				a.TextAttributes == b.TextAttributes &&
				a.DeviceBytes == b.DeviceBytes &&
				a.DeviceByte1 == b.DeviceByte1 &&
				a.DeviceByte2 == b.DeviceByte2 &&
				a.DeviceByte3 == b.DeviceByte3 &&
				a.DeviceByte4 == b.DeviceByte4 &&
				a.CharacterBank == b.CharacterBank &&
				a.HasTerminalCommand == b.HasTerminalCommand &&
				a.TerminalCommandType == b.TerminalCommandType;
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
			string s = $"{AtlasX},{AtlasY},{ConnectorID},{TextAttributes.GetHashCode()},{DeviceBytes},{DeviceByte1},{DeviceByte2},{DeviceByte3},{DeviceByte4},{CharacterBank},{HasTerminalCommand},{TerminalCommandType}";
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
			return GetChar(terminalDefinition) == ' ' && !HasTerminalCommand && CharacterBank == TerminalCharacterBank.ASCII;
		}
	}
}
