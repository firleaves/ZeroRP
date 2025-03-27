using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace ZeroRP
{
    public partial class ZeroRPRenderGraphRecorder : IRenderGraphRecorder, IDisposable
    {
        private static readonly ProfilingSampler TempPassSampler = new ProfilingSampler("TempPass");

        private static readonly ShaderTagId[] ShaderTagIds = new ShaderTagId[]
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("ZeroRPForward")
        }; //渲染标签IDs

        private TextureHandle _colorHandle;
        private TextureHandle _depthHandle;

        private RTHandle _colorRTHandle;
        private RTHandle _depthRTHandle;

 

        public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<CameraData>();


            CreateRenderGraphCameraRenderTargets(renderGraph, cameraData);


            var clearFlags = cameraData.GetClearFlags();

            if (clearFlags != RTClearFlags.None)
            {
                AddClearRenderTargetPass(renderGraph, cameraData);
            }
            
            AddDrawOpaqueObjectsPass(renderGraph, cameraData);
        }


        private void CreateRenderGraphCameraRenderTargets(RenderGraph renderGraph, CameraData cameraData)
        {
            var targetTexture = cameraData.Camera.targetTexture;
            var cameraTargetTexture = targetTexture;
            bool isBuildInTexture = (cameraTargetTexture == null);
            bool isCameraTargetOffscreenDepth = !isBuildInTexture && targetTexture.format == RenderTextureFormat.Depth;
            RenderTargetIdentifier targetColorId = isBuildInTexture ? BuiltinRenderTextureType.CameraTarget : new RenderTargetIdentifier(cameraTargetTexture);
            if (_colorRTHandle == null)
            {
                _colorRTHandle = RTHandles.Alloc(targetColorId, "Color RT");
            }

            RenderTargetIdentifier targetDepthId = isBuildInTexture ? BuiltinRenderTextureType.Depth : new RenderTargetIdentifier(cameraTargetTexture);
            if (_depthRTHandle == null)
            {
                _depthRTHandle = RTHandles.Alloc(targetDepthId, "Depth RT");
            }

            Color clearColor = cameraData.GetClearColor();
            RTClearFlags clearFlags = cameraData.GetClearFlags();

            bool clearOnFirstUse = !renderGraph.nativeRenderPassesEnabled;
            bool discardColorBackbufferOnLastUse = !renderGraph.nativeRenderPassesEnabled;
            bool discardDepthBackbufferOnLastUse = !isCameraTargetOffscreenDepth;


            ImportResourceParams importBackbufferColorParams = new ImportResourceParams();
            importBackbufferColorParams.clearOnFirstUse = clearOnFirstUse;
            importBackbufferColorParams.clearColor = clearColor;
            importBackbufferColorParams.discardOnLastUse = discardColorBackbufferOnLastUse;

            ImportResourceParams importBackbufferDepthParams = new ImportResourceParams();
            importBackbufferDepthParams.clearOnFirstUse = clearOnFirstUse;
            importBackbufferDepthParams.clearColor = clearColor;
            importBackbufferDepthParams.discardOnLastUse = discardDepthBackbufferOnLastUse;

            bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            RenderTargetInfo importInfoColor = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth;
            if (isBuildInTexture)
            {
                importInfoColor.width = Screen.width;
                importInfoColor.height = Screen.height;
                importInfoColor.volumeDepth = 1;
                importInfoColor.msaaSamples = 1;
                importInfoColor.format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, colorRT_sRGB);
                importInfoColor.bindMS = false;

                importInfoDepth = importInfoColor;
                importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            }
            else
            {
                importInfoColor.width = cameraTargetTexture.width;
                importInfoColor.height = cameraTargetTexture.height;
                importInfoColor.volumeDepth = cameraTargetTexture.volumeDepth;
                importInfoColor.msaaSamples = cameraTargetTexture.antiAliasing;
                importInfoColor.format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, colorRT_sRGB);
                importInfoColor.bindMS = false;

                importInfoDepth = importInfoColor;
                importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            }

            _colorHandle = renderGraph.ImportTexture(_colorRTHandle, importInfoColor, importBackbufferColorParams);
            _depthHandle = renderGraph.ImportTexture(_depthRTHandle, importInfoDepth, importBackbufferDepthParams);
        }

        #region Clar RenderTarget

        internal class ClearRenderTargetPassData
        {
            internal RTClearFlags ClearFlags { get; set; }
            internal Color ClearColor { get; set; }
        }

        private void AddClearRenderTargetPass(RenderGraph renderGraph, CameraData cameraData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<ClearRenderTargetPassData>("Clear Render Target Pass", out var passData))
            {
                passData.ClearColor = cameraData.GetClearColor();
                passData.ClearFlags = cameraData.GetClearFlags();

                if (_colorHandle.IsValid()) builder.SetRenderAttachment(_colorHandle, 0, AccessFlags.Write);
                if (_depthHandle.IsValid()) builder.SetRenderAttachmentDepth(_depthHandle, AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((ClearRenderTargetPassData data, RasterGraphContext context) => { context.cmd.ClearRenderTarget(data.ClearFlags, data.ClearColor, 1, 0); });
            }
        }

        #endregion


        #region Draw Opaque Objects

        internal class DrawOpaqueObjectsPassData
        {
            internal RendererListHandle OpaqueRendererListHandle { get; set; }
        }

        private void AddDrawOpaqueObjectsPass(RenderGraph renderGraph, CameraData cameraData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<DrawOpaqueObjectsPassData>("Draw Opaque Objects Pass", out var passData))
            {
                //创建不透明对象渲染列表
                var opaqueRendererDesc = new RendererListDesc(ShaderTagIds, cameraData.CullingResults, cameraData.Camera);
                opaqueRendererDesc.sortingCriteria = SortingCriteria.CommonOpaque;
                opaqueRendererDesc.renderQueueRange = RenderQueueRange.opaque;
                passData.OpaqueRendererListHandle = renderGraph.CreateRendererList(opaqueRendererDesc);
                //RenderGraph引用不透明渲染列表
                builder.UseRendererList(passData.OpaqueRendererListHandle);


                if (_colorHandle.IsValid()) builder.SetRenderAttachment(_colorHandle, 0, AccessFlags.Write);
                if (_depthHandle.IsValid()) builder.SetRenderAttachmentDepth(_depthHandle, AccessFlags.Write);
                

                builder.AllowPassCulling(false);
                //TODO 无法理解
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((DrawOpaqueObjectsPassData data, RasterGraphContext context) =>
                {
                   Debug.Log("Draw Opaque Objects Pass");
                });
            }
        }

        #endregion

        #region Draw Skybox

        #endregion
        
        #region Draw Transparent Objects

        #endregion

        public void Dispose()
        {
        }
    }
}