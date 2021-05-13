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
        Copy,
        BloomHorizontal,
        BloomVertical,
        BloomCombine
    }

    int fxSourceId = Shader.PropertyToID("_PostFXSource");
    int fxSource2Id = Shader.PropertyToID("_PostFXSource2");

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
        for (int i = 1; i < maxBloomPyramidLevel * 2; i++)
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
        PostFXSettings.BloomSettings bloomSettings = settings.Bloom;

        buffer.BeginSample("Bloom");
        int width = camera.pixelWidth / 2;
        int height = camera.pixelHeight / 2;

        if (bloomSettings.maxIterations == 0 ||
            height < bloomSettings.downscaleLimit || width < bloomSettings.downscaleLimit)
        {
            Draw(SourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            buffer.EndSample("Bloom");
            return;
        }

        RenderTextureFormat format = RenderTextureFormat.Default;
        //We are using 2 pass per bloom level
        int fromId = SourceId, toId = bloomPyramidId + 1;

        int i;
        for (i = 0; i < bloomSettings.maxIterations; i++)
        {
            if (width < bloomSettings.downscaleLimit || height < bloomSettings.downscaleLimit)
            {
                break;
            }
            int midId = toId - 1;
            buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);

            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);

            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        //Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        //release the mid for last level
        buffer.ReleaseTemporaryRT(fromId - 1);
        //set toId to the mid of one level higher
        toId -= 5;

        if (i > 1)
        {
            //stop at the 1st level, special case for level before last level
            //just in-order to reuse RTs
            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(fromId, toId, Pass.BloomCombine);
                buffer.ReleaseTemporaryRT(fromId);
                buffer.ReleaseTemporaryRT(toId + 1);
                fromId = toId;
                toId -= 2;
            }
        }
        else 
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }
        
        buffer.SetGlobalTexture(fxSource2Id, SourceId);
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.BloomCombine);
        buffer.ReleaseTemporaryRT(fromId);

        buffer.EndSample("Bloom");
    }
}
