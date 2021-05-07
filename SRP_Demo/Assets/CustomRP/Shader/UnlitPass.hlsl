#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED


struct Attributes
{
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	float3 normalOS : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID//this will store the instance ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	float3 normalWS : VAR_NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
	Varyings output;
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//transfer instance ID to frag
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	//transform UV based on permaterial ST
	output.baseUV = TransformBaseUV(input.baseUV);

	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	//transfer normal
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET 
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);

	//use the new packed config instead of UV
	InputConfig config = GetInputConfig(input.baseUV);

	//get the basemap * basecolor
	float4 Color = GetBase(config);

#if defined(_CLIPPING)
	clip(Color.a - GetCutoff(config));
#endif
	return Color;
}

#endif
