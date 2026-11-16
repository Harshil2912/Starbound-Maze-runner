Shader "Custom/AR_ShadowCatcher"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0.0, 0.0, 0.0, 0.45)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent-1" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ShadowCatcher"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                // Shadow attenuation: 1 = unshadowed (clear table), 0 = full shadow
                float shadow = mainLight.shadowAttenuation;
                float shadowStrength = (1.0 - shadow) * _ShadowColor.a;

                return half4(_ShadowColor.rgb, shadowStrength);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

