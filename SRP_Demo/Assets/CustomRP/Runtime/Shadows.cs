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

    const int 
        maxShadowedDirectionalLightCount = 4,
        maxCascades = 4;
    int ShadowedDirectionalLightCount;

    static int
        dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
        dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
        cascadeCountId = Shader.PropertyToID("_CascadeCount"),
        cascadeCullingSphereId = Shader.PropertyToID("_CascadeCullingSpheres"),
        cascadeDataId = Shader.PropertyToID("_CascadeData"),
        shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

    static Vector4[] 
        cascadeCullingSpheres = new Vector4[maxCascades],
        cascadeData = new Vector4[maxCascades];

    static Matrix4x4[]
        dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }

    ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

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

    public Vector2 ReserveDirectioanlShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        //the getshadowCasterBound will return false if the light does not effect any oject(can cast shadow) in shadow range
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
                new ShadowedDirectionalLight { visibleLightIndex = visibleLightIndex };
            return new Vector2(light.shadowStrength, shadowSettings.directional.cascadeCount * ShadowedDirectionalLightCount++);
        }
        else
        {
            return Vector2.zero;
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

        //if more than 1 dir light(MAX 4), the split should be 2(sqrt2)
        int tiles = ShadowedDirectionalLightCount * shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            //call Render DirShadows with spilt and tilesize
            RenderDirectionalShadows(i, split, tileSize);
        }

        //set cascadecount and cascade culling sphere to GPU
        buffer.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSphereId, cascadeCullingSpheres);
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        //set the shadowdistance to GPU to help reduce shadow strength to 0 outside the distance
        //otherwise the sample is cut based on culling sphere,which is extend beyond the distance
        //also add distance fade and cascade sphere fade, both using (1-d/max)/f, pass the max and f inverted
        float f = 1f - shadowSettings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId,
            new Vector4(1 / shadowSettings.maxDistance,
            1 / shadowSettings.distanceFade,
            1f / (1f - f * f))
            );
        //set the dir shadow light world to atlas Matrix in global
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        //end profile
        buffer.EndSample(buffername);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        //create a shadowdrawsetting based on the light index
        var shadowDSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        //take into count that each light will render max 4 cascades
        int cascadeCount = shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSettings.directional.CascadeRatios;
        //get the lightviewMatrix, lightprojectionMatrix, clip space box for the dirctional lights
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, cascadeCount, ratios, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData);
            shadowDSettings.splitData = splitData;
            //assign the cascade culling sphere, all lights uses the same culling spheres and same cascade datas
            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            int tileIdex = tileOffset + i;
            //set the render view port and get the offset for modify the dir shadow matrix
            //set the matrix from wolrd space to shadow Atlas space
            dirShadowMatrices[tileIdex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIdex, split, tileSize),
                split
                );
            //set the ViewProjectionMatrices for the buffer
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            //experimental depth bias
            //buffer.SetGlobalDepthBias(50000f, 0f);
            //using slop bias
            //buffer.SetGlobalDepthBias(0f, 3f);
            ExecuteBuffer();
            
            //draw shadow caster on the buffer
            context.DrawShadows(ref shadowDSettings);
            //Debug.Log(shadowDSettings
            //buffer.SetGlobalDepthBias(0f, 0f);
        }

    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        //Debug.Log(cullingSphere);
        float texelSize = 2f * cullingSphere.w / tileSize;
        
        //cascadeData[index].x = 1f / cullingSphere.w;//preinverted R for GPU
        //to reduce the calculation in gpu(square distance), we send in the sphere with radius = R * R
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        //setting the cascade data x is square invert R,y is the texsize * 1.414(square root of 2)
        cascadeData[index] = new Vector4(1f / cullingSphere.w, texelSize * 1.414f);
    }

    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        //check if we are using a reversed Z buffer, if so negete
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        
        //CliP space is -1~0~1 but texture depth is 0~1
        //need to bake the scale and offset into the matrix
        //matrix [Scale] * [offset.x,offset.y,0] * [scale0.5, offset0.5] * [m]
        
        float scale = 1f/ split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        /*
        //this is equal as above but more wasted 0 caculation
        Matrix4x4 m_clipscale = Matrix4x4.Scale(new Vector3(0.5f, 0.5f, 0.5f));
        Matrix4x4 m_cliptranslate = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f));
        Matrix4x4 m_AtlasOffset = Matrix4x4.Translate(new Vector3(offset.x, offset.y, 0f));
        Matrix4x4 m_AtlasScale = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        Debug.Log(m_AtlasScale);
        m = m_AtlasScale * m_AtlasOffset * m_cliptranslate * m_clipscale * m;
        */
        return m;
    }

    public void CleanUp()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

}
