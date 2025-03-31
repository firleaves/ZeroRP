using UnityEngine;

namespace DeferredRP
{
    public class ShaderConstants
    {
        public static readonly int TileLightsArgsBuffer = Shader.PropertyToID("_TileLightsArgsBuffer");
        public static readonly int TileLightsIndicesBuffer = Shader.PropertyToID("_TileLightsIndicesBuffer");
        public static readonly int DeferredTileParams = Shader.PropertyToID("_DeferredTileParams");
        public static readonly int ZBufferParams = Shader.PropertyToID("_ZBufferParams");
        public static readonly int CameraNearPlaneLB = Shader.PropertyToID("_CameraNearPlaneLB");
        public static readonly int CameraNearBasis = Shader.PropertyToID("_CameraNearBasis");
        public static readonly int RWTileLightsArgsBuffer = Shader.PropertyToID("_RWTileLightsArgsBuffer");
        public static readonly int RWTileLightsIndicesBuffer = Shader.PropertyToID("_RWTileLightsIndicesBuffer");

        public static readonly int PointLightPosRadiusBuffer = Shader.PropertyToID("_PointLightPosRadiusBuffer");
        public static readonly int PointLightColorBuffer = Shader.PropertyToID("_PointLightColorBuffer");
        public static readonly int PointLightCount = Shader.PropertyToID("_PointLightCount");


        public static readonly int MainLightDirection = Shader.PropertyToID("_MainLightDirection");
        public static readonly int MainLightColor = Shader.PropertyToID("_MainLightColor");

        public static readonly int DepthTex = Shader.PropertyToID("_DepthTex");
    }
}