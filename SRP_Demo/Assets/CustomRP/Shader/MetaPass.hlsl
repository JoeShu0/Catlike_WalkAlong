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
	float4 positionCS_SS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	float2 detailUV : VAR_DETAIL_UV;
};

Varyings MetaPassVertex(Attributes input)
{
	Varyings output;

	//lightmap stuff????
	input.positionOS.xy =
		input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
	input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
	output.positionCS_SS = TransformWorldToHClip(input.positionOS);

	//transform UV based on permaterial ST
	output.baseUV = TransformBaseUV(input.baseUV);
	output.detailUV = TransformDetailUV(input.baseUV);

	return output;
}

float4 MetaPassFragment(Varyings input) : SV_TARGET
{
	//use the new packed config instead of UV
	InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV, input.detailUV);

	//get the basemap * basecolor + Detail color
	float4 base = GetBase(config);

	Surface surface;
	ZERO_INITIALIZE(Surface, surface);
	surface.color = base.rgb;
	surface.metallic = GetMetallic(config);
	surface.smoothness = GetSmoothness(config);
	BRDF brdf = GetBRDF(surface);

	float4 meta = 0.0;
	if (unity_MetaFragmentControl.x)
	{
		meta = float4(brdf.diffuse, 1.0);
		meta.rgb += brdf.specular * brdf.roughness * 0.5f;
		meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
	}
	else if (unity_MetaFragmentControl.y)//separate emission pass
	{
		meta = float4(GetEmission(config), 1.0);
		//meta = float4(1.0,1.0,1.0,1.0);
	}
	//meta = float4(1.0,0.0,0.0,1.0);
	return meta;
}

#endif