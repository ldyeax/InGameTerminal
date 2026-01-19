Shader "InGameTerminal/FWidthAdaptiveBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DistanceScale ("Distance Scale", Range(1, 2000)) = 100.0
		_CameraPos ("Camera Position", Vector) = (0,0,0,0)
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "DisableBatching" = "True"
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
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
				float dist : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _DistanceScale;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.dist = distance(_WorldSpaceCameraPos, o.worldPos);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {float d = distance(_WorldSpaceCameraPos, i.worldPos);
return float4(frac(d * 0.01), 0, 0, 1);

                return fixed4(i.dist/10000.0f, 0, 0, 1);
            }
            ENDCG
        }
    }
}
