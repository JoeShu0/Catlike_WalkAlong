#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../ShaderLib/Surface.hlsl"
#include "../ShaderLib/Shadows.hlsl"
#include "../ShaderLib/Light.hlsl"
#include "../ShaderLib/BRDF.hlsl"
#include "../ShaderLib/GI.hlsl"
#include "../ShaderLib/Lighting.hlsl"

struct Attributes
{
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	GI_ATTRIBUTE_DATA//this is to have the lightmap UV
	UNITY_VERTEX_INPUT_INSTANCE_ID//this will store the instance ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	float2 detailUV : VAR_DETAIL_UV;
	float3 normalWS : VAR_NORMAL;
	#if defined(_NORMAL_MAP)
		float4 tangentWS : VAR_TANGENT;
	#endif
	float3 positionWS : VAR_POSITION;
	GI_VARYINGS_DATA//this is to have the lightmap UV
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{
	Varyings output;
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//transfer instance ID to frag
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	//transfer lightmap UV
	TRANSFER_GI_DATA(input, output);

	//transform UV based on permaterial ST
	output.baseUV = TransformBaseUV(input.baseUV);
	output.detailUV = TransformDetailUV(input.baseUV);

	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);

	//transfer normal
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);

	#if defined(_NORMAL_MAP)
		//transfer tangent
		output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
	#endif

	return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET 
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);

	//LOD fade, the fade factor is in x com of unity_LODFade
	ClipLOD(input.positionCS.xy, unity_LODFade.x);
	
	//get the basemap * basecolor
	float4 base = GetBase(input.baseUV, input.detailUV);
	
#if defined(_CLIPPING)
	clip(base.a - GetCutoff(input.baseUV));
#endif

	Surface surface;
	surface.position = input.positionWS;
	#if defined(_NORMAL_MAP)
		surface.interpolatedNormalWS = input.normalWS;
		surface.normal = NormalTangentToWorld(GetNormalTS(input.baseUV, input.detailUV), input.normalWS, input.tangentWS);
	#else
		surface.normal = normalize(input.normalWS);
		surface.interpolatedNormalWS = surface.normal;
	#endif
	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = GetMetallic(input.baseUV);
	surface.occlusion = GetOcclusion(input.baseUV);
	surface.smoothness = GetSmoothness(input.baseUV, input.detailUV);
	surface.fresnelStrength = GetFresnel(input.baseUV);
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	surface.depth = -TransformWorldToView(input.positionWS).z;
	surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

#if defined(_PREMULTIPLY_ALPHA)
	BRDF brdf = GetBRDF(surface, true);
#else
	BRDF brdf = GetBRDF(surface);
#endif

	//get the lightmap UV = GI_FRAGMENT_DATA(input)
	GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
	//shadow mask debug
	//return  gi.shadowMask.shadows;
	
	float3 color = GetLighting(surface, brdf, gi);

	//emission
	color += GetEmission(input.baseUV);

	//return float4(surface.normal,1.0f);

	return float4(color, surface.alpha);
}

#endif
