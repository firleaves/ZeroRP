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

        public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<CameraData>();

//             using (var builder = renderGraph.AddUnsafePass<InitRenderGraphFramePassData>("Init RenderGraph Frame Pass", out var passData,
//                        new ProfilingSampler("Init RenderGraph Frame Pass")))
//             {
//                 builder.AllowPassCulling(false);
//                 builder.SetRenderFunc((InitRenderGraphFramePassData data, UnsafeGraphContext context) =>
//                 {
//                     UnsafeCommandBuffer cmd = context.cmd;
// #if UNITY_EDITOR
//                     float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
// #else
//                     float time = Time.time;
// #endif
//                     float deltaTime = Time.deltaTime;
//                     float smoothDeltaTime = Time.smoothDeltaTime;
//
//                     
//                 });
//             }

            CreateRenderGraphCameraRenderTargets(renderGraph, cameraData);
            // _clearRenderTargetPass.Render(renderGraph, frameData,_colorHandle,_depthHandle);


            _gBufferPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);

            _deferredPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);
            // _skyBoxPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);


            //Editor
            AddEditorRenderTargetPass(renderGraph);
            AddDrawEditorGizmoPass(renderGraph, cameraData, GizmoSubset.PreImageEffects);
        }


        private void CreateRenderGraphCameraRenderTargets(RenderGraph renderGraph, CameraData cameraData)
        {
            var colorDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32)
            {
                useMipMap = false,
                autoGenerateMips = false,
                msaaSamples = 1
            };

            if (_colorRTHandle == null)
            {
                RenderTexture rt = new RenderTexture(colorDescriptor);
                rt.Create();
                _colorRTHandle = RTHandles.Alloc(rt, name: "BackBuffer color");
            }

            var depthDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Depth, 24)
            {
                useMipMap = false,
                autoGenerateMips = false,
                msaaSamples = 1
            };

            if (_depthRTHandle == null)
            {
                RenderTexture rt = new RenderTexture(depthDescriptor);
                rt.Create();
                _depthRTHandle = RTHandles.Alloc(rt, name: "BackBuffer depth");
            }

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

            bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

            RenderTargetInfo importInfoColor = new RenderTargetInfo
            {
                width = Screen.width,
                height = Screen.height,
                volumeDepth = 1,
                msaaSamples = 1,
                format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, colorRT_sRGB),
                bindMS = false
            };

            RenderTargetInfo importInfoDepth = new RenderTargetInfo
            {
                width = Screen.width,
                height = Screen.height,
                volumeDepth = 1,
                msaaSamples = 1,
                format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil),
                bindMS = false
            };

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

                builder.SetRenderFunc((DrawOpaqueObjectsPassData data, RasterGraphContext context) => { context.cmd.DrawRendererList(data.OpaqueRendererListHandle); });
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