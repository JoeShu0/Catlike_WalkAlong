#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

//Special texture declare for shadow map
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
	int _CascadeCount;
	float4 _ShadowAtlasSize;//x is the atlas size, y is the texel size
	float4 _ShadowDistanceFade;
	float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
	float4 _CascadeData[MAX_CASCADE_COUNT];
	float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
CBUFFER_END

struct DirectionalShadowData
{
	float strength;
	int tileIndex;
	float normalBias;
};

struct ShadowData//per fragment data
{
	int cascadeIndex;
	float cascadeBlend;
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
	data.cascadeBlend = 1.0;
	int i;
	for (i = 0; i < _CascadeCount; i++)
	{
		float4 sphere = _CascadeCullingSpheres[i];
		float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
		if (distanceSqr < sphere.w)
		{
			//cal cascade blend factor
			float fade = FadeShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);

			if (i == _CascadeCount - 1)
			{
				data.strength *= fade;
			}
			else
			{
				data.cascadeBlend = fade;
			}
			
			break;
		}
	}
	
	if (i == _CascadeCount)
	{
		//this makes sure when frag goes outside the shadow distance, strength will be 0
		data.strength = 0.0;
	}
	//if dither
	#if defined(_CASCADE_BLEND_DITHER)
		else if (data.cascadeBlend < surfaceWS.dither){
			i += 1;
		}
	#endif
	//if not soft blend
	#if !defined(_CASCADE_BLEND_SOFT)
		data.cascadeBlend = 1.0;
	#endif
	
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

float FilterDirectionalShadow(float3 positionSTS)
{
#if defined(DIRECTIONAL_FILTER_SETUP)
	float weights[DIRECTIONAL_FILTER_SAMPLES];
	float2 positions[DIRECTIONAL_FILTER_SAMPLES];
	float4 size = _ShadowAtlasSize.yyxx;
	DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
	float shadow = 0;
	for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++)
	{
		shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
	}
	return shadow;
#else
	return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

float GetDirectionalShadowAttenuation(DirectionalShadowData directionalSD, ShadowData globalSD, Surface surfaceWS)
{
	//make the light strength attenuation with shadow as 1.0, 
	//when the light have 0 shadow strength or the surface is mark to not receive shadows
	#if !defined(_RECEIVE_SHADOWS)
	return 1.0;
	#endif
	if (directionalSD.strength <= 0.0)
	{
		return 1.0;
	}
	//offset the surface by the width of a texel to avoid shadow acne
	float3 normalBias = surfaceWS.normal *(directionalSD.normalBias * _CascadeData[globalSD.cascadeIndex].y);
	//float3 normalBias = 0;
	float3 positionSTS = mul(_DirectionalShadowMatrices[directionalSD.tileIndex], float4(surfaceWS.position+ normalBias, 1.0f)).xyz;


	//float shadow = SampleDirectionalShadowAtlas(positionSTS);
	//using filtered soft shadow
	float shadow = FilterDirectionalShadow(positionSTS);

	//check the blend value and if in blend zone, sample the next cascade
	if (globalSD.cascadeBlend < 1.0)
	{
		normalBias = surfaceWS.normal * (directionalSD.normalBias * _CascadeData[globalSD.cascadeIndex+1].y);
		positionSTS = mul(_DirectionalShadowMatrices[directionalSD.tileIndex+1], float4(surfaceWS.position + normalBias, 1.0f)).xyz;
		shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, globalSD.cascadeBlend);
	}
	
	//return shadow;
	return lerp(1.0, shadow, directionalSD.strength);
}



#endif