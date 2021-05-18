﻿using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;

    Lighting lighting = new Lighting();

    PostFXStack postFXStack = new PostFXStack();

    const string buffername = "Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = buffername };

    CullingResults cullingResults;

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId LitShaderTadId = new ShaderTagId("CustomLit");

    //static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
    static int depthAttachmentId = Shader.PropertyToID("_CameraDelpthAttachment");
    static int depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
    static int sourceTextureId = Shader.PropertyToID("_SourceTexture");


    bool useHDR;
    bool useDepthTexture, useIntermediateBuffer;
    
    static CameraSettings defaultCameraSettings = new CameraSettings();

    Material material;

    Texture2D missingTexture;//used when the texture to sample is missing, help for debug.

    //Constructor
    public CameraRenderer(Shader shader)
    {
        material = CoreUtils.CreateEngineMaterial(shader);
        missingTexture = new Texture2D(1, 1) {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing>_<!"
        };
        missingTexture.SetPixel(0, 0, Color.white * 0.5f);
        missingTexture.Apply(true, true);
    }
     

    public void Render(ScriptableRenderContext IN_context, 
        Camera IN_camera, CameraBufferSettings cameraBufferSettings, bool useDynameicBatching, 
        bool useGPUInstancing, bool useLightPerObject,
        ShadowSettings shadowSetting, PostFXSettings postFXSettings, int colorLUTResolution)
    {
        this.context = IN_context;
        this.camera = IN_camera;

        //setup custom camera settings
        //for per camera blend, PostFX settings
        var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;

        //use depthtexture so shader can access the current buffer depth    
        if (camera.cameraType == CameraType.Reflection)
        {
            useDepthTexture = cameraBufferSettings.copyDepthReflection;
        }
        else 
        {
            useDepthTexture = cameraBufferSettings.copyDepth && cameraSettings.copyDepth;
        }

        if (cameraSettings.overridePostFX)
        {
            //override PostFX option for each cam
            postFXSettings = cameraSettings.postFXSettings;
        }

        //change buffer name to the camera name
        PrepareBuffer();

        //add UI (WorldGeometry) to the scene camera, so we can see UI in editor view
        PrepareForSceneView();
        if (!Cull(shadowSetting.maxDistance))
        {
            return;
        }

        this.useHDR = cameraBufferSettings.allowHDR && camera.allowHDR;

        buffer.BeginSample(SampleName);//Include lights and shadow rendering in main cam profile 
        ExecuteBuffer();
        //get transfer DirLight data to GPU
        //Setup shadow RT and shadow rendering
        lighting.Setup(context, cullingResults, shadowSetting, useLightPerObject,
            cameraSettings.maskLights ? cameraSettings.RenderingLayerMask : -1);

        //setup postFX
        postFXStack.Setup(context, camera, postFXSettings, useHDR, colorLUTResolution, cameraSettings.finalBlendMode);

        buffer.EndSample(SampleName);

        //Setup rendertarget for normal oject rendering
        Setup();
        DrawVisibleGeometry(useDynameicBatching, useGPUInstancing, useLightPerObject, cameraSettings.RenderingLayerMask);

        //this makes the Legacy shader draw upon the tranparent object
        //makes it wired, but they are not supported who cares~
        DrawUnsupportedShaders();

        DrawGizmosBeforeFX();

        if (postFXStack.IsActive)
        {
            postFXStack.Render(colorAttachmentId);
        }
        else if (useIntermediateBuffer)
        {
            // we need to copy the image from intermediate to final 
            //otherwise nothing goes to camera target, Since PFX is not active 
            Draw(colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
            ExecuteBuffer();
        }

        DrawGizmosAfterFX();

        Cleanup();

        //all action will be buffered and render action only begin after submit!
        Submit();
    }

    void Setup()
    {
        //settng up camera before clearRT will give us more efficient process
        context.SetupCameraProperties(camera);
        //Get the clear flags from camera
        CameraClearFlags flags = camera.clearFlags;

        //make sure we have a depth texture 
        useIntermediateBuffer = useDepthTexture || postFXStack.IsActive;

        //setup camera frame buffer for post FX
        //Without this, the content is directly draw to camera target
        if (useIntermediateBuffer)
        {
            if (flags > CameraClearFlags.Color)
            { flags = CameraClearFlags.Color; }

            //Separate the color and depth for the camera buffer
            buffer.GetTemporaryRT(colorAttachmentId, camera.pixelWidth, 
                camera.pixelHeight, 0, FilterMode.Bilinear, 
                useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            buffer.GetTemporaryRT(depthAttachmentId, camera.pixelWidth,
                camera.pixelHeight, 32, FilterMode.Point,
                RenderTextureFormat.Depth);
            buffer.SetRenderTarget(colorAttachmentId, 
                RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store,
                depthAttachmentId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }

        //clearRT not need to be in profiling, it is self sampled using the buffer name
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
            );//clear the RT based on the clear flags
        //Profile injection, so we can use profiler to monitor what happens in between(B   eing&End)
        buffer.BeginSample(SampleName);

        //Set the depthtexture to missing, anything rendered after and before CopyAttachments()
        //will have invalid depth texture(ALL opaque objects and skybox)
        buffer.SetGlobalTexture(depthTextureId, missingTexture);

        //Excute the Profile injection
        ExecuteBuffer();
        //buffer.Clear();

        
    }

    void DrawVisibleGeometry(bool useDynameicBatching, bool useGPUInstancing, bool useLightPerObject,
        int renderingLayerMask)
    {
        //per Object light data stuff
        PerObjectData lightPerObjectFlags = useLightPerObject ?
            PerObjectData.LightData | PerObjectData.LightIndices :
            PerObjectData.None;
        
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
                PerObjectData.ReflectionProbes |//send reflection probes to GPU
                lightPerObjectFlags
        };
        drawingSettings.SetShaderPassName(1, LitShaderTadId);

        //filter object queue as well as RenderingLayerMask
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask:(uint)renderingLayerMask);

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings);

        //we are drawing in order like opaque->skybox->tranparent
        context.DrawSkybox(camera);

        //copy the depth of all opaque and sky
        //so if the opaque object tries to sample the _CameraDepthTexture, result will be invalid
        CopyAttachments();

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


    void Cleanup()
    {
        //cleanup light(null) and shadows(render target)
        lighting.CleanUp();

        if (useIntermediateBuffer)
        {
            buffer.ReleaseTemporaryRT(colorAttachmentId);
            buffer.ReleaseTemporaryRT(depthAttachmentId);
            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);
            }
        }
    }

    public void Dispose()
    {
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missingTexture);
    }

    void CopyAttachments()
    {
        if (useDepthTexture)
        {
            buffer.GetTemporaryRT(
                depthTextureId, camera.pixelWidth, camera.pixelHeight,
                32, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.CopyTexture(depthAttachmentId, depthTextureId);
            ExecuteBuffer();
        }
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to)
    {
        buffer.SetGlobalTexture(sourceTextureId, from);
        buffer.SetRenderTarget(to,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
    }
}