using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ZeroRP
{
    public class ZeroRenderPipeline : RenderPipeline
    {
        private RenderGraph _renderGraph;
        private ZeroRPRenderGraphRecorder _renderGraphRecorder;
        private ContextContainer _contextContainer;

        private ZeroRPAsset _asset = null;

        public ZeroRenderPipeline(ZeroRPAsset asset)
        {
            _asset = asset;
            InitializeRenderGraph();
        }

        protected override void Dispose(bool disposing)
        {
            
            CleanupRenderGraph();
            
            base.Dispose(disposing);
        }


        private void InitializeRenderGraph()
        {
            _renderGraph = new RenderGraph("ZeroRP Render Graph");
            _renderGraphRecorder = new ZeroRPRenderGraphRecorder();
            _contextContainer = new ContextContainer();
        }

        private void CleanupRenderGraph()
        {
            _renderGraphRecorder?.Dispose();
            _renderGraphRecorder = null;

            _renderGraphRecorder?.Dispose();
            _renderGraphRecorder = null;

            _renderGraph.Cleanup();
            _renderGraph = null;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
        }


        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            BeginContextRendering(context, cameras);

            for (int i = 0; i < cameras.Count; i++)
            {
                Camera camera = cameras[i];
                RenderCamera(context, camera);
            }

            _renderGraph.EndFrame();
            EndContextRendering(context, cameras);
        }

        private void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            BeginCameraRendering(context, camera);
            if (!PrepareFrameData(context, camera))
                return;
            CommandBuffer cmd = CommandBufferPool.Get($"Render Camera : {camera.name}");
            SetupPerCameraShaderConstants(cmd);
            RecordAndExecuteRenderGraph(context, camera, cmd);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            context.Submit();
            EndCameraRendering(context, camera);
        }

        private void SetupPerCameraShaderConstants(CommandBuffer cmd)
        {
        }

        private void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, Camera camera)
        {
        }

        private bool PrepareFrameData(ScriptableRenderContext context, Camera camera)
        {
            //获取相机剔除参数，并进行剔除
            if (!camera.TryGetCullingParameters(out var cullingParameters))
                return false;
            //构建剔除参数
            SetupCullingParameters(ref cullingParameters, camera);
            CullingResults cullingResults = context.Cull(ref cullingParameters);

            // 初始化摄像机帧数据
            CameraData cameraData = _contextContainer.GetOrCreate<CameraData>();
            cameraData.Camera = camera;
            cameraData.CullingResults = cullingResults;
            
            return true;
        }


        private void RecordAndExecuteRenderGraph(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            RenderGraphParameters renderGraphParameters = new RenderGraphParameters()
            {
                executionName = camera.name,
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount
            };
            _renderGraph.BeginRecording(renderGraphParameters);
            //开启录制时间线
            _renderGraphRecorder.RecordRenderGraph(_renderGraph, _contextContainer);
            _renderGraph.EndRecordingAndExecute();
        }
    }
}