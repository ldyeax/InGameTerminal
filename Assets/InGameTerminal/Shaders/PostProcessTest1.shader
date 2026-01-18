

Shader "InGameTerminal/PostProcessTest1"
{
    Properties
{
    _MainTex ("Input", 2D) = "white" {}
    _Color ("Tint", Color) = (1,1,1,1)
    [Toggle] _PixelSnap ("Pixel Snap", Float) = 1
    _PixelRoundness ("Pixel Roundness", Range(0, 2)) = 1
    _RoundnessAspect ("Roundness Aspect (H/V)", Range(0.1, 10)) = 1
	_ScanLineGap ("ScanLine Gap", Range(0, 0.5)) = 0.022
	_YOffset ("Y Offset", Range(-1.0, 1.0)) = 0.71
	_Thickness ("Thickness", range(1, 5)) = 1.0
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
            
            v2f vert(appdata v)
            {
				v.uv = float2(1.0, 1.0)-v.uv;
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            
			float4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;

				// 24 * 12 = 288 intended scanlines
				const float scanLines = (float)(ROWS * GLYPH_HEIGHT);

				// Phase increases by 1 per scanline
				float phase = uv.y * scanLines;

				// Screen-space anti-aliasing width (bigger when the object is far away)
				float aa = fwidth(phase);          // ~ how many "phase units" per pixel
				aa = max(aa, 1e-5);

				float scanLineGap = _ScanLineGap;
				// if (aa > 1.0) {
				// 	scanLineGap = 0;
				// }

				// Distance to the center of the scanline band (0 at center, 0.5 at boundary)
				float d = abs(frac(phase) - 0.5);

				// Your gap: interpret as how much of each scanline band is darkened.
				// Convert 0..0.5 into a center-bright region size.
				float brightHalfWidth = saturate(0.5 - scanLineGap);

				// Smooth mask: 1 near center, 0 near edges, AA'd in screen-space
				float mask = 1.0 - smoothstep(brightHalfWidth - aa, brightHalfWidth + aa, d);

				// Optional: snap the *sample* to the scanline center (your original idea)
				float lineIndex = floor(phase + _YOffset);
				float snappedY = (lineIndex + 0.5) / scanLines;
				float2 snappedUV = float2(uv.x, snappedY);

				// Force mip 0 so distance doesn't collapse detail via mip selection
				float4 col = tex2Dlod(_MainTex, float4(snappedUV, 0, 0));

				// // How many source texels (vertically) contribute to this output pixel?
				// float uvSpanY    = fwidth(snappedUV.y);              // UV units per pixel
				// float texelSpanY = uvSpanY * _MainTex_TexelSize.w;   // texels per pixel

				// // Number of texel taps needed to cover that span
				// int tapsY = (int)ceil(texelSpanY);

				// // Safety cap (prevents accidental TDR)
				// tapsY = clamp(tapsY, 1, 16);

				// // Sample across the covered vertical span, centered on snappedUV.y
				// float4 maxCol = float4(0,0,0,0);

				// for (int ty = 0; ty < tapsY; ty++)
				// {
				// 	// ty runs 0..tapsY-1, map to offsets centered around 0
				// 	float centered = ((ty + 0.5) / tapsY) - 0.5; // -0.5..+0.5
				// 	float2 uv2 = snappedUV + float2(0, centered * uvSpanY);

				// 	float4 s = tex2Dlod(_MainTex, float4(uv2, 0, 0));
				// 	maxCol = max(maxCol, s);
				// }

				// col = maxCol;

				// set col to the max of all pixels horizontally from -thickness to +thickness
				float thickness = _Thickness;
				thickness = max(thickness, aa);
				thickness = min(thickness, 2.0);

				if (thickness > 1.0) {
					float4 maxCol = col;
					for (float offset = -thickness; offset <= thickness; offset += 0.5) {
						float2 offsetUV = float2(snappedUV.x + offset * _MainTex_TexelSize.x, snappedUV.y);
						float4 sampleCol = tex2Dlod(_MainTex, float4(offsetUV, 0, 0));
						maxCol = max(maxCol, sampleCol);
						// if (scanLineGap < 0.001)
						// {
						// 	for (float offsetY = -thickness; offsetY <= thickness; offsetY += 0.5)
						// 	{
						// 		float2 offsetUVY = float2(snappedUV.x, snappedUV.y + offsetY * _MainTex_TexelSize.y);
						// 		float4 sampleColY = tex2Dlod(_MainTex, float4(offsetUVY, 0, 0));
						// 		maxCol = max(maxCol, sampleColY);
						// 	}
						// }
					}
					col = maxCol;
				}

				col.rgb *= mask;
				return col;
			}

            ENDHLSL
        }
    }
    
    Fallback "UI/Default"

}
