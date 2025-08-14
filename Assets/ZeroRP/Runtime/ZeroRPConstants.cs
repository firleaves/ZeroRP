using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ZeroRP
{
    public static class ZeroRPConstants
    {
        internal const int GBufferSize = 4;
        public const int GBufferAlbedoIndex = 0;      // Albedo + Occlusion
        public const int GBufferNormalIndex = 1;      // Normal + Smoothness
        public const int GBufferMetallicIndex = 2;    // Metallic + Specular
        public const int GBufferLightingIndex = 3;    // Lighting
        internal static readonly GraphicsFormat[] GBufferFormats = new GraphicsFormat[]
        {
            GraphicsFormat.R8G8B8A8_SRGB,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.R8G8B8A8_UNorm 
        };

        internal static readonly string[] GBufferNames = new string[]
        {
            "_GBuffer0",
            "_GBuffer1",
            "_GBuffer2",
            "_GBuffer3"
        };

        internal static readonly int[] GBufferShaderPropertyIDs = new int[]
        {
            Shader.PropertyToID(ZeroRPConstants.GBufferNames[0]),
            Shader.PropertyToID(ZeroRPConstants.GBufferNames[1]),
            Shader.PropertyToID(ZeroRPConstants.GBufferNames[2]),
            Shader.PropertyToID(ZeroRPConstants.GBufferNames[3]),
        };
    }
}