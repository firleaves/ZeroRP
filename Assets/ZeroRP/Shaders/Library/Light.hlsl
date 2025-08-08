#ifndef LIGHT_HLSL
#define LIGHT_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"


#define MAX_LIGHT_COUNT 1024
#define MAX_LIGHT_COUNT_PER_TILE 32

CBUFFER_START(_CustomLight)
    float4 _MainLightDirection;
    float4 _MainLightColor;
CBUFFER_END


StructuredBuffer<float4> _PointLightPosRadiusBuffer;
StructuredBuffer<half4> _PointLightColorBuffer;


//这里不能用RWStructuredBuffer，shader编译不过，所以要和compute shader  使用两个变量，cs里面使用 RWStructuredBuffer

//每个tile内灯光数量
StructuredBuffer<uint> _TileLightsArgsBuffer;
//储存每个tile可见灯光索引
StructuredBuffer<uint> _TileLightsIndicesBuffer;

#endif
