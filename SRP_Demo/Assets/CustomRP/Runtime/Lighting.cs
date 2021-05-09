using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
public class Lighting
{
    const string buffername = "Lighting";
    const int maxDirLightCount = 4;
    const int maxOtherLightCount = 64;

    static int
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        dirLightColorId = Shader.PropertyToID("_DirectionalLightColors"),
        dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirections"),
        dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    static Vector4[]
        dirLightColors = new Vector4[maxDirLightCount],
        dirLightDirections = new Vector4[maxDirLightCount],
        dirLightShadowData = new Vector4[maxDirLightCount];

    static int
        otherLightCountID = Shader.PropertyToID("_OtherLightCount"),
        otherLightColorsID = Shader.PropertyToID("_OtherLightColors"),
        otherLightPositionsID = Shader.PropertyToID("_OtherLightPositions"),
        otherLightDirectionsID = Shader.PropertyToID("_OtherLightDirections"),
        otherLightSpotAnhglesID = Shader.PropertyToID("_OtherLightSpotAngles"),
        otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");

    static Vector4[]
        otherLightColors = new Vector4[maxOtherLightCount],
        otherLightPositions = new Vector4[maxOtherLightCount],
        otherLightDirections = new Vector4[maxOtherLightCount],
        otherLightSpotAngles = new Vector4[maxOtherLightCount],
        otherLightShadowData = new Vector4[maxOtherLightCount];

    CommandBuffer buffer = new CommandBuffer {
        name = buffername
    };

    CullingResults cullingResults;

    Shadows shadows = new Shadows();

    static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";

    public void Setup(
        ScriptableRenderContext context, 
        CullingResults cullingResults, 
        ShadowSettings shadowSettings,
        bool useLightPerObject)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(buffername);
        //Setup shadows before setup lights
        shadows.Setup(context, cullingResults, shadowSettings);
        //get all the Lightsinfo and sent to GPU
        SetupLights(useLightPerObject);
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

    void SetupPointLight(int index, ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1 / Mathf.Max(visibleLight.range * visibleLight.range, 0.000001f);
        otherLightPositions[index] = position;
        //dummy a and b for spot angle attenuation to make sure point light have no angle att
        otherLightSpotAngles[index] = new Vector4(0.0f, 1.0f);

        Light light = visibleLight.light;
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, index);
    }

    void SetupSpotLight(int index, ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1 / Mathf.Max(visibleLight.range * visibleLight.range, 0.000001f);
        otherLightPositions[index] = position;
        otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

        //calculate a and b for spot light angle attenutation
        Light light = visibleLight.light;
        float innderCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float OuterCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innderCos - OuterCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -OuterCos * angleRangeInv);

        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, index);
    }

    void SetupLights(bool useLightPerObject)
    {
        //get a temp light indice array
        NativeArray<int> indexMap = useLightPerObject ?
            cullingResults.GetLightIndexMap(Allocator.Temp) : 
            default;
        
        //Switch to get lights via Culling result
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        int dirLightCount = 0, otherLightCount = 0;
        int i;
        for (i = 0; i < visibleLights.Length; i++)
        {
            // set the index for directional lights to -1 unity will not include this into per object lighting
            int newIndex = -1;
            VisibleLight visibleLight = visibleLights[i];
            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount < maxDirLightCount)
                    {
                        SetupDirectionalLight(dirLightCount++, ref visibleLight);
                    }
                    break;
                case LightType.Point:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupPointLight(otherLightCount++, ref visibleLight);
                    }
                    break;
                case LightType.Spot:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupSpotLight(otherLightCount++, ref visibleLight);
                    }
                    break;
            }
            if (useLightPerObject)//set index for visible lights
            {
                indexMap[i] = newIndex;
            }
            
        }
        if (useLightPerObject)//set index of invisible lights to -1
        {
            for (; i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            }
            cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();
            Shader.EnableKeyword(lightsPerObjectKeyword);
        }
        else
        {
            Shader.DisableKeyword(lightsPerObjectKeyword);
        }


            //Set to global so all shader for dirctional lights
            buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(dirLightColorId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionId, dirLightDirections);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }
        //set params for other lights
        buffer.SetGlobalInt(otherLightCountID, otherLightCount);
        if (otherLightCount > 0)
        {
            buffer.SetGlobalVectorArray(otherLightColorsID, otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsID, otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsID, otherLightDirections);
            buffer.SetGlobalVectorArray(otherLightSpotAnhglesID, otherLightSpotAngles);
            buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
        }

        
    }

    public void CleanUp()
    {
        shadows.CleanUp();
    }
}
