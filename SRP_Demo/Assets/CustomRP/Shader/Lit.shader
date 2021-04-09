﻿Shader "Custom_RP/Lit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "White"{}
        _BaseColor("Color", Color) = (0.5,0.5,0.5,1.0)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0 //this is a toggle, on shader will have "_Clipping", defined
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha("Premultiply Alpha", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
    }
    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "CustomLit"//indicate we are using custom lighting model
            }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _CLIPPING//make unity complie 2 shader with and without _CLIPPING define
            #pragma shader_feature _PREMULTIPLY_ALPHA
            //multicompile for shadow sample "_ " means use PCF2x2 when no keywords
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7 
            //multicompile for shadow cascade blending
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile_instancing//make unity complie 2 shader with and without GPU instancing
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #include "LitPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                 "LightMode" = "ShadowCaster"//add a pass, only shader with this pass is drawn in shadow buffer
            }

            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _CLIPPING//make unity complie 2 shader with and without _CLIPPING define
            #pragma multi_compile_instancing//make unity complie 2 shader with and without GPU instancing
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "CustomShaderGUI"
}
