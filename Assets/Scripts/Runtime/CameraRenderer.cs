using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeferredRP
{
    public partial class CameraRenderer
    {
        private ScriptableRenderContext _context;
        private Camera _camera;
        private CullingResults _cullingResults;
        private Light _light;
        private GBufferPass _GBufferPass;
        private TileLightCulling _tileLightCulling;
        private LightPass _lightPass;

        public CameraRenderer(DeferredRenderPipelineAsset asset)
        {
            _light = new Light(asset);
            _GBufferPass = new GBufferPass();
            _tileLightCulling = new TileLightCulling(asset);
            _lightPass = new LightPass(asset);
        }


        //摄像机渲染器的渲染函数，在当前渲染上下文的基础上渲染当前摄像机
        public void Render(ScriptableRenderContext context, Camera camera)
        {
            _context = context;
            _camera = camera;

            //剔除 -> 设置相机 -> 清理target -> 渲染 -> 清理

            //获得剔除参数，进行剔除操作
            if (!camera.TryGetCullingParameters(out var cullingParameters)) return;
            _cullingResults = context.Cull(ref cullingParameters);

            _light.Setup(context, _cullingResults);

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
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();


            //生成gbuffer
            _GBufferPass.RenderCamera(context, camera, _cullingResults);

            //裁剪灯光
            _tileLightCulling.Execute(context, camera, _cullingResults);

            //deferred light shading
            _lightPass.RenderCamera(context, camera, _cullingResults);

            //遇到一个问题，绘制天空盒吧上面绘制内容覆盖 或者上面绘制内容把天空盒覆盖了
            //推测： 深度信息没有写入？还是说被覆盖了？
            //找到原因了：在gbuffer里面写了深度图，但是深度缓存是空的，需要在light里面 读取深度图里面的值，重新写入深度缓冲区
            if (clearSkybox)
            {
                context.DrawSkybox(camera);
                context.Submit();
            }

            //绘制半透明

#if UNITY_EDITOR
            DrawUnsupportedShaders();
            if (camera.cameraType == CameraType.SceneView && Handles.ShouldRenderGizmos())
            {
                _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            }
#endif
            CommandBufferPool.Release(cmd);
            // 
        }

        public void Dispose()
        {
            _tileLightCulling.Dispose();
        }
    }
}

//          cmd.ClearRenderTarget(true, true, Color.clear);


//           
// //渲染排序，渲染状态，
// var sortingSettings = new SortingSettings(camera);
// var drawSettings = new DrawingSettings(_shaderTagId, sortingSettings);
// //不透明物体
// sortingSettings.criteria = SortingCriteria.CommonOpaque;
// //渲染过滤
// var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
// context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);

// //绘制半透明物体
// sortingSettings.criteria = SortingCriteria.CommonTransparent;
// filterSettings = new FilteringSettings(RenderQueueRange.transparent);
// context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);