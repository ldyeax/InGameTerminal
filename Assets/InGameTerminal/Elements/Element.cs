using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGameTerminal
{
	[ExecuteAlways]
	public abstract class Element : MonoBehaviour
	{
		[SerializeField]
		public RectTransform RectTransform;
		virtual protected void EnsureSetup()
		{
			RectTransform = Util.GetOrCreateComponent<RectTransform>(gameObject);
		}
		private void Reset() => EnsureSetup();
		private void OnValidate() => EnsureSetup();
		private void Awake() { if (!Application.isPlaying) EnsureSetup(); }
		public Vector2 GetPixelPosition()
		{
			float x = RectTransform.offsetMin.x;
			float y = -RectTransform.offsetMax.y;
			var ret = new Vector2Int((int)x, (int)y);
			return ret;
		}
		public Rect GetPixelBounds()
		{
			float x = RectTransform.offsetMin.x;
			float y = -RectTransform.offsetMax.y;
			float width = RectTransform.offsetMax.x - RectTransform.offsetMin.x;
			float height = RectTransform.offsetMax.y - RectTransform.offsetMin.y;
			
			var ret = new Rect(x, y, width, height);
			return ret;
		}
		public Vector2Int GetTerminalPosition(ITerminalDefinition terminalDefinition)
		{
			Vector2Int ret = new Vector2Int();
			var pixelPos = GetPixelPosition();
			ret.x = (int)(pixelPos.x / terminalDefinition.GlyphWidth);
			ret.y = (int)(pixelPos.y / terminalDefinition.GlyphHeight);
			return ret;
		}
		public RectInt GetTerminalBounds(ITerminalDefinition terminalDefinition)
		{
			var pixelBounds = GetPixelBounds();
			RectInt ret = new RectInt();
			ret.x = (int)(pixelBounds.xMin / terminalDefinition.GlyphWidth);
			ret.y = (int)(pixelBounds.yMin / terminalDefinition.GlyphHeight);
			ret.width = (int)(pixelBounds.width / terminalDefinition.GlyphWidth);
			ret.height = (int)(pixelBounds.height / terminalDefinition.GlyphHeight);
			return ret;
		}
		/// <summary>
		/// 1 for beyond, -1 for before, 0 for inside
		/// </summary>
		public int ContainsPixelX(float x)
		{
			var pixelBounds = GetPixelBounds();
			//return x + 0.5f >= pixelBounds.xMin && x + 0.5f < pixelBounds.xMax;
			x += 0.5f;
			if (x < pixelBounds.xMin)
			{
				return -1;
			}
			if (x >= pixelBounds.xMax)
			{
				return 1;
			}
			return 0;
		}
		/// <summary>
		/// 1 for beyond, -1 for before, 0 for inside
		/// </summary>
		public int ContainsPixelY(float y)
		{
			var pixelBounds = GetPixelBounds();
			//return y + 0.5f >= pixelBounds.yMin && y + 0.5f < pixelBounds.yMax;
			y += 0.5f;
			if (y < pixelBounds.yMin)
			{
				return -1;
			}
			if (y >= pixelBounds.yMax)
			{
				return 1;
			}
			return 0;
		}

		public virtual void Align(ITerminalDefinition terminalDefinition)
		{
			Vector2 offsetMin = RectTransform.offsetMin;
			Util.Align(terminalDefinition, ref offsetMin);
			Vector2 offsetMax = RectTransform.offsetMax;
			Util.Align(terminalDefinition, ref offsetMax);

			// Bounds check - no zero size, no invalid negative rectangle
			if (offsetMin.y >= offsetMax.y || offsetMin.x >= offsetMax.x)
			{
				offsetMax = offsetMin;
				offsetMax.x += terminalDefinition.GlyphWidth;
				offsetMax.y += terminalDefinition.GlyphHeight;
			}
			// Debug.Log(offsetMax.y);

			//RectTransform.position = position;
			RectTransform.offsetMin = offsetMin;
			RectTransform.offsetMax = offsetMax;
			RectTransform.anchorMin = new Vector2(0, 1);
			RectTransform.anchorMax = new Vector2(0, 1);
			RectTransform.pivot = new Vector2(0, 1);
		}
	}
}
