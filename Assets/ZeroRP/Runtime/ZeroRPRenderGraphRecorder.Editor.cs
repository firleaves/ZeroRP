
// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.RenderGraphModule;

// namespace ZeroRP
// {
//     public partial class ZeroRPRenderGraphRecorder
//     {



//         internal class EditorRenderTargetPassData
//         {
//         }
//         partial void AddEditorRenderTargetPass(RenderGraph renderGraph)
//         {
//             using (var builder = renderGraph.AddUnsafePass<EditorRenderTargetPassData>("Editor RenderTarget Pass", out var passData))
//             {
//                 builder.AllowPassCulling(false);

//                 builder.SetRenderFunc((EditorRenderTargetPassData data, UnsafeGraphContext context) =>
//                 {
//                     context.cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
//                         RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, // color
//                         RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare); // depth
//                 });
//             }
//         }


//         internal class DrawEditorGizmoPassData
//         {
//             internal RendererListHandle GizmoRendererListHandle;
//         }

//         partial void AddDrawEditorGizmoPass(RenderGraph renderGraph, CameraData cameraData, GizmoSubset gizmoSubset)
//         {

//             if (!Handles.ShouldRenderGizmos() || cameraData.Camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered)
//                 return;

//             bool renderPreGizmos = (gizmoSubset == GizmoSubset.PreImageEffects);
//             var passName = renderPreGizmos ? "Draw Pre Gizmos Pass" : "Draw Post Gizmos Pass";
//             using (var builder = renderGraph.AddRasterRenderPass<DrawEditorGizmoPassData>(passName, out var passData))
//             {
//                 if (_colorHandle.IsValid()) builder.SetRenderAttachment(_colorHandle, 0, AccessFlags.Write);
//                 if (_depthHandle.IsValid()) builder.SetRenderAttachmentDepth(_depthHandle, AccessFlags.Write);

//                 passData.GizmoRendererListHandle = renderGraph.CreateGizmoRendererList(cameraData.Camera, gizmoSubset);
//                 builder.UseRendererList(passData.GizmoRendererListHandle);
//                 builder.AllowPassCulling(false);

//                 builder.SetRenderFunc((DrawEditorGizmoPassData data, RasterGraphContext context) =>
//                 {
//                     context.cmd.DrawRendererList(data.GizmoRendererListHandle);
//                 });
//             }

//         }






//     }
// }

// #endif