Shader "LaserHero/BackdropBlur"
{
    Properties
    {
        [PerRendererData] _MainTex("World snapshot", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                float4 _MainTex_TexelSize;
                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                    float4 color : COLOR;
                };
                struct Varyings
                {
                    float4 positionCS : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float4 color : COLOR;
                };
                Varyings vert(Attributes a)
                {
                    Varyings o;
                    o.positionCS = TransformObjectToHClip(a.positionOS.xyz);
                    o.uv = a.uv;
                    o.color = a.color;
                    return o;
                }
                half4 frag(Varyings i) : SV_Target
                {
                    float2 d = _MainTex_TexelSize.xy * 2;
                    half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * .2;
                    c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(d.x, 0)) * .12;
                    c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(d.x, 0)) * .12;
                    c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, d.y)) * .12;
                    c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(0, d.y)) * .12;
                    c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + d) * .08;
                    c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - d) * .08;
                    c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(d.x, -d.y)) * .08;
                    c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-d.x, d.y)) * .08;
                    return half4(c.rgb * i.color.rgb, i.color.a);
                }
            ENDHLSL
        }
    }
}
