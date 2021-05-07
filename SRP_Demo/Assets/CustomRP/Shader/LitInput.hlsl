#ifndef CUSTOM_LIT_INPUT_INCLUDED
#define CUSTOM_LIT_INPUT_INCLUDED
//this file wil be store all the input def and functions for the lit pass

//short cut unity access per material property
#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

TEXTURE2D(_BaseMap);
TEXTURE2D(_MaskMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_DetailMap);
TEXTURE2D(_DetailNormalMap);
SAMPLER(sampler_DetailMap);


TEXTURE2D(_NormalMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailAlbedo)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailSmoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailNormalScale)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
// the error assert 0==m_CurrentBuildInBindMask may cased by the GPU instance option os not on in the material


float GetFresnel(float2 baseUV)
{
	return INPUT_PROP(_Fresnel);
}

float2 TransformBaseUV(float2 baseUV)
{
	float4 baseST = INPUT_PROP(_BaseMap_ST);
	return baseUV * baseST.xy + baseST.zw;
}

float2 TransformDetailUV (float2 baseUV)
{
	float4 detailST = INPUT_PROP(_DetailMap_ST);
	return baseUV * detailST.xy + detailST.zw;
}


float3 GetEmission(float2 baseUV)
{
	float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, baseUV);
	float4 color = INPUT_PROP( _EmissionColor);
	return map.rgb * color.rgb;
}

float4 GetMask (float2 baseUV)
{
	return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, baseUV);
}

float4 GetDetail (float2 detailUV)
{
	float4 map = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, detailUV);
	return map * 2.0f - 1.0f;
}

//get base color = BC map * bc param
float4 GetBase(float2 baseUV, float2 detailUV = 0.0)
{
	float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
	float4 baseColor = INPUT_PROP(_BaseColor);

	float detail = GetDetail(detailUV).r * INPUT_PROP(_DetailAlbedo);
	float mask = GetMask(baseUV).b;
	map.rgb = lerp(sqrt(map.rgb), detail>0.0f ? 1.0 : 0.0, abs(detail) * mask);
	map.rgb *= map.rgb;

	return map * baseColor;
}

float3 GetNormalTS(float2 baseUV, float2 detailUV = 0.0)
{
	float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, baseUV);
	float scale  = INPUT_PROP(_NormalScale);
	float3 normal = DecodeNormal(map, scale);

	map = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailMap, detailUV);
	scale = INPUT_PROP(_DetailNormalScale);
	float3 detailNormal = DecodeNormal(map, scale);

	normal = BlendNormalRNM(normal, detailNormal);

	return normal;
}

// below functions don't need UV, But for later use we do need it for texture sample
float GetCutoff(float2 baseUV)
{
	return INPUT_PROP(_Cutoff);
}

float GetMetallic(float2 baseUV)
{
	float metallic = INPUT_PROP(_Metallic);
	metallic *= GetMask(baseUV).r;
	return metallic;
}

float GetSmoothness(float2 baseUV, float2 detailUV = 0.0)
{
	float smoothness = INPUT_PROP(_Smoothness);
	smoothness *= GetMask(baseUV).a;

	float mask = GetMask(baseUV).b;
	float detailSmoothness = GetDetail(detailUV).b * INPUT_PROP(_DetailSmoothness);
	smoothness = lerp(smoothness, detailSmoothness < 0.0 ? 0.0 : 1.0, abs(detailSmoothness)*mask);
	return smoothness;
}

float GetOcclusion(float2 baseUV)
{
	float strength = INPUT_PROP(_Occlusion);
	float occlusion = GetMask(baseUV).g;
	return lerp(occlusion, 1.0, strength);
}



#endif