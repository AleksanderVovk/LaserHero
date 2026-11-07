Shader "LaserHero/BurnParticle"
{
    Properties
    {
        _MainTex("Fire flipbook", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                struct A
                {
                    float4 positionOS : POSITION;
                    float4 color : COLOR;
                    float2 uv : TEXCOORD0;
                };
                struct V
                {
                    float4 positionCS : SV_POSITION;
                    float4 color : COLOR;
                    float2 uv : TEXCOORD0;
                };
                V vert(A a)
                {
                    V o;
                    o.positionCS = TransformObjectToHClip(a.positionOS.xyz);
                    o.color = a.color;
                    o.uv = a.uv;
                    return o;
                }
                half4 frag(V i) : SV_Target
                {
                    half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                    return half4(i.color.rgb * 1.3, t.a * i.color.a);
                }
            ENDHLSL
        }
    }
}
