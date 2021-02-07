using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    CameraRender render = new CameraRender();

    bool useDynameicBatching, useGPUInstancing;

    public CustomRenderPipeline(bool useDynameicBatching, bool useGPUInstancing, bool useSRPBatcher)
    {
        this.useDynameicBatching = useDynameicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            render.Render(context, camera, useDynameicBatching, useGPUInstancing);
        }
    }
}