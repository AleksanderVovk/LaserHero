Shader "LaserHero/VoxelColor"
{
    Properties
    {
        _BaseColor("Color", Color) =(1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
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
                struct A
                {
                    float4 positionOS : POSITION;
                    float3 normalOS : NORMAL;
                    float4 color : COLOR;
                };
                struct V
                {
                    float4 positionCS : SV_POSITION;
                    float4 color : COLOR;
                    float3 normalWS : TEXCOORD0;
                };
                CBUFFER_START(UnityPerMaterial)
                    float4 _BaseColor;
                CBUFFER_END
                V vert(A a)
                {
                    V o;
                    o.positionCS = TransformObjectToHClip(a.positionOS.xyz);
                    o.color = a.color * _BaseColor;
                    o.normalWS = TransformObjectToWorldNormal(a.normalOS);
                    return o;
                }
                half4 frag(V i) : SV_Target
                {
                    float light = .58 + .42 * saturate(dot(normalize(i.normalWS), normalize(float3(-.4, .85, -.3))));
                    return half4(i.color.rgb * light, i.color.a);
                }
            ENDHLSL
        }
    }
}
