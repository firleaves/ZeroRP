using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ZeroRP
{
    public class EditorRenderTargetPass
    {
        internal class EditorRenderTargetPassData
        {
        }
        public void Render(RenderGraph renderGraph)
        {
            using (var builder = renderGraph.AddUnsafePass<EditorRenderTargetPassData>("Editor RenderTarget Pass", out var passData))
            {
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((EditorRenderTargetPassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, // color
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare); // depth
                });
            }
        }
    }
}