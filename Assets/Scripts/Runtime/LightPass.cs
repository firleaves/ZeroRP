using UnityEngine;
using UnityEngine.Rendering;

namespace DeferredRP
{
    public class LightPass
    {
        private const string _passName = "LightPass";
        private Material _lightingMaterial;

        private Material LightMaterial
        {
            get
            {
                if (_lightingMaterial == null)
                {
                    _lightingMaterial = new Material(Shader.Find("DeferredRP/PBRLit"));
                }

                return _lightingMaterial;
            }
        }


        public LightPass(DeferredRenderPipelineAsset asset)
        {
        }


        public void RenderCamera(ScriptableRenderContext context, Camera camera, CullingResults cullingResults)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_passName);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            // cmd.Blit(_GBufferPass.GBufferRTs[0], BuiltinRenderTextureType.CameraTarget, LightMaterial);
            cmd.DrawProcedural(Matrix4x4.identity, LightMaterial, 1, MeshTopology.Triangles, 3);
        //     var mesh = new Mesh();
        //       Vector3[] vertices = new Vector3[4]{
        //     new Vector3(1, 1, 0),
        //     new Vector3(-1, 1, 0),
        //     new Vector3(1, -1, 0),
        //     new Vector3(-1, -1, 0)
        // };
        //
        // mesh.vertices = vertices;
        //
        // // 通过顶点为网格创建三角形
        // int[] triangles = new int[2 * 3]{
        //     0, 3, 1, 　　0, 2, 3
        // };
        //  mesh.triangles = triangles;
        //     cmd.DrawMesh(mesh,Matrix4x4.identity,new Material(Shader.Find("Hidden/InternalErrorShader")));
            context.ExecuteCommandBuffer(cmd);
            context.Submit();
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            
            
            
                        // cmd.Clear();
            // cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            // // cmd.Blit(_GBufferPass.GBufferRTs[0], BuiltinRenderTextureType.CameraTarget, LightMaterial);
            // cmd.DrawProcedural(Matrix4x4.identity, LightMaterial, 1, MeshTopology.Triangles, 3);
            // context.ExecuteCommandBuffer(cmd);
            // context.Submit();
            // cmd.Clear();

            // cmd.Clear();
            // cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            // Material mat = new Material(Shader.Find("DeferredRP/LightPass"));
            // cmd.Blit(_GBufferPass.GBufferIDs[0], BuiltinRenderTextureType.CameraTarget, mat);
            // context.ExecuteCommandBuffer(cmd);
            // context.Submit();
        }
    }
}