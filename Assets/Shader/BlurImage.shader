Shader "UI/BlurImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
       
        [Space(50)]
        _BlurX ("X Blur", Range(0.0, 0.5)) = 0.001
        _BlurY ("Y Blur", Range(0.0, 0.5)) = 0.001
       
        [Space]
        _Focus ("Focus", Range(0.0, 1.0)) = 0
        _Distribution ("Distribution", Range(0.0, 1.0)) = 0.18
        _Iterations ("Iterations", Integer) = 5
       
        [Space(50)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
 
        _ColorMask ("Color Mask", Float) = 15
 
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
     }
 
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
 
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
 
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
 
        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
 
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
 
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
 
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
 
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float2 worldPosition  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
 
            sampler2D _MainTex;
            fixed4 _Color;
       
            fixed _BlurX;
            fixed _BlurY;
       
            fixed _Focus;
            fixed _Distribution;
            int _Iterations;
       
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
 
            float4 tex2Dblur(float2 position, float2 offset)
            {
                const float2 blur_offset = position.xy + float2(_BlurX, _BlurY).xy * offset * (1 - _Focus);
                return tex2D(_MainTex, blur_offset) + _TextureSampleAdd;
            }
 
            float calculateWeight(float distance)
            {
                return lerp(1, _Distribution, distance);
            }
 
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
 
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }
 
            fixed4 frag(v2f IN) : SV_Target
            {
                const int2 iterations = int2(_Iterations, _Iterations);
                const float centralPixelWeight = 1;
 
                float4 color_sum = float4(0,0,0,0);
                float weight_sum = 0;
 
                // Add central pixel
                color_sum += tex2Dblur(IN.texcoord, float2(0, 0)) * centralPixelWeight;
                weight_sum += centralPixelWeight;
 
                // Add central column
                for (int horizontal = 1; horizontal < iterations.x; ++horizontal)
                {
                    const float offset = (float)horizontal / iterations.x;
                    const float weight = calculateWeight(offset);
                   
                    color_sum += tex2Dblur(IN.texcoord, float2(offset, 0)) * weight;
                    color_sum += tex2Dblur(IN.texcoord, float2(-offset, 0)) * weight;
                    weight_sum += weight * 2;
                }
 
                // Add central row
                for (int vertical = 1; vertical < iterations.y; ++vertical)
                {
                    const float offset = (float)vertical / iterations.y;
                    const float weight = calculateWeight(offset);
                   
                    color_sum += tex2Dblur(IN.texcoord, float2(0, offset)) * weight;
                    color_sum += tex2Dblur(IN.texcoord, float2(0, -offset)) * weight;
                    weight_sum += weight * 2;
                }
 
                // Add quads
                for (int x = 1; x < iterations.x; ++x)
                {
                    for (int y = 1; y < iterations.y; ++y)
                    {
                        float2 offset = float2((float)x / iterations.x, (float)y / iterations.y);
                        const float offsetLength = length(offset);
                        const float weight = calculateWeight(offsetLength);
                       
                        color_sum += tex2Dblur(IN.texcoord, float2(offset.x, offset.y)) * weight;
                        color_sum += tex2Dblur(IN.texcoord, float2(-offset.x, offset.y)) * weight;
                        color_sum += tex2Dblur(IN.texcoord, float2(-offset.x, -offset.y)) * weight;
                        color_sum += tex2Dblur(IN.texcoord, float2(offset.x, -offset.y)) * weight;
                        weight_sum += weight * 4;
                    }
                }
 
                float4 final_color = color_sum / weight_sum * IN.color;
 
                #ifdef UNITY_UI_CLIP_RECT
                final_color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
 
                #ifdef UNITY_UI_ALPHACLIP
                clip(final_color.a - 0.001);
                #endif
               
                return final_color;
            }
        ENDCG
        }
    }
}