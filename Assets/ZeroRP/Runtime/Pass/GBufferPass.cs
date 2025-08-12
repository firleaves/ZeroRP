using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RendererUtils;
namespace ZeroRP
{
    public class GBufferPass
    {

        private class PassData
        {
            internal TextureHandle[] GBufferTextureHandles;
            internal RendererListHandle RendererListHandle;
        }

        private const string PassName = "GBuffer";
        private ProfilingSampler _profilingSampler = new ProfilingSampler(PassName);

        private ShaderTagId _shaderTagId = new ShaderTagId(PassName);

        public static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph, RenderTextureDescriptor desc, string name, bool clear,
                  FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            TextureDesc rgDesc = new TextureDesc(desc.width, desc.height);
            rgDesc.dimension = desc.dimension;
            rgDesc.clearBuffer = clear;
            rgDesc.bindTextureMS = desc.bindMS;
            rgDesc.format = (desc.depthStencilFormat != GraphicsFormat.None) ? desc.depthStencilFormat : desc.graphicsFormat;
            rgDesc.slices = desc.volumeDepth;
            rgDesc.msaaSamples = (MSAASamples)desc.msaaSamples;
            rgDesc.name = name;
            rgDesc.enableRandomWrite = desc.enableRandomWrite;
            rgDesc.filterMode = filterMode;
            rgDesc.wrapMode = wrapMode;
            rgDesc.isShadowMap = desc.shadowSamplingMode != ShadowSamplingMode.None && desc.depthStencilFormat != GraphicsFormat.None;
            rgDesc.vrUsage = desc.vrUsage;
            rgDesc.useDynamicScale = desc.useDynamicScale;
            rgDesc.useDynamicScaleExplicit = desc.useDynamicScaleExplicit;

            return renderGraph.CreateTexture(rgDesc);
        }

        public void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraColor, TextureHandle cameraDepth)
        {
            var cameraData = frameData.Get<CameraData>();
            var deferredData = frameData.Get<DeferredData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData, _profilingSampler))
            {
                var gBuffer = passData.GBufferTextureHandles = deferredData.GBuffer;

                for (int i = 0; i < ZeroRPConstants.GBufferSize; i++)
                {
                    if (i == ZeroRPConstants.GBufferLightingIndex)
                    {
                        gBuffer[i] = cameraColor;
                    }
                    else
                    {
                  
                        // 参考URP的实现方式，但需要转换为TextureDesc
                        var gbufferSlice = new RenderTextureDescriptor(cameraData.Camera.pixelWidth, cameraData.Camera.pixelHeight);
                        gbufferSlice.depthStencilFormat = GraphicsFormat.None; // 确保不创建深度表面
                        gbufferSlice.stencilFormat = GraphicsFormat.None;
                        gbufferSlice.graphicsFormat = ZeroRPConstants.GBufferFormats[i];

                        gBuffer[i] = CreateRenderGraphTexture(renderGraph, gbufferSlice, ZeroRPConstants.GBufferNames[i], true);
                    }
                    builder.SetRenderAttachment(gBuffer[i], i, AccessFlags.Write);
                }
                builder.SetRenderAttachmentDepth(cameraDepth, AccessFlags.Write);

                var rendererDesc = new RendererListDesc(new ShaderTagId[] { _shaderTagId }, cameraData.CullingResults, cameraData.Camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                    excludeObjectMotionVectors = false
                };

                passData.RendererListHandle = renderGraph.CreateRendererList(rendererDesc);
                builder.UseRendererList(passData.RendererListHandle);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                // 设置渲染函数
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if (!data.RendererListHandle.IsValid())
                    {
                        Debug.LogError("RendererList is invalid in render function!");
                        return;
                    }
                    context.cmd.DrawRendererList(data.RendererListHandle);
                });
            }
        }
    }
}