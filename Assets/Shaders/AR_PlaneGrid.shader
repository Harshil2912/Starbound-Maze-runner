Shader "Custom/AR_PlaneGrid"
{
    Properties
    {
        _Color ("Dot Color", Color) = (0.86, 0.87, 0.89, 0.36)
        _PlaneColor ("Plane Tint Color", Color) = (0.86, 0.87, 0.89, 0.05)
        _GridSpacing ("Dot Spacing", Float) = 0.075
        _DotRadius ("Dot Radius", Range(0.001, 0.03)) = 0.006
        _EdgeSoftness ("Edge Softness", Range(0.0005, 0.01)) = 0.002
        _FadeRadius ("Soft Radial Falloff", Float) = 0.0
        _AlbedoAlpha ("Debug Albedo Alpha", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float2 localXZ      : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _PlaneColor;
                float  _GridSpacing;
                float  _DotRadius;
                float  _EdgeSoftness;
                float  _FadeRadius;
                float  _AlbedoAlpha;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.localXZ = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // World-space dotted pattern aligned to detected floor/table dimensions
                float spacing = max(_GridSpacing, 0.01);
                float2 cellOffset = frac(input.positionWS.xz / spacing) - 0.5;
                float distToDot = length(cellOffset * spacing);
                float softness = max(_EdgeSoftness, 0.0005);
                float dotMask = 1.0 - smoothstep(_DotRadius, _DotRadius + softness, distToDot);

                float fade = 1.0;
                if (_FadeRadius > 0.01)
                {
                    float dist = length(input.localXZ);
                    fade = saturate(1.0 - smoothstep(_FadeRadius * 0.5, _FadeRadius, dist));
                }

                half4 col = lerp(_PlaneColor, _Color, dotMask);
                col.a *= fade * saturate(_AlbedoAlpha);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
