using InGameTerminal.TerminalDefinitions;
using System.Collections.Generic;
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

#if UNITY_EDITOR
		public static List<T> GetAllScriptableObjectInstances<T>() where T : ScriptableObject
		{
			// Find all asset GUIDs of the specified type
			string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);

			List<T> assets = new List<T>();
			foreach (string guid in guids)
			{
				// Get the asset path from the GUID
				string path = AssetDatabase.GUIDToAssetPath(guid);
				// Load the asset at the path
				T asset = AssetDatabase.LoadAssetAtPath<T>(path);
				if (asset != null)
				{
					assets.Add(asset);
				}
			}
			return assets;
		}
		public static UnityTerminalDefinitionBase GetDefaultTerminalDefinition()
		{
			var allDefinitions = GetAllScriptableObjectInstances<VT320_ScriptableObject>();
			if (allDefinitions.Count > 0)
			{
				return allDefinitions[0];
			}
			Debug.LogError("No terminal definitions found in project. Please create at least one terminal definition.");
			return null;
		}
#endif
	}
}
