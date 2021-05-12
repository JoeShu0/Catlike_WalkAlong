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
    PostFXSettings postFXSettings = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(
            useDynameicBatching, useGPUInstancing, 
            useSRPBatcher, useLightPerObject, 
            shadows, postFXSettings);
        //throw new System.NotImplementedException();
    }
}