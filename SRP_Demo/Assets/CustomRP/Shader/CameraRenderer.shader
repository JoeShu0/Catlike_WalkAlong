﻿Shader "Custom_RP/CameraRenderer"
{
	SubShader
	{
		Cull Off
		ZTest Always
		ZWrite Off


		HLSLINCLUDE
		#include "../ShaderLib/Common.hlsl"
		#include "CameraRendererPasses.hlsl"
		ENDHLSL
		 
		Pass {
			Name "Copy"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment CopyPassFragment
			ENDHLSL
		}
	}
}