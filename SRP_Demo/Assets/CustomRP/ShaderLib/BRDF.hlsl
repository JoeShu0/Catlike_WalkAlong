﻿#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

struct BRDF
{
	float3 diffuse;
	float3 specular;
	float roughness;
};

BRDF GetBRDF(Surface surface)
{
	BRDF brdf;
	float oneMinusReflectivity = oneMinusReflectivity(surface.metallic);
	brdf.diffuse = surface.color * oneMinusReflectivity;
	brdf.specular = 0.0;
	brdf.roughness = 1.0;
	return brdf;
}

float oneMinusReflectivity(float metallic)
{
	float range = 1.0 - MIN_REFLECTIVITY;
	return range - metallic * range;
}

#endif
