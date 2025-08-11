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
        private ProfilingSampler _profilingSampler =  new ProfilingSampler(PassName);



        private readonly GraphicsFormat[] GBufferFormats = new GraphicsFormat[]
        {
            GraphicsFormat.R8G8B8A8_SRGB,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.R8G8B8A8_UNorm
        };

        private readonly string[] GBufferNames = new string[]
        {
            "_GBuffer0",
            "_GBuffer1",
            "_GBuffer2",
            "_GBuffer3"
        };
        
        private RTHandle _depthRT;
        private RenderTargetIdentifier _depthID;


        private ShaderTagId _shaderTagId = new ShaderTagId(PassName);
        

        public void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraColor, TextureHandle cameraDepth)
        {


            var cameraData = frameData.Get<CameraData>();
            var deferredData = frameData.Get<DeferredData>();

            var testFormat = GraphicsFormat.R8G8B8A8_UNorm;
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData, _profilingSampler))
            {
                passData.GBufferTextureHandles = new TextureHandle[4];
                var gBuffer = passData.GBufferTextureHandles = deferredData.GBuffer;
                for (int i = 0; i < ZeroRPConstants.GBufferSize; i++)
                {
                    if (i == 3)
                    {
                        gBuffer[i] = cameraColor;
                    }
                    else
                    {
                        TextureDesc rgDesc = new TextureDesc(cameraData.Camera.pixelWidth, cameraData.Camera.pixelHeight);
                        rgDesc.dimension = TextureDimension.Tex2D;
                        rgDesc.clearBuffer = true;
                        rgDesc.clearColor = Color.clear;
                        rgDesc.format = testFormat;
                        rgDesc.name = GBufferNames[i];
                        rgDesc.enableRandomWrite = false;
                        rgDesc.filterMode = FilterMode.Point;
                        rgDesc.wrapMode = TextureWrapMode.Clamp;
                        rgDesc.msaaSamples = MSAASamples.None;
                   
                        gBuffer[i] = renderGraph.CreateTexture(rgDesc);
                        if (!gBuffer[i].IsValid())
                        {
                            Debug.LogError($"Failed to create GBuffer{i}!");
                        }
                    }
                    
                    builder.SetRenderAttachment(gBuffer[i], i, AccessFlags.Write);
                }
                
                
                builder.SetRenderAttachmentDepth(cameraDepth, AccessFlags.Write);
              

                var rendererDesc = new RendererListDesc( _shaderTagId , cameraData.CullingResults, cameraData.Camera)
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
    
                    
                    // 添加调试标记
                    
                    // 绘制渲染列表
                    context.cmd.DrawRendererList(data.RendererListHandle);
                    
                    
                });


            }
        }

       
    }
}