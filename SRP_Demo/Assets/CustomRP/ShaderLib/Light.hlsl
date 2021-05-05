#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_CustomLight)
int _DirectionalLightCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light
{
	float3 color;
	float3 direction;
	float attenuation;
};

int GetDirectionalLightCount()
{
	return _DirectionalLightCount;
}

DirectionalShadowData GetDirectionalLightShadowData(int lightIndex, ShadowData shadowData)
{
	DirectionalShadowData data;

	//due to we want to use Global ShadowData strength(cascade strength) for realtime to baked transition
	//We can not jius apply it here
	data.strength = _DirectionalLightShadowData[lightIndex].x; //* shadowData.strength;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
	return data;
}

Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData)
{
	Light light;
	light.color = _DirectionalLightColors[index].xyz;
	light.direction = _DirectionalLightDirections[index].xyz;
	DirectionalShadowData dirShadowData = GetDirectionalLightShadowData(index, shadowData);
	//this attenuation is perfragment not per light
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
	//light.attenuation = shadowData.cascadeIndex * 0.25;
	return light;
}

#endif
