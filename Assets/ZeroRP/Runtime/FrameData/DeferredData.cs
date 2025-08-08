using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace ZeroRP
{
    public class DeferredData : ContextItem
    {
        public TextureHandle[] GBuffer { get;  set; } = new TextureHandle[ZeroRPConstants.GBufferSize];

        public override void Reset()
        {
            for (int i = 0; i < GBuffer.Length; i++)
            {
                GBuffer[i] = TextureHandle.nullHandle;
            }
        }
    }
}