using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRender
{
    ScriptableRenderContext context;
    Camera camera;

    Lighting lighting = new Lighting();

    const string buffername = "Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = buffername };

    CullingResults cullingResults;

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId LitShaderTadId = new ShaderTagId("CustomLit");


    public void Render(ScriptableRenderContext IN_context, Camera IN_camera, bool useDynameicBatching, bool useGPUInstancing, ShadowSettings shadowSetting)
    {
        this.context = IN_context;
        this.camera = IN_camera;

        //change buffer name to he camera name
        PrepareBuffer();
        //add UI (WorldGeometry) to the scene camera, so we can see UI in editor view
        PrepareForSceneView();
        if (!Cull(shadowSetting.maxDistance))
        {
            return;
        }

        buffer.BeginSample(SampleName);//Include lights and shadow rendering in main cam profile 
        ExecuteBuffer();
        //get transfer DirLight data to GPU
        //Setup shadow RT and shadow rendering
        lighting.Setup(context, cullingResults, shadowSetting);
        buffer.EndSample(SampleName);

        //Setup rendertarget for normal boject rendering
        Setup();
        DrawVidibleGeometry(useDynameicBatching, useGPUInstancing);

        //this makes the Legacy shader draw upon the tranparent object
        //makes it wired, but they are not supported who cares~
        DrawUnsupportedShaders();

        DrawGizmos();

        //cleanup light(null) and shadows(render target)
        lighting.CleanUp();

        //all action will be buffered and render action only begin after submit!
        Submit();
    }

    void Setup()
    {
        //settng up camera before clearRT will give us more efficient process
        context.SetupCameraProperties(camera);
        //Get the clear flags from camera
        CameraClearFlags flags = camera.clearFlags;
        //clearRT not need to be in profiling, it is self sampled using the buffer name
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
            );//clear the RT based on the clear flags
        //Profile injection, so we can use profiler to monitor what happens in between(B   eing&End)
        buffer.BeginSample(SampleName);

        //Excute the Profile injection
        ExecuteBuffer();
        //buffer.Clear();

        
    }

    void DrawVidibleGeometry(bool useDynameicBatching, bool useGPUInstancing)
    {
        //draw opaque
        var sortingSettings = new SortingSettings(camera) { 
            criteria = SortingCriteria.CommonOpaque};
        //drawing setting what kind of shader should be draw
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings) {
            enableDynamicBatching = useDynameicBatching, 
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps |//lightmap UV
                PerObjectData.LightProbe |//lighting Probe coefficient
                PerObjectData.LightProbeProxyVolume |// LPPV data
                PerObjectData.ShadowMask |//shadowmask texture
                PerObjectData.OcclusionProbe|//for using lightmap on dynamic assets
                PerObjectData.OcclusionProbeProxyVolume |//same above for LPPV
                PerObjectData.ReflectionProbes//send reflection probes to GPU
        };
        drawingSettings.SetShaderPassName(1, LitShaderTadId);

        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings);

        //we are drawing in order like opaque->skybox->tranparent
        context.DrawSkybox(camera);
        //DrawUnsupportedShaders();

        //draw transparent
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings);
    }



    void Submit()
    {
        //end of profiling, the actions will be nested under the buffer name in profiler 
        buffer.EndSample(SampleName);
        //Execute all the command in buffer as well as Profile end
        ExecuteBuffer();

        context.Submit();
    }

    void ExecuteBuffer()
    {
        //Execute all the commands in the buffer
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);//set shadow dist, from renderPPAsset
            cullingResults = context.Cull(ref p);//ref here is just to prevent duplicate the P
            return true;
        }
        return false;
    }

    
}
