#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ZeroRP
{
    public class EditorGizmoPass
    {

        internal class DrawEditorGizmoPassData
        {
            internal RendererListHandle GizmoRendererListHandle;
        }

        public void Render(RenderGraph renderGraph, CameraData cameraData, GizmoSubset gizmoSubset, TextureHandle colorHandle, TextureHandle depthHandle)
        {

            if (!Handles.ShouldRenderGizmos() || cameraData.Camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered)
                return;

            var passName = (gizmoSubset == GizmoSubset.PreImageEffects) ? "Draw Pre Gizmos Pass" : "Draw Post Gizmos Pass";
            using (var builder = renderGraph.AddRasterRenderPass<DrawEditorGizmoPassData>(passName, out var passData))
            {
                if (colorHandle.IsValid()) builder.SetRenderAttachment(colorHandle, 0, AccessFlags.Write);
                if (depthHandle.IsValid()) builder.SetRenderAttachmentDepth(depthHandle, AccessFlags.Write);



                passData.GizmoRendererListHandle = renderGraph.CreateGizmoRendererList(cameraData.Camera, gizmoSubset);
                builder.UseRendererList(passData.GizmoRendererListHandle);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((DrawEditorGizmoPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.GizmoRendererListHandle);
                });
            }

        }
    }
}
#endif