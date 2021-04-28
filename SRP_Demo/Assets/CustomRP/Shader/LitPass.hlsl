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
	GI_ATTRIBUTE_DATA//this is to have the lightmap UV
	UNITY_VERTEX_INPUT_INSTANCE_ID//this will store the instance ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	float3 normalWS : VAR_NORMAL;
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

	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);

	//transfer normal
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET 
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	
	//get the basemap * basecolor
	float4 base = GetBase(input.baseUV);
	
#if defined(_CLIPPING)
	clip(base.a - GetCutoff(input.baseUV));
#endif

	Surface surface;
	surface.position = input.positionWS;
	surface.normal = normalize(input.normalWS.xyz);
	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = GetMetallic(input.baseUV);
	surface.smoothness = GetSmoothness(input.baseUV);
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	surface.depth = -TransformWorldToView(input.positionWS).z;
	surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

#if defined(_PREMULTIPLY_ALPHA)
	BRDF brdf = GetBRDF(surface, true);
#else
	BRDF brdf = GetBRDF(surface);
#endif

	//get the lightmap UV = GI_FRAGMENT_DATA(input)
	GI gi = GetGI(GI_FRAGMENT_DATA(input), surface);

	float3 color = GetLighting(surface, brdf, gi);

	//emission
	color += GetEmission(input.baseUV);

	return float4(color, surface.alpha);
}

#endif
