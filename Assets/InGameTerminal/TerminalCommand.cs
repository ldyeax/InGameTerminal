
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

		ShowCursor,
		HideCursor,

		/// <summary>
		/// DCS is the 8-bit control code Device Control Sequence, which can be represented by the 7-bit sequence ESC P.
		/// </summary>
		DeviceControlSequence,
		/// <summary>
		/// <para>Selects the DRCS font buffer to load.</para>
		/// <para>The VT320 has one DRCS font buffer. Pfn has two valid values, 0 and 1. Both values refer to the same DRCS buffer.</para>
		/// </summary>
		Param_FontNumber,
		/// <summary>
		/// <para>Selects where to load the first character in the DRCS font buffer.</para>
		/// <para>The location corresponds to a location in the ASCII code table and is affected by character set size (Pcss).</para>
		/// </summary>
		Param_StartingCharacter,
		/// <summary>
		/// <para>Selects which characters to erase from the DRCS buffer before loading the new font.</para>
		/// <para>0 = erase all characters in the DRCS buffer with this number, width, and rendition.</para>
		/// <para>1 = erase only characters in locations being reloaded.</para>
		/// <para>2 = erase all renditions of the soft character set (normal, bold, 80-column, 132-column).</para>
		/// </summary>
		Param_EraseControl,
		/// <summary>
		/// <para>Selects the maximum character cell width.</para>
		/// <para>0 = default (15 pixels for 80 columns, 9 pixels for 132 columns). Values 2-15 set explicit width; values over 15 are illegal.</para>
		/// </summary>
		Param_CharacterMatrixWidth,
		/// <summary>
		/// <para>Selects the number of columns per line (font set size).</para>
		/// <para>0/1 = 80 columns (default). 2 = 132 columns.</para>
		/// </summary>
		Param_FontWidth,
		/// <summary>
		/// <para>Defines the font as a text font or full-cell font.</para>
		/// <para>0/1 = text (default). 2 = full cell.</para>
		/// </summary>
		Param_TextOrFullCell,
		/// <summary>
		/// <para>Selects the maximum character cell height.</para>
		/// <para>0 = 12 pixels high (default). Values 1-12 set explicit height; values over 12 are illegal.</para>
		/// </summary>
		Param_CharacterMatrixHeight,
		/// <summary>
		/// <para>Defines the character set as a 94- or 96-character graphic set.</para>
		/// <para>0 = 94-character set (default). 1 = 96-character set.</para>
		/// </summary>
		Param_CharacterSetSize,
		/// <summary>
		/// <para>Defines the character set name used in the Select Character Set (SCS) sequence.</para>
		/// <para>One to three characters; the final character is in the range '0' to '~'.</para>
		/// </summary>
		CharacterSetName,

		/// <summary>
		/// ST is the 8-bit control code String Terminator, which can be represented by the 7-bit sequence ESC \.
		/// </summary>
		StringTerminator,
	}
	public struct TerminalCommand
	{
		public TerminalCommandType CommandType;
		public int X;
		public int Y;
		public int Z;
		public override readonly string ToString()
		{
			return CommandType.ToString() + "(" + X + ", " + Y + ", " + Z + ")";
		}
	}
}

