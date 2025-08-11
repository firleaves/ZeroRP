using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRP
{
    public class CameraData : ContextItem
    {
        public Camera Camera { get; set; }
        public CullingResults CullingResults { get; set; }

        public RenderTextureDescriptor CameraTargetDescriptor { get; set; }

        public override void Reset()
        {
            Camera = null;
            CullingResults = default;
            CameraTargetDescriptor = default;
        }

        public float GetCameraAspectRatio()
        {
            return (float)Camera.pixelWidth / (float)Camera.pixelHeight;
        }

        public RTClearFlags GetClearFlags()
        {
            CameraClearFlags clearFlags = Camera.clearFlags;
            return clearFlags switch
            {
                CameraClearFlags.Depth => RTClearFlags.DepthStencil,
                CameraClearFlags.Nothing => RTClearFlags.None,
                _ => RTClearFlags.All
            };
        }

        public Color GetClearColor()
        {
            return CoreUtils.ConvertSRGBToActiveColorSpace(Camera.backgroundColor);
        }
    }
}