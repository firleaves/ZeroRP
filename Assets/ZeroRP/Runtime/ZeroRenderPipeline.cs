using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ZeroRP
{
    public class ZeroRenderPipeline : RenderPipeline
    {

        public const string ShaderTagName = "ZeroRP";

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

            // _renderGraphRecorder?.Dispose();
            _renderGraphRecorder = null;

            _renderGraph.Cleanup();
            _renderGraph = null;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
        }


        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
             //开始渲染上下文
            BeginContextRendering(context, cameras);

            for (int i = 0; i < cameras.Count; i++)
            {
                Camera camera = cameras[i];
                RenderCamera(context, camera);
            }

            //渲染结束，需要调用该API
            _renderGraph.EndFrame();
            //结束渲染上下文
            EndContextRendering(context, cameras);
        }

        private void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            BeginCameraRendering(context, camera);
            if (!PrepareFrameData(context, camera))
                return;
            CommandBuffer cmd = CommandBufferPool.Get($"Render Camera : {camera.name}");

            context.SetupCameraProperties(camera);
            //设置每个相机的Shader环境光参数
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
            
            // 初始化cameraTargetDescriptor，参考URP的实现
            cameraData.CameraTargetDescriptor = CreateRenderTextureDescriptor(camera, 1);

            _contextContainer.GetOrCreate<DeferredData>();
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
            //录制时间线
            _renderGraphRecorder.RecordRenderGraph(_renderGraph, _contextContainer);
            _renderGraph.EndRecordingAndExecute();
        }

        public static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph, RenderTextureDescriptor desc, string name, bool clear,
 FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            TextureDesc rgDesc = new TextureDesc(desc.width, desc.height);
            rgDesc.dimension = desc.dimension;
            rgDesc.clearBuffer = clear;
            rgDesc.bindTextureMS = desc.bindMS;
            rgDesc.format = (desc.depthStencilFormat != GraphicsFormat.None) ? desc.depthStencilFormat : desc.graphicsFormat;
            rgDesc.slices = desc.volumeDepth;
            rgDesc.msaaSamples = (MSAASamples)desc.msaaSamples;
            rgDesc.name = name;
            rgDesc.enableRandomWrite = desc.enableRandomWrite;
            rgDesc.filterMode = filterMode;
            rgDesc.wrapMode = wrapMode;
            rgDesc.isShadowMap = desc.shadowSamplingMode != ShadowSamplingMode.None && desc.depthStencilFormat != GraphicsFormat.None;
            rgDesc.vrUsage = desc.vrUsage;
            rgDesc.useDynamicScale = desc.useDynamicScale;
            rgDesc.useDynamicScaleExplicit = desc.useDynamicScaleExplicit;

            return renderGraph.CreateTexture(rgDesc);
        }

        internal static RenderTextureDescriptor CreateRenderTextureDescriptor(Camera camera, int msaaSamples)
        {
            RenderTextureDescriptor desc;

            if (camera.targetTexture == null)
            {
                // 使用相机的实际像素尺寸，而不是Screen.width/height
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                desc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

                desc.sRGB = true;
            }
            else
            {
                desc = camera.targetTexture.descriptor;

                // 确保使用相机的实际像素尺寸
                desc.width = camera.pixelWidth;
                desc.height = camera.pixelHeight;
            }
            desc.enableRandomWrite = false;
            desc.bindMS = false;
            desc.useDynamicScale = camera.allowDynamicResolution;
            desc.msaaSamples = msaaSamples;

            return desc;
        }
    }
}
