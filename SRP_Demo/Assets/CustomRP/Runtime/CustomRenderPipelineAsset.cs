using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset 
{
    [SerializeField]
    bool useDynameicBatching = true, 
        useGPUInstancing = true, 
        useSRPBatcher = true,
        useLightPerObject = true;

    [SerializeField]
    ShadowSettings shadows = default;

    [SerializeField]
    bool allowHDR = true;

    [SerializeField]
    PostFXSettings postFXSettings = default;

    public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64}
    [SerializeField]
    ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(allowHDR,
            useDynameicBatching, useGPUInstancing, 
            useSRPBatcher, useLightPerObject, 
            shadows, postFXSettings, (int)colorLUTResolution);
        //throw new System.NotImplementedException();
    }
}