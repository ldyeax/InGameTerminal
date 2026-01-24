Shader "InGameTerminal/VT320 Fancy 2"
{
	Properties
	{
	_MainTex ("Atlas Texture", 2D) = "white" {}
	_Color ("Tint", Color) = (1,1,1,1)
	[Toggle] _PixelSnap ("Pixel Snap", Float) = 1
	_ScanlineGap ("Scanline Gap", Range(0, 1)) = 0
	_PixelRoundness ("Pixel Roundness", Range(0, 2)) = 1
	_RoundnessAspect ("Roundness Aspect (H/V)", Range(0.1, 10)) = 0.8
	_VerticalSpan ("Vertical Span (Texels)", Range(0.1, 4)) = 1
	_PassThrough ("Pass Through", Range(0, 1)) = 0
	_Threshold ("Threshold", Range(0,1)) = 0.8
	_BlurFactorX ("Blur Factor X", Range(0, 10)) = 0
	_BlurFactorY ("Blur Factor Y", Range(0, 10)) = 0
	_RoundnessType ("Roundness Type", Range(0,2)) = 0
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
		
		Blend Off
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "common.hlsl"
			
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
			float _Threshold;
			float _RoundnessType;
			float _BlurFactorX;
			float _BlurFactorY;
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

				if (_PassThrough > 0.5)
				{
					return tex2D(_MainTex, uv) * i.color;
				}
				
				fixed4 colorFloor = 0;
				col = tex2D(_MainTex, uv);

				// Calculate UV derivatives to understand texel-to-pixel mapping
				float2 uvDdx = ddx(uv);
				float2 uvDdy = ddy(uv);
					
				// How much UV changes per screen pixel
				float uvPerPixelX = length(float2(uvDdx.x, uvDdy.x));
				float uvPerPixelY = length(float2(uvDdx.y, uvDdy.y));
					
				// Convert to texels per pixel
				float texelsPerPixelX = uvPerPixelX * _MainTex_TexelSize.z;
				float texelsPerPixelY = uvPerPixelY * _MainTex_TexelSize.w;

				float texelsPerPixelMax = max(texelsPerPixelX, texelsPerPixelY);
				
				// if (texelsPerPixelMax > 1.0)
				// {
				// 	float smoothsteppedDistanceFactor = smoothstep(0.0, 6, texelsPerPixelMax - 1.0)/6.0;
				// 	col += smoothsteppedDistanceFactor*i.color;
				// 	CHECKMAX(col);
				// 	col.a = 1;
				// 	return col;
				// }

				if (texelsPerPixelMax > 1.0)
				{
					// col += i.color * smoothstep(0, 1, texelsPerPixelMax - 32.0);
					// CHECKMAX(col);
					// colorFloor = col;
				}

				// Calculate position within the current texel (0 to 1)
				// For vertical span > 1, group multiple texels together
				float2 texelCoord = uv * float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
				float2 texelPos = frac(texelCoord);
				
				// Calculate position within vertical span group (0 to 1 across _VerticalSpan texels)
				//float verticalGroupPos = frac(texelCoord.y / _VerticalSpan);
				float verticalGroupPos = frac(uv.y*24*12);


				//return float4(verticalGroupPos, 0, 0, 1);
				
				// Apply vertical-only pixel rounding (horizontal scanline style)
				// This creates continuous horizontal lines while keeping vertical dot separation
				float scanlineMask = smoothstep(0.0, 1-_ScanlineGap, abs(0.5-verticalGroupPos)*2.0);

				// // Apply scanline gap effect
				if (_ScanlineGap < 1.0)
				{
					// Create gap at the bottom of each texel row
					// When texelPos.y is close to 1 (bottom of texel), darken the pixel
					//float gapStart = 1.0 - _ScanlineGap;
					//float scanlineMask = smoothstep(gapStart, verticalGroupPos, texelPos.y)/_VerticalSpan;
					
					
					// Darken the gap area
					col.rgb *= (1.0 - scanlineMask);
				}

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
					float edgeStart = (1.0)- _PixelRoundness;
					float dotMask = (1.0) - smoothstep(edgeStart, 1.0, dist);
					
					//col.rgb *= dotMask;
					
					// Round horizontal edges where pixel meets non-pixel (capsule/stadium shape)
					// Sample neighboring texels to detect horizontal boundaries
					float2 texelSize = _MainTex_TexelSize.xy;
					fixed4 leftNeighbor = tex2D(_MainTex, uv - float2(texelSize.x*1.0, 0));
					fixed4 leftLeftNeighbor = tex2D(_MainTex, uv - float2(texelSize.x*1.5, 0));
					fixed4 rightNeighbor = tex2D(_MainTex, uv +  float2(texelSize.x*1.0, 0));
					fixed4 rightRightNeighbor = tex2D(_MainTex, uv + float2(texelSize.x*1.5, 0));
					
					// Detect if we're at a left or right edge (current pixel is lit, neighbor is not)
					float currentBrightness = max(col.r, max(col.g, col.b)) * col.a;
					float leftBrightness = max(leftNeighbor.r, max(leftNeighbor.g, leftNeighbor.b)) * leftNeighbor.a;
					float leftLeftBrightness = max(leftLeftNeighbor.r, max(leftLeftNeighbor.g, leftLeftNeighbor.b)) * leftLeftNeighbor.a;
					float rightBrightness = max(rightNeighbor.r, max(rightNeighbor.g, rightNeighbor.b)) * rightNeighbor.a;
					float rightRightBrightness = max(rightRightNeighbor.r, max(rightRightNeighbor.g, rightRightNeighbor.b)) * rightRightNeighbor.a;

					//col.r = leftNeighbor.g;

					float threshold = 0.01;
					bool isLeftEdge = (currentBrightness > threshold) && (leftBrightness < threshold);
					bool isRightEdge = (currentBrightness > threshold) && (rightBrightness < threshold);
					
					if (_RoundnessType < 1.0)
					{
						if (currentBrightness < threshold)
						{
							float4 c1 = float4(0,0,0,0);
							float4 c2 = float4(0,0,0,0);
							float4 c = float4(0,0,0,0);
							float centeredX = texelPos.x * 2.0 - 1.0;
							if (leftBrightness > threshold)
							{
								return float4(1,0,0,1);
								// Left edge: create elliptical shape on left side
								float2 edgePos = float2(centeredX, centeredY / _RoundnessAspect);
								float edgeDist = length(edgePos);
								float edgeMask = 1.0 - smoothstep(edgeStart, 1.0, edgeDist);
								c1 = edgeMask / max(dotMask, 0.001);
								c += c1;
								c.a += 1.0;
							}

							if (rightBrightness > threshold)
							{
								return float4(0,1,1,1);
								// Right edge: create elliptical shape on right side
								float2 edgePos = float2(centeredX, centeredY / _RoundnessAspect);
								float edgeDist = length(edgePos);
								float edgeMask = 1.0 - smoothstep(edgeStart, 1.0, edgeDist);
								c2 = edgeMask / max(dotMask, 0.001);
								c += c2;
								c.a += 1.0;
							}
							if (c.a > 0)
							{
								c /= c.a;
								col.rgb *= c;
							}
						}
						
					}
					else
					{
						// Convert horizontal texel position to -1 to 1 range
						float centeredX = texelPos.x * 2.0 - 1.0;

						if (leftBrightness < threshold && rightBrightness < threshold)
						{
							if (currentBrightness > threshold)
							{
								// create a lone circle
								float2 edgePos = float2(centeredX, centeredY / _RoundnessAspect);
								float edgeDist = length(edgePos);
								float edgeMask = 1.0 - smoothstep(0, 1.01, edgeDist);
								col.rgb *= edgeMask / (max(dotMask, 0.001));
							}
						}
						
						if (leftBrightness > threshold && rightBrightness > threshold)
						{
							float2 edgePos = float2(0, centeredY / _RoundnessAspect);
							float edgeDist = length(edgePos);
							float edgeMask = 1.0 - smoothstep(0, 1.01, edgeDist);

							col.rgb *= edgeMask / (max(dotMask, 0.001));
						}
						else
						{
							//return float4(texelPos.x, texelPos.y, 0, 1);
						
							if (leftBrightness > threshold)
							{
								float2 edgePos = float2(texelPos.x, centeredY / _RoundnessAspect);
								float edgeDist = length(edgePos);
								float edgeMask = 1.0 - smoothstep(0, 0.99, edgeDist);

								col.rgb *= edgeMask / (max(dotMask, 0.001));
							}
							if (rightBrightness > threshold)
							{
								float2 edgePos = float2(1-texelPos.x, centeredY / _RoundnessAspect);
								float edgeDist = length(edgePos);
								float edgeMask = 1.0 - smoothstep(0, 0.99, edgeDist);
								col.rgb *= edgeMask / max(dotMask, 0.001);
							}
						}

					}
				}
				


				half4 color_ret = col * i.color;
				// if (color_ret.r >= _Threshold)
				// {
				// 	color_ret.r = 1.0;
				// }
				// if (color_ret.g >= _Threshold)
				// {
				// 	color_ret.g = 1.0;
				// }
				// if (color_ret.b >= _Threshold)
				// {
				// 	color_ret.b = 1.0;
				// }
				color_ret.a = 1.0;
				color_ret = max(color_ret, colorFloor);
				return color_ret;
			}
			ENDCG
		}

		// horizontal Gaussian blur
		Pass
		{
			// blend add 
			Blend One One
			CGPROGRAM
			#pragma vertex vert_blur
			#pragma fragment frag_blur_h
			
			#include "UnityCG.cginc"
			#include "common.hlsl"
			
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float _BlurFactorX;
			float _Threshold;
			
			struct appdata_blur
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			
			struct v2f_blur
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};
			
			v2f_blur vert_blur(appdata_blur v)
			{
				v2f_blur o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}
			
			// 9-tap Gaussian weights (approx sigma ~ 2.0)
			static const float w0 = 0.2270270270; // center
			static const float w1 = 0.1945945946;
			static const float w2 = 0.1216216216;
			static const float w3 = 0.0540540541;
			static const float w4 = 0.0162162162;
			
			float4 Blur1D(float2 uv, float2 dir, float amount)
			{
				float2 stepUV = dir * _MainTex_TexelSize.xy * amount;
				
				float4 c = tex2D(_MainTex, uv) * w0;
				c += tex2D(_MainTex, uv + stepUV * 1.0) * w1;
				c += tex2D(_MainTex, uv - stepUV * 1.0) * w1;
				c += tex2D(_MainTex, uv + stepUV * 2.0) * w2;
				c += tex2D(_MainTex, uv - stepUV * 2.0) * w2;
				c += tex2D(_MainTex, uv + stepUV * 3.0) * w3;
				c += tex2D(_MainTex, uv - stepUV * 3.0) * w3;
				c += tex2D(_MainTex, uv + stepUV * 4.0) * w4;
				c += tex2D(_MainTex, uv - stepUV * 4.0) * w4;
				
				return c;
			}

			float4 frag_blur_h (v2f_blur i) : SV_Target
			{
				if (_BlurFactorX < 0.1)
				{
					discard;return float4(0,0,0,0);
				}

				float4 col = tex2D(_MainTex, i.uv);
				// if (col.r + col.g + col.b < .03 || _BlurFactor < .01)
				// {
				// 	discard;return float4(0,0,0,0);
				// }
					
				// Calculate UV derivatives to understand texel-to-pixel mapping
				float2 uvDdx = ddx(i.uv);
				float2 uvDdy = ddy(i.uv);

				// How much UV changes per screen pixel
				float uvPerPixelX = length(float2(uvDdx.x, uvDdy.x));
				float uvPerPixelY = length(float2(uvDdx.y, uvDdy.y));

				// Convert to texels per pixel
				//float texelsPerPixelX = uvPerPixelX * _MainTex_TexelSize.z;
				float texelsPerPixelX = uvPerPixelX * _MainTex_TexelSize.z;
				texelsPerPixelX = abs(texelsPerPixelX);

				GAUSS(texelsPerPixelX, float2(1,0), _BlurFactorX);

				discard;return float4(0,0,0,0);
			}
			ENDCG
		}

		// // vertical Gaussian blur
		// Pass
		// {
		// 	Blend One One
		// 	CGPROGRAM
		// 	#pragma vertex vert_blur
		// 	#pragma fragment frag_blur_v
			
		// 	#include "UnityCG.cginc"
		// 	#include "common.hlsl"
			
		// 	sampler2D _MainTex;
		// 	float4 _MainTex_TexelSize;
		// 	float _BlurFactorY;
		// 	float _Threshold;
			
		// 	struct appdata_blur
		// 	{
		// 		float4 vertex : POSITION;
		// 		float2 uv : TEXCOORD0;
		// 	};
			
		// 	struct v2f_blur
		// 	{
		// 		float4 vertex : SV_POSITION;
		// 		float2 uv : TEXCOORD0;
		// 		float3 worldPos : TEXCOORD1;
		// 	};
			
		// 	v2f_blur vert_blur(appdata_blur v)
		// 	{
		// 		v2f_blur o;
		// 		o.vertex = UnityObjectToClipPos(v.vertex);
		// 		o.uv = v.uv;
		// 		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		// 		return o;
		// 	}
			
		// 	// 9-tap Gaussian weights (approx sigma ~ 2.0)
		// 	static const float w0 = 0.2270270270; // center
		// 	static const float w1 = 0.1945945946;
		// 	static const float w2 = 0.1216216216;
		// 	static const float w3 = 0.0540540541;
		// 	static const float w4 = 0.0162162162;
			
		// 	float4 Blur1D(float2 uv, float2 dir, float amount)
		// 	{
		// 		float2 stepUV = dir * _MainTex_TexelSize.xy * amount;
				
		// 		float4 c = tex2D(_MainTex, uv) * w0;
		// 		c += tex2D(_MainTex, uv + stepUV * 1.0) * w1;
		// 		c += tex2D(_MainTex, uv - stepUV * 1.0) * w1;
		// 		c += tex2D(_MainTex, uv + stepUV * 2.0) * w2;
		// 		c += tex2D(_MainTex, uv - stepUV * 2.0) * w2;
		// 		c += tex2D(_MainTex, uv + stepUV * 3.0) * w3;
		// 		c += tex2D(_MainTex, uv - stepUV * 3.0) * w3;
		// 		c += tex2D(_MainTex, uv + stepUV * 4.0) * w4;
		// 		c += tex2D(_MainTex, uv - stepUV * 4.0) * w4;
				
		// 		return c;
		// 	}

		// 	float4 frag_blur_v (v2f_blur i) : SV_Target
		// 	{
		// 		if (_BlurFactorY < .1) {
		// 			discard;
		// 			return float4(0,0,0,0);
		// 		}
		// 		float4 col = tex2D(_MainTex, i.uv);
		// 		// if (col.r + col.g + col.b < .03 || _BlurFactor < .01)
		// 		// {
		// 		// 	discard;return float4(0,0,0,0);
		// 		// }

		// 		// Calculate UV derivatives to understand texel-to-pixel mapping
		// 		float2 uvDdx = ddx(i.uv);
		// 		float2 uvDdy = ddy(i.uv);

		// 		// How much UV changes per screen pixel
		// 		float uvPerPixelX = length(float2(uvDdx.x, uvDdy.x));
		// 		float uvPerPixelY = length(float2(uvDdx.y, uvDdy.y));
					
		// 		// Convert to texels per pixel
		// 		float texelsPerPixelY = abs(uvPerPixelY * _MainTex_TexelSize.w);

		// 		GAUSS(texelsPerPixelY, float2(0,1), _BlurFactorY);

		// 		discard;return float4(0,0,0,0);
		// 	}
		// 	ENDCG
		// }
	}
	
	Fallback "UI/Default"
}
