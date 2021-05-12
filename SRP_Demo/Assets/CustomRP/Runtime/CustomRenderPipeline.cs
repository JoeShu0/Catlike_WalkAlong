using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    CameraRender render = new CameraRender();

    bool useDynameicBatching, useGPUInstancing ,useLightPerObject;

    ShadowSettings shadowsetting;

    PostFXSettings postFXSettings;

    public CustomRenderPipeline(
        bool useDynameicBatching, 
        bool useGPUInstancing, 
        bool useSRPBatcher,
        bool useLightPerObject,
        ShadowSettings shadowsetting,
        PostFXSettings postFXSettings)
    {
        this.useDynameicBatching = useDynameicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.shadowsetting = shadowsetting;
        this.useLightPerObject = useLightPerObject;

        this.postFXSettings = postFXSettings;

        //
        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            render.Render(context, camera, 
                useDynameicBatching, useGPUInstancing, 
                useLightPerObject, shadowsetting,
                postFXSettings);
        }
    }
}