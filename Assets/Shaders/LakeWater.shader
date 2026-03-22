Shader "Custom/LakeWater"
{
    Properties
    {
        [HDR] _ShallowColor ("Shallow Color", Color) = (0.19, 0.46, 0.48, 0.78)
        [HDR] _DeepColor ("Deep Color", Color) = (0.04, 0.16, 0.21, 0.88)
        [HDR] _FoamColor ("Foam Color", Color) = (0.79, 0.91, 0.96, 1.0)
        [HDR] _ReflectionColor ("Reflection Tint", Color) = (0.36, 0.54, 0.68, 1.0)
        _Opacity ("Opacity", Range(0, 1)) = 0.78
        _DepthFadeDistance ("Depth Fade Distance", Range(0.1, 20)) = 4.0
        _FoamDistance ("Foam Distance", Range(0.01, 4)) = 0.65
        _ShorelineWashDistance ("Shoreline Wash Distance", Range(0.1, 8)) = 1.8
        _ShorelineWashStrength ("Shoreline Wash Strength", Range(0, 2)) = 0.95
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.4)) = 0.08
        _WaveFrequency ("Wave Frequency", Range(0.1, 4)) = 1.25
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 0.45
        _NormalStrength ("Normal Strength", Range(0, 1.5)) = 0.7
        _NormalTiling ("Normal Tiling", Range(0.05, 4)) = 0.55
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 4.2
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                half4 _ReflectionColor;
                half _Opacity;
                half _DepthFadeDistance;
                half _FoamDistance;
                half _ShorelineWashDistance;
                half _ShorelineWashStrength;
                half _WaveAmplitude;
                half _WaveFrequency;
                half _WaveSpeed;
                half _NormalStrength;
                half _NormalTiling;
                half _FresnelPower;
                half _FresnelStrength;
            CBUFFER_END

            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash12(i);
                float b = Hash12(i + float2(1.0, 0.0));
                float c = Hash12(i + float2(0.0, 1.0));
                float d = Hash12(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float FractalNoise(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                [unroll(3)]
                for (int octave = 0; octave < 3; octave++)
                {
                    value += Noise(p) * amplitude;
                    p = p * 2.03 + 17.13;
                    amplitude *= 0.5;
                }

                return value;
            }

            float WaveHeight(float2 worldXZ, float time)
            {
                float2 uv = worldXZ * _NormalTiling;
                float primary = sin((uv.x + uv.y * 0.8 + time * _WaveSpeed) * _WaveFrequency);
                float secondary = sin((uv.x * -0.7 + uv.y * 1.2 - time * _WaveSpeed * 1.3) * (_WaveFrequency * 1.5));
                float ripples = FractalNoise(uv * 1.8 + time * _WaveSpeed * 0.35) * 2.0 - 1.0;
                return (primary * 0.55 + secondary * 0.3 + ripples * 0.15) * _WaveAmplitude;
            }

            float3 WaveNormal(float2 worldXZ, float time)
            {
                const float sampleOffset = 0.2;

                float heightCenter = WaveHeight(worldXZ, time);
                float heightX = WaveHeight(worldXZ + float2(sampleOffset, 0.0), time);
                float heightZ = WaveHeight(worldXZ + float2(0.0, sampleOffset), time);

                float3 tangentX = normalize(float3(sampleOffset, (heightX - heightCenter) * _NormalStrength, 0.0));
                float3 tangentZ = normalize(float3(0.0, (heightZ - heightCenter) * _NormalStrength, sampleOffset));
                return normalize(cross(tangentZ, tangentX));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                worldPos.y += WaveHeight(worldPos.xz, _Time.y);

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.viewDirWS = GetWorldSpaceViewDir(worldPos);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);

                #if UNITY_REVERSED_Z
                if (rawDepth <= 0.0001)
                {
                    discard;
                }
                #else
                if (rawDepth >= 0.9999)
                {
                    discard;
                }
                #endif

                float sceneEyeDepth = unity_OrthoParams.w == 0 ? LinearEyeDepth(rawDepth, _ZBufferParams) : LinearDepthToEyeDepth(rawDepth);
                float surfaceEyeDepth = LinearEyeDepth(input.positionCS.z / input.positionCS.w, _ZBufferParams);
                float waterDepth = sceneEyeDepth - surfaceEyeDepth;

                if (waterDepth <= 0.0)
                {
                    discard;
                }

                float time = _Time.y;
                float depthFactor = saturate(waterDepth / max(_DepthFadeDistance, 0.001));
                float foamBand = 1.0 - smoothstep(0.0, max(_FoamDistance, 0.001), waterDepth);
                float foamNoise = smoothstep(0.42, 0.8, FractalNoise(input.positionWS.xz * 1.5 + time * 0.3));
                float shorelineDistance = max(_ShorelineWashDistance, _FoamDistance);
                float shorelineMask = 1.0 - smoothstep(0.0, shorelineDistance, waterDepth);
                float shorelineNoise = FractalNoise(input.positionWS.xz * 0.18 + float2(time * 0.06, -time * 0.04));
                float shorelinePhase = (waterDepth / shorelineDistance) * 2.35 - time * max(_WaveSpeed, 0.05) * 0.55 + shorelineNoise * 0.45;
                float shorelineWave = abs(frac(shorelinePhase) * 2.0 - 1.0);
                float shorelineBreak = shorelineMask * smoothstep(0.32, 0.0, shorelineWave);
                shorelineBreak *= saturate((1.0 - depthFactor) * 1.2) * _ShorelineWashStrength;
                float foamMask = saturate(max(foamBand * foamNoise, shorelineBreak));

                float3 normalWS = WaveNormal(input.positionWS.xz, time);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower) * _FresnelStrength;

                half3 baseColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);
                baseColor = lerp(baseColor, _ReflectionColor.rgb, saturate(fresnel));
                baseColor += foamBand * foamNoise * _FoamColor.rgb * 0.3;
                baseColor += shorelineBreak * _FoamColor.rgb * 0.65;
                baseColor = lerp(baseColor, _FoamColor.rgb, shorelineBreak * 0.18);

                half alpha = saturate(_Opacity * lerp(0.45, 1.0, depthFactor) + fresnel * 0.18 + foamMask * 0.22);
                half3 finalColor = MixFog(baseColor, input.fogFactor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
