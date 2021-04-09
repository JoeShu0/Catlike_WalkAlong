#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

//Special texture declare for shadow map
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
	int _CascadeCount;
	float4 _ShadowDistanceFade;
	float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
	float4 _CascadeData[MAX_CASCADE_COUNT];
	float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
CBUFFER_END

struct DirectionalShadowData
{
	float strength;
	int tileIndex;
};

struct ShadowData//per fragment data
{
	int cascadeIndex;
	float strength;
};

float FadeShadowStrength(float distance, float scale, float fade)
{
	//here the scale is the invert of maxdistance, fade is the invert of fade
	return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surfaceWS)
{
	ShadowData data;
	//refuce shadow strength to 0 outside the shadoedistance
	data.strength = FadeShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
	//data.strength = 1.0;
	int i;
	for (i = 0; i < _CascadeCount; i++)
	{
		float4 sphere = _CascadeCullingSpheres[i];
		float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
		if (distanceSqr < sphere.w)
		{
			if (i == _CascadeCount - 1)
			{
				data.strength *= FadeShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
			}
			
			break;
		}
	}
	
	if (i == _CascadeCount)
	{
		//this makes sure when frag goes outside the shadow distance, strength will be 0
		data.strength = 0.0;
	}
	
	//data.strength = float(i) / 4.0f;
	data.cascadeIndex = i;
	//float4 sphere = _CascadeCullingSpheres[1];
	//float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
	//data.cascadeIndex = sphere.w>0 ? 3 : 0;
	//data.cascadeIndex = 0;
	return data;
}

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS); 
}

float GetDirectionalShadowAttenuation(DirectionalShadowData directionalSD, ShadowData globalSD, Surface surfaceWS)
{
	if (directionalSD.strength <= 0.0)
	{
		return 1.0;
	}
	//offset the surface by the width of a texel to avoid shadow acne
	float3 normalBias = surfaceWS.normal * _CascadeData[globalSD.cascadeIndex].y;
	//float3 normalBias = 0;
	float3 positionSTS = mul(_DirectionalShadowMatrices[directionalSD.tileIndex], float4(surfaceWS.position+ normalBias, 1.0f)).xyz;


	float shadow = SampleDirectionalShadowAtlas(positionSTS);

	
	//return shadow;
	return lerp(1.0, shadow, directionalSD.strength);
}

#endif