Shader "InGameTerminal/VT320 Basic"
{
	
	
    Properties
	{
		_MainTex ("Atlas Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		
		
		_PixelRoundness ("Pixel Roundness", Range(0, 2)) = 1
		_RoundnessAspect ("Roundness Aspect (H/V)", Range(0.1, 10)) = 0.8
		_HorizontalFactor ("Horizontal Factor", Range(0.0, 1.0)) = 0.0
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
            
            float _PixelRoundness;
			float _HorizontalFactor;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // Store screen position before snapping for fragment shader
                o.screenPos = (o.vertex.xy / o.vertex.w + 1.0) * 0.5 * _ScreenParams.xy;
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 col;
                col = tex2D(_MainTex, uv);
				return col*i.color;
            }
            ENDCG
        }

    }
    
    Fallback "UI/Default"
}
