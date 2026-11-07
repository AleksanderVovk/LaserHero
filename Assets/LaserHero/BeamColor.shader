Shader "LaserHero/BeamColor"
{
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Blend SrcAlpha One
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
                };
                struct V
                {
                    float4 positionCS : SV_POSITION;
                    float4 color : COLOR;
                };
                V vert(A a)
                {
                    V o;
                    o.positionCS = TransformObjectToHClip(a.positionOS.xyz);
                    o.color = a.color;
                    return o;
                }
                half4 frag(V i) : SV_Target
                {
                    return i.color;
                }
            ENDHLSL
        }
    }
}
