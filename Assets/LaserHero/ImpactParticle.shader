Shader "LaserHero/ImpactParticle"
{
    Properties
    {
        _SrcBlend("Source blend", Float) = 5
        _DstBlend("Destination blend", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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
                    float soft = pow(saturate(1 - length(i.uv - .5) * 2), 1.6);
                    return half4(i.color.rgb, i.color.a * soft);
                }
            ENDHLSL
        }
    }
}
