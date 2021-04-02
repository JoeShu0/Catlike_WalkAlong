using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
public class Shadows
{
    const string buffername = "Shadows";

    CommandBuffer buffer = new CommandBuffer {
        name = buffername
    };

    ScriptableRenderContext context;
    CullingResults cullingResults;
    ShadowSettings shadowSettings;

    const int maxShadoweDirectionalLightCount = 1;
    int ShadowedDirectionalLightCount;

    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }

    ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadoweDirectionalLightCount];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        this.context = context;
        this.shadowSettings = shadowSettings;

        ShadowedDirectionalLightCount = 0;
        /*
        buffer.BeginSample(buffername);
        SetupLights();
        buffer.EndSample(buffername);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        */
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public void ReserveDirectioanlShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadoweDirectionalLightCount && 
            light.shadows != LightShadows.None && light.shadowStrength > 0f &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b)) 
            //the getshadowCasterBound will return false if the light does not effect any oject(can cast shadow) in shadow range
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount++] =
                new ShadowedDirectionalLight { visibleLightIndex = visibleLightIndex };
        }
    }

    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            //prevent default RT problem in WebGL2.0
            buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
        
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        //create temp rendertex to save shadow depth tex
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //order GPU to render on this RT, Load.dontcare since we goingto write on it, We need oit to store
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //clear the buffer for rendering
        buffer.ClearRenderTarget(true, false, Color.clear);
        //begin profile
        buffer.BeginSample(buffername);
        ExecuteBuffer();

        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, atlasSize);//currently only one light
        }

        //end profile
        buffer.EndSample(buffername);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows(int index, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        //create a shadowdrawsetting based on the light index
        var shadowDSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        //get the lightviewMatrix, lightprojectionMatrix, clip space box for the dirctional lights
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
            out ShadowSplitData splitData
            );

        shadowDSettings.splitData = splitData;
        //set the ViewProjectionMatrices for the buffer
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        //draw shadow caster on the buffer
        context.DrawShadows(ref shadowDSettings);
        //Debug.Log(shadowDSettings);
    }

    public void CleanUp()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

}
