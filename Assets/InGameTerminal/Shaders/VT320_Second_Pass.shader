Shader "InGameTerminal/VT320 Second Pass"
{
	Properties
	{
	_MainTex ("Atlas Texture", 2D) = "white" {}
	_Color ("Tint", Color) = (1,1,1,1)
	[Toggle] _PixelSnap ("Pixel Snap", Float) = 1
	_ScanlineGap ("Scanline Gap", Range(0, 1)) = 0
	_PixelRoundness ("Pixel Roundness", Range(0, 2)) = 1
	_RoundnessAspect ("Roundness Aspect (H/V)", Range(0.1, 10)) = 0.8
	_PassThrough ("Pass Through", Range(0, 1)) = 0
	_Threshold ("Threshold", Range(0,1)) = 0.8
	_BlurFactorX ("Blur Factor X", Range(0, 10)) = 0
	_BlurFactorY ("Blur Factor Y", Range(0, 10)) = 0
	_MaxTexelCheck ("_MaxTexelCheck", Range(1, 250)) = 1
	_FadeStart ("Start fade to Glow", Range(0,100)) = 1
	_FadeEnd ("End fade to Glow", Range(0, 100)) = 5
	_MainFadeStart ("Main Fade Start", Range(0,100)) = 1
	_MainFadeEnd ("Main Fade End", Range(0, 100)) = 5
	_NeighborFactor ("Neighbor Factor", Range(0, 5)) = 0.5
	[Toggle] _MainPass ("Enable Main Pass", Float) = 1
	[Toggle] _PhosphorPass ("Enable Phosphor Pass", Float) = 1
	[Toggle] _BlurPass ("Enable Blur Pass", Float) = 0
	[Toggle] _FadePass ("Enable Fade Pass", Float) = 1
	[Toggle] _GaussXPass ("Enable Gaussian X Pass", Float) = 0
	[Toggle] _GaussYPass ("Enable Gaussian Y Pass", Float) = 0
	[Toggle] _HideSinglePixel ("Hide Single Pixel", Float) = 1
	[Toggle] _InvertUV ("Invert UV", Float) = 0
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
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "common.hlsl"

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

			float _ScanlineGap;
			float _PixelRoundness;

			float _PassThrough;
			float _Threshold;

			float _FadeStart;
			float _FadeEnd;

			float _MainPass;
			float _MainFadeStart;
			float _MainFadeEnd;

			float _HideSinglePixel;
			float _InvertUV;

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

				if (_InvertUV > 0.5)
				{
					o.uv = float2(1.0 - o.uv.x, 1.0 - o.uv.y);
				}

				o.color = v.color * _Color;

				//o.snappedTerminalPixel = floor(o.uv / TERMINAL_PIXEL_DELTA) * TERMINAL_PIXEL_DELTA;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;
				fixed4 col;

				float2 snappedTerminalPixel = floor(i.uv / TERMINAL_PIXEL_DELTA) * TERMINAL_PIXEL_DELTA;

				if (_MainPass < 0.5) {
					return fixed4(0,0,0,1);
				}

				if (_PassThrough > 0.5)
				{
					return tex2D(_MainTex, uv) * i.color;
				}

				col = tex2D(_MainTex, uv);

				float startRed = col.r;

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

				// Calculate position within the current texel (0 to 1)
				// For vertical span > 1, group multiple texels together
				float2 texelCoord = uv * float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
				float2 texelPos = frac(texelCoord);

				float verticalGroupPos = frac(uv.y*24*12);
				float horizontalGroupPos = frac(uv.x*80*15);

				//return fixed4(verticalGroupPos, 0, 0, 1);
				_ScanlineGap *= (1.0 - smoothstep(0, 1, texelsPerPixelY - 1.0));
				// Apply vertical-only pixel rounding (horizontal scanline style)
				// This creates continuous horizontal lines while keeping vertical dot separation
				float scanlineMask = smoothstep(0.0, 1-_ScanlineGap, abs(0.5-verticalGroupPos)*2.0);

				// // Apply scanline gap effect
				if (_ScanlineGap > 0.0)
				{
					// Darken the gap area
					col.rgb *= (1.0 - scanlineMask);
				}

				if (_PixelRoundness > 0.001)
				{
					// if (col.b > 0) {
					// 	return fixed4(0,0,0,0);
					// }
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

					float2 checkDelta = TERMINAL_PIXEL_DELTA;
					float2 centerOfTerminalPixel = snappedTerminalPixel + checkDelta * 0.5;
					float2 centerDelta = uv - centerOfTerminalPixel;
					centerDelta.y /= _RoundnessAspect;
					float normalizedCenterDist = length(centerDelta / (checkDelta));

					float2 verticalDelta = centerDelta;
					verticalDelta.x = 0;
					float normalizedVerticalDist = length(verticalDelta / (checkDelta));

					float2 leftEdge = float2(snappedTerminalPixel.x, centerOfTerminalPixel.y);
					float2 leftDelta = uv - leftEdge;
					leftDelta.y /= _RoundnessAspect;
					float normalizedLeftDist = length(leftDelta / (checkDelta));
					//normalizedLeftDist = horizontalGroupPos;

					float2 rightEdge = float2(snappedTerminalPixel.x + checkDelta.x, centerOfTerminalPixel.y);
					float2 rightDelta = uv - rightEdge;
					rightDelta.y /= _RoundnessAspect;
					float normalizedRightDist = length(rightDelta / (checkDelta));

					//return fixed4(0, normalizedLeftDist, 0, 1);



					float2 neighborDelta = checkDelta * 0.4;
					neighborDelta.y = 0;

					// Round horizontal edges where pixel meets non-pixel (capsule/stadium shape)
					// Sample neighboring texels to detect horizontal boundaries
					fixed4 leftNeighbor = tex2D(_MainTex, leftEdge - float2(neighborDelta.x, 0));
					//fixed4 leftLeftNeighbor = tex2D(_MainTex, centerOfTerminalPixel - float2(checkDelta.x*1.5, 0));
					fixed4 rightNeighbor = tex2D(_MainTex, rightEdge +  float2(neighborDelta.x*2, 0));
					//fixed4 rightRightNeighbor = tex2D(_MainTex, centerOfTerminalPixel + float2(checkDelta.x*1.5, 0));

					// Detect if we're at a left or right edge (current pixel is lit, neighbor is not)
					float currentBrightness = col.g;
					float leftBrightness = leftNeighbor.g;
					float rightBrightness = rightNeighbor.g;

					float threshold = 1e-5;

					float edgeDist = 0;

					if (leftBrightness < threshold && rightBrightness < threshold)
					{
						if (currentBrightness > threshold)
						{
							edgeDist = normalizedCenterDist;
						}
					}

					if (currentBrightness > threshold) {
						if (leftBrightness > threshold) {
							if (rightBrightness > threshold) {
								edgeDist = normalizedVerticalDist;
							}
							else {
								edgeDist = normalizedLeftDist;
							}
						}
						else if (rightBrightness > threshold) {
							edgeDist = normalizedRightDist;
						}
						else {
							if (_HideSinglePixel > 0.5) {
								return fixed4(startRed,0,0,1);
							}
							float2 dotDelta = centerDelta;
							dotDelta.x *= 2.0;
							edgeDist = length(dotDelta / (checkDelta));
						}
						col.rgb *= (1.0-edgeDist);
					}
				}

				fixed4 color_ret = col.g * i.color;

				float fadeOff = smoothstep(0, 1, (texelsPerPixelMax-_MainFadeStart)/(_MainFadeEnd - _MainFadeStart));

				color_ret *= (1.0 - fadeOff);

				color_ret.a = 1.0;
				color_ret.r = startRed;
				return color_ret;
			}
			ENDHLSL
		}


		Blend One One

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "common.hlsl"

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

			float _ScanlineGap;
			float _PixelRoundness;

			float _PassThrough;
			float _Threshold;

			float _FadeStart;
			float _FadeEnd;

			float _MainPass;
			float _MainFadeStart;
			float _MainFadeEnd;

			float _HideSinglePixel;
			float _InvertUV;

			float _PhosphorPass;

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

				if (_InvertUV > 0.5)
				{
					o.uv = float2(1.0 - o.uv.x, 1.0 - o.uv.y);
				}

				o.color = v.color * _Color;

				//o.snappedTerminalPixel = floor(o.uv / TERMINAL_PIXEL_DELTA) * TERMINAL_PIXEL_DELTA;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;
				fixed4 col;

				float2 snappedTerminalPixel = floor(i.uv / TERMINAL_PIXEL_DELTA) * TERMINAL_PIXEL_DELTA;

				if (_PhosphorPass < 0.5) {
					return fixed4(0,0,0,0);
				}

				if (_PassThrough > 0.5)
				{
					return tex2D(_MainTex, uv) * i.color;
				}

				col = tex2D(_MainTex, uv);

				// if (col.b < 0.01) {
				// 	return fixed4(0,0,0,0);
				// }
				// bool isFading = true;
				// if (col.g > 0) {
				// 	isFading = false;
				// }

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

				// Calculate position within the current texel (0 to 1)
				// For vertical span > 1, group multiple texels together
				float2 texelCoord = uv * float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
				float2 texelPos = frac(texelCoord);

				float verticalGroupPos = frac(uv.y*24*12);
				float horizontalGroupPos = frac(uv.x*80*15);

				//return fixed4(verticalGroupPos, 0, 0, 1);
				_ScanlineGap *= (1.0 - smoothstep(0, 1, texelsPerPixelY - 1.0));
				// Apply vertical-only pixel rounding (horizontal scanline style)
				// This creates continuous horizontal lines while keeping vertical dot separation
				float scanlineMask = smoothstep(0.0, 1-_ScanlineGap, abs(0.5-verticalGroupPos)*2.0);

				// // Apply scanline gap effect
				if (_ScanlineGap > 0.0)
				{
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

					float2 checkDelta = TERMINAL_PIXEL_DELTA;
					float2 centerOfTerminalPixel = snappedTerminalPixel + checkDelta * 0.5;
					float2 centerDelta = uv - centerOfTerminalPixel;
					centerDelta.y /= _RoundnessAspect;
					float normalizedCenterDist = length(centerDelta / (checkDelta));

					float2 verticalDelta = centerDelta;
					verticalDelta.x = 0;
					float normalizedVerticalDist = length(verticalDelta / (checkDelta));

					float2 leftEdge = float2(snappedTerminalPixel.x, centerOfTerminalPixel.y);
					float2 leftDelta = uv - leftEdge;
					leftDelta.y /= _RoundnessAspect;
					float normalizedLeftDist = length(leftDelta / (checkDelta));
					//normalizedLeftDist = horizontalGroupPos;

					float2 rightEdge = float2(snappedTerminalPixel.x + checkDelta.x, centerOfTerminalPixel.y);
					float2 rightDelta = uv - rightEdge;
					rightDelta.y /= _RoundnessAspect;
					float normalizedRightDist = length(rightDelta / (checkDelta));

					//return fixed4(0, normalizedLeftDist, 0, 1);



					float2 neighborDelta = checkDelta * 0.4;
					neighborDelta.y = 0;

					// Round horizontal edges where pixel meets non-pixel (capsule/stadium shape)
					// Sample neighboring texels to detect horizontal boundaries
					fixed4 leftNeighbor = tex2D(_MainTex, leftEdge - float2(neighborDelta.x, 0));
					fixed4 rightNeighbor = tex2D(_MainTex, rightEdge +  float2(neighborDelta.x*2, 0));

					float threshold = 1e-5;

					float currentBrightness = col.b;
					float currentBrightness_green = col.g;
					bool useCurrent = currentBrightness > threshold && currentBrightness_green < threshold;

					float leftBrightness = leftNeighbor.b;
					float leftBrightness_green = leftNeighbor.g;
					float rightBrightness = rightNeighbor.b;
					float rightBrightness_green = rightNeighbor.g;

					bool isLeftYounger = leftNeighbor.r < col.r;
					bool isRightYounger = rightNeighbor.r < col.r;

					bool useLeft = leftBrightness > threshold && leftBrightness_green < threshold;
					bool useRight = rightBrightness > threshold && rightBrightness_green < threshold;

					useLeft = leftBrightness > threshold;
					useRight = rightBrightness > threshold;
					useCurrent = currentBrightness > threshold;

					// if (!isFading && currentBrightness_green > threshold) {
					// 	if (useLeft && useRight) {
					// 		currentBrightness = (leftBrightness + rightBrightness) * 0.5;
					// 		isFading = true;
					// 	}
					// 	else if (useLeft) {
					// 		currentBrightness = leftBrightness;
					// 		isFading = true;
					// 	}
					// 	else if (useRight) {
					// 		currentBrightness = rightBrightness;
					// 		isFading = true;
					// 	}
					// 	if (isFading) {
					// 		col.b = currentBrightness;
					// 		col.g = 0;
					// 	}
					// }



					float edgeDist = 0;

					// if (useLeft && useRight)
					// {
					// 	if (useCurrent)
					// 	{
					// 		edgeDist = normalizedCenterDist;
					// 	}
					// }

					if (useCurrent) {
						if (useLeft) {
							if (useRight) {
								edgeDist = normalizedVerticalDist;
							}
							else {
								edgeDist = normalizedLeftDist;
							}
						}
						else if (useRight) {
							edgeDist = normalizedRightDist;
						}
						else {
							if (_HideSinglePixel > 0.5) {
								return fixed4(0,0,0,1);
							}
							float2 dotDelta = centerDelta;
							dotDelta.x *= 2.0;
							edgeDist = length(dotDelta / (checkDelta));

						}
						col.rgb *= (1.0-edgeDist);
					}
				}

				return fixed4(0,0,col.b,1);

				fixed4 color_ret = col.b * i.color;

				float fadeOff = smoothstep(0, 1, (texelsPerPixelMax-_MainFadeStart)/(_MainFadeEnd - _MainFadeStart));

				color_ret *= (1.0 - fadeOff);
				color_ret.r = 0;
				color_ret.g = 0;

				color_ret.a = color_ret.b;
				//color_ret = max(color_ret, colorFloor);
				// if (!isFading) {
				// 	return fixed4(0,0,0,0);
				// }
				//color_ret.r = startRed;
				return color_ret;
			}
			ENDHLSL
		}


		// Blur Pass
		Blend One One

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "common.hlsl"

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
			float _MinPixelSize;
			float _ScanlineGap;
			float _PixelRoundness;
			float _PassThrough;
			float _Threshold;
			float _RoundnessType;
			float _BlurFactorX;
			float _BlurFactorY;
			float _FadeStart;
			float _FadeEnd;
			float _NeighborFactor;
			float _BlurPass;
			float _MainFadeEnd;
			float _MainFadeStart;
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
				if (_BlurPass < 0.5) {
					discard; return fixed4(0,0,0,0);
				}
				float2 uv = i.uv;
				fixed4 col;

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

				fixed4 ret = fixed4(0,0,0,0);
				// if (texelsPerPixelX > 3.0) {
				// 	ret.r = 1.0;
				// } else if (texelsPerPixelX > 2.0) {
				// 	ret.r = 0.5;
				// } else if (texelsPerPixelX > 1.0) {
				// 	ret.r = 0.25;
				// }
				//float tX = min(3.0, texelsPerPixelX);

#define GETUVOFFSET(uvdd, k, texPerPix) (uvdd * k / texPerPix)
//#define GETUVOFFSET(uvdd, k, texPerPix) (uvdd * k)
#define LIT2(x) LIT(x)
				float samples = 0.0;
				fixed4 tmp;
				for (float k = 1.0; k <= 16; k++) {
					if (k < texelsPerPixelX) {
						tmp = tex2D(
							_MainTex,
							uv + GETUVOFFSET(uvDdx, k, texelsPerPixelX)
						);
						if (LIT2(tmp)) {
							ret += tmp;
							samples += 1.0;
						}
						tmp = tex2D(
							_MainTex,
							uv - GETUVOFFSET(uvDdx, k, texelsPerPixelX)
						);
						if (LIT2(tmp)) {
							ret += tmp;
							samples += 1.0;
						}
					}
					if (k < texelsPerPixelY) {
						tmp = tex2D(
							_MainTex,
							uv + GETUVOFFSET(uvDdy, k, texelsPerPixelY)
						);
						if (LIT2(tmp)) {
							ret += tmp;
							samples += 1.0;
						}
						tmp = tex2D(
							_MainTex,
							uv - GETUVOFFSET(uvDdy, k, texelsPerPixelY)
						);
						if (LIT2(tmp)) {
							ret += tmp;
							samples += 1.0;
						}
					}
				}
				if (samples < 1.0) {
					discard; return fixed4(0,0,0,0);
				}
				ret /= samples;
				ret.a = 1;
				// if (texelsPerPixelY > 3.0) {
				// 	ret.b = 1.0;
				// } else if (texelsPerPixelY > 2.0) {
				// 	ret.b = 0.5;
				// } else if (texelsPerPixelY > 1.0) {
				// 	ret.b = 0.25;
				// }

				//float fadeOff = smoothstep(0, 1, (texelsPerPixelMax-_FadeStart)/(_FadeEnd - _FadeStart));
				float mainFadeOff = smoothstep(0, 1, (texelsPerPixelMax-_MainFadeStart)/(_MainFadeEnd - _MainFadeStart));
				// float fadeOff = smoothstep(0, 1, (texelsPerPixelMax-_FadeStart)/(_FadeEnd - _FadeStart));


				ret *= i.color * mainFadeOff;
				return ret;

				fixed4 colorFloor = fixed4(_Color.r, _Color.g, _Color.b, 1) * smoothstep(_FadeStart, _FadeEnd, texelsPerPixelMax);


				// Calculate position within the current texel (0 to 1)
				// For vertical span > 1, group multiple texels together
				float2 texelCoord = uv * float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
				float2 texelPos = frac(texelCoord);



				fixed4 neighborCol = tex2D(_MainTex, uv + float2(uvDdx.x, uvDdy.y)*_NeighborFactor*texelsPerPixelMax);

				col = (col + neighborCol) * 0.5;
				col.a = 1;
				return col * i.color;

			}
			ENDHLSL
		}


		// Fade
		Pass
		{
			// blend multiply
			Blend DstColor Zero
			HLSLPROGRAM
			#pragma vertex vert_blur
			#pragma fragment frag_blur_h

			#include "UnityCG.cginc"
			#include "common.hlsl"

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float _FadeStart;
			float _FadeEnd;
			fixed4 _Color;
			float _FadePass;

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

			fixed4 frag_blur_h(v2f_blur i) : SV_Target
			{
				if (_FadePass < 0.5) {
					discard; return fixed4(1,1,1,1);
				}
				fixed4 col = tex2D(_MainTex, i.uv);

				// Calculate UV derivatives to understand texel-to-pixel mapping
				float2 uvDdx = ddx(i.uv);
				float2 uvDdy = ddy(i.uv);

				// How much UV changes per screen pixel
				float uvPerPixelX = length(float2(uvDdx.x, uvDdy.x));
				float uvPerPixelY = length(float2(uvDdx.y, uvDdy.y));

				// Convert to texels per pixel
				float texelsPerPixelX = uvPerPixelX * _MainTex_TexelSize.z;
				texelsPerPixelX = abs(texelsPerPixelX);
				float texelsPerPixelY = uvPerPixelY * _MainTex_TexelSize.w;
				texelsPerPixelY = abs(texelsPerPixelY);

				float texelsPerPixelMax = max(texelsPerPixelX, texelsPerPixelY);
				//return fixed4(_Color.r, _Color.g, _Color.b, 1) * smoothstep(_FadeStart, _FadeEnd, texelsPerPixelMax);


				float fadeOff = smoothstep(0, 1, (texelsPerPixelMax-_FadeStart)/(_FadeEnd - _FadeStart));
				fixed4 fadeRet = fixed4(1,1,1,1) * (1.0-fadeOff);
				fadeRet.a = 1;
				return fadeRet;
			}
			ENDHLSL
		}

		// horizontal Gaussian blur
		Pass
		{
			// blend add
			Blend One One
			HLSLPROGRAM
			#pragma vertex vert_blur
			#pragma fragment frag_blur_h

			#include "UnityCG.cginc"
			#include "common.hlsl"

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float _BlurFactorX;
			float _Threshold;
			float _MaxTexelCheck;
			float _GaussXPass;

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

			fixed4 Blur1D(float2 uv, float2 dir, float amount)
			{
				float2 stepUV = dir * _MainTex_TexelSize.xy * amount;

				fixed4 c = tex2D(_MainTex, uv) * w0;
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

			fixed4 frag_blur_h(v2f_blur i) : SV_Target
			{
				if (_GaussXPass < 0.5 || _BlurFactorX < 0.1)
				{
					discard;return fixed4(0,0,0,0);
				}

				fixed4 col = tex2D(_MainTex, i.uv);
				if (col.r > 0 || col.r > 0 || col.g > 0) {
					discard; return fixed4(0,0,0,0);
				}
				// if (col.r + col.g + col.b < .03 || _BlurFactor < .01)
				// {
				// 	discard;return fixed4(0,0,0,0);
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
				//return fixed4(1,0,1,1);
				float maxTexelCheck = _MaxTexelCheck;
				float texelsCheck = smoothstep(1, maxTexelCheck, texelsPerPixelX);
				// sample all texels
				//return fixed4(texelsCheck/maxTexelCheck,0,0,1);
				for (float x = -texelsCheck; x <= texelsCheck; x += 1.0)
				{
					float2 offsetUV = i.uv + float2(x * _MainTex_TexelSize.x, 0);
					fixed4 sampleCol = tex2D(_MainTex, offsetUV);
					col = max(col, sampleCol);
				}
				return col;

				GAUSS(texelsPerPixelX, float2(1,0), _BlurFactorX);

				discard;return fixed4(0,0,0,0);
			}
			ENDHLSL
		}

		// vertical Gaussian blur
		Pass
		{
			Blend One One
			HLSLPROGRAM
			#pragma vertex vert_blur
			#pragma fragment frag_blur_v

			#include "UnityCG.cginc"
			#include "common.hlsl"

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float _BlurFactorY;
			float _Threshold;
			float _GaussYPass;

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

			fixed4 Blur1D(float2 uv, float2 dir, float amount)
			{
				float2 stepUV = dir * _MainTex_TexelSize.xy * amount;

				fixed4 c = tex2D(_MainTex, uv) * w0;
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

			fixed4 frag_blur_v(v2f_blur i) : SV_Target
			{
				if (_GaussYPass < 0.5 || _BlurFactorY < .1) {
					discard;
					return fixed4(0,0,0,0);
				}
				fixed4 col = tex2D(_MainTex, i.uv);
				// if (col.r + col.g + col.b < .03 || _BlurFactor < .01)
				// {
				// 	discard;return fixed4(0,0,0,0);
				// }

				// Calculate UV derivatives to understand texel-to-pixel mapping
				float2 uvDdx = ddx(i.uv);
				float2 uvDdy = ddy(i.uv);

				// How much UV changes per screen pixel
				float uvPerPixelX = length(float2(uvDdx.x, uvDdy.x));
				float uvPerPixelY = length(float2(uvDdx.y, uvDdy.y));

				// Convert to texels per pixel
				float texelsPerPixelY = abs(uvPerPixelY * _MainTex_TexelSize.w);

				GAUSS(texelsPerPixelY, float2(0,1), _BlurFactorY);

				discard;return fixed4(0,0,0,0);
			}
			ENDHLSL
		}
	}

	Fallback "UI/Default"
}
