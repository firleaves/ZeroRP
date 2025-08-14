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
        private ProfilingSampler _profilingSampler = new ProfilingSampler("Draw GBuffer");

        private ShaderTagId _shaderTagId = new ShaderTagId(PassName);

     

        public void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraColor, TextureHandle cameraDepth)
        {
            var cameraData = frameData.Get<CameraData>();
            var deferredData = frameData.Get<DeferredData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData, _profilingSampler))
            {
                var gBuffer = new TextureHandle[4];
                
                // 创建GBuffer渲染目标
                for (int i = 0; i < ZeroRPConstants.GBufferSize; i++)
                {
                    if (i == ZeroRPConstants.GBufferLightingIndex)
                    {
                        // 光照缓冲区复用主渲染目标
                        gBuffer[i] = cameraColor;
                    }
                    else
                    {
                        // 创建GBuffer纹理
                        var gbufferSlice = new RenderTextureDescriptor(Screen.width, Screen.height);
                        gbufferSlice.depthStencilFormat = GraphicsFormat.None; // 确保不创建深度表面
                        gbufferSlice.stencilFormat = GraphicsFormat.None;
                        gbufferSlice.graphicsFormat = ZeroRPConstants.GBufferFormats[i];
                        gbufferSlice.msaaSamples = 1;
                        gbufferSlice.useMipMap = false;
                        gbufferSlice.autoGenerateMips = false;
                        
                        gBuffer[i] = ZeroRenderPipeline.CreateRenderGraphTexture(renderGraph, gbufferSlice, ZeroRPConstants.GBufferNames[i], true);
                    }
                    
                    // 设置渲染附件，确保MRT正确配置
                    // builder.SetRenderAttachment(gBuffer[i], i, AccessFlags.Write);
                }
                //
                // 保存GBuffer引用到帧数据中
                deferredData.GBuffer = gBuffer;
                builder.SetRenderAttachment(gBuffer[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(gBuffer[1], 1, AccessFlags.Write);
                builder.SetRenderAttachment(gBuffer[2], 2, AccessFlags.Write);
                builder.SetRenderAttachment(gBuffer[3], 3, AccessFlags.Write);
                // 设置深度附件
                builder.SetRenderAttachmentDepth(cameraDepth, AccessFlags.Write);

                // 创建渲染列表
                var rendererDesc = new RendererListDesc(_shaderTagId, cameraData.CullingResults, cameraData.Camera)
                {
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    renderQueueRange = RenderQueueRange.opaque,
                };

                passData.RendererListHandle = renderGraph.CreateRendererList(rendererDesc);
                builder.UseRendererList(passData.RendererListHandle);
                
                // 重要：允许全局状态修改，确保MRT设置生效
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
                    
                    // 确保MRT设置正确 - 这是关键！
                    context.cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
                    context.cmd.DrawRendererList(data.RendererListHandle);
                });
            }
        }
    }
}