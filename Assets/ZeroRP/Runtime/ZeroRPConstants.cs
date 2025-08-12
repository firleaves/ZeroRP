using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ZeroRP
{
    public static class ZeroRPConstants
    {
        internal const int GBufferSize = 4;
        internal const int GBufferLightingIndex = 3;

        internal static readonly GraphicsFormat[] GBufferFormats = new GraphicsFormat[]
        {
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.None // GBuffer3: Lighting 
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