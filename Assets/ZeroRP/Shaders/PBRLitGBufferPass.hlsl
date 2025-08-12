#ifndef PBRLITGBUFFERPASS_HLSL
#define PBRLITGBUFFERPASS_HLSL

#include "Library/Input.hlsl"
#include "Library/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Library/GBuffer.hlsl"
#include "Library/ShaderVarablesFunctions.hlsl"

// 添加缺失的宏定义
#ifndef TRANSFORM_TEX
#define TRANSFORM_TEX(tex, name) ((tex.xy) * name##_ST.xy + name##_ST.zw)
#endif

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
};


struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half4 tangentWS : TEXCOORD3; // xyz: tangent, w: sign
};


void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

    inputData.positionCS = input.positionCS;
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    float sgn = input.tangentWS.w; // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
}


Varyings LitGBufferPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    output.normalWS = TransformObjectToWorldNormal(input.normalOS);

    half3 tangentWS = real3(TransformObjectToWorldDir(input.tangentOS.xyz));
    real sign = input.tangentOS.w* GetOddNegativeScale();
    output.tangentWS = half4(tangentWS, sign);

    return output;
}

FragmentOutput LitGBufferPassFragment(Varyings input)
{


    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
    
    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    
    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.smoothness, brdfData);
    
    return BRDFDataToGBuffer(brdfData, inputData, surfaceData.smoothness, half3(0.1, 0.1, 0), surfaceData.occlusion);

}
#endif
