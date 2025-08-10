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
            internal TextureHandle DepthTextureHandle;
            internal RendererListHandle RendererListHandle;
            internal RendererListHandle ObjectsWithErrorRendererListHandle;
            internal RendererList RendererList;
            internal RendererList ObjectsWithErrorRendererList;
        }
        
        // 参考URP的GBuffer索引定义
        private const int GBufferAlbedoIndex = 0;           // Albedo + Material Flags
        private const int GBufferSpecularMetallicIndex = 1; // Specular + Metallic
        private const int GBufferNormalSmoothnessIndex = 2; // Normal + Smoothness
        private const int GBufferLightingIndex = 3;         // Lighting Buffer (不创建纹理)
        
        private Mesh _fullMesh;
        private Material _material;

        public DeferredPass()
        {
            
        }
       
        private Mesh CreateFullscreenMesh()
        {
            var mesh = new Mesh();
            mesh.name = "Fullscreen Quad";
    
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-1, -1, 0),  // 左下
                new Vector3(-1,  1, 0),  // 左上  
                new Vector3( 1, -1, 0),  // 右下
                new Vector3( 1,  1, 0)   // 右上
            };
    
            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0),  // 左下
                new Vector2(0, 1),  // 左上
                new Vector2(1, 0),  // 右下
                new Vector2(1, 1)   // 右上
            };
    
            int[] triangles = new int[]
            {
                0, 1, 2,  
                2, 1, 3   
            };
    
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
    
            return mesh;
        }
        
        public void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraColor, TextureHandle cameraDepth)
        {
            if(_fullMesh == null)
            {
                _fullMesh = CreateFullscreenMesh();
            }
            if(_material == null)
            {
                _material = new Material(Shader.Find("ZeroRP/DeferredLight"));
            }

            var cameraData = frameData.Get<CameraData>();
            var deferredData = frameData.Get<DeferredData>();
            
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData, _profilingSampler))
            {
                // 设置GBuffer纹理作为输入 - 前3个是实际纹理，第4个是camera color引用
                for (int i = 0; i < deferredData.GBuffer.Length; ++i)
                {
                    // 跳过第4个GBuffer (索引3)，因为它是camera color引用，不需要作为输入
                    if (i != GBufferLightingIndex && deferredData.GBuffer[i].IsValid())
                    {
                        builder.UseTexture(deferredData.GBuffer[i], AccessFlags.Read);
                    }
                    else if (i == GBufferLightingIndex)
                    {
                    }
                    else
                    {
                    }
                }
                
                // 设置深度纹理
                if (cameraDepth.IsValid()) 
                {
                    builder.SetRenderAttachmentDepth(cameraDepth, AccessFlags.Write);
                }
                
                // 设置输出颜色目标
                builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    
                    
                    // 绘制全屏四边形进行延迟光照计算
                    context.cmd.DrawMesh(_fullMesh, Matrix4x4.identity, _material, 0, 0);
                    
                    
                    
                });
            }
        }
    }
}