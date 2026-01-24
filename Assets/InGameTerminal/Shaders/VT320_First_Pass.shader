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
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			float _RoundnessAspect;
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
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
			
			// Attribute bit thresholds (from AttributesToVertexColor encoding)
			// Bold: 0.5, Italic: 0.25, Underline: 0.125, Blink: 0.0625, Inverted: 0.03125
			#define BOLD_THRESHOLD 0.375
			#define ITALIC_THRESHOLD 0.1875
			#define UNDERLINE_THRESHOLD 0.09375
			#define BLINK_THRESHOLD 0.046875
			#define INVERTED_THRESHOLD 0.0234375
			
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
				// Decode attributes from vertex color red channel
				float attrValue = i.color.r;
				bool isBold = attrValue >= BOLD_THRESHOLD;
				if (isBold) attrValue -= 0.5;
				bool isItalic = attrValue >= ITALIC_THRESHOLD;
				if (isItalic) attrValue -= 0.25;
				bool isUnderline = attrValue >= UNDERLINE_THRESHOLD;
				if (isUnderline) attrValue -= 0.125;
				bool isBlink = attrValue >= BLINK_THRESHOLD;
				if (isBlink) attrValue -= 0.0625;
				bool isInverted = attrValue >= INVERTED_THRESHOLD;
				
				// Position within the glyph (0 to 1)
				float2 glyphPos = frac(i.uv * float2(_AtlasCols, _AtlasRows));
				
				// Row within glyph (0 at top, _GlyphHeight-1 at bottom)
				// UV y=1 is top, y=0 is bottom, so invert
				float rowInGlyph = (1.0 - glyphPos.y) * _GlyphHeight;
				
				float2 uv = i.uv;
				fixed4 col;
				// 1. ITALIC SHEAR - row-dependent horizontal shift
				// Top rows: 0 shift, progressively more shift toward bottom
				// Shift ranges from 0 to ~3 pixels over glyph height
				
				bool skipFromItalic = false;
				if (isItalic)
				{
					// Calculate shear amount based on row (0 at top, increasing toward bottom)
					// We want: top=0, upper-middle=1, lower-middle=2, bottom=3 pixels
					// float shearPixels = floor(rowInGlyph / (_GlyphHeight / 4.0));
					// shearPixels = clamp(shearPixels, 0, 3);
					
					// // Convert pixel shift to UV shift
					// float shearUV = shearPixels / (_MainTex_TexelSize.z);
					// uv.x += shearUV;
					// if (glyphPos.y < 0.5)
					// {
					// 	uv.x += 1.0/(_GlyphWidth*_AtlasCols);
					// }
					// else if (glyphPos.y > 0.66)
					// {
					// 	uv.x += 1.0/(_GlyphWidth*_AtlasCols);
					// }
					// //return fixed4(0,0,glyphPos.y,1);

					float2 uvUnit = float2(1.0/(_GlyphWidth*_AtlasCols), 0);
					// if (glyphPos.y < 0.25)
					// {
					// 	if (glyphPos.x > 1.0 - 1.0/_GlyphWidth) {
					// 		skipFromItalic = true;
					// 	}
					// 	else {
					// 		uv += uvUnit * 1.0;
					// 	}
					// }
					// else if (glyphPos.y < 0.5)
					// {
					// 	uv += uvUnit * 0.5;
					// }
					// else if (glyphPos.y < 0.75)
					// {
					// 	if (glyphPos.x < 1.0/_GlyphWidth) {
					// 		skipFromItalic = true;
					// 	}
					// 	else {
					// 		uv -= uvUnit * 0.5;
					// 	}
						
					// }
					// else
					// {
					// 	if (glyphPos.x < 2.0/_GlyphWidth) {
					// 		skipFromItalic = true;
					// 	}
					// 	else {
					// 		uv -= uvUnit * 1.0;
					// 	}
					// }
					float divisions = 12;
					for (float i = 1; i <= divisions; i += 1.0)
					{
						if (glyphPos.y <= i/divisions)
						{
							//int shiftAmount = (int)((0.5*(divisions+1)) - i);
							int shiftAmount = (int)(0.5*(divisions ) - i);
							if (shiftAmount < 0)
							{
								// Check if we would shift outside glyph bounds
								if (glyphPos.x < (-shiftAmount)/_GlyphWidth)
								{
									skipFromItalic = true;
								}
							}
							if (shiftAmount > 0)
							{
								// Check if we would shift outside glyph bounds
								if (glyphPos.x > 1.0 - (shiftAmount)/_GlyphWidth)
								{
									skipFromItalic = true;
									break;
								}
							}
							uv += shiftAmount * uvUnit;
							break;
						}
					}
				}

				if (skipFromItalic)
				{
					col = fixed4(0,0,0,1);
				}
				else
				{
					// Sample the texture atlas
					col = tex2D(_MainTex, uv);
				}
				
				glyphPos = frac(uv * float2(_AtlasCols, _AtlasRows));
				

				// Apply tint color
				col *= _Color;
				

				
				// 3. UNDERLINE - force a specific row to be lit
				// The underline row is near the bottom of the cell
				if (isUnderline)
				{
					float rowPixel = floor(rowInGlyph);
					if (rowPixel >= _UnderlineRow && rowPixel < _UnderlineRow + 1.0)
					{
						// Force this row to be fully lit (white)
						col.rgb = _Color;
					}
				}

				// 2. BOLD - OR with pixel shifted right by 1
				// Moved after underline since we changed to just using max values
				// Sample the pixel one texel to the left and OR the intensities
				if (isBold)
				{
					// float2 boldUV = uv;
					// boldUV.x -= _MainTex_TexelSize.x; // Sample one pixel to the left
					// fixed4 boldCol = tex2D(_MainTex, boldUV);
					
					// // OR operation: take maximum of both pixels
					// col.rgb = max(col.rgb, boldCol.rgb);
					// col.a = max(col.a, boldCol.a);
					if (col.r > 0) col.r = 1.0;
					if (col.g > 0) col.g = 1.0;
					if (col.b > 0) col.b = 1.0;
					col.a = 1.0;
				}

				
				// 4. INVERT - flip pixel values
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
			ENDCG
		}
	}
	Fallback "UI/Default"
}
