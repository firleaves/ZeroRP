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

        internal class InitRenderGraphFramePassData
        {
        }

        private static readonly ShaderTagId s_shaderTagId = new ShaderTagId("SRPDefaultUnlit"); //渲染标签ID

        private TextureHandle _colorHandle;
        private TextureHandle _depthHandle;

        private RTHandle _colorRTHandle;
        private RTHandle _depthRTHandle;

        private GBufferPass _gBufferPass = new GBufferPass();
        private DeferredPass _deferredPass = new DeferredPass();
        private ClearRenderTargetPass _clearRenderTargetPass = new ClearRenderTargetPass();
        private SkyBoxPass _skyBoxPass = new SkyBoxPass();
#if UNITY_EDITOR
        private EditorRenderTargetPass _editorRenderTargetPass = new EditorRenderTargetPass();
        private EditorGizmoPass _editorGizmoPass = new EditorGizmoPass();
#endif
        public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<CameraData>();


            CreateRenderGraphCameraRenderTargets(renderGraph, cameraData);
            // _clearRenderTargetPass.Render(renderGraph, frameData,_colorHandle,_depthHandle);


            _gBufferPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);

            _deferredPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);
            // _skyBoxPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);


#if UNITY_EDITOR
            //Editor
            _editorRenderTargetPass.Render(renderGraph);
            _editorGizmoPass.Render(renderGraph, cameraData, GizmoSubset.PreImageEffects, _colorHandle, _depthHandle);
            _editorGizmoPass.Render(renderGraph, cameraData, GizmoSubset.PostImageEffects, _colorHandle, _depthHandle);
#endif
        }


        private void CreateRenderGraphCameraRenderTargets(RenderGraph renderGraph, CameraData cameraData)
        {
            int cameraWidth = cameraData.CameraTargetDescriptor.width;
            int cameraHeight = cameraData.CameraTargetDescriptor.height;

            var colorDescriptor = new RenderTextureDescriptor(cameraWidth, cameraHeight, RenderTextureFormat.ARGB32)
            {
                useMipMap = false,
                autoGenerateMips = false,
                msaaSamples = 1,
                depthBufferBits = 0  // 颜色RT不需要深度
            };



            var depthDescriptor = new RenderTextureDescriptor(cameraWidth, cameraHeight)
            {
                useMipMap = false,
                autoGenerateMips = false,
                msaaSamples = 1,
                graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil),
                depthBufferBits = 24
            };

            if (_colorRTHandle == null || !RTHandleMatches(_colorRTHandle, colorDescriptor))
            {
                RTHandles.Release(_colorRTHandle);
                _colorRTHandle = RTHandles.Alloc(colorDescriptor, name: "BackBuffer color");
            }

            if (_depthRTHandle == null || !RTHandleMatches(_depthRTHandle, depthDescriptor))
            {
                RTHandles.Release(_depthRTHandle);
                _depthRTHandle = RTHandles.Alloc(depthDescriptor, name: "BackBuffer depth");
            }

            // 6. 设置清除参数
            Color clearColor = cameraData.GetClearColor();
            RTClearFlags clearFlags = cameraData.GetClearFlags();

            bool clearOnFirstUse = !renderGraph.nativeRenderPassesEnabled;
            bool discardColorBackbufferOnLastUse = !renderGraph.nativeRenderPassesEnabled;
            bool discardDepthBackbufferOnLastUse = false;

            ImportResourceParams importBackbufferColorParams = new ImportResourceParams
            {
                clearOnFirstUse = clearOnFirstUse,
                clearColor = clearColor,
                discardOnLastUse = discardColorBackbufferOnLastUse
            };

            ImportResourceParams importBackbufferDepthParams = new ImportResourceParams
            {
                clearOnFirstUse = clearOnFirstUse,
                clearColor = clearColor,
                discardOnLastUse = discardDepthBackbufferOnLastUse
            };

#if UNITY_EDITOR
            if (cameraData.Camera.cameraType == CameraType.SceneView)
                importBackbufferDepthParams.discardOnLastUse = false;
#endif

            bool colorRT_sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;

            RenderTargetInfo importInfoColor = new RenderTargetInfo
            {
                width = cameraData.CameraTargetDescriptor.width,
                height = cameraData.CameraTargetDescriptor.height,
                volumeDepth = 1,
                msaaSamples = 1,
                format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, colorRT_sRGB),
                bindMS = false
            };

            RenderTargetInfo importInfoDepth = new RenderTargetInfo
            {
                width = cameraData.CameraTargetDescriptor.width,
                height = cameraData.CameraTargetDescriptor.height,
                volumeDepth = 1,
                msaaSamples = 1,
                format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil),
                bindMS = false
            };

            _colorHandle = renderGraph.ImportTexture(_colorRTHandle, importInfoColor, importBackbufferColorParams);
            _depthHandle = renderGraph.ImportTexture(_depthRTHandle, importInfoDepth, importBackbufferDepthParams);
        }

        // 辅助方法：检查RTHandle是否匹配描述符
        private bool RTHandleMatches(RTHandle handle, RenderTextureDescriptor descriptor)
        {
            if (handle == null || handle.rt == null)
                return false;

            var rt = handle.rt;
            return rt.width == descriptor.width &&
                   rt.height == descriptor.height &&
                   rt.format == descriptor.colorFormat &&
                   rt.antiAliasing == descriptor.msaaSamples;
        }

  


        public void Dispose()
        {
            RTHandles.Release(_colorRTHandle);
            _colorRTHandle = null;
            RTHandles.Release(_depthRTHandle);
            _depthRTHandle = null;
        }
    }
}




    //   #region Draw Opaque Objects

    //     internal class DrawOpaqueObjectsPassData
    //     {
    //         internal TextureHandle backbufferHandle;
    //         internal RendererListHandle OpaqueRendererListHandle { get; set; }
    //     }

    //     private void AddDrawOpaqueObjectsPass(RenderGraph renderGraph, CameraData cameraData)
    //     {
    //         using (var builder = renderGraph.AddRasterRenderPass<DrawOpaqueObjectsPassData>("Draw Opaque Objects Pass", out var passData))
    //         {
    //             //创建不透明对象渲染列表
    //             var opaqueRendererDesc = new RendererListDesc(s_shaderTagId, cameraData.CullingResults, cameraData.Camera);
    //             opaqueRendererDesc.sortingCriteria = SortingCriteria.CommonOpaque;
    //             opaqueRendererDesc.renderQueueRange = RenderQueueRange.opaque;
    //             passData.OpaqueRendererListHandle = renderGraph.CreateRendererList(opaqueRendererDesc);
    //             //RenderGraph引用不透明渲染列表
    //             builder.UseRendererList(passData.OpaqueRendererListHandle);

    //             // passData.backbufferHandle = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CurrentActive);
    //             // builder.SetRenderAttachment(passData.backbufferHandle, 0, AccessFlags.Write);
    //             if (_colorHandle.IsValid()) builder.SetRenderAttachment(_colorHandle, 0, AccessFlags.Write);
    //             if (_depthHandle.IsValid()) builder.SetRenderAttachmentDepth(_depthHandle, AccessFlags.Write);

    //             builder.AllowPassCulling(false);
    //             // builder.AllowPassCulling(false);
    //             // //TODO 啥意思呢
    //             // builder.AllowGlobalStateModification(true);

    //             builder.SetRenderFunc((DrawOpaqueObjectsPassData data, RasterGraphContext context) => { context.cmd.DrawRendererList(data.OpaqueRendererListHandle); });
    //         }
    //     }

    //     #endregion