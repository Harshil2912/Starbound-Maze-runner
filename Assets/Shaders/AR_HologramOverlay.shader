Shader "Custom/AR_HologramOverlay"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Holo Base Color", Color) = (0.12, 0.32, 0.38, 0.78)
        _RimColor ("Rim Glow Color", Color) = (0.20, 0.65, 0.75, 1.0)
        _RimPower ("Rim Power", Range(0.5, 6.0)) = 2.8
        _ScanColor ("Scan Beam Color", Color) = (0.30, 0.70, 0.78, 1.0)
        _ScanSpeed ("Scan Speed", Float) = 0.85
        _ScanFreq ("Scan Frequency", Float) = 0.08
        _ScanSharpness ("Scan Sharpness", Range(1.0, 30.0)) = 16.0
        _ScanIntensity ("Scan Intensity", Range(0.0, 3.0)) = 0.22
        _TextureMix ("Texture Detail Mix", Range(0.0, 1.0)) = 0.72
        _HoloAlpha ("Hologram Opacity", Range(0.1, 1.0)) = 0.76
        [HDR] _EmissionColor ("Holo Emission Color", Color) = (0, 0, 0, 0)
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
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _RimColor;
                float4 _ScanColor;
                float4 _EmissionColor;
                float  _RimPower;
                float  _ScanSpeed;
                float  _ScanFreq;
                float  _ScanSharpness;
                float  _ScanIntensity;
                float  _TextureMix;
                float  _HoloAlpha;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalize(normalInput.normalWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // Normal & View direction in world space
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                // Sample base texture
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // Soft ambient shading for natural depth (directional light from above-side)
                float ambientShade = 0.78 + 0.22 * saturate(dot(normalWS, float3(0.2, 0.9, 0.3)));

                // Subtle Fresnel / Rim glow for clean, soft glass edges (balanced and soft)
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _RimPower);
                half3 rimGlow = fresnel * _RimColor.rgb * 0.50;

                // Subtle, elegant horizontal scan sheen (scale-adapted with diagonal projection so rotated mazes don't stall)
                float scan = sin((input.positionWS.z + input.positionWS.x * 0.25) * _ScanFreq - _Time.y * _ScanSpeed);
                float scanBeam = pow(saturate(scan), _ScanSharpness) * _ScanIntensity;
                half3 beamGlow = scanBeam * _ScanColor.rgb * 0.45;

                // Modulate underlying texture with holographic teal palette
                half3 texturedBase = tex.rgb * _BaseColor.rgb * 1.4;
                half3 surfaceColor = lerp(_BaseColor.rgb, texturedBase, _TextureMix) * ambientShade;

                // Balanced composite (+ optional category HDR glow for hazards, lasers, portals, pickups)
                half3 customEmission = _EmissionColor.rgb * (0.45 + 0.55 * fresnel);
                half3 finalRGB = surfaceColor + rimGlow + beamGlow + customEmission;

                // Balanced glass opacity
                float emitBoost = saturate(max(_EmissionColor.r, max(_EmissionColor.g, _EmissionColor.b)) * 0.08);
                float alpha = saturate(_HoloAlpha + fresnel * 0.18 + scanBeam * 0.12 + emitBoost);

                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
