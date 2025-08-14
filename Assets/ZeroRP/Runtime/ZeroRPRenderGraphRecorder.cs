using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
            new ShaderTagId("ExampleLightModeTag")
        }; //渲染标签IDs

        private static readonly ShaderTagId s_shaderTagId = new ShaderTagId("SRPDefaultUnlit"); //渲染标签ID
        
        private TextureHandle _colorHandle;
        private TextureHandle _depthHandle;

        private RTHandle _colorRTHandle;
        private RTHandle _depthRTHandle;

        private GBufferPass _gBufferPass = new GBufferPass();
        private DeferredPass _deferredPass = new DeferredPass();
        private ClearRenderTargetPass _clearRenderTargetPass = new ClearRenderTargetPass();
        private SkyBoxPass _skyBoxPass = new SkyBoxPass();

        public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<CameraData>();


            CreateRenderGraphCameraRenderTargets(renderGraph, cameraData);
            _clearRenderTargetPass.Render(renderGraph, frameData,_colorHandle,_depthHandle);
            _gBufferPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);
           
            _deferredPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);
            _skyBoxPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);
          

            //Editor
            AddEditorRenderTargetPass(renderGraph);
            AddDrawEditorGizmoPass(renderGraph, cameraData, GizmoSubset.PreImageEffects);
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
            importBackbufferColorParams.clearOnFirstUse = true;
            importBackbufferColorParams.clearColor = clearColor;
            importBackbufferColorParams.discardOnLastUse = true;

            ImportResourceParams importBackbufferDepthParams = new ImportResourceParams();
            importBackbufferDepthParams.clearOnFirstUse = true;
            importBackbufferDepthParams.clearColor = clearColor;
            importBackbufferDepthParams.discardOnLastUse = true;

            bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            RenderTargetInfo importInfoColor = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth;
            if (isBuildInTexture)
            {
                importInfoColor.width = cameraData.Camera.pixelWidth;
                importInfoColor.height = cameraData.Camera.pixelHeight;
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
        

        #region Draw Opaque Objects

        internal class DrawOpaqueObjectsPassData
        {
            internal TextureHandle backbufferHandle;
            internal RendererListHandle OpaqueRendererListHandle { get; set; }
        }
        
        private void AddDrawOpaqueObjectsPass(RenderGraph renderGraph, CameraData cameraData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<DrawOpaqueObjectsPassData>("Draw Opaque Objects Pass", out var passData))
            {
                //创建不透明对象渲染列表
                var opaqueRendererDesc = new RendererListDesc(s_shaderTagId, cameraData.CullingResults, cameraData.Camera);
                opaqueRendererDesc.sortingCriteria = SortingCriteria.CommonOpaque;
                opaqueRendererDesc.renderQueueRange = RenderQueueRange.opaque;
                passData.OpaqueRendererListHandle = renderGraph.CreateRendererList(opaqueRendererDesc);
                //RenderGraph引用不透明渲染列表
                builder.UseRendererList(passData.OpaqueRendererListHandle);
        
                // passData.backbufferHandle = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CurrentActive);
                // builder.SetRenderAttachment(passData.backbufferHandle, 0, AccessFlags.Write);
                if (_colorHandle.IsValid()) builder.SetRenderAttachment(_colorHandle, 0, AccessFlags.Write);
                if (_depthHandle.IsValid()) builder.SetRenderAttachmentDepth(_depthHandle, AccessFlags.Write);
        
                builder.AllowPassCulling(false);
                // builder.AllowPassCulling(false);
                // //TODO 啥意思呢
                // builder.AllowGlobalStateModification(true);
        
                builder.SetRenderFunc((DrawOpaqueObjectsPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.OpaqueRendererListHandle);
                });
            }
        }

        #endregion


        partial void AddEditorRenderTargetPass(RenderGraph renderGraph);
        partial void AddDrawEditorGizmoPass(RenderGraph renderGraph, CameraData cameraData, GizmoSubset gizmoSubset);

        public void Dispose()
        {
        }


     
    }
}