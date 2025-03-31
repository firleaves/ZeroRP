using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace DeferredRP
{
    public class GBufferPass
    {
        public const string PassName = "GBufferPass";
        private ProfilingSampler _profilingSampler;
        private RenderTargetIdentifier[] _GBufferRTIs;
        private RenderTexture[] _GBufferRTs;
        public RenderTargetIdentifier[] GBufferIDs => _GBufferRTIs;
        // private int[] _GBufferIDs;

        private RenderTexture _depthRT;
        private RenderTargetIdentifier _depthID;
        private const int _GBufferCount = 4;


        private ShaderTagId _shaderTagId;


        /*
         * https://docs.unity3d.com/cn/2019.4/Manual/RenderTech-DeferredShading.html
         *  GBuffer分布
         * RT0，ARGB32 格式：漫射颜色 (RGB)，遮挡 (A)。
         * RT1，ARGB32 格式：镜面反射颜色 (RGB)，粗糙度 (A)。
         * RT2，ARGB2101010 格式：世界空间法线 (RGB)，未使用 (A)。
         * RT3，ARGB2101010（非 HDR）或 ARGBHalf (HDR) 格式：发射 + 光照 + 光照贴图 + 反射探针缓冲区
         */
        public GBufferPass()
        {
            _profilingSampler = new ProfilingSampler(PassName);


            _depthRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
            _depthID = _depthRT;
            Shader.SetGlobalTexture(ShaderConstants.DepthTex, _depthRT);
            // 创建纹理
            _GBufferRTs = new RenderTexture[_GBufferCount]
            {
                new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear),
                new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear),
                new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear),
                new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear)
            };

            _GBufferRTIs = new RenderTargetIdentifier[_GBufferCount];
            for (int i = 0; i < _GBufferCount; i++)
            {
                _GBufferRTIs[i] = _GBufferRTs[i];
            }


            // _GBufferIDs = new[]
            // {
            //     Shader.PropertyToID("_GT0"),
            //     Shader.PropertyToID("_GT1"),
            //     Shader.PropertyToID("_GT2"),
            //     Shader.PropertyToID("_GT3")
            // };

            for (int i = 0; i < _GBufferCount; i++)
            {
                Shader.SetGlobalTexture("_GT" + i, _GBufferRTs[i]);
            }

            _shaderTagId = new ShaderTagId("GBuffer");
        }

        public void RenderCamera(ScriptableRenderContext context, Camera camera, CullingResults cullingResults)
        {
            CommandBuffer cmd = CommandBufferPool.Get(PassName);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            using (new ProfilingScope(cmd, _profilingSampler))
            {
                //设置渲染目标为MRT，并清理它们
                cmd.SetRenderTarget(_GBufferRTIs, _depthID);
                cmd.ClearRenderTarget(true, true, Color.clear);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                camera.TryGetCullingParameters(out var cullingParameters);
                cullingResults = context.Cull(ref cullingParameters);

                SortingSettings sortingSettings = new SortingSettings(camera);
                DrawingSettings drawingSettings = new DrawingSettings(_shaderTagId, sortingSettings);
                FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
                context.Submit();
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}