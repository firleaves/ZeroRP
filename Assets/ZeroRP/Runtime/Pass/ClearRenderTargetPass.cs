using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ZeroRP
{
    public class ClearRenderTargetPass
    {
        class ClearRenderTargetPassData
        {
            internal RTClearFlags ClearFlags { get; set; }
            internal Color ClearColor { get; set; }
        }

        public void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraColor, TextureHandle cameraDepth)
        {
            var cameraData = frameData.Get<CameraData>();
            var clearFlags = cameraData.GetClearFlags();

            if (clearFlags == RTClearFlags.None)
            {
                return;
            }

            using (var builder = renderGraph.AddRasterRenderPass<ClearRenderTargetPassData>("Clear Render Target Pass", out var passData))
            {
                passData.ClearColor = cameraData.GetClearColor();
                passData.ClearFlags = cameraData.GetClearFlags();

                builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(cameraDepth, AccessFlags.Write);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((ClearRenderTargetPassData data, RasterGraphContext context) =>
                { 
                    context.cmd.SetupCameraProperties(cameraData.Camera);
                    context.cmd.ClearRenderTarget(data.ClearFlags, data.ClearColor, 1, 0);
                });
            }
        }
    }
}