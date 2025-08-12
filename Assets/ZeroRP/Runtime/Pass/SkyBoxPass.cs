using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ZeroRP
{
    public class SkyBoxPass
    {
        class SkyBoxPassData
        {
            public RendererListHandle SkyboxRenderListHandle;
        }

        public void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraColor, TextureHandle cameraDepth)
        {
            var cameraData = frameData.Get<CameraData>();
            var clearFlags = cameraData.Camera.clearFlags;
            if (clearFlags != CameraClearFlags.Skybox || RenderSettings.skybox == null)
            {
                return;
            }

            using (var builder = renderGraph.AddRasterRenderPass<SkyBoxPassData>("Draw SkyBox Pass", out var passData))
            {
                passData.SkyboxRenderListHandle = renderGraph.CreateSkyboxRendererList(cameraData.Camera);
                builder.UseRendererList(passData.SkyboxRenderListHandle);


                builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);

                
                // var backbufferHandle = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CurrentActive);
                // builder.SetRenderAttachment(backbufferHandle, 0, AccessFlags.Write);
                
                builder.SetRenderFunc((SkyBoxPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.SkyboxRenderListHandle);
                });
            }
        }
    }
}