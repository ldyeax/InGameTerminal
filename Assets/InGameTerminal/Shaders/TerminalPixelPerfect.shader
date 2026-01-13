Shader "InGameTerminal/PixelPerfect"
{
    Properties
    {
        _MainTex ("Atlas Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Toggle] _PixelSnap ("Pixel Snap", Float) = 1
        _MinPixelSize ("Min Screen Pixels Per Texel", Range(1, 4)) = 1
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
                
                return col * i.color;
            }
            ENDCG
        }
    }
    
    Fallback "UI/Default"
}
