using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Unity.Collections;

namespace DeferredRP
{
    public class Light
    {
        private DeferredRenderPipelineAsset _asset;

        private ComputeBuffer _pointLightPosRadiusBuffer;
        private NativeArray<float4> _pointLightPosRadiusArray;

        private ComputeBuffer _pointLightColorBuffer;
        private NativeArray<float4> _pointLightColorArray;

        public Light(DeferredRenderPipelineAsset asset)
        {
            _asset = asset;
            _pointLightPosRadiusBuffer = new ComputeBuffer(_asset.MaxLightCount, sizeof(float) * 4);
            _pointLightPosRadiusArray = new NativeArray<float4>(_asset.MaxLightCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);


            _pointLightColorBuffer = new ComputeBuffer(_asset.MaxLightCount, sizeof(float) * 4);
            _pointLightColorArray = new NativeArray<float4>(_asset.MaxLightCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            Shader.SetGlobalBuffer(ShaderConstants.PointLightPosRadiusBuffer, _pointLightPosRadiusBuffer);
            Shader.SetGlobalBuffer(ShaderConstants.PointLightColorBuffer, _pointLightColorBuffer);
        }


        public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
        {
            var visibleLights = cullingResults.visibleLights;

            var pointLights = new NativeList<VisibleLight>(visibleLights.Length, Allocator.Temp);

            //循环配置两个Vector数组
            var mainLightIndex = -1;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                var light = visibleLights[i];

                switch (light.lightType)
                {
                    case LightType.Directional:
                        mainLightIndex = i;
                        break;
                    case LightType.Point:
                        pointLights.Add(light);
                        break;
                    default:
                        Debug.LogWarning("不支持其他类型灯光");
                        break;
                }

                if (pointLights.Length > _asset.MaxLightCount)
                {
                    break;
                }
            }

            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                var pos = light.light.transform.position;
                var radius = light.light.range;
                _pointLightPosRadiusArray[i] = new float4(pos, radius);

                //已经计算过强度值的颜色
                var color = light.finalColor;
                _pointLightColorArray[i] = new float4(color.r, color.g, color.b, color.a);
            }

            if (mainLightIndex != -1)
            {
                var mainLight = visibleLights[mainLightIndex];
                Shader.SetGlobalVector(ShaderConstants.MainLightDirection, -mainLight.light.transform.forward);
                Shader.SetGlobalVector(ShaderConstants.MainLightColor, mainLight.finalColor);
            }
            else
            {
                Shader.SetGlobalVector(ShaderConstants.MainLightColor, Color.clear);
            }

            _pointLightPosRadiusBuffer.SetData(_pointLightPosRadiusArray, 0, 0, pointLights.Length);
            _pointLightColorBuffer.SetData(_pointLightColorArray, 0, 0, pointLights.Length);
            Shader.SetGlobalInt(ShaderConstants.PointLightCount, pointLights.Length);

            pointLights.Dispose();
        }


        public void Dispose()
        {
            //释放ShadowAtlas内存
            _pointLightPosRadiusBuffer.Dispose();
            _pointLightPosRadiusArray.Dispose();

            _pointLightColorArray.Dispose();
            _pointLightColorBuffer.Dispose();
        }
    }
}