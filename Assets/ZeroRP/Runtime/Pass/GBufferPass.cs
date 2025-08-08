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
            int maxMRT = SystemInfo.supportedRenderTargetCount;
            Debug.Log($"Max MRT supported: {maxMRT}, trying to use: {deferredData.GBuffer.Length}");

            if (deferredData.GBuffer.Length > maxMRT)
            {
                Debug.LogError($"Exceeding MRT limit! Max: {maxMRT}, Requested: {deferredData.GBuffer.Length}");
            }
            var testFormat = GraphicsFormat.R8G8B8A8_UNorm;
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData, _profilingSampler))
            {


               // 创建新的纹理数组并存储到 passData 中
                passData.GBufferTextureHandles = new TextureHandle[4];
                var gBuffer = passData.GBufferTextureHandles;
                deferredData.GBuffer = gBuffer;
                // 如果 2 个 MRT 测试成功，可以改为 4
                int mrtCount = 4; // 测试完整的 4 个 RT
                Debug.Log($"Testing with {mrtCount} MRTs");
                
                for (int i = 0; i < mrtCount; i++)
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
                    else
                    {
                        Debug.Log($"Successfully created GBuffer{i}: {GBufferNames[i]}");
                    }
                }
                
                // 设置 MRT 附件 - 只绑定实际创建的RT
                Debug.Log("Setting MRT attachments...");
                
                // 只绑定实际创建的RT数量
                for (int i = 0; i < mrtCount; i++)
                {
                    if (gBuffer[i].IsValid())
                    {
                        builder.SetRenderAttachment(gBuffer[i], i, AccessFlags.Write);
                        Debug.Log($"Set GBuffer{i} to attachment slot {i} - Valid: {gBuffer[i].IsValid()}");
                    }
                    else
                    {
                        Debug.LogError($"GBuffer{i} is invalid, cannot set as render attachment!");
                    }
                }
                
                Debug.Log($"MRT setup complete with {mrtCount} targets");
                

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
    
                    Debug.Log("GBuffer render function executing...");
                    
                    // 添加调试标记
                    context.cmd.BeginSample("GBuffer MRT Test");
                    
                    // 绘制渲染列表
                    context.cmd.DrawRendererList(data.RendererListHandle);
                    
                    context.cmd.EndSample("GBuffer MRT Test");
                    
                    Debug.Log("GBuffer render function completed");
                });


            }
        }

       
    }
}