using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    CameraRender render = new CameraRender();

    bool useDynameicBatching, useGPUInstancing;

    ShadowSettings shadowsetting;

    public CustomRenderPipeline(bool useDynameicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowsetting)
    {
        this.useDynameicBatching = useDynameicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.shadowsetting = shadowsetting;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            render.Render(context, camera, useDynameicBatching, useGPUInstancing, shadowsetting);
        }
    }
}