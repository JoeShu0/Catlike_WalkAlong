#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface
{
	float3 normal;
	float3 color;
	float3 viewDirection;//from frag to view
	float alpha;
	float metallic;
	float smoothness;
};

#endif
