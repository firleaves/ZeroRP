using UnityEngine;
using UnityEngine.Rendering;

namespace DeferredRP
{
    public class DeferredRenderPipeline : RenderPipeline
    {
        private CameraRenderer _cameraRenderer;

        public DeferredRenderPipelineAsset Asset;

        public DeferredRenderPipeline(DeferredRenderPipelineAsset asset)
        {
            Asset = asset;
            _cameraRenderer = new CameraRenderer(asset);
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            BeginFrameRendering(context, cameras);
            foreach (var camera in cameras)
            {
                BeginCameraRendering(context, camera);
                _cameraRenderer.Render(context, camera);
                EndCameraRendering(context, camera);
                // RenderCamera(context, camera);
            }

            EndFrameRendering(context, cameras);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _cameraRenderer.Dispose();
        }

        private void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            
            if (!camera.TryGetCullingParameters(out var cullingParameters)) return;
            var _cullingResults = context.Cull(ref cullingParameters);
        
            //为相机创建CommandBuffer
            var cmd = CommandBufferPool.Get(camera.name);
            //设置相机参数
            context.SetupCameraProperties(camera);
        
        
            //清理
            var clearFlags = camera.clearFlags;
            var clearSkybox = clearFlags == CameraClearFlags.Skybox;
            var clearDepth = clearFlags != CameraClearFlags.Nothing;
            var clearColor = clearFlags == CameraClearFlags.Color;
        
        
            //清理渲染目标
            cmd.ClearRenderTarget(clearDepth, clearColor, CoreUtils.ConvertSRGBToActiveColorSpace(camera.backgroundColor));
        
            //绘制天空盒
            if (clearSkybox)
            {
                var skyboxRendererList = context.CreateSkyboxRendererList(camera);
                cmd.DrawRendererList(skyboxRendererList);
            }
        
        
            // _GBufferPass.RenderCamera(context, camera);
        
        
           // 渲染排序，渲染状态，
             // var sortingSettings = new SortingSettings(camera);
             // ShaderTagId _shaderTagId = new ShaderTagId("LightPass");
             // var drawSettings = new DrawingSettings(_shaderTagId, sortingSettings);
             //
             // //不透明物体
             // sortingSettings.criteria = SortingCriteria.CommonOpaque;
             // //渲染过滤
             // var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
             // //创建渲染列表
             // var rendererListParams = new RendererListParams(_cullingResults, drawSettings, filterSettings);
             // var rendererList = context.CreateRendererList(ref rendererListParams);
             // //绘制渲染列表
             // cmd.DrawRendererList(rendererList);
             //
             //
             // //绘制半透明物体
             // sortingSettings.criteria = SortingCriteria.CommonTransparent;
             // //指定渲染过滤设置FilterSettings
             // filterSettings = new FilteringSettings(RenderQueueRange.transparent);
             // //创建渲染列表
             // rendererListParams = new RendererListParams(_cullingResults, drawSettings, filterSettings);
             // rendererList = context.CreateRendererList(ref rendererListParams);
             // //绘制渲染列表
             // cmd.DrawRendererList(rendererList);
        
        
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            context.Submit();
            //结束渲染相机
        
            
        }
    }
}