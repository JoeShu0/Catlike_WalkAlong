using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    CameraRender render = new CameraRender();
    bool allowHDR;
    bool useDynameicBatching, useGPUInstancing ,useLightPerObject;

    ShadowSettings shadowsetting;

    PostFXSettings postFXSettings;
    int colorLUTResolution;


    public CustomRenderPipeline(
        bool allowHDR,
        bool useDynameicBatching, 
        bool useGPUInstancing, 
        bool useSRPBatcher,
        bool useLightPerObject,
        ShadowSettings shadowsetting,
        PostFXSettings postFXSettings,
        int colorLUTResolution)
    {
        this.useDynameicBatching = useDynameicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.shadowsetting = shadowsetting;
        this.useLightPerObject = useLightPerObject;
        this.postFXSettings = postFXSettings;
        this.allowHDR = allowHDR;
        this.colorLUTResolution = colorLUTResolution;
        //
        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            render.Render(context, camera, allowHDR,
                useDynameicBatching, useGPUInstancing, 
                useLightPerObject, shadowsetting,
                postFXSettings, colorLUTResolution);
        }
    }
}