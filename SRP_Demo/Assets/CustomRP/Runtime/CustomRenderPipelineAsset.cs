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
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(allowHDR,
            useDynameicBatching, useGPUInstancing, 
            useSRPBatcher, useLightPerObject, 
            shadows, postFXSettings);
        //throw new System.NotImplementedException();
    }
}