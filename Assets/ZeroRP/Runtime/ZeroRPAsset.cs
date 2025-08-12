using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRP
{

    [CreateAssetMenu(menuName = "Zero Render Pipeline/ZeroRPAsset")]
    public class ZeroRPAsset : RenderPipelineAsset<ZeroRenderPipeline>
    {

        public override string renderPipelineShaderTag => ZeroRenderPipeline.ShaderTagName;

        protected override RenderPipeline CreatePipeline()
        {
            return new ZeroRenderPipeline(this);
        }

    }
}
