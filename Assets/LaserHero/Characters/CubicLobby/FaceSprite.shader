Shader "LaserHero/Character Face Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex("Face sprite", 2D) = "white" {}
        _Color("Tint", Color) =(1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "CanUseSpriteAtlas" = "True"
        }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                CBUFFER_START(UnityPerMaterial)
                    half4 _Color;
                CBUFFER_END
                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                    half4 color : COLOR;
                };
                struct Varyings
                {
                    float4 positionCS : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    half4 color : COLOR;
                };
                Varyings vert(Attributes i)
                {
                    Varyings o;
                    o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                    o.uv = i.uv;
                    o.color = i.color * _Color;
                    return o;
                }
                half4 frag(Varyings i) : SV_Target
                {
                    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                }
            ENDHLSL
        }
    }
}
