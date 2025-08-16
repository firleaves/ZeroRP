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
            internal RendererListHandle RendererListHandle;
        }

        private const string PassName = "GBuffer";
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Draw GBuffer");

        private readonly ShaderTagId _shaderTagId = new ShaderTagId(PassName);


        public GBufferPass()
        {
            
        }
        

        public void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraColor, TextureHandle cameraDepth)
        {
            var cameraData = frameData.Get<CameraData>();
            var deferredData = frameData.Get<DeferredData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData, _profilingSampler))
            {
                var gBuffer = deferredData.GBuffer;

                for (int i = 0; i < ZeroRPConstants.GBufferSize; i++)
                {
                    // 创建GBuffer纹理
                    var gBufferRTDesc = new RenderTextureDescriptor(cameraData.CameraTargetDescriptor.width, cameraData.CameraTargetDescriptor.height)
                    {
                        depthStencilFormat = GraphicsFormat.None, // 确保不创建深度表面
                        stencilFormat = GraphicsFormat.None,
                        graphicsFormat = ZeroRPConstants.GBufferFormats[i],
                        msaaSamples = 1,
                        useMipMap = false,
                        autoGenerateMips = false
                    };

                    gBuffer[i] = ZeroRenderPipeline.CreateRenderGraphTexture(renderGraph, gBufferRTDesc, ZeroRPConstants.GBufferNames[i], true);

                    builder.SetRenderAttachment(gBuffer[i], i, AccessFlags.Write);
                }
                var depthDesc = new TextureDesc(cameraData.CameraTargetDescriptor.width, cameraData.CameraTargetDescriptor.height);
                depthDesc.depthBufferBits = DepthBits.Depth32;
                var depthTexture = renderGraph.CreateTexture(depthDesc);
                deferredData.GBuffer = gBuffer;

                // 设置深度附件
                builder.SetRenderAttachmentDepth(depthTexture, AccessFlags.Write);

                // 创建渲染列表
                var rendererDesc = new RendererListDesc(_shaderTagId, cameraData.CullingResults, cameraData.Camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
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