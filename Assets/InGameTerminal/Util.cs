using InGameTerminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace InGameTerminal
{
	public static class Util
	{
		public static T GetOrCreateComponent<T>(GameObject parent) where T : Component
		{
			T ret = parent.GetComponent<T>();
			if (!ret)
			{
				ret = parent.AddComponent<T>();
			}
			return ret;
		}
		public static void AlignX(this ITerminalDefinition terminalDefinition, ref float x)
		{
			x /= terminalDefinition.GlyphWidth;
			x = Mathf.Round(x);
			x *= terminalDefinition.GlyphWidth;
		}
		public static void AlignY(this ITerminalDefinition terminalDefinition, ref float y)
		{
			y /= terminalDefinition.GlyphHeight;
			y = Mathf.Round(y);
			y *= terminalDefinition.GlyphHeight;
		}
		public static void Align(Element element, ITerminalDefinition terminalDefinition)
		{
			var position = element.RectTransform.position;
			position.z = 0f;
			AlignX(terminalDefinition, ref position.x);
			AlignY(terminalDefinition, ref position.y);
			element.RectTransform.position = position;
		}
		public static void Align(ITerminalDefinition terminalDefinition, ref Vector3 position)
		{
			position.z = 0f;
			AlignX(terminalDefinition, ref position.x);
			AlignY(terminalDefinition, ref position.y);
		}
		public static void Align(ITerminalDefinition terminalDefinition, ref Vector2 position)
		{
			AlignX(terminalDefinition, ref position.x);
			AlignY(terminalDefinition, ref position.y);
		}
		public static void Align(ITerminalDefinition terminalDefinition, ref Rect rect)
		{
			float x = rect.x;
			float y = rect.y;
			AlignX(terminalDefinition, ref x);
			AlignY(terminalDefinition, ref y);
			float width = rect.width;
			float height = rect.height;
			AlignX(terminalDefinition, ref width);
			AlignY(terminalDefinition, ref height);
			rect.x = x;
			rect.y = y;
			rect.width = width;
			rect.height = height;
		}
		public static Vector2Int CharToXY(this ITerminalDefinition terminalDefinition, char c)
		{
			Vector2Int ret = new Vector2Int(-1, -1);
			if (terminalDefinition is IChartoXY chartoXY)
			{
				ret = chartoXY.CharToXY(c);
			}
			if (ret.x < 0 || ret.y < 0)
			{
				int index = (int)c;
				ret.x = index % terminalDefinition.AtlasCols;
				ret.y = index / terminalDefinition.AtlasCols;
			}
			return ret;
		}
		public static Vector2Int ByteToXY(this ITerminalDefinition terminalDefinition, byte b)
		{
			int index = (int)b;
			Vector2Int ret = new Vector2Int();
			ret.x = index % terminalDefinition.AtlasCols;
			ret.y = index / terminalDefinition.AtlasCols;
			
			return ret;
		}
		public static char XYToChar(this ITerminalDefinition terminalDefinition, int x, int y)
		{
			char ret = '\0';
			if (terminalDefinition is IChartoXY chartoXY)
			{
				ret = chartoXY.XYToChar(x, y);
			}
			if (ret == '\0')
			{
				ret = (char)(y * terminalDefinition.AtlasCols + x);
			}
			return ret;
		}
	}
}
