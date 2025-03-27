using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRP
{
    
     [CreateAssetMenu(menuName = "Zero Render Pipeline/ZeroRPAsset")]
    public class ZeroRPAsset : RenderPipelineAsset<ZeroRenderPipeline>
    {
        protected override RenderPipeline CreatePipeline()
        {
            return new ZeroRenderPipeline(this);
        }
    }
}
