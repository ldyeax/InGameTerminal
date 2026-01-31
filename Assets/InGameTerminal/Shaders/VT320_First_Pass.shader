Shader "InGameTerminal/VT320 First Pass"
{
	Properties
	{
		_MainTex ("Atlas Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[Toggle] _PixelSnap ("Pixel Snap", Float) = 1
		_GlyphWidth ("Glyph Width", Float) = 15
		_GlyphHeight ("Glyph Height", Float) = 12
		_AtlasCols ("Atlas Columns", Float) = 32
		_AtlasRows ("Atlas Rows", Float) = 9
		_UnderlineRow ("Underline Row", Float) = 10
		_BlinkState ("Blink State", Float) = 1
	}
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			float _RoundnessAspect;
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
				float2 screenPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize; // x=1/width, y=1/height, z=width, w=height
			fixed4 _Color;
			float _PixelSnap;
			float _GlyphWidth;
			float _GlyphHeight;
			float _AtlasCols;
			float _AtlasRows;
			float _UnderlineRow;
			float _BlinkState;
			// Attribute bitmask (from TextAttributes.GetHashCode())
			// Bold: bit 0 (1), Italic: bit 1 (2), Underline: bit 2 (4)
			// Blink: bit 3 (8), Inverted: bit 4 (16)
			// PreviousItalic: bit 5 (32), NextItalic: bit 6 (64)
			#define BOLD_BIT 1
			#define ITALIC_BIT 2
			#define UNDERLINE_BIT 4
			#define BLINK_BIT 8
			#define INVERTED_BIT 16
			#define PREV_ITALIC_BIT 32
			#define NEXT_ITALIC_BIT 64
			#define IS_CURSOR_BIT 128
			#define PREV_BOLD_BIT 1
			#define NEXT_BOLD_BIT 2

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				// Store screen position before snapping for fragment shader
				o.screenPos = (o.vertex.xy / o.vertex.w + 1.0) * 0.5 * _ScreenParams.xy;

				// Pixel snap: round vertex position to nearest screen pixel
				if (_PixelSnap > 0.5)
				{
					// Snap to nearest pixel
					float2 snappedScreenPos = floor(o.screenPos + 0.5);

					// Convert back to clip space
					o.vertex.xy = (snappedScreenPos / _ScreenParams.xy * 2.0 - 1.0) * o.vertex.w;
				}

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// Decode attributes from vertex color channels
				// R: bitmask, G: previous atlas index, B: next atlas index, A: neighbor bold flags (prev=1, next=2)
				int attrBits = (int)(i.color.r * 255.0 + 0.5);
				int prevAtlasIndex = (int)(i.color.g * 255.0 + 0.5);
				int nextAtlasIndex = (int)(i.color.b * 255.0 + 0.5);
				int neighborBoldBits = (int)(i.color.a * 255.0 + 0.5);

				bool isBold = (attrBits & BOLD_BIT) != 0;
				bool isItalic = (attrBits & ITALIC_BIT) != 0;
				bool isUnderline = (attrBits & UNDERLINE_BIT) != 0;
				bool isBlink = (attrBits & BLINK_BIT) != 0;
				bool isInverted = (attrBits & INVERTED_BIT) != 0;
				bool isPrevItalic = (attrBits & PREV_ITALIC_BIT) != 0;
				bool isNextItalic = (attrBits & NEXT_ITALIC_BIT) != 0;
				bool isCursor = (attrBits & IS_CURSOR_BIT) != 0;
				bool isPrevBold = (neighborBoldBits & PREV_BOLD_BIT) != 0;
				bool isNextBold = (neighborBoldBits & NEXT_BOLD_BIT) != 0;

				// Position within the glyph (0 to 1)
				float2 glyphPos = frac(i.uv * float2(_AtlasCols, _AtlasRows));

				// Row within glyph (0 at top, _GlyphHeight-1 at bottom)
				// UV y=1 is top, y=0 is bottom, so invert
				float rowInGlyph = (1.0 - glyphPos.y) * _GlyphHeight;

				float2 uv = i.uv;
				fixed4 baseCol = fixed4(0,0,0,0);
				fixed4 prevBleedCol = fixed4(0,0,0,0);
				fixed4 nextBleedCol = fixed4(0,0,0,0);
				float2 uvUnit = float2(1.0/(_GlyphWidth*_AtlasCols), 0);
				float divisions = 12.0;

				// Calculate italic shear amount for current row
				// Positive = shift right (top rows), Negative = shift left (bottom rows)
				int shiftAmount = 0;
				for (float idx = 1.0; idx <= divisions; idx += 1.0)
				{
					if (glyphPos.y <= idx/divisions)
					{
						shiftAmount = (int)(0.5 * divisions - idx);
						break;
					}
				}

				// 1. ITALIC SHEAR - row-dependent horizontal shift
				bool skipFromItalic = false;
				if (isItalic)
				{
					if (shiftAmount < 0)
					{
						// Bottom rows shift left - check if we'd go into previous cell
						if (glyphPos.x < (-shiftAmount)/_GlyphWidth)
						{
							skipFromItalic = true;
						}
					}
					if (shiftAmount > 0)
					{
						// Top rows shift right - check if we'd go into next cell
						if (glyphPos.x > 1.0 - (shiftAmount)/_GlyphWidth)
						{
							skipFromItalic = true;
						}
					}
					if (!skipFromItalic)
					{
						uv += shiftAmount * uvUnit;
					}
				}

				if (skipFromItalic)
				{
					baseCol = fixed4(0,0,0,1);
				}
				else
				{
					// Sample the texture atlas
					baseCol = tex2D(_MainTex, uv);
				}

				// 2. Handle previous italic character bleeding into this cell
				// When previous char is italic, its bottom portion shifts left and bleeds into our left side
				if (isPrevItalic && shiftAmount < 0)
				{
					float bleedPixels = -shiftAmount;
					float bleedThreshold = bleedPixels / _GlyphWidth;

					if (glyphPos.x < bleedThreshold)
					{
						// Calculate UV for the previous character
						int prevAtlasX = prevAtlasIndex % (uint)_AtlasCols;
						int prevAtlasY = prevAtlasIndex / (uint)_AtlasCols;

						// Sample from the right edge of the previous glyph, shifted by italic amount
						float prevGlyphX = 1.0 - bleedThreshold + glyphPos.x;
						float2 prevUV;
						prevUV.x = (prevAtlasX + prevGlyphX) / _AtlasCols;
						prevUV.y = ((float)prevAtlasY + (1.0 - glyphPos.y)) / _AtlasRows;
						prevUV.y = 1.0 - prevUV.y; // Flip Y for atlas

						fixed4 prevCol = tex2D(_MainTex, prevUV);
						prevBleedCol = max(prevBleedCol, prevCol);
					}
				}

				// 3. Handle next italic character bleeding into this cell
				// When next char is italic, its top portion shifts right and bleeds into our right side
				if (isNextItalic && shiftAmount > 0)
				{
					float bleedPixels = shiftAmount;
					float bleedThreshold = 1.0 - bleedPixels / _GlyphWidth;

					if (glyphPos.x > bleedThreshold)
					{
						// Calculate UV for the next character
						int nextAtlasX = nextAtlasIndex % (uint)_AtlasCols;
						int nextAtlasY = nextAtlasIndex / (uint)_AtlasCols;

						// Sample from the left edge of the next glyph, shifted by italic amount
						float nextGlyphX = glyphPos.x - bleedThreshold;
						float2 nextUV;
						nextUV.x = (nextAtlasX + nextGlyphX) / _AtlasCols;
						nextUV.y = ((float)nextAtlasY + (1.0 - glyphPos.y)) / _AtlasRows;
						nextUV.y = 1.0 - nextUV.y; // Flip Y for atlas

						fixed4 nextCol = tex2D(_MainTex, nextUV);
						nextBleedCol = max(nextBleedCol, nextCol);
					}
				}

				glyphPos = frac(uv * float2(_AtlasCols, _AtlasRows));

				// Apply tint color
				baseCol *= _Color;
				prevBleedCol *= _Color;
				nextBleedCol *= _Color;

				// 4. UNDERLINE - force a specific row to be lit
				// The underline row is near the bottom of the cell
				if (isUnderline)
				{
					float rowPixel = floor(rowInGlyph);
					if (rowPixel >= _UnderlineRow && rowPixel < _UnderlineRow + 1.0)
					{
						// Force this row to be fully lit (white)
						baseCol.rgb = _Color.rgb;
						baseCol.a = 1.0;
					}
				}

				// 5. BOLD - maximize lit pixel intensities
				if (isBold)
				{
					if (baseCol.r > 0) baseCol.r = 1.0;
					if (baseCol.g > 0) baseCol.g = 1.0;
					if (baseCol.b > 0) baseCol.b = 1.0;
					if (baseCol.a > 0) baseCol.a = 1.0;
				}
				if (isPrevItalic && isPrevBold)
				{
					if (prevBleedCol.r > 0) prevBleedCol.r = 1.0;
					if (prevBleedCol.g > 0) prevBleedCol.g = 1.0;
					if (prevBleedCol.b > 0) prevBleedCol.b = 1.0;
					if (prevBleedCol.a > 0) prevBleedCol.a = 1.0;
				}
				if (isNextItalic && isNextBold)
				{
					if (nextBleedCol.r > 0) nextBleedCol.r = 1.0;
					if (nextBleedCol.g > 0) nextBleedCol.g = 1.0;
					if (nextBleedCol.b > 0) nextBleedCol.b = 1.0;
					if (nextBleedCol.a > 0) nextBleedCol.a = 1.0;
				}

				fixed4 col = max(baseCol, max(prevBleedCol, nextBleedCol));

				if (isBlink) {
					col *= _BlinkState;
				}

				if (isCursor && _BlinkState > 0) {
					col *= 0;
					if (_BlinkState > 0) {
						isInverted = !isInverted;
					}
				}

				// 6. INVERT - flip pixel values
				// Happens after all other attribute processing
				if (isInverted)
				{
					float3 subtractFrom = _Color.rgb;
					if (isBold)
					{
						if (subtractFrom.r > 0) subtractFrom.r = 1.0;
						if (subtractFrom.g > 0) subtractFrom.g = 1.0;
						if (subtractFrom.b > 0) subtractFrom.b = 1.0;
					}
					col.rgb = subtractFrom - col.rgb;
					// Inverted cells show background, so make fully opaque
					col.a = 1.0;
				}

				return col;
			}
			ENDHLSL
		}
	}
	Fallback "UI/Default"
}
