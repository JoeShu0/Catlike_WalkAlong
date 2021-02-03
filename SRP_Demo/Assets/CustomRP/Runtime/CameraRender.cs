using UnityEngine;
using UnityEngine.Rendering;

public class CameraRender
{
    ScriptableRenderContext context;
    Camera camera;

    const string buffername = "Render Camera 00";
    CommandBuffer buffer = new CommandBuffer { name = buffername };

    CullingResults cullingResults;

    public void Render(ScriptableRenderContext IN_context, Camera IN_camera)
    {
        this.context = IN_context;
        this.camera = IN_camera;

        if (!Cull())
        {
            return;
        }

        Setup();

        DrawVidibleGeometry();

        //all action will be buffered and render action only begin after submit!
        Submit();
    }

    void Setup()
    {
        //settng up camera before clearRT will give us more efficient process
        context.SetupCameraProperties(camera);
        //clearRT not need to be in profiling, it is self sampled using the buffer name
        buffer.ClearRenderTarget(true, true, Color.clear);
        //Profile injection, so we can use profiler to monitor what happens in between(B   eing&End)
        buffer.BeginSample(buffername);

        //Excute the Profile injection
        ExecuteBuffer();
        //buffer.Clear();

        
    }

    void DrawVidibleGeometry()
    {
        context.DrawSkybox(camera);
    }

    void Submit()
    {
        //end of profiling, the actions will be nested under the buffer name in profiler 
        buffer.EndSample(buffername);
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
