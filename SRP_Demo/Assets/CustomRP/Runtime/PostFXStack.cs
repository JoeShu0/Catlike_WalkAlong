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
    bool useHDR;

    public bool IsActive => settings != null;

    enum Pass 
    {
        Copy,
        BloomHorizontal,
        BloomVertical,
        BloomAdd,
        BloomPrefilter,
        BloomPrefilterFireFlies,
        BloomScatter,
        BloomScatterFinal,
        ToneMappingACES,
        ToneMappingNeutral,
        ToneMappingReinhard
      
    }

    int fxSourceId = Shader.PropertyToID("_PostFXSource");
    int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    int bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBocubicUpsampling");
    int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    int bloomThreshold = Shader.PropertyToID("_BloomThreshold");
    int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    int bloomResultId = Shader.PropertyToID("_BloomResult");

    public void Setup
        (ScriptableRenderContext context,
        Camera camera, PostFXSettings settings,
        bool useHDR)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        this.useHDR = useHDR;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        //buffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        //Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        if (DoBloom(sourceId))
        {
            DoToneMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoToneMapping(sourceId);
        }
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

    bool DoBloom(int SourceId)
    {
        PostFXSettings.BloomSettings bloomSettings = settings.Bloom;

        int width = camera.pixelWidth / 2;
        int height = camera.pixelHeight / 2;

        if (bloomSettings.maxIterations == 0 || bloomSettings.intensity <= 0f ||
            height < bloomSettings.downscaleLimit || width < bloomSettings.downscaleLimit)
        {
            //Draw(SourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            //buffer.EndSample("Bloom");
            return false;
        }

        buffer.BeginSample("Bloom");
        //compute the constant part of bloom threshold knee
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
        threshold.y = threshold.x * bloomSettings.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThreshold, threshold);
        //unform format for easier switch HDR
        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        //We start bloom at half resolution
        buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(SourceId, bloomPrefilterId, 
            settings.Bloom.Fade_FireFlies? Pass.BloomPrefilterFireFlies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        //We are using 2 pass per bloom level
        int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;

        int i;
        for (i = 0; i < bloomSettings.maxIterations; i++)
        {
            if (width < bloomSettings.downscaleLimit * 2 || height < bloomSettings.downscaleLimit * 2)
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

        buffer.ReleaseTemporaryRT(bloomPrefilterId);
        //Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        //release the mid for last level
        buffer.ReleaseTemporaryRT(fromId - 1);
        //set toId to the mid of one level higher
        toId -= 5;

        // combine bloom
        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloomSettings.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloomSettings.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            buffer.SetGlobalFloat(bloomIntensityId, bloomSettings.intensity);
            finalIntensity = Mathf.Min(bloomSettings.intensity, 0.95f);
        }

        buffer.SetGlobalFloat(bloomBicubicUpsamplingId, bloomSettings.bicubicUpsampling ? 1f : 0f);
        
        if (i > 1)
        {
            //stop at the 1st level, special case for level before last level
            //just in-order to reuse RTs
            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(fromId, toId, combinePass);
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

        buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
        buffer.SetGlobalTexture(fxSource2Id, SourceId);
        buffer.GetTemporaryRT(bloomResultId, camera.pixelWidth, camera.pixelHeight, 0,
            FilterMode.Bilinear, format);
        //Draw the bloom result on a RT for tone mapping
        Draw(fromId, bloomResultId, finalPass);
        buffer.ReleaseTemporaryRT(fromId);

        buffer.EndSample("Bloom");
        return true;
    }

    void DoToneMapping(int sourceId)
    {
        PostFXSettings.ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass pass = mode < 0 ? Pass.Copy : Pass.ToneMappingACES + (int)mode;
        Draw(sourceId, BuiltinRenderTextureType.CameraTarget, pass);
    }
}
