Shader "Custom/HaloGlow"
{
    Properties
    {
        [HDR] _BaseColor ("Halo Color", Color) = (1.0, 0.12, 0.08, 0.75)
        _GlowIntensity ("Glow Intensity", Range(0.1, 6.0)) = 2.0
        _FalloffPower ("Falloff Power", Range(0.5, 6.0)) = 2.0
        _PulseSpeed ("Pulse Speed", Float) = 2.4
        _PulseAmount ("Pulse Amount", Range(0.0, 0.5)) = 0.14
        _ShapeMode ("Shape Mode (0=Sphere 1=RingDisc 2=BoxRim 3=LaserBeam)", Float) = 0
        _Cull ("Cull Mode (2=Back 1=Front 0=Off)", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+5"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One
        ZWrite Off
        ZTest LEqual
        Cull [_Cull]

        Pass
        {
            Name "HaloForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _GlowIntensity;
                float  _FalloffPower;
                float  _PulseSpeed;
                float  _PulseAmount;
                float  _ShapeMode;
                float  _Cull;
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
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                float ndv = saturate(abs(dot(normalWS, viewDirWS)));

                float shapeAlpha = 0.0;

                if (_ShapeMode < 0.5)
                {
                    // Mode 0: Soft 3D spherical volumetric aura (zero hard silhouette edge, clear core)
                    float outerFade = pow(ndv, _FalloffPower);
                    float centerHollow = 1.0 - 0.76 * pow(ndv, 2.4);
                    shapeAlpha = outerFade * centerHollow;
                }
                else if (_ShapeMode < 1.5)
                {
                    // Mode 1: Crisp portal halo ring around perimeter (keeps inner disc crisp)
                    float r = length(input.uv - 0.5) * 2.0;
                    float edgeClip = 1.0 - smoothstep(0.84, 1.0, r);
                    float innerClip = smoothstep(0.32, 0.52, r);
                    float ringGlow = exp(-pow((r - 0.68) / 0.17, 2.0));
                    shapeAlpha = ringGlow * edgeClip * innerClip;
                }
                else if (_ShapeMode < 2.5)
                {
                    // Mode 2: Softened box rim silhouette fading toward quad edges
                    float2 uvEdge = abs(input.uv - 0.5) * 2.0;
                    float edgeFade = (1.0 - smoothstep(0.55, 1.0, uvEdge.x)) * (1.0 - smoothstep(0.55, 1.0, uvEdge.y));
                    shapeAlpha = pow(ndv, _FalloffPower) * edgeFade;
                }
                else
                {
                    // Mode 3: Laser beam volumetric glow sheath across LineRenderer ribbon
                    float across = abs(input.uv.y - 0.5) * 2.0;
                    float tipFade = smoothstep(0.0, 0.035, input.uv.x) * smoothstep(1.0, 0.965, input.uv.x);
                    float radial = exp(-across * across * 3.8) * saturate(1.0 - pow(across, 3.0));
                    shapeAlpha = radial * tipFade;
                }

                float finalAlpha = saturate(shapeAlpha * _BaseColor.a * pulse);
                half3 finalRGB = _BaseColor.rgb * (_GlowIntensity * pulse);
                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
