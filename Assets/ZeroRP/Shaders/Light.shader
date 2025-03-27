Shader "ZeroRP/Light"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicMap("Metallic", 2D) = "white" {}
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _NormalMap("NormalTex", 2D) = "bump" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }


        Pass
        {
            Name "Light"
            Tags
            {
                "LightMode" = "Light"
            }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment


            #include "Assets/ZeroRP/Shaders/Library/UnityBuiltIn.hlsl"
            #include "Library\Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Smoothness;
                float _Metallic;
            CBUFFER_END


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_MetallicMap);
            SAMPLER(sampler_MetallicMap);

            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);


            Varyings Vertex(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            void Fragment(Varyings i, out float4 GT0 : SV_Target0, out float4 GT1 : SV_Target1, out float4 GT2 : SV_Target2, out float4 GT3 : SV_Target3)
            {
                float3 diffuseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                float occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, i.uv).g;
                float3 specularColor = 0;

                GT0 = half4(diffuseColor, occlusion);
                GT1 = half4(specularColor, _Smoothness);
                GT2 = half4(i.normalWS * 0.5f + 0.5f, 1.0f);
                GT3 = float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
        
    }
}