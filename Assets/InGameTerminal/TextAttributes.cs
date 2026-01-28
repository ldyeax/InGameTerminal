using UnityEngine;

namespace InGameTerminal
{
	public struct TextAttributes
	{
		public bool Bold;
		public bool Italic;
		public bool Underline;
		public bool Blink;
		public bool Inverted;
		public bool PreviousItalic;
		public bool NextItalic;
		/// <summary>
		/// Used only be meshrenderer, doesn't need to go in the actual vertex color
		/// </summary>
		public int ID;

		private readonly byte GetAttributeMask()
		{
			byte mask = 0;
			if (Bold) mask |= 1;
			if (Italic) mask |= 2;
			if (Underline) mask |= 4;
			if (Blink) mask |= 8;
			if (Inverted) mask |= 16;
			if (PreviousItalic) mask |= 32;
			if (NextItalic) mask |= 64;
			return mask;
		}

		public readonly UnityEngine.Color AttributesToVertexColor()
		{
			UnityEngine.Color ret = UnityEngine.Color.white;
			ret.r = GetAttributeMask() / 255.0f;
			return ret;
		}
		public readonly UnityEngine.Color AttributesToVertexColor32()
		{
			byte mask = GetAttributeMask();
			return new UnityEngine.Color32(mask, 255, 255, 255);
		}
		public static bool operator ==(TextAttributes a, TextAttributes b)
		{
			return a.Bold == b.Bold &&
				   a.Italic == b.Italic &&
				   a.Underline == b.Underline &&
				   a.Blink == b.Blink &&
				   a.Inverted == b.Inverted &&
				   a.PreviousItalic == b.PreviousItalic &&
				   a.NextItalic == b.NextItalic &&
				   a.ID == b.ID;
		}
		public static bool operator !=(TextAttributes a, TextAttributes b)
		{
			return !(a == b);
		}
		public override readonly bool Equals(object obj)
		{
			if (obj is TextAttributes tOther)
			{
				return this == tOther;
			}
			return false;
		}
		public override readonly int GetHashCode()
		{
			return (Bold ? 1 : 0) |
				   (Italic ? 2 : 0) |
				   (Underline ? 4 : 0) |
				   (Blink ? 8 : 0) |
				   (Inverted ? 16 : 0) |
				   (PreviousItalic ? 32 : 0) |
				   (NextItalic ? 64 : 0) |
				   (ID << 8);
		}
		public TextAttributes(Color32 color32)
		{
			int hash = color32.r;
			Bold = (hash & 1) != 0;
			Italic = (hash & 2) != 0;
			Underline = (hash & 4) != 0;
			Blink = (hash & 8) != 0;
			Inverted = (hash & 16) != 0;
			PreviousItalic = (hash & 32) != 0;
			NextItalic = (hash & 64) != 0;
			ID = -1;
		}
	}
}
