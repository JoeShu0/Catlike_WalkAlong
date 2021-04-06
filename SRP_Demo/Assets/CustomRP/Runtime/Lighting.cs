using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
public class Lighting
{
    const string buffername = "Lighting";
    const int maxDirLightCount = 4;

    static int
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        dirLightColorId = Shader.PropertyToID("_DirectionalLightColors"),
        dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirections"),
        dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    static Vector4[]
        dirLightColors = new Vector4[maxDirLightCount],
        dirLightDirections = new Vector4[maxDirLightCount],
        dirLightShadowData = new Vector4[maxDirLightCount];

    CommandBuffer buffer = new CommandBuffer {
        name = buffername
    };

    CullingResults cullingResults;

    Shadows shadows = new Shadows();

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(buffername);
        //Setup shadows before setup lights
        shadows.Setup(context, cullingResults, shadowSettings);
        //get all the Lightsinfo and sent to GPU
        SetupLights();
        //render shadow
        shadows.Render();

        buffer.EndSample(buffername);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        /*
        Light light = RenderSettings.sun;
        buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
        buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);
        */

        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        //register the shadow casting lights
        dirLightShadowData[index] = shadows.ReserveDirectioanlShadows(visibleLight.light, index);
    }

    void SetupLights()
    {
        //Switch to get lights via Culling result
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType == LightType.Directional) 
            {
                SetupDirectionalLight(dirLightCount++, ref visibleLight);
                //Debug.Log(visibleLight.finalColor);
                if (dirLightCount >= maxDirLightCount)
                {
                    break;
                }
            }
            
        }

        //Set to global so all shader can get these params
        buffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
        buffer.SetGlobalVectorArray(dirLightColorId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionId, dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
    }

    public void CleanUp()
    {
        shadows.CleanUp();
    }
}
