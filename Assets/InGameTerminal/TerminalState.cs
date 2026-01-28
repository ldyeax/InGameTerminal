using UnityEngine;

namespace InGameTerminal
{
	public struct TerminalState
	{
		public TerminalBufferValue[,] terminalBuffer;
		public TerminalBufferValue[,] previousTerminalBuffer;
		public TextAttributes TextAttributes;
		public Vector2Int ExpectedTerminalPosition;
	}
}
