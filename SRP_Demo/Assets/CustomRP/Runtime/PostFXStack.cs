using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack
{
    const string buffername = "PostFX";

    CommandBuffer buffer = new CommandBuffer
    {
        name = buffername
    };

    ScriptableRenderContext context;

    Camera camera;

    PostFXSettings settings;

    const int maxBloomPyramidLevel = 16;

    int bloomPyramidId;

    public bool IsActive => settings != null;

    enum Pass 
    {
        Copy
    }

    int fxSourceId = Shader.PropertyToID("_PostFXSource");

    public void Setup
        (ScriptableRenderContext context,
        Camera camera, PostFXSettings settings)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        //buffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        //Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        DoBloom(sourceId);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < maxBloomPyramidLevel; i++)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    void Draw(RenderTargetIdentifier from,
        RenderTargetIdentifier to,
        Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(to,
            RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity,
            settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    void DoBloom(int SourceId)
    {
        buffer.BeginSample("Bloom");
        int width = camera.pixelWidth / 2;
        int height = camera.pixelHeight / 2;
        RenderTextureFormat format = RenderTextureFormat.Default;
        int fromId = SourceId, toId = bloomPyramidId;

        int i;
        for (i = 0; i < maxBloomPyramidLevel; i++)
        {
            if (height < 1 || width < 1)
            {
                break;
            }
            buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);

            Draw(fromId, toId, Pass.Copy);
            fromId = toId;
            toId += 1;
            width /= 2;
            height /= 2;
        }

        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);

        for (i -= 1; i >= 0; i--)
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId + i);
        }

        buffer.EndSample("Bloom");
    }
}
