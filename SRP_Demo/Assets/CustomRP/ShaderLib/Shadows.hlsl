#ifndef CUSTOM_SHADOW_INCLUDED
#define CUSTOM_SHADOW_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4

struct DirectionalShadowData
{
	float strength;
	int tileIndex;
};

//Special texture declare for shadow map
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
	float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS); 
}

float GetDirectionalShadowAttenuation(DirectionalShadowData data, Surface surfaceWS)
{
	if (data.strength <= 0.0) 
	{
		return 1.0;
	}

	float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.position, 1.0f)).xyz;


	float shadow = SampleDirectionalShadowAtlas(positionSTS);

	
	//return shadow;
	return lerp(1.0, shadow, data.strength);
}

#endif