// MorphologicalRounding.shader
// Efficient morphological "rounding" (closing = dilate then erode) + optional feather.
// Intended for post-process / RenderTexture workflows (e.g., OnRenderImage or custom blit chain).
//
// Typical usage (3 blits):
//   Graphics.Blit(src,  rtA, mat, 0); // Pass 0: DILATE (max filter)
//   Graphics.Blit(rtA,  rtB, mat, 1); // Pass 1: ERODE  (min filter)
//   Graphics.Blit(rtB,  dst, mat, 2); // Pass 2: FEATHER + THRESHOLD (smooth edge)
//
// Notes:
// - Works best with binary-ish inputs (0/1 alpha or luminance mask).
// - Radius is discrete: 1 or 2 (kept small for speed). Radius=2 is a nice "rounding" default.
// - Kernel is disk-ish (13 taps at r=2): center, cross, diagonals, and 2-away cross.

Shader "InGameTerminal/MorphologicalRounding"
{
	Properties
	{
		_MainTex ("Input", 2D) = "white" {}

		// 1 or 2 (texels). Higher radii need more taps/passes.
		_Radius ("Radius (texels: 1 or 2)", Range(1,2)) = 2

		// How to interpret the mask:
		// 0 = use alpha, 1 = use luminance(rgb)
		_MaskFromLuma ("Mask From Luma (0=Alpha,1=Luma)", Float) = 0

		// Feather pass:
		_Threshold ("Threshold", Range(0,1)) = 0.5
		_Softness  ("Softness (edge width)", Range(0.0001,0.5)) = 0.08

		// Output options:
		_OutputAlphaOnly ("Output Alpha Only (0=Color*Alpha,1=Alpha)", Float) = 0
		_Invert ("Invert Mask", Float) = 0
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Overlay" }

		HLSLINCLUDE
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		float _Radius;
		float _MaskFromLuma;
		float _Threshold;
		float _Softness;
		float _OutputAlphaOnly;
		float _Invert;

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv	 : TEXCOORD0;
		};

		struct v2f
		{
			float4 pos : SV_POSITION;
			float2 uv  : TEXCOORD0;
		};

		v2f Vert(appdata v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv  = v.uv;
			return o;
		}

		inline float Luma(float3 rgb)
		{
			return dot(rgb, float3(0.2126, 0.7152, 0.0722));
		}

		inline float GetMaskAtUV(float2 uv)
		{
			float4 c = tex2D(_MainTex, uv);
			float m = (_MaskFromLuma > 0.5) ? Luma(c.rgb) : c.a;
			if (_Invert > 0.5) m = 1.0 - m;
			return saturate(m);
		}

		// Disk-ish tap offsets for r=1 and r=2.
		// r=1 taps: center + 4-neighborhood + diagonals (9 taps)
		// r=2 adds: ±2 in x/y (13 taps total)
		inline void SampleKernel(float2 uv, out float taps0, out float taps1, out float taps2, out float taps3,
								 out float taps4, out float taps5, out float taps6, out float taps7, out float taps8,
								 out float taps9, out float taps10, out float taps11, out float taps12)
		{
			float2 t = _MainTex_TexelSize.xy;
			float2 o1 = t;		 // 1 texel
			float2 o2 = 2.0 * t;   // 2 texels

			// 9-tap core (r=1)
			taps0 = GetMaskAtUV(uv);						   // (0,0)
			taps1 = GetMaskAtUV(uv + float2( o1.x, 0));		// (+1,0)
			taps2 = GetMaskAtUV(uv + float2(-o1.x, 0));		// (-1,0)
			taps3 = GetMaskAtUV(uv + float2(0,  o1.y));		// (0,+1)
			taps4 = GetMaskAtUV(uv + float2(0, -o1.y));		// (0,-1)
			taps5 = GetMaskAtUV(uv + float2( o1.x,  o1.y));	// (+1,+1)
			taps6 = GetMaskAtUV(uv + float2( o1.x, -o1.y));	// (+1,-1)
			taps7 = GetMaskAtUV(uv + float2(-o1.x,  o1.y));	// (-1,+1)
			taps8 = GetMaskAtUV(uv + float2(-o1.x, -o1.y));	// (-1,-1)

			// Extra cross taps for r=2 (treated as unused when radius < 1.5)
			taps9  = GetMaskAtUV(uv + float2( o2.x, 0));	   // (+2,0)
			taps10 = GetMaskAtUV(uv + float2(-o2.x, 0));	   // (-2,0)
			taps11 = GetMaskAtUV(uv + float2(0,  o2.y));	   // (0,+2)
			taps12 = GetMaskAtUV(uv + float2(0, -o2.y));	   // (0,-2)
		}

		// Max filter (dilate)
		inline float KernelMax(float2 uv)
		{
			float a0,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12;
			SampleKernel(uv, a0,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12);

			float m = max(max(max(a0,a1), max(a2,a3)), max(a4, max(a5, max(a6, max(a7,a8)))));
			if (_Radius > 1.5)
			{
				m = max(m, max(max(a9,a10), max(a11,a12)));
			}
			return m;
		}

		// Min filter (erode)
		inline float KernelMin(float2 uv)
		{
			float a0,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12;
			SampleKernel(uv, a0,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12);

			float m = min(min(min(a0,a1), min(a2,a3)), min(a4, min(a5, min(a6, min(a7,a8)))));
			if (_Radius > 1.5)
			{
				m = min(m, min(min(a9,a10), min(a11,a12)));
			}
			return m;
		}

		// Mean filter (for feather). Not a true blur, but cheap and stable.
		inline float KernelMean(float2 uv)
		{
			float a0,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12;
			SampleKernel(uv, a0,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12);

			float sum = a0+a1+a2+a3+a4+a5+a6+a7+a8;
			float div = 9.0;

			if (_Radius > 1.5)
			{
				sum += a9+a10+a11+a12;
				div = 13.0;
			}

			return sum / div;
		}

		inline float4 ApplyOutput(float2 uv, float alpha)
		{
			float4 c = tex2D(_MainTex, uv);
			if (_Invert > 0.5) alpha = 1.0 - alpha;

			alpha = saturate(alpha);

			if (_OutputAlphaOnly > 0.5)
				return float4(alpha, alpha, alpha, 1.0);

			// Multiply color by alpha, preserve alpha as alpha (good for UI RTs)
			c.rgb *= alpha;
			c.a = alpha;
			return c;
		}

		ENDHLSL

		// PASS 0: DILATE (MAX)
		Pass
		{
			Name "DILATE_MAX"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			float4 Frag(v2f i) : SV_Target
			{
				float a = KernelMax(i.uv);
				// Output mask into alpha (and rgb for convenience)
				return float4(a, a, a, a);
			}
			ENDHLSL
		}

		// PASS 1: ERODE (MIN)
		Pass
		{
			Name "ERODE_MIN"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			float4 Frag(v2f i) : SV_Target
			{
				float a = KernelMin(i.uv);
				return float4(a, a, a, a);
			}
			ENDHLSL
		}

		// PASS 2: FEATHER + THRESHOLD
		Pass
		{
			Name "FEATHER_THRESHOLD"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			float4 Frag(v2f i) : SV_Target
			{
				// Cheap local averaging to create a soft edge, then smooth threshold.
				float m = KernelMean(i.uv);

				// Smooth threshold:
				// m ~ 0..1, edge around _Threshold with width _Softness
				float a = smoothstep(_Threshold - _Softness, _Threshold + _Softness, m);

				return ApplyOutput(i.uv, a);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
