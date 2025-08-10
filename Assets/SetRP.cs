using UnityEngine;
using UnityEngine.Rendering;

public class SetRP : MonoBehaviour
{
     public RenderPipelineAsset currentPipeLineAsset;
    private void OnEnable()
    {
        GraphicsSettings.defaultRenderPipeline = currentPipeLineAsset;
    }

    private void OnValidate()
    {
        GraphicsSettings.defaultRenderPipeline = currentPipeLineAsset;
    }
}
