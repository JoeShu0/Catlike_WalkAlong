using UnityEngine;
using UnityEngine.Rendering;

public class CameraRender
{
    ScriptableRenderContext context;

    Camera camera;

    public void Render(ScriptableRenderContext IN_context, Camera IN_camera)
    {
        this.context = IN_context;
        this.camera = IN_camera;

        Setup();

        DrawVidibleGeometry();

        //all action will be buffered and render action only begin after submit!
        Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
    }

    void DrawVidibleGeometry()
    {
        context.DrawSkybox(camera);
    }

    void Submit()
    {
        context.Submit();
    }
}
