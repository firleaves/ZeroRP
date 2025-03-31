using UnityEngine;
using UnityEngine.Rendering;

namespace DeferredRP
{
    public class TileLightCulling
    {
        private ComputeShader _computeShader;
        private CommandBuffer _commandbuffer = new CommandBuffer();

        //当前灯光索引缓存
        private ComputeBuffer _tileLightsIndicesBuffer;

        //剔除后可见灯光索引
        private ComputeBuffer _tileLightsArgsBuffer;

        private DeferredRenderPipelineAsset _asset;

        private int _kernel;

        public TileLightCulling(DeferredRenderPipelineAsset asset)
        {
            _asset = asset;
            _commandbuffer.name = "TileLightCulling";

            if (asset.TileLightCullingSetting.TileLightComputeShader == null) return;
            
            _computeShader = asset.TileLightCullingSetting.TileLightComputeShader;
            
            _kernel = _computeShader.FindKernel("TileLight");
        }

        private Vector4 BuildZBufferParams(float near, float far)
        {
            var result = new Vector4();
            result.x = 1 - far / near;
            result.y = 1 - result.x;
            result.z = result.x / far;
            result.w = result.y / far;
            return result;
        }


        private void EnsureTileComputeBuffer(int tileCountX, int tileCountY)
        {
            var tileCount = tileCountX * tileCountY;
            var argsBufferSize = tileCount;
            var indicesBufferSize = tileCount * _asset.TileLightCullingSetting.MaxLightPerTile;
            if (_tileLightsIndicesBuffer != null && _tileLightsArgsBuffer.count < argsBufferSize)
            {
                _tileLightsIndicesBuffer.Dispose();
                _tileLightsIndicesBuffer = null;
            }

            if (_tileLightsIndicesBuffer != null && _tileLightsIndicesBuffer.count < indicesBufferSize)
            {
                _tileLightsIndicesBuffer.Dispose();
                _tileLightsIndicesBuffer = null;
            }

            if (_tileLightsIndicesBuffer == null)
            {
                _tileLightsIndicesBuffer = new ComputeBuffer(indicesBufferSize, sizeof(int));
                Shader.SetGlobalBuffer(ShaderConstants.TileLightsIndicesBuffer, _tileLightsIndicesBuffer);
                _computeShader.SetBuffer(_kernel, ShaderConstants.RWTileLightsIndicesBuffer, _tileLightsIndicesBuffer);
            }


            if (_tileLightsArgsBuffer == null)
            {
                _tileLightsArgsBuffer = new ComputeBuffer(argsBufferSize, sizeof(int));
                Shader.SetGlobalBuffer(ShaderConstants.TileLightsArgsBuffer, _tileLightsArgsBuffer);
                _computeShader.SetBuffer(_kernel, ShaderConstants.RWTileLightsArgsBuffer, _tileLightsArgsBuffer);
            }
        }

        public void Execute(ScriptableRenderContext context, Camera camera, CullingResults cullingResults)
        {
            if (!_computeShader)
            {
                return;
            }

            _computeShader.GetKernelThreadGroupSizes(_kernel, out var tileSizeX, out var tileSizeY, out _);

            var screenWidth = camera.pixelWidth;
            var screenHeight = camera.pixelHeight;
            var tileCountX = Mathf.CeilToInt(screenWidth * 1f / tileSizeX);
            var tileCountY = Mathf.CeilToInt(screenHeight * 1f / tileSizeY);

            _commandbuffer.Clear();
            _commandbuffer.SetGlobalVector(ShaderConstants.DeferredTileParams, new Vector4(tileSizeX, tileSizeY, tileCountX, tileCountY));

            EnsureTileComputeBuffer(tileCountX, tileCountY);

            var nearPlaneZ = camera.nearClipPlane;
            var nearPlaneHeight = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f) * 2 * camera.nearClipPlane;
            var nearPlaneWidth = camera.aspect * nearPlaneHeight;

            var zbufferParams = BuildZBufferParams(camera.nearClipPlane, camera.farClipPlane);
            _commandbuffer.SetComputeVectorParam(_computeShader, ShaderConstants.ZBufferParams, zbufferParams);

            //near平面 左下角起点
            var nearPlaneLeftBottom = new Vector4(-nearPlaneWidth / 2, -nearPlaneHeight / 2, nearPlaneZ, 0);
            _commandbuffer.SetComputeVectorParam(_computeShader, ShaderConstants.CameraNearPlaneLB, nearPlaneLeftBottom);

            // 每个tile偏移量
            var basis = new Vector2(tileSizeX * nearPlaneWidth / screenWidth, tileSizeY * nearPlaneHeight / screenHeight);
            _commandbuffer.SetComputeVectorParam(_computeShader, ShaderConstants.CameraNearBasis, basis);
            
            //执行
            _commandbuffer.DispatchCompute(_computeShader, _kernel, tileCountX, tileCountY, 1);
            
            
            context.ExecuteCommandBuffer(_commandbuffer);
            _commandbuffer.Clear();
        }

        public void Dispose()
        {
            if (_tileLightsIndicesBuffer != null)
            {
                _tileLightsIndicesBuffer.Dispose();
                _tileLightsIndicesBuffer = null;
            }

            if (_tileLightsArgsBuffer != null)
            {
                _tileLightsArgsBuffer.Dispose();
                _tileLightsArgsBuffer = null;
            }
        }
    }
}