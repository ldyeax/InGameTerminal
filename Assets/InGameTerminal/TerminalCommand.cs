namespace InGameTerminal
{
	public enum TerminalCommandType
	{
		Char = 0,
		Byte,

		/// <summary>
		/// Designate character banks for more efficient switching later
		/// </summary>
		InitBanks,

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
		/// <summary>
		/// EL - Erase in Line: CSI Ps K
		/// </summary>
		EL_CursorToEnd,
		EL_BeginningToCursor,
		/// <summary>
		/// ED - Erase in Display: CSI Ps J
		/// </summary>
		EraseInDisplay,
		HomeCursor,
		CharacterBank,

		Box_Horizontal,
		Box_Vertical,
		Box_TopLeftCorner,
		Box_TopRightCorner,
		Box_BottomLeftCorner,
		Box_BottomRightCorner,
		Box_Cross,
		Box_LeftTee,
		Box_RightTee,
		Box_UpTee,
		Box_DownTee,

	}
	public struct TerminalCommand
	{
		public TerminalCommandType CommandType;
		public int X;
		public int Y;
		public override string ToString()
		{
			return CommandType.ToString() + "(" + X + ", " + Y + ")";
		}
	}
}

