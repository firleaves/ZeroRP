Shader "ZeroRP/DeferredLight"
{
   Properties
   {
   }
   SubShader
   {
       Tags
       {
           "RenderType"="Opaque"
       }
       Pass
       {
           Name "DeferredLightPass"
           Tags
           {
               "LightMode" = "DeferredLight"
           }
           ZWrite On
           HLSLPROGRAM
           #pragma vertex Vertex
           #pragma fragment Fragment
                      #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
  #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
           #include "Assets/ZeroRP/Shaders/Library/Input.hlsl"
           #include "Assets/ZeroRP/Shaders/Library/UnityInput.hlsl"

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
           SAMPLER(sampler_GT0);
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

           float4 Fragment(Varyings i):SV_TARGET
           {
               float4 col0 = SAMPLE_TEXTURE2D(_GT0, sampler_GT0, i.uv);
               float4 col1 = SAMPLE_TEXTURE2D(_GT1, sampler_GT0, i.uv);
               float4 col2 = SAMPLE_TEXTURE2D(_GT2, sampler_GT0, i.uv);
               float4 col3 = SAMPLE_TEXTURE2D(_GT3, sampler_GT0, i.uv);

               float3 diffuseColor = col0.rgb;
               float occlusion = col0.a;
               float3 specularColor = col1.rgb;
               float smoothness = col1.a;
               float roughness = 1 - smoothness;
               float3 normalWorld = normalize(col2.rgb * 2 - 1);

               // float d = SAMPLE_DEPTH_TEXTURE(_DepthTex, sampler_DepthTex, i.uv);
               // //深度写入深度缓冲区
               // //TODO 似乎精度问题，导致物体又一圈黑边
               // outDepth = d;
               // float depth = Linear01Depth(d, _ZBufferParams);
               //
               // // 反投影重建世界坐标
               // float4 ndcPos = float4(i.uv * 2 - 1, d, 1);
               // float4 worldPos = mul(_ScreenToWorldMatrix, ndcPos);
               // worldPos.xyz *= worldPos.w;
               //
               // float3 N = normalWorld;
               // float3 L = normalize(_MainLightDirection.xyz);
               // float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
               //
               // float directColor = DirectBRDF(N, V, L, diffuseColor, _MainLightColor, roughness, _Metallic);
               return float4(1,0,0,1);
           }
           ENDHLSL
       }

   }
}