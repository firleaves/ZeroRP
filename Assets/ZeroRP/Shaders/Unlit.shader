Shader "ZeroRP/Unlit"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "white" {}
        _Color("Main Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        ENDHLSL
        


        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "GBuffer"
            }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma enable_d3d11_debug_symbols

            #include "Assets/ZeroRP/Shaders/Library/UnityBuiltIn.hlsl"
            #include "Library\Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };


            Varyings Vertex(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            void Fragment(Varyings i, out float4 GT0 : SV_Target0, out float4 GT1 : SV_Target1, out float4 GT2 : SV_Target2, out float4 GT3 : SV_Target3)
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float occlusion = 0.0;
                float3 specularColor = col.rgb;
                float smoothness = 0.5;

                GT0 = float4(col.rgb, occlusion);
                GT1 = float4(specularColor, smoothness);
                GT2 = float4(i.normalWS * 0.5f + 0.5f, 1.0f);
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
            //            ZWrite On
            //              ZTest GEqual
            //            ZWrite Off
            //            ZClip false
            //            Cull Front
            //                      Blend One One, Zero One
            //            BlendOp Add, Add
            //               Blend One SrcAlpha, Zero One
            //            Blend Zero OneMinusDstAlpha

            Cull Off ZWrite On ZTest Always
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma enable_d3d11_debug_symbols
            #include "Assets/ZeroRP/Shaders/Library/UnityBuiltIn.hlsl"
            #include "Library\Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"


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

            TEXTURE2D(_GT0);
            TEXTURE2D(_GT1);
            TEXTURE2D(_GT2);
            TEXTURE2D(_GT3);
            TEXTURE2D(_DepthTex);
            SAMPLER(sampler_GT0);


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
                //使用这个无法渲染
                // o.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                // o.uv = GetFullScreenTriangleTexCoord(vertexID);

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

            float4 Fragment(Varyings i):SV_TARGET
            {
                float4 col = SAMPLE_TEXTURE2D(_GT0, sampler_GT0, i.uv);
                col.a = 1;
                return col;
            }
            ENDHLSL
        }

        //        Pass
        //        {
        //            name "DepthOnly"
        //            Tags
        //            {
        //                "LightMode" = "DepthOnly"
        //            }
        //            ZWrite On
        //            HLSLPROGRAM
        //            #pragma vertex Vertex
        //            #pragma fragment Fragment
        //
        //
        //            #include "Assets/DeferredRP/Shaders/Library/UnityBuiltIn.hlsl"
        //            #include "Assets/DeferredRP/Shaders/Library/InputMacro.hlsl"
        //            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        //            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
        //
        //
        //            struct Attributes
        //            {
        //                float4 position : POSITION;
        //                float2 uv : TEXCOORD0;
        //            };
        //
        //            struct Varyings
        //            {
        //                float2 depth : TEXCOORD0;
        //                float4 positionCS : SV_POSITION;
        //            };
        //
        //            Varyings Vertex(Attributes input)
        //            {
        //                Varyings o;
        //
        //                o.positionCS = TransformObjectToHClip(input.position.xyz);
        //                o.depth = o.positionCS.zw;
        //                return o;
        //            }
        //
        //            float4 Fragment(Varyings i) :SV_TARGET
        //            {
        //                return 1;
        //                // float d = i.depth.x / i.depth.y;
        //                // #if defined (UNITY_REVERSED_Z)
        //                //     d = 1.0 - d; //(1, 0)-->(0, 1)
        //                // #elif defined (SHADER_TARGET_GLSL)
        //                //     depth = depth * 0.5 + 0.5; //(-1, 1)-->(0, 1)
        //                // #endif
        //                // return d;
        //            }
        //            ENDHLSL
        //        }
    }
}