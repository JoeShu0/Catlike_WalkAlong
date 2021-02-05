#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLib/Common.hlsl"
/*
//Enable unity SRP Batcher forbuffer per material
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
CBUFFER_END
*/
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
// the error assert 0==m_CurrentBuildInBindMask may cased by the GPU instance option os not on in the material


struct Attributes
{
	float3 positionOS : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID//this will store the instance ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
	Varyings output;
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	//transfer instance ID to frag
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET 
{
	//Setup the instance ID for Input
	UNITY_SETUP_INSTANCE_ID(input);
	float4 Color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	return Color;
}

#endif
