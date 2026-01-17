

Shader "InGameTerminal/PostProcessTest1"
{
    Properties
{
    _MainTex ("Input", 2D) = "white" {}
    _Color ("Tint", Color) = (1,1,1,1)
    [Toggle] _PixelSnap ("Pixel Snap", Float) = 1
    _PixelRoundness ("Pixel Roundness", Range(0, 2)) = 1
    _RoundnessAspect ("Roundness Aspect (H/V)", Range(0.1, 10)) = 0.8
	_ScanLineGap ("ScanLine Gap", Range(0, 0.5)) = 0.1
	_YOffset ("Y Offset", Range(-1.0, 1.0)) = 0.0
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
            
            v2f vert(appdata v)
            {
				v.uv = float2(1.0, 1.0)-v.uv;
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // // Store screen position before snapping for fragment shader
                o.screenPos = (o.vertex.xy / o.vertex.w + 1.0) * 0.5 * _ScreenParams.xy;
                
                // // Pixel snap: round vertex position to nearest screen pixel
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
				
				float scanLines = (ROWS * GLYPH_HEIGHT);
				float distBetweenScanLines= 1.0 / scanLines;

				float2 snappedUV = uv;
				snappedUV.y = uv.y - abs(fmod(uv.y, distBetweenScanLines));
				snappedUV.y += _YOffset * distBetweenScanLines;
				
				fixed4 col = tex2D(_MainTex, snappedUV);

				float yFracInTexel = fmod(uv.y, distBetweenScanLines)/distBetweenScanLines;
				if (yFracInTexel > 0.5)
				{
					yFracInTexel = 1.0-yFracInTexel;
				}
				//if (yFracInTexel<_ScanLineGap)return float4(0,1,0,1);

				col.rgb *= smoothstep(0.0, 0.2, yFracInTexel);

                return col;
            }
            ENDCG
        }
    }
    
    Fallback "UI/Default"

}
