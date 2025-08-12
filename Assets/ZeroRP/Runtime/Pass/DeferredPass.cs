using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace ZeroRP
{
    public class DeferredPass
    {
        private const string PassName = "DeferredLight";

        private readonly ProfilingSampler _profilingSampler = new(PassName);

        private class PassData
        {
            internal TextureHandle[] GBufferTextureHandles;


            internal RendererListHandle RendererListHandle;

        }


        private Mesh _fullMesh;
        private Material _material;


        public DeferredPass()
        {
        }

        private Mesh CreateFullscreenMesh()
        {
            Vector3[] positions =
            {
                new Vector3(-1.0f, 1.0f, 0.0f),
                new Vector3(-1.0f, -3.0f, 0.0f),
                new Vector3(3.0f, 1.0f, 0.0f)
            };

            int[] indices = { 0, 1, 2 };

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;

            return mesh;
        }

        public void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraColor, TextureHandle cameraDepth)
        {
            if (_fullMesh == null)
            {
                _fullMesh = CreateFullscreenMesh();
            }

            if (_material == null)
            {
                _material = new Material(Shader.Find("ZeroRP/DeferredLight"));
            }


            var cameraData = frameData.Get<CameraData>();
            var deferredData = frameData.Get<DeferredData>();
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData, _profilingSampler))
            {
                builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(cameraDepth, AccessFlags.Write);

                // for (int i = 0; i < deferredData.GBuffer.Length; ++i)
                // {
                //     if (i != ZeroRPConstants.GBufferLightingIndex)
                //     {
                //         builder.UseTexture(deferredData.GBuffer[i], AccessFlags.Read);
                //     }
                // }

                passData.GBufferTextureHandles = deferredData.GBuffer;


                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // for (int i = 0; i < data.GBufferTextureHandles.Length; i++)
                    // {
                    //     if (i != ZeroRPConstants.GBufferLightingIndex)
                    //     {
                    //         _material.SetTexture(ZeroRPConstants.GBufferShaderPropertyIDs[i], data.GBufferTextureHandles[i]);
                    //     }
                    // }
                    context.cmd.DrawMesh(_fullMesh, Matrix4x4.identity, _material, 0, 0);
                });
            }
        }
    }
}