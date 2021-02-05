using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRender
{
    ScriptableRenderContext context;
    Camera camera;

    const string buffername = "Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = buffername };

    CullingResults cullingResults;

    static ShaderTagId unlitShaderTagID = new ShaderTagId("SRPDefaultUnlit");
    



    public void Render(ScriptableRenderContext IN_context, Camera IN_camera)
    {
        this.context = IN_context;
        this.camera = IN_camera;

        //change buffer name to he camera name
        PrepareBuffer();
        //add UI (WorldGeometry) to the scen camera, so we can see UI in editor view
        PrepareForSceneView();
        if (!Cull())
        {
            return;
        }

        Setup();

        DrawVidibleGeometry();

        //this makes the Legacy shader draw upon the tranparent object
        //makes it wired, but they are not supported who cares~
        DrawUnsupportedShaders();

        DrawGizmos();

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

    void DrawVidibleGeometry()
    {
        //draw opaque
        var sortingSettings = new SortingSettings(camera) { 
            criteria = SortingCriteria.CommonOpaque};
        var drawingSettings = new DrawingSettings(unlitShaderTagID, sortingSettings);
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

    bool Cull()
    {
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p))
        {
            cullingResults = context.Cull(ref p);//ref here is just to prevent duplicate the P
            return true;
        }
        return false;
    }

    
}
