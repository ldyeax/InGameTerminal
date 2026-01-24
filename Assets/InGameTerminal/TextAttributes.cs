namespace InGameTerminal
{
	public struct TextAttributes
	{
		public bool Bold;
		public bool Italic;
		public bool Underline;
		public bool Blink;
		public bool Inverted;

		public readonly UnityEngine.Color AttributesToVertexColor()
		{
			// Pack attributes into vertex color
			// R: Bold (1 bit), Italic (1 bit), Underline (1 bit), Blink (1 bit), Inverted (1 bit), unused (3 bits)
			UnityEngine.Color ret = UnityEngine.Color.white;
			float r = 0.0f;
			if (Bold) r += 0.5f;
			if (Italic) r += 0.25f;
			if (Underline) r += 0.125f;
			if (Blink) r += 0.0625f;
			if (Inverted) r += 0.03125f;
			ret.r = r;
			return ret;
		}
		public static bool operator ==(TextAttributes a, TextAttributes b)
		{
			return a.Bold == b.Bold &&
				   a.Italic == b.Italic &&
				   a.Underline == b.Underline &&
				   a.Blink == b.Blink &&
				   a.Inverted == b.Inverted;
		}
		public static bool operator !=(TextAttributes a, TextAttributes b)
		{
			return !(a == b);
		}
		public override bool Equals(object obj)
		{
			if (obj is TextAttributes tOther)
			{
				return this == tOther;
			}
			return false;
		}
		public override int GetHashCode()
		{
			return (Bold ? 1 : 0) |
				   (Italic ? 2 : 0) |
				   (Underline ? 4 : 0) |
				   (Blink ? 8 : 0) |
				   (Inverted ? 16 : 0);
		}
	}
}
