Shader "Hidden/Custom_RP/Post FX Stack"
{
    SubShader
    {
        Cull off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "../ShaderLib/Common.hlsl"
        #include "PostFXStackPasses.hlsl"
        ENDHLSL

        Pass
        {
            Name "Copy"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment CopyPassFragment
            ENDHLSL
        }
    }
}
