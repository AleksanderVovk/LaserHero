Shader "LaserHero/Stud Plastic"
{
    Properties
    {
        _MainTex("Stud texture", 2D) = "white" {}
        _BumpMap("Stud normals", 2D) = "bump" {}
        _Color("Plastic colour", Color) =(1, 1, 1, 1)
        _UseVertexColor("Use vertex palette", Float) = 0
        [HideInInspector] _UseStudAnchor("Use attached texture frame", Float) = 0
        _Scale("Tiling scale", Float) = .5
        _OffsetX("Offset X", Float) = 0
        _OffsetY("Offset Y", Float) = 0
        _IsDynamic("Local coordinates", Float) = 1
        _ObjectScale("Object scale", Vector) =(1, 1, 1, 1)
        _BumpScale("Relief strength", Range(0, 1)) = .3
        _TextureStrength("Texture contrast", Range(0, 1)) = .55
        _Smoothness("Plastic shine", Range(0, 1)) = .35
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
                #pragma multi_compile_fragment _ _SHADOWS_SOFT
                #pragma multi_compile_fog
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                TEXTURE2D(_BumpMap);
                SAMPLER(sampler_BumpMap);
                CBUFFER_START(UnityPerMaterial)
                    float4 _Color, _ObjectScale;
                    float _Scale, _OffsetX, _OffsetY, _IsDynamic, _BumpScale, _TextureStrength, _Smoothness, _UseVertexColor, _UseStudAnchor;
                    float4x4 _StudAnchor, _StudAnchorInverse;
                CBUFFER_END
                struct A
                {
                    float4 positionOS : POSITION;
                    float3 normalOS : NORMAL;
                    float4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };
                struct V
                {
                    float4 positionCS : SV_POSITION;
                    float3 positionWS : TEXCOORD0;
                    float3 positionOS : TEXCOORD1;
                    float3 normalWS : TEXCOORD2;
                    float3 normalOS : TEXCOORD3;
                    float fog : TEXCOORD4;
                    half3 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };
                V vert(A i)
                {
                    V o;
                    UNITY_SETUP_INSTANCE_ID(i);
                    UNITY_TRANSFER_INSTANCE_ID(i, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                    o.positionCS = p.positionCS;
                    o.positionWS = p.positionWS;
                    o.positionOS = i.positionOS.xyz;
                    o.normalOS = i.normalOS;
                    o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                    o.fog = ComputeFogFactor(p.positionCS.z);
                    o.color = SRGBToLinear(i.color.rgb);
                    return o;
                }
                half4 frag(V i) : SV_Target
                {
                    UNITY_SETUP_INSTANCE_ID(i);
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                    float3 baseNormal = normalize(lerp(i.normalWS, i.normalOS, saturate(_IsDynamic)));
                    float3 texturePosition = lerp(i.positionWS, i.positionOS * _ObjectScale.xyz, saturate(_IsDynamic));
                    if (_UseStudAnchor > .5)
                    {
                        texturePosition = mul(_StudAnchor, float4(i.positionOS, 1)).xyz;
                        baseNormal = normalize(mul(transpose((float3x3)_StudAnchorInverse), i.normalOS));
                    }
                    float3 p = texturePosition * _Scale;
                    float2 offset = float2(_OffsetX, _OffsetY);
                    float3 weights = pow(abs(baseNormal), 4);
                    weights /= max(dot(weights, 1), .0001);
                    half3 textureColour = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, p.yz + offset).rgb * weights.x + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, p.xz + offset).rgb * weights.y + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, p.xy + offset).rgb * weights.z;
                    half3 albedo = lerp(_Color.rgb, i.color, saturate(_UseVertexColor)) * lerp(1, textureColour, _TextureStrength);
                    // Unity's normal decoder handles platform-specific BC5/DXT5nm packing.
                    float3 nx = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, p.yz + offset), _BumpScale);
                    float3 ny = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, p.xz + offset), _BumpScale);
                    float3 nz = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, p.xy + offset), _BumpScale);
                    float3 normal = normalize(float3(nx.z * sign(baseNormal.x), nx.x, nx.y) * weights.x + float3(ny.x, ny.z * sign(baseNormal.y), ny.y) * weights.y + float3(nz.x, nz.y, nz.z * sign(baseNormal.z)) * weights.z);
                    if (_UseStudAnchor > .5)normal = TransformObjectToWorldNormal(mul(transpose((float3x3)_StudAnchor), normal));
                    else normal = normalize(lerp(normal, TransformObjectToWorldNormal(normal), saturate(_IsDynamic)));
                    Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                    float diffuse = saturate(dot(normal, sun.direction) * .5 + .5);
                    // Bright sky fill keeps plastic saturated on vertical faces and in shade.
                    half3 lighting = .64 + sun.color *(.42 * diffuse * lerp(.45, 1, sun.shadowAttenuation));
                    float3 view = GetWorldSpaceNormalizeViewDir(i.positionWS);
                    float specular = pow(saturate(dot(normal, normalize(sun.direction + view))), 24) * _Smoothness * .12 * sun.shadowAttenuation;
                    return half4(MixFog(albedo * lighting + specular * sun.color, i.fog), 1);
                }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
