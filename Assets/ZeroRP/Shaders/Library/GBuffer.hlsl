#ifndef GBUFFER_INCLUDED
#define GBUFFER_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Assets/ZeroRP/Shaders/Library/BRDF.hlsl"

// struct FragmentOutput
// {
//     half4 GBuffer0 : SV_Target0;
//     half4 GBuffer1 : SV_Target1;
//     half4 GBuffer2 : SV_Target2;
//     half4 GBuffer3 : SV_Target3; 
// };

struct FragmentOutput
{
    half4 GBuffer0 ;
    half4 GBuffer1 ;
    half4 GBuffer2 ;
    half4 GBuffer3 ; 
};

// 使用八面体编码将3D法线压缩到2D
half3 PackNormal(half3 n)
{
    float2 octNormalWS = PackNormalOctQuadEncode(n);                  // values between [-1, +1], must use fp32 on some platforms.
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0, +1]
    return half3(PackFloat2To888(remappedOctNormalWS));               // values between [ 0, +1]
}

half3 UnpackNormal(half3 pn)
{
    half2 remappedOctNormalWS = half2(Unpack888ToFloat2(pn));          // values between [ 0, +1]
    half2 octNormalWS = remappedOctNormalWS.xy * half(2.0) - half(1.0);// values between [-1, +1]
    return half3(UnpackNormalOctQuadEncode(octNormalWS));              // values between [-1, +1]
}
float PackMaterialFlags(uint materialFlags)
{
    return materialFlags * (1.0h / 255.0h);
}

FragmentOutput BRDFDataToGBuffer(BRDFData brdfData, InputData inputData, half smoothness, half3 globalIllumination, half occlusion = 1.0)
{
    half3 packedNormalWS = PackNormal(inputData.normalWS);

    uint materialFlags = 0;
    
    half3 packedSpecular;
    packedSpecular.r = brdfData.reflectivity;
    packedSpecular.gb = 0.0;
    
    FragmentOutput output;
    output.GBuffer0 = half4(brdfData.albedo.rgb, PackMaterialFlags(materialFlags));  // diffuse           diffuse         diffuse         materialFlags   (sRGB rendertarget)
    output.GBuffer1 = half4(packedSpecular, occlusion);                              // metallic/specular specular        specular        occlusion
    output.GBuffer2 = half4(packedNormalWS, smoothness);                             // encoded-normal    encoded-normal  encoded-normal  smoothness
    output.GBuffer3 = half4(globalIllumination, 1);                                  // GI                GI              GI              unused          (lighting buffer)

    // output.GBuffer0 = half4(1,0,0,0);
    // output.GBuffer1 = half4(0,1,0,0);
    // output.GBuffer2 = half4(0,0,1,0);
    // output.GBuffer3 = half4(1,1,0,0);
    return output;
}



#endif