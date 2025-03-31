using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace DeferredRP
{
    [CreateAssetMenu(menuName = "RenderPipeline/DeferredRenderPipelineAsset")]
    public class DeferredRenderPipelineAsset : RenderPipelineAsset
    {
        #region Define

        public enum LightCullingType
        {
            Tile,
            Cluster
        }


        [Serializable]
        public class TileLightCulling
        {
            public int TileX = 16;
            public int TileY = 16;
            public int MaxLightPerTile = 64;
             public ComputeShader TileLightComputeShader;
        }

        #endregion

        public int MaxLightCount = 1024;

        public TileLightCulling TileLightCullingSetting;


        protected override RenderPipeline CreatePipeline()
        {
            return new DeferredRenderPipeline(this);
        }
    }
}