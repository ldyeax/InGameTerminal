

Shader "InGameTerminal/PostProcessTest2"
{
	Properties
{
	_MainTex ("Input", 2D) = "white" {}
	_Color ("Tint", Color) = (1,1,1,1)
	[Toggle] _PixelSnap ("Pixel Snap", Float) = 1
	_PixelRoundness ("Pixel Roundness", Range(0, 2)) = 1
	_RoundnessAspect ("Roundness Aspect (H/V)", Range(0.1, 10)) = 1
	_YOffset ("Y Offset", Range(-1.0, 1.0)) = 0
	_Thickness ("Thickness", range(1, 5)) = 1.0
	_PassThrough ("Pass Through", Range(0, 1)) = 0
	_Mip0 ("Force Mip 0", Range(0, 1)) = 1
	_DistanceCutoff ("Distance Cutoff", Range(0, 5)) = 0.5
	_DistanceFactor ("Distance Factor", Range(0, 5)) = 5.0
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
		
		//Cull Off
		Lighting Off
		//ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		
		Pass
		{
			HLSLPROGRAM

			#define GLYPH_WIDTH 15
			#define GLYPH_HEIGHT 12
			#define PIXEL_HEIGHT (11.0 / 4.0)
			#define GLYPH_HEIGHT_PIXELS (GLYPH_HEIGHT * PIXEL_HEIGHT)
			#define SCREEN_HEIGHT_PIXELS (ROWS * GLYPH_HEIGHT_PIXELS)
			#define ROWS 24
			#define COLS 80
			#define UV_Y_PER_PIXEL (1.0 / SCREEN_HEIGHT_PIXELS)

			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			float _RoundnessAspect;
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				float2 originalUV : TEXCOORD1;
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
			float _PixelRoundness;
			float _ScanLineGap;
			float _YOffset;
			float _Thickness;
			float _PassThrough;
			float _Mip0;
			float _DistanceCutoff;
			float _DistanceFactor;
			
			v2f vert(appdata v)
			{
				v.uv = float2(1.0, 1.0)-v.uv;
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color * _Color;
				o.color.a = 1.0;
				o.screenPos = o.vertex;
				return o;
			}
			
			float4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;

				// Force mip 0 so distance doesn't collapse detail via mip selection
				float4 col;
				if (_Mip0)
				{
					col = tex2Dlod(_MainTex, float4(uv, 0, 0));
				}
				else
				{
					col = tex2D(_MainTex, uv);
				}
				col.a = 1;
				if (_PassThrough > 0.5) {
					return col;
				}

				float thickness = _Thickness;
				float test1 = abs(ddx(i.screenPos.x));
				test1 = smoothstep(0.0,2.0,test1-_DistanceCutoff)*_DistanceFactor;

				thickness = max(thickness, test1);
				

				if (thickness > 1.0) {
					float4 maxCol = col;
					for (float offset = -thickness; offset <= thickness; offset += 0.5) {
						float2 offsetUV = float2(uv.x + offset * _MainTex_TexelSize.x, uv.y);
						float4 sampleCol = tex2Dlod(_MainTex, float4(offsetUV, 0, 0));
						maxCol = max(maxCol, sampleCol);
					}
					col = maxCol;
				}

				//col.rgb *= mask;
				//return float4(col.r, abs(ddx(uv.x))*100.0f, col.b, col.a);
				//col.g = test1;
				return col;
			}

			ENDHLSL
		}
	}
	
	Fallback "UI/Default"

}
