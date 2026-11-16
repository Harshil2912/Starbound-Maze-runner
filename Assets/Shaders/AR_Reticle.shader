Shader "Custom/AR_Reticle"
{
    Properties
    {
        _RingColor ("Ring Color", Color) = (0.20, 0.90, 1.0, 1.0)
        _CenterColor ("Center Dot Color", Color) = (0.30, 1.0, 0.85, 1.0)
        _RingRadius ("Ring Radius", Range(0.1, 0.5)) = 0.35
        _RingThickness ("Ring Thickness", Range(0.01, 0.1)) = 0.03
        _PulseSpeed ("Pulse Speed", Float) = 3.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+10" 
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
                float2 uv           : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _RingColor;
                float4 _CenterColor;
                float  _RingRadius;
                float  _RingThickness;
                float  _PulseSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 centerUV = input.uv - 0.5;
                float dist = length(centerUV);

                // Animated breathing pulse
                float pulse = sin(_Time.y * _PulseSpeed) * 0.025;
                float currentRadius = _RingRadius + pulse;

                // Outer Ring (Cyan / Teal)
                float ringDist = abs(dist - currentRadius);
                float ring = 1.0 - smoothstep(0.0, _RingThickness, ringDist);

                // Center Dot
                float dotRadius = 0.04;
                float centerDot = 1.0 - smoothstep(0.0, dotRadius, dist);

                // 4 Corner Alignment Ticks
                float angle = atan2(centerUV.y, centerUV.x);
                float ticks = cos(angle * 4.0);
                float tickMask = smoothstep(0.92, 1.0, ticks) * (1.0 - smoothstep(currentRadius - 0.06, currentRadius + 0.06, dist));

                // High-contrast dark shadow / rim backdrop for readability on pure white floors/tables
                float shadowThickness = _RingThickness * 2.6;
                float shadowRing = 1.0 - smoothstep(0.0, shadowThickness, ringDist);
                float shadowDot = 1.0 - smoothstep(0.0, dotRadius * 2.2, dist);
                float shadowTicks = smoothstep(0.85, 1.0, ticks) * (1.0 - smoothstep(currentRadius - 0.08, currentRadius + 0.08, dist));
                float shadowMask = saturate(shadowRing * 0.75 + shadowDot * 0.75 + shadowTicks * 0.65);

                // Foreground illuminated reticle elements
                half4 fgColor = _RingColor * ring + _CenterColor * centerDot + _RingColor * tickMask;
                float fgAlpha = saturate(ring + centerDot + tickMask);

                // Composite: Dark contrasting border behind the vibrant emissive reticle
                half3 darkBackdrop = half3(0.03, 0.06, 0.10);
                half3 finalRGB = lerp(darkBackdrop, fgColor.rgb, fgAlpha);
                float finalAlpha = saturate(fgAlpha + shadowMask * 0.78);

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
