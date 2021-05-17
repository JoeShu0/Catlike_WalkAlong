#ifndef CUSTOM_LIT_INPUT_INCLUDED
#define CUSTOM_LIT_INPUT_INCLUDED
//this file wil be store all the input def and functions for the lit pass

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
// the error assert 0==m_CurrentBuildInBindMask may cased by the GPU instance option os not on in the material

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

struct InputConfig {
	float2 baseUV;
};

InputConfig GetInputConfig (float2 baseUV, float2 detailUV = 0.0) {
	InputConfig c;
	c.baseUV = baseUV;
	return c;
}


float2 TransformBaseUV(float2 baseUV)
{
	float4 baseST = INPUT_PROP(_BaseMap_ST);
	return baseUV * baseST.xy + baseST.zw;
}

float2 TransformDetailUV (float2 baseUV) {
	return 0.0;
}

//get base color = BC map * bc param
float4 GetBase(InputConfig c)
{
	float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
	float4 baseColor = INPUT_PROP(_BaseColor);
	float4 base = baseColor * map;
	return base;
}

float4 GetMask (InputConfig c) {
	return 1.0;
}

float4 GetDetail (InputConfig c) {
	return 0.0;
}

float3 GetNormalTS (InputConfig c) {
	return float3(0.0, 0.0, 1.0);
}

float3 GetEmission (InputConfig c) {
	return GetBase(c).rgb;
}

float GetCutoff (InputConfig c) {
	return INPUT_PROP(_Cutoff);
}

float GetMetallic (InputConfig c) {
	return 0.0;
}

float GetSmoothness (InputConfig c) {
	return 0.0;
}

float GetFresnel (InputConfig c) {
	return 0.0;
}

float GetFinalAlpha(float alpha)
{
	//if the material write depth, Alpha should be 1
	return INPUT_PROP(_ZWrite) ? 1.0 : alpha;
}


#endif