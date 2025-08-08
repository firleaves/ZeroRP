#ifndef PBRLITINPUT_HLSL
#define PBRLITINPUT_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Assets/ZeroRP/Shaders/Library/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _BaseMap_TexelSize;
half4 _BaseColor;

half _NormalStrength;

half _MetallicStrength;
half _Smoothness;

half _OcclusionStrength;

half _Cutoff;
half _Surface;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_MetallicMap);
SAMPLER(sampler_MetallicMap);

TEXTURE2D(_OcclusionMap);
SAMPLER(sampler_OcclusionMap);



struct InputData
{
    float3  positionWS;
    float4  positionCS;
    float3  normalWS;
    half3   viewDirectionWS;
};



inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha =half4(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv));
    outSurfaceData.alpha = albedoAlpha.a* _BaseColor.a;

    // #if defined(_ALPHATEST_ON)
    //     clip(outSurfaceData.alpha- _Cutoff);
    // #endif

    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    
    // r metallic   gb unused   a smoothness
    half4 matallicColor = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, uv);

    outSurfaceData.metallic = matallicColor.r * _MetallicStrength;
   

    outSurfaceData.smoothness = matallicColor.a * _Smoothness;
    outSurfaceData.normalTS = UnpackNormalScale(half4(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv)),_NormalStrength);
    outSurfaceData.occlusion = LerpWhiteTo(SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g,_OcclusionStrength);
}


#endif