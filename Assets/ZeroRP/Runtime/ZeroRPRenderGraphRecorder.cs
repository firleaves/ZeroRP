// using System;
// #if UNITY_EDITOR
// using UnityEditor;
// #endif
// using UnityEngine;
// using UnityEngine.Experimental.Rendering;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.RendererUtils;
// using UnityEngine.Rendering.RenderGraphModule;

// namespace ZeroRP
// {
//     public partial class ZeroRPRenderGraphRecorder : IRenderGraphRecorder, IDisposable
//     {
//         private static readonly ProfilingSampler TempPassSampler = new ProfilingSampler("TempPass");

//         private static readonly ShaderTagId[] ShaderTagIds = new ShaderTagId[]
//         {
//             new ShaderTagId("SRPDefaultUnlit"),
//             new ShaderTagId("GBuffer")
//         }; //渲染标签IDs

//         private TextureHandle _colorHandle;
//         private TextureHandle _depthHandle;

//         private RTHandle _colorRTHandle;
//         private RTHandle _depthRTHandle;

//         private GBufferPass _gBufferPass = new GBufferPass();
//         private DeferredPass _deferredPass = new DeferredPass();

//         public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
//         {
//             var cameraData = frameData.Get<CameraData>();

//             CreateRenderGraphCameraRenderTargets(renderGraph, cameraData);

//             var clearFlags = cameraData.Camera.clearFlags;

//             // if (!renderGraph.nativeRenderPassesEnabled && clearFlags != CameraClearFlags.Nothing)
//             // {
//             //     AddClearRenderTargetPass(renderGraph, cameraData);
//             // }

//             // _gBufferPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);
//             // _deferredPass.Render(renderGraph, frameData, _colorHandle, _depthHandle);
//             AddDrawOpaqueObjectsPass(renderGraph, cameraData);

//             // AddDrawSkyBoxPass(renderGraph, cameraData);


//             //Editor
//             // AddEditorRenderTargetPass(renderGraph);
//             // AddDrawEditorGizmoPass(renderGraph, cameraData, GizmoSubset.PreImageEffects);
//             //  AddDrawEditorGizmoPass(renderGraph, cameraData, GizmoSubset.PostImageEffects);
//         }


//         private void CreateRenderGraphCameraRenderTargets(RenderGraph renderGraph, CameraData cameraData)
//         {
// //             var targetTexture = cameraData.Camera.targetTexture;
// //             var cameraTargetTexture = targetTexture;
// //             bool isBuildInTexture = (cameraTargetTexture == null);
// //             bool isCameraTargetOffscreenDepth = !isBuildInTexture && targetTexture.format == RenderTextureFormat.Depth;
// //             RenderTargetIdentifier targetColorId = isBuildInTexture ? BuiltinRenderTextureType.CameraTarget : new RenderTargetIdentifier(cameraTargetTexture);
// //             if (_colorRTHandle == null)
// //             {
// //                 _colorRTHandle = RTHandles.Alloc(targetColorId, "Color RT");
// //             }
// //             else if (_colorRTHandle.nameID != targetColorId)
// //             {
// //                 RTHandleStaticHelpers.SetRTHandleUserManagedWrapper(ref _colorRTHandle, targetColorId);
// //             }

// //             RenderTargetIdentifier targetDepthId = isBuildInTexture ? BuiltinRenderTextureType.Depth : new RenderTargetIdentifier(cameraTargetTexture);
// //             if (_depthRTHandle == null)
// //             {
// //                 _depthRTHandle = RTHandles.Alloc(targetDepthId, "Depth RT");
// //             }
// //             else if (_depthRTHandle.nameID != targetDepthId)
// //             {
// //                 RTHandleStaticHelpers.SetRTHandleUserManagedWrapper(ref _depthRTHandle, targetDepthId);
// //             }

// //             Color clearColor = cameraData.GetClearColor();
// //             RTClearFlags clearFlags = cameraData.GetClearFlags();

// //             bool clearOnFirstUse = !renderGraph.nativeRenderPassesEnabled;
// //             bool discardColorBackbufferOnLastUse = !renderGraph.nativeRenderPassesEnabled;
// //             bool discardDepthBackbufferOnLastUse = !isCameraTargetOffscreenDepth;


// //             ImportResourceParams importBackbufferColorParams = new ImportResourceParams();
// //             importBackbufferColorParams.clearOnFirstUse = clearOnFirstUse;
// //             importBackbufferColorParams.clearColor = clearColor;
// //             importBackbufferColorParams.discardOnLastUse = discardColorBackbufferOnLastUse;

// //             ImportResourceParams importBackbufferDepthParams = new ImportResourceParams();
// //             importBackbufferDepthParams.clearOnFirstUse = clearOnFirstUse;
// //             importBackbufferDepthParams.clearColor = clearColor;
// //             importBackbufferDepthParams.discardOnLastUse = discardDepthBackbufferOnLastUse;


// // #if UNITY_EDITOR
// //             // on TBDR GPUs like Apple M1/M2, we need to preserve the backbuffer depth for overlay cameras in Editor for Gizmos
// //             if (cameraData.Camera.cameraType == CameraType.SceneView)
// //                 importBackbufferDepthParams.discardOnLastUse = false;
// // #endif

// //             bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
// //             RenderTargetInfo importInfoColor = new RenderTargetInfo();
// //             RenderTargetInfo importInfoDepth = new RenderTargetInfo();

// //             // 使用相机的实际像素尺寸，确保所有缓冲区尺寸一致
// //             int width = cameraData.Camera.pixelWidth;
// //             int height = cameraData.Camera.pixelHeight;

// //             if (isBuildInTexture)
// //             {
// //                 importInfoColor.width = Screen.width;
// //                 importInfoColor.height = Screen.height;
// //                 importInfoColor.volumeDepth = 1;
// //                 importInfoColor.msaaSamples = 1;
// //                 importInfoColor.format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, colorRT_sRGB);
// //                 importInfoColor.bindMS = false;

// //                 importInfoDepth = importInfoColor;
// //                 importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
// //             }
// //             else
// //             {
// //                 importInfoColor.width = cameraTargetTexture.width;
// //                 importInfoColor.height = cameraTargetTexture.height;
// //                 importInfoColor.volumeDepth = cameraTargetTexture.volumeDepth;
// //                 importInfoColor.msaaSamples = cameraTargetTexture.antiAliasing;
// //                 importInfoColor.format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, colorRT_sRGB);
// //                 importInfoColor.bindMS = false;

// //                 importInfoDepth = importInfoColor;
// //                 importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
// //             }

// //             _colorHandle = renderGraph.ImportTexture(_colorRTHandle, importInfoColor, importBackbufferColorParams);
// //             _depthHandle = renderGraph.ImportTexture(_depthRTHandle, importInfoDepth, importBackbufferDepthParams);
//         }

//         #region Clar RenderTarget

//         internal class ClearRenderTargetPassData
//         {
//             internal RTClearFlags ClearFlags { get; set; }
//             internal Color ClearColor { get; set; }
//         }

//         private void AddClearRenderTargetPass(RenderGraph renderGraph, CameraData cameraData)
//         {
//             using (var builder = renderGraph.AddRasterRenderPass<ClearRenderTargetPassData>("Clear Render Target Pass", out var passData))
//             {
//                 passData.ClearColor = cameraData.GetClearColor();
//                 passData.ClearFlags = cameraData.GetClearFlags();

//                 if (_colorHandle.IsValid()) builder.SetRenderAttachment(_colorHandle, 0, AccessFlags.Write);
//                 if (_depthHandle.IsValid()) builder.SetRenderAttachmentDepth(_depthHandle, AccessFlags.Write);

//                 builder.AllowPassCulling(false);

//                 builder.SetRenderFunc((ClearRenderTargetPassData data, RasterGraphContext context) =>
//                 {
//                     context.cmd.ClearRenderTarget(data.ClearFlags, data.ClearColor, 1, 0);
//                 });
//             }
//         }

//         #endregion


//         #region Draw Opaque Objects

//         internal class DrawOpaqueObjectsPassData
//         {
//             internal RendererListHandle OpaqueRendererListHandle { get; set; }
//         }

//           private static readonly ShaderTagId s_shaderTagId = new ShaderTagId("SRPDefaultUnlit");

//         private void AddDrawOpaqueObjectsPass(RenderGraph renderGraph, CameraData cameraData)
//         {
//             using (var builder = renderGraph.AddRasterRenderPass<DrawOpaqueObjectsPassData>("Draw Opaque Objects Pass", out var passData))
//             {
//                 //创建不透明对象渲染列表
//                 var opaqueRendererDesc = new RendererListDesc(s_shaderTagId, cameraData.CullingResults, cameraData.Camera);
//                 opaqueRendererDesc.sortingCriteria = SortingCriteria.CommonOpaque;
//                 opaqueRendererDesc.renderQueueRange = RenderQueueRange.opaque;
//                 passData.OpaqueRendererListHandle = renderGraph.CreateRendererList(opaqueRendererDesc);
//                 //RenderGraph引用不透明渲染列表
//                 builder.UseRendererList(passData.OpaqueRendererListHandle);

//                 // if (_colorHandle.IsValid()) builder.SetRenderAttachment(_colorHandle, 0);
//                 // if (_depthHandle.IsValid()) builder.SetRenderAttachmentDepth(_depthHandle);

//                 var backbufferHandle = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CurrentActive);
//                 builder.SetRenderAttachment(backbufferHandle, 0, AccessFlags.Write);
//                 builder.AllowPassCulling(false);
//                 //TODO 啥意思呢
//                 // builder.AllowGlobalStateModification(true);

//                 builder.SetRenderFunc((DrawOpaqueObjectsPassData data, RasterGraphContext context) =>
//                 {
//                     context.cmd.DrawRendererList(data.OpaqueRendererListHandle);
//                 });
//             }
//         }

//         #endregion

//         #region Draw Skybox
//         internal class SkyBoxPassData
//         {
//             internal RendererListHandle skyboxRenderListHandle;
//         }
//         private void AddDrawSkyBoxPass(RenderGraph renderGraph, CameraData cameraData)
//         {
//             using (var builder =
//                    renderGraph.AddRasterRenderPass<SkyBoxPassData>("Draw SkyBox Pass", out var passData))
//             {
//                 passData.skyboxRenderListHandle = renderGraph.CreateSkyboxRendererList(cameraData.Camera);
//                 builder.UseRendererList(passData.skyboxRenderListHandle);

//                 if (_colorHandle.IsValid())
//                     builder.SetRenderAttachment(_colorHandle, 0, AccessFlags.Write);
//                 if (_depthHandle.IsValid())
//                     builder.SetRenderAttachmentDepth(_depthHandle, AccessFlags.Write);

//                 builder.AllowPassCulling(false);

//                 builder.SetRenderFunc((SkyBoxPassData data, RasterGraphContext context) =>
//                 {
//                     context.cmd.DrawRendererList(data.skyboxRenderListHandle);
//                 });
//             }
//         }

//         #endregion

//         #region Draw Transparent Objects

//         #endregion


//         partial void AddEditorRenderTargetPass(RenderGraph renderGraph);
//         partial void AddDrawEditorGizmoPass(RenderGraph renderGraph, CameraData cameraData, GizmoSubset gizmoSubset);

//         public void Dispose()
//         {
//         }



//     }
// }


using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace ZeroRP
{

    public class CameraData : ContextItem
    {
        public Camera camera;
        public CullingResults cullingResults;
        public override void Reset()
        {
            camera = null;
            cullingResults = default;
        }
    }
    public partial class ZeroRPRenderGraphRecorder : IRenderGraphRecorder
    {
        public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            //下节课实现
            AddDrawObjectsPass(renderGraph, frameData);
        }

        private static readonly ProfilingSampler s_DrawObjectsProfilingSampler = new ProfilingSampler("Draw Objects");
        private static readonly ShaderTagId s_shaderTagId = new ShaderTagId("SRPDefaultUnlit"); //渲染标签ID
        internal class DrawObjectsPassData
        {
            internal TextureHandle backbufferHandle;
            internal RendererListHandle opaqueRendererListHandle;
            internal RendererListHandle transparentRendererListHandle;
        }
        private void AddDrawObjectsPass(RenderGraph renderGraph, ContextContainer frameData)
        {
            CameraData cameraData = frameData.Get<CameraData>();
            using (var builder = renderGraph.AddRasterRenderPass<DrawObjectsPassData>("Draw Objects Pass", out var passData, s_DrawObjectsProfilingSampler))
            {
                //创建不透明对象渲染列表
                RendererListDesc opaqueRendererDesc = new RendererListDesc(s_shaderTagId, cameraData.cullingResults, cameraData.camera);
                opaqueRendererDesc.sortingCriteria = SortingCriteria.CommonOpaque;
                opaqueRendererDesc.renderQueueRange = RenderQueueRange.opaque;
                passData.opaqueRendererListHandle = renderGraph.CreateRendererList(opaqueRendererDesc);
                //RenderGraph引用不透明渲染列表
                builder.UseRendererList(passData.opaqueRendererListHandle);

                //创建半透明对象渲染列表
                RendererListDesc transparentRendererDesc = new RendererListDesc(s_shaderTagId, cameraData.cullingResults, cameraData.camera);
                transparentRendererDesc.sortingCriteria = SortingCriteria.CommonTransparent;
                transparentRendererDesc.renderQueueRange = RenderQueueRange.transparent;
                passData.transparentRendererListHandle = renderGraph.CreateRendererList(transparentRendererDesc);
                //RenderGraph引用不透明渲染列表
                builder.UseRendererList(passData.transparentRendererListHandle);

                //导入BackBuffer
                passData.backbufferHandle = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CurrentActive);
                builder.SetRenderAttachment(passData.backbufferHandle, 0, AccessFlags.Write);

                //设置渲染全局状态
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((DrawObjectsPassData passData, RasterGraphContext context) =>
                {
                    //调用渲染指令绘制
                    context.cmd.DrawRendererList(passData.opaqueRendererListHandle);
                    context.cmd.DrawRendererList(passData.transparentRendererListHandle);
                });
            }
        }
    }
}
