Shader "InGameTerminal/VT320 Fancy 2"
{
	Properties
	{
	_MainTex ("Atlas Texture", 2D) = "white" {}
	_Color ("Tint", Color) = (1,1,1,1)
	[Toggle] _PixelSnap ("Pixel Snap", Float) = 1
	_MinPixelSize ("Min Screen Pixels Per Texel", Range(1, 4)) = 1.0
	_ScanlineGap ("Scanline Gap", Range(0, 0.5)) = 0
	_PixelRoundness ("Pixel Roundness", Range(0, 2)) = 1
	_RoundnessAspect ("Roundness Aspect (H/V)", Range(0.1, 10)) = 0.8
	_VerticalSpan ("Vertical Span (Texels)", Range(1, 16)) = 1
	_PassThrough ("Pass Through", Range(0, 1)) = 0
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
		
		Lighting Off
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
			float _MinPixelSize;
			float _ScanlineGap;
			float _PixelRoundness;
			float _VerticalSpan;
			
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
				o.color = v.color * _Color;
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;
				fixed4 col;
				
				if (_MinPixelSize > 1.01)
				{
					// Calculate UV derivatives to understand texel-to-pixel mapping
					float2 uvDdx = ddx(uv);
					float2 uvDdy = ddy(uv);
					
					// How much UV changes per screen pixel
					float uvPerPixelX = length(float2(uvDdx.x, uvDdy.x));
					float uvPerPixelY = length(float2(uvDdx.y, uvDdy.y));
					
					// Convert to texels per pixel
					float texelsPerPixelX = uvPerPixelX * _MainTex_TexelSize.z;
					float texelsPerPixelY = uvPerPixelY * _MainTex_TexelSize.w;
					
					// Calculate sample offset to check neighboring texels
					// We sample in a small neighborhood and take the maximum alpha (dilation)
					float2 texelSize = _MainTex_TexelSize.xy;
					
					// Sample center
					col = tex2D(_MainTex, uv);
					
					// Dilate by sampling neighbors - this ensures thin lines expand
					// The number of samples depends on MinPixelSize
					float halfSize = (_MinPixelSize - 1.0) * 0.5;
					
					// Sample in a cross pattern for efficiency
					// Each sample is offset by a fraction of a texel
					for (float ox = -halfSize; ox <= halfSize; ox += 1.0)
					{
						for (float oy = -halfSize; oy <= halfSize; oy += 1.0)
						{
							if (ox == 0.0 && oy == 0.0) continue; // Skip center, already sampled
							
							float2 offset = float2(ox * texelSize.x * texelsPerPixelX, 
												   oy * texelSize.y * texelsPerPixelY);
							fixed4 neighbor = tex2D(_MainTex, uv + offset);
							
							// Dilation: take maximum (brightest/most opaque) value
							// This expands bright features like lines
							col = max(col, neighbor);
						}
					}
				}
				else
				{
					col = tex2D(_MainTex, uv);
				}
				
				// Calculate position within the current texel (0 to 1)
				// For vertical span > 1, group multiple texels together
				float2 texelCoord = uv * float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
				float2 texelPos = frac(texelCoord);
				
				// Calculate position within vertical span group (0 to 1 across _VerticalSpan texels)
				float verticalGroupPos = frac(texelCoord.y / _VerticalSpan);
				
				// Apply vertical-only pixel rounding (horizontal scanline style)
				// This creates continuous horizontal lines while keeping vertical dot separation
				if (_PixelRoundness > 0.001)
				{
					// Only use vertical position for the dot effect
					// Convert vertical group position to -1 to 1 range (center at 0)
					float centeredY = verticalGroupPos * 2.0 - 1.0;

					// Calculate elliptical distance from vertical center
					// Scale Y by aspect ratio to create oval shape
					float scaledY = centeredY / _RoundnessAspect;
					float dist = abs(scaledY);
					
					// Create vertical falloff (rounded top and bottom, but continuous horizontally)
					//float edgeStart = 1.0 - _PixelRoundness * 0.5;
					float edgeStart = 1.0 - _PixelRoundness;
					float dotMask = 1.0 - smoothstep(edgeStart, 1.0, dist);
					
					col.rgb *= dotMask;
					
					// Round horizontal edges where pixel meets non-pixel (capsule/stadium shape)
					// Sample neighboring texels to detect horizontal boundaries
					float2 texelSize = _MainTex_TexelSize.xy;
					fixed4 leftNeighbor = tex2D(_MainTex, uv - float2(texelSize.x, 0));
					fixed4 rightNeighbor = tex2D(_MainTex, uv + float2(texelSize.x, 0));
					
					// Detect if we're at a left or right edge (current pixel is lit, neighbor is not)
					float currentBrightness = max(col.r, max(col.g, col.b));
					float leftBrightness = max(leftNeighbor.r, max(leftNeighbor.g, leftNeighbor.b));
					float rightBrightness = max(rightNeighbor.r, max(rightNeighbor.g, rightNeighbor.b));
					
					float threshold = 0.01;
					bool isLeftEdge = (currentBrightness > threshold) && (leftBrightness < threshold);
					bool isRightEdge = (currentBrightness > threshold) && (rightBrightness < threshold);
					
					// Convert horizontal texel position to -1 to 1 range
					float centeredX = texelPos.x * 2.0 - 1.0;
					if (isLeftEdge && centeredX < 0)
					{
						// Left edge: create elliptical shape on left side
						float2 edgePos = float2(centeredX, centeredY / _RoundnessAspect);
						float edgeDist = length(edgePos);
						float edgeMask = 1.0 - smoothstep(edgeStart, 1.0, edgeDist);
						col.rgb *= edgeMask / max(dotMask, 0.001);
					}

					if (isRightEdge && centeredX > 0)
					{
						// Right edge: create elliptical shape on right side
						float2 edgePos = float2(centeredX, centeredY / _RoundnessAspect);
						float edgeDist = length(edgePos);
						float edgeMask = 1.0 - smoothstep(edgeStart, 1.0, edgeDist);
						col.rgb *= edgeMask / max(dotMask, 0.001);
					}
				}
				
				// Apply scanline gap effect
				if (_ScanlineGap > 0.001)
				{
					// Create gap at the bottom of each texel row
					// When texelPos.y is close to 1 (bottom of texel), darken the pixel
					float gapStart = 1.0 - _ScanlineGap;
					float scanlineMask = smoothstep(gapStart, 1.0, texelPos.y);
					
					// Darken the gap area
					col.rgb *= (1.0 - scanlineMask);
				}
				
				return col * i.color;
			}
			ENDCG
		}
		
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
			float _MinPixelSize;
			float _ScanlineGap;
			float _PixelRoundness;
			float _VerticalSpan;
			float _PassThrough;
			
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
				//o.uv = float2(1.0-o.uv.x, 1.0-o.uv.y);
				v.color.a = 1.0;
				_Color.a = 1.0;
				o.color = v.color * _Color;
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;
				fixed4 col;

				if (_PassThrough > 0.5)
				{
					return tex2D(_MainTex, uv) * i.color;
				}
				
			   
				col = tex2D(_MainTex, uv);
				
				
				// Calculate position within the current texel (0 to 1)
				// For vertical span > 1, group multiple texels together
				float2 texelCoord = uv * float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
				float2 texelPos = frac(texelCoord);
				
				// Calculate position within vertical span group (0 to 1 across _VerticalSpan texels)
				float verticalGroupPos = frac(texelCoord.y / _VerticalSpan);
				
				// Apply vertical-only pixel rounding (horizontal scanline style)
				// This creates continuous horizontal lines while keeping vertical dot separation
				if (_PixelRoundness > 0.001)
				{
					// Only use vertical position for the dot effect
					// Convert vertical group position to -1 to 1 range (center at 0)
					float centeredY = verticalGroupPos * 2.0 - 1.0;

					// Calculate elliptical distance from vertical center
					// Scale Y by aspect ratio to create oval shape
					float scaledY = centeredY / _RoundnessAspect;
					float dist = abs(scaledY);
					
					// Create vertical falloff (rounded top and bottom, but continuous horizontally)
					//float edgeStart = 1.0 - _PixelRoundness * 0.5;
					float edgeStart = 1.0 - _PixelRoundness;
					float dotMask = 1.0 - smoothstep(edgeStart, 1.0, dist);
					
					col.rgb *= dotMask;
					
					// Round horizontal edges where pixel meets non-pixel (capsule/stadium shape)
					// Sample neighboring texels to detect horizontal boundaries
					float2 texelSize = _MainTex_TexelSize.xy;
					fixed4 leftNeighbor = tex2D(_MainTex, uv - float2(texelSize.x, 0));
					fixed4 rightNeighbor = tex2D(_MainTex, uv + float2(texelSize.x, 0));
					
					// Detect if we're at a left or right edge (current pixel is lit, neighbor is not)
					float currentBrightness = max(col.r, max(col.g, col.b));
					float leftBrightness = max(leftNeighbor.r, max(leftNeighbor.g, leftNeighbor.b));
					float rightBrightness = max(rightNeighbor.r, max(rightNeighbor.g, rightNeighbor.b));
					
					float threshold = 0.01;
					bool isLeftEdge = (currentBrightness > threshold) && (leftBrightness < threshold);
					bool isRightEdge = (currentBrightness > threshold) && (rightBrightness < threshold);
					
					// Convert horizontal texel position to -1 to 1 range
					float centeredX = texelPos.x * 2.0 - 1.0;
					if (isLeftEdge && centeredX < 0)
					{
						// Left edge: create elliptical shape on left side
						float2 edgePos = float2(centeredX, centeredY / _RoundnessAspect);
						float edgeDist = length(edgePos);
						float edgeMask = 1.0 - smoothstep(edgeStart, 1.0, edgeDist);
						col.rgb *= edgeMask / max(dotMask, 0.001);
					}

					if (isRightEdge && centeredX > 0)
					{
						// Right edge: create elliptical shape on right side
						float2 edgePos = float2(centeredX, centeredY / _RoundnessAspect);
						float edgeDist = length(edgePos);
						float edgeMask = 1.0 - smoothstep(edgeStart, 1.0, edgeDist);
						col.rgb *= edgeMask / max(dotMask, 0.001);
					}
				}
				
				// Apply scanline gap effect
				if (_ScanlineGap > 0.001)
				{
					// Create gap at the bottom of each texel row
					// When texelPos.y is close to 1 (bottom of texel), darken the pixel
					float gapStart = 1.0 - _ScanlineGap;
					float scanlineMask = smoothstep(gapStart, 1.0, texelPos.y);
					
					// Darken the gap area
					col.rgb *= (1.0 - scanlineMask);
				}
				
				return col * i.color;
			}
			ENDCG
		}
	}
	
	Fallback "UI/Default"
}
