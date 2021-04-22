#ifndef CUSTOM_GI_INCLUDED
#define CUSTOM_GI_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

//TEX and sampler for lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
//TEX and SP for LPPV
TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

#if defined(LIGHTMAP_ON)
	#define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
	#define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
	#define TRANSFER_GI_DATA(input, output) \
			output.lightMapUV = input.lightMapUV * \
			unity_LightmapST.xy + unity_LightmapST.zw;
	#define GI_FRAGMENT_DATA(input) input.lightMapUV //A variable so no ";"
#else
	#define GI_ATTRIBUTE_DATA
	#define GI_VARYINGS_DATA
	#define TRANSFER_GI_DATA(input, output)
	#define GI_FRAGMENT_DATA(input) 0.0
#endif

struct GI
{
	float3 diffuse;
};

float3 SampleLightMap(float2 lightMapUV)
{
	#if defined(LIGHTMAP_ON)
	return SampleSingleLightmap(
		TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),//pass the lighttex and sample as to args
		lightMapUV,//lightmap UV
		float4(1.0,1.0,0.0,0.0),//UV transformation(We have done this in the TRANSFER_GI_DATA)
		#if defined(UNTIY_LIGHTMAP_FULL_HDR)//Is the texture compressed
			false,
		#else
			true,
		#endif
		float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
		);
	#else
	return 0.0;
	#endif
}

float3 SampleLightProbe(Surface surfaceWS)
{
	#if defined(LIGHTMAP_ON)
		return 0.0;
	#else
		if (unity_ProbeVolumeParams.x)
		{
			return SampleProbeVolumeSH4(
				TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
				surfaceWS.position, surfaceWS.normal,
				unity_ProbeVolumeWorldToObject,
				unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
				unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
			);//sample the LPPV 
		}
		float4 coefficient[7];
		coefficient[0] = unity_SHAr;
		coefficient[1] = unity_SHAg;
		coefficient[2] = unity_SHAb;
		coefficient[3] = unity_SHBr;
		coefficient[4] = unity_SHBg;
		coefficient[5] = unity_SHBb;
		coefficient[6] = unity_SHC;
		return max(0.0, SampleSH9(coefficient, surfaceWS.normal));
		//sample the probe based on normalWS 
	#endif
}

GI GetGI(float2 lightMapUV, Surface surfaceWS)
{
	GI gi;
	gi.diffuse = SampleLightMap(lightMapUV) + SampleLightProbe(surfaceWS);
	return gi;
}




#endif