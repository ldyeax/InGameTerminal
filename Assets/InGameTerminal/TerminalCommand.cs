namespace InGameTerminal
{
	public enum TerminalCommandType
	{
		Char = 0,
		Up,
		Down,
		Left,
		Right,
		MoveTo,
		CarriageReturn,
		LineFeed,
		Italic,
		Bold,
		Underline,
		Invert,
		Blink,
		EL,
		EraseInDisplay,
		HomeCursor,
	}
	public struct TerminalCommand
	{
		public char Char;
		public TerminalCommandType CommandType;
		public int X;
		public int Y;
	}

}

