#ifndef BRDF_HLSL
#define BRDF_HLSL

#define PI  3.14159265359

#define kDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)


struct BRDFData
{
    half3 albedo;
    half3 diffuse;
    half3 specular;
    half reflectivity;
    half roughness;
};

half OneMinusReflectivityMetallic(half metallic)
{
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in kDielectricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = kDielectricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}


 void InitializeBRDFData(half3 albedo, half metallic,  half smoothness, out BRDFData outBRDFData)
{
    half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
    half reflectivity = half(1.0) - oneMinusReflectivity;
    half3 brdfDiffuse = albedo * oneMinusReflectivity;
    half3 brdfSpecular = lerp(kDielectricSpec.rgb, albedo, metallic);

    outBRDFData = (BRDFData)0;
    outBRDFData.albedo = albedo;
    outBRDFData.diffuse = brdfDiffuse;
    outBRDFData.specular = brdfSpecular;
    outBRDFData.reflectivity = reflectivity;
    outBRDFData.roughness = PerceptualSmoothnessToRoughness(smoothness);
}


//https://learnopengl.com/PBR/Theory
//Cook-Torrance

//D
float DistributionGGX(float NdotH, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH2 = NdotH * NdotH;

    float nom = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return nom / denom;
}

//F 
float3 FresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;

    float nom = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return nom / denom;
}

// G  
float GeometrySmith(float NdotV, float NdotL, float roughness)
{
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);

    float ggx1 = GeometrySchlickGGX(NdotL, roughness);
    return ggx1 * ggx2;
}


float3 DirectBRDF(float3 N, float3 V, float3 L, float3 albedo, float3 radiance, float roughness, float metallic)
{
    //unity内限制 roughness 范围，保证光滑度=0的时候，还保留一点高光效果
    roughness = max(0.002, roughness);

    float3 H = normalize(V + L);
    float NdotH = max(dot(N, H), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float NdotV = max(dot(N, V), 0.0);
    float HdotV = max(dot(H, V), 0.0);

    float D = DistributionGGX(NdotH, roughness);

    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);
    float3 F = FresnelSchlick(NdotH, F0);

    float G = GeometrySmith(NdotV, NdotL, roughness);

    float3 f_diffuse = albedo / PI;


    //金属(Metallic)材质会立即吸收所有折射光，故而金属只有镜面反射，而没有折射光引起的漫反射。
    //菲尼尔函数已经获得了材质反射部分，所以这里ks = F
    float3 k_s = F;

    //一般kd =  1- ks 即可，这里* (1.0 - metallic) 代表金属表面不反射光，没有漫反射颜色，非金属值漫反射贡献就靠 1- metallic得到
    float3 k_d = (float3(1.0, 1.0, 1.0) - k_s) * (1.0 - metallic);

    float3 f_specular = (D * F * G) / (4.0 * NdotV * NdotL + 0.0001);

    // Unity Albedo 没乘 PI, 为保持 d 和 s 的比例, Specular 也乘以 PI
    f_diffuse *= PI;
    f_specular *= PI;

    float3 color = (k_d * f_diffuse + f_specular) * radiance * NdotL;

    return color;
}

float3 IndirectIBL(float3 N, float3 V, float3 albedo, float roughness, float metallic, samplerCUBE sampler_diffuseIBL,
                   samplerCUBE sampler_specularIBL, sampler2D sampler_brdfLut)
{
    return 0;
}

#endif
