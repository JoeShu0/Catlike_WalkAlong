#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "../ShaderLib/Surface.hlsl"
#include "../ShaderLib/Shadows.hlsl"
#include "../ShaderLib/Light.hlsl"
#include "../ShaderLib/BRDF.hlsl"

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

struct Attributes
{
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	float2 lightMapUV : TEXCOORD1;//lightMap UV
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
};

Varyings MetaPassVertex(Attributes input)
{
	Varyings output;

	//lightmap stuff????
	input.positionOS.xy =
		input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
	input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
	output.positionCS = TransformWorldToHClip(input.positionOS);

	//transform UV based on permaterial ST
	output.baseUV = TransformBaseUV(input.baseUV);

	return output;
}

float4 MetaPassFragment(Varyings input) : SV_TARGET
{
	float4 base = GetBase(input.baseUV);
	Surface surface;
	ZERO_INITIALIZE(Surface, surface);
	surface.color = base.rgb;
	surface.metallic = GetMetallic(input.baseUV);
	surface.smoothness = GetSmoothness(input.baseUV);
	BRDF brdf = GetBRDF(surface);

	float4 meta = 0.0;
	if (unity_MetaFragmentControl.x)
	{
		meta = float4(brdf.diffuse, 1.0);
		meta.rgb += brdf.specular * brdf.roughness * 0.5f;
		meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
	}
	//meta = float4(1.0,0.0,0.0,1.0);
	return meta;
}

#endif