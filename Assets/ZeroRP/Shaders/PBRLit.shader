Shader "ZeroRP/PBRLit"
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
            "RenderPipeline" = "ZeroRenderPipeline"
            "RenderType"="Opaque"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float _Smoothness;
            float _Metallic;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ZeroRPForward"
            Tags
            {
                "LightMode" = "ZeroRPForward"
            }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment


            #include "Assets/ZeroRP/Shaders/Library/UnityBuiltIn.hlsl"
            #include "Library\Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            #include "Library/BRDF.hlsl"


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
                float3 specularColor = lerp(kDielectricSpec.rgb, diffuseColor, _Metallic);;
                GT0 = half4(diffuseColor, _Metallic);
                GT1 = half4(specularColor, _Smoothness);
                GT2 = half4(i.normalWS * 0.5f + 0.5f, 1.0f);
                GT3 = float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "LightPass"
            Tags
            {
                "LightMode" = "LightPass"
            }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma enable_d3d11_debug_symbols

            // #include <HLSLSupport.cginc>

            #include "Assets/ZeroRP/Shaders/Library/UnityBuiltIn.hlsl"
            #include "Library/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            #include "Library/BRDF.hlsl"
            #include "Library/Light.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            // TEXTURE2D(_GT0);
            // TEXTURE2D(_GT1);
            // TEXTURE2D(_GT2);
            // TEXTURE2D(_GT3);

            Texture2D _GT0;
            Texture2D _GT1;
            Texture2D _GT2;
            Texture2D _GT3;
            SamplerState sampler_pointer_clamp;


            TEXTURE2D(_DepthTex);
            SAMPLER(sampler_DepthTex);


            //SV_VertexID 顶点编号 绘制三角编号 0 1 2
            // 顶点位置转换到裁剪空间位置
            // 只考虑xy,通过第一行代码得到，
            // 索引0, x: 0<<1=00, 00&10=00 y: 00&10=0，所以0号位对应坐标为0,0
            // 索引1, x: 1<<1=10, 10&10=10 y: 01&10=0, 所以1号位对应坐标为2,0
            // 索引2, x: 2<<1=100, 100&010=000 y:10&10=10,所以2号位对应坐标为0,2
            // *2-1映射后，顶点的坐标为
            // v0:-1，-1
            // v1:3，-1
            // v2:-1，3

            // UV坐标为：
            // 索引0, x: 0<<1=00, 00&10=00 y: 00&10=0，所以0号位对应UV坐标为0,0
            // 索引1, x: 1<<1=10, 10&10=10 y: 01&10=0, 所以1号位对应UV坐标为2,0
            // 索引2, x: 2<<1=100, 100&010=000 y:10&10=10,所以2号位对应UV坐标为0,

            Varyings Vertex(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.positionCS = float4(
                    vertexID <= 1 ? -1.0 : 3.0,
                    vertexID == 1 ? 3.0 : -1.0,
                    0.0, 1.0
                );
                o.uv = float2(
                    vertexID <= 1 ? 0.0 : 2.0,
                    vertexID == 1 ? 2.0 : 0.0
                );
                return o;
            }

            //,
            float4 Fragment(Varyings i,out float outDepth : SV_Depth):SV_TARGET
            {
                // float4 col0 = SAMPLE_TEXTURE2D(_GT0, sampler_GT, i.uv);
                // float4 col1 = SAMPLE_TEXTURE2D(_GT1, sampler_GT, i.uv);
                // float4 col2 = SAMPLE_TEXTURE2D(_GT2, sampler_GT, i.uv);
                // float4 col3 = SAMPLE_TEXTURE2D(_GT3, sampler_GT, i.uv);


                float4 col0 = _GT0.Sample(sampler_pointer_clamp, i.uv);
                float4 col1 = _GT1.Sample(sampler_pointer_clamp, i.uv);
                float4 col2 = _GT2.Sample(sampler_pointer_clamp, i.uv);
                // float4 col3 = _GT0.Sample( sampler_GT, i.uv);
                float3 diffuseColor = col0.rgb;
                float matelllic = col0.a;
                float3 specularColor = col1.rgb;
                float smoothness = col1.a;
                float roughness = 1 - smoothness;
                float3 normalWorld = normalize(col2.rgb * 2 - 1);

                float d = SAMPLE_DEPTH_TEXTURE(_DepthTex, sampler_DepthTex, i.uv);
                //深度写入深度缓冲区
                //TODO 似乎精度问题，导致物体又一圈黑边
                outDepth = d;
                float depth = Linear01Depth(d, _ZBufferParams);

                // 反投影重建世界坐标
                float4 ndcPos = float4(i.uv * 2 - 1, d, 1);
                float4 worldPos = mul(UNITY_MATRIX_I_VP, ndcPos);
                worldPos.xyz *= worldPos.w;

                float3 N = normalWorld;
                float3 L = normalize(_MainLightDirection.xyz);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                //计算方向光
                float3 directColor = DirectBRDF(N, V, L, diffuseColor, _MainLightColor, roughness, matelllic);
                //点光源

                //计算当前所在tile index
                float2 coord = _ScreenParams.xy * i.uv;
                uint2 tileId = floor(coord / _DeferredTileParams.xy);
                uint tileIndex = tileId.y * _DeferredTileParams.z + tileId.x;
                uint lightCount = _TileLightsArgsBuffer[tileIndex];
                uint tileLightOffset = tileIndex * MAX_LIGHT_COUNT_PER_TILE;
                for (uint i = 0; i < lightCount; i++)
                {
                    uint lightIndex = _TileLightsIndicesBuffer[tileLightOffset + i];
                    float4 lightSphere = _PointLightPosRadiusBuffer[lightIndex];
                    half4 lightColor = _PointLightColorBuffer[lightIndex];

                    L = normalize(worldPos.xyz - lightSphere.xyz);
                    directColor += DirectBRDF(N, V, L, diffuseColor, lightColor, roughness, matelllic);
                }
                return float4(directColor.rgb, 1);
                
                // float lightCountDebug = lightCount * 1.0 / MAX_LIGHT_COUNT_PER_TILE;
                // return lightCountDebug;

                // return float4(lightCount * 1.0 / MAX_LIGHT_COUNT_PER_TILE,0,0,1);
                // float value = tileIndex / (_DeferredTileParams.z * _DeferredTileParams.w);
                // return float4(value, tileIndex%2==0?value:0, 0, 1);

                // return float4(directColor.r, directColor.g, directColor.b, 1);
            }
            ENDHLSL
        }
    }
}
// half oneMinusReflectivity = OneMinusReflectivityMetallic(_Metallic);
//                half reflectivity = half(1.0) - oneMinusReflectivity;
//                half3 brdfDiffuse = albedo * oneMinusReflectivity;
//                half3 brdfSpecular = lerp(kDieletricSpec.rgb, albedo, metallic);