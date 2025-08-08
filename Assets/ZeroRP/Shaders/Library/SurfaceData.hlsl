#ifndef SURFACE_DATA_INCLUDED
#define SURFACE_DATA_INCLUDED

struct SurfaceData
{
    half3 albedo;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half  occlusion;
    half  alpha;
};

#endif
