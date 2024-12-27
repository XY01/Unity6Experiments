Shader "XY01/VolumetricFog"
{
    Properties
    {       
        _Color("Color", Color) = (1, 1, 1, 1)
        _MaxDistance("Max Distance", Float) = 100
        _StepSize("Step Size", Range(0.1, 20)) = 1
        _DensityMultiplier("Density Multiplier", Range(0, 10)) = 1
        _NoiseOffset("Noise Offset", float) = 0
        
        _FogNoise("Fog Noise", 3D) = "white" {}
        _NoiseTiling("Noise Tiling", float) = 1
        _DensityThreshold("Density Threshold", Range(0, 1)) = 0.1
        _NoiseSpeed("Noise Speed", float) = 1
        
        _LightScattering("Light Scattering", Range(0,1)) = 0.2
        [HDR] _LightContribution("Light Contribution", Color) = (1, 1, 1, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
      
        
         // Banding removal using interleaved gradient noise
        Pass
        {
            Name "Raymarch3DTextureWithLighting"
            
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            
            float _MaxDistance;
            float _StepSize;
            float _DensityMultiplier;
            float _NoiseOffset;

            TEXTURE3D(_FogNoise);
            float _DensityThreshold;
            float _NoiseTiling;
            float _NoiseSpeed;

            float4 _LightContribution;
            float _LightScattering;

            float henyey_greenstein(float angle, float scattering)
            {
                return (1.0 - angle * angle) / (4.0 * PI * pow(1.0 + scattering * scattering - (2.0 * scattering) * angle, 1.5f));
            }

            float remap(float value, float oldMin, float oldMax, float newMin, float newMax)
            {
                float t = saturate((value - oldMin) / (oldMax - oldMin));
                return saturate(lerp(newMin, newMax, t));
            }

            float GetDensity(float3 worldPos)
            {
                float3 samplePos = worldPos * 0.01 * _NoiseTiling;
                samplePos.y += _Time.y * _NoiseSpeed;
                samplePos.x += _Time.y * _NoiseSpeed * .1;
                float noise = _FogNoise.SampleLevel(sampler_TrilinearRepeat, samplePos, 0);
                noise = 1-saturate(noise);
                //noise = remap(noise,0,1,.2,1);
               // float density = dot(noise.x, noise.x);
                float density = saturate(noise.x - _DensityThreshold) * _DensityMultiplier;

                float neutralPlaneHeight = 1;
                float npScalar = smoothstep(neutralPlaneHeight, neutralPlaneHeight + 1.5, worldPos.y);
                
                return density;// * npScalar;
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                  // Interleaved gradient noise
                float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;
                float distLimit = min(viewLength, _MaxDistance);
                
                // Initialized dist travelled differently for each ray to help remove banding
                int frameCount = (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x));
                float distTravelled = InterleavedGradientNoise(pixelCoords, frameCount) * _NoiseOffset;                
                float transmittance = 1;
                float4 fogCol = _Color;

                while (distTravelled < distLimit)
                {
                    float3 rayPos = entryPoint + rayDir * distTravelled;                    
                    float density = GetDensity(rayPos);
                    if(density > 0 && transmittance > 0)
                    {
                        Light mainLight = GetMainLight(TransformWorldToShadowCoord(rayPos));
                        float lightScattering = henyey_greenstein(dot(rayDir, mainLight.direction), _LightScattering);
                        fogCol.rgb += mainLight.color.rgb * _LightContribution.rgb * lightScattering * density * mainLight.shadowAttenuation * _StepSize;
                        transmittance *= exp(-density * _StepSize);
                    }
                    
                    distTravelled += _StepSize;
                }

                // half4(worldPos, 1);//
                return lerp(col, fogCol, 1.0 - saturate(transmittance));
            }
              
            ENDHLSL
        }
        
         // Banding removal using interleaved gradient noise
        Pass
        {
            Name "Raymarch3DTexture"
            
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            
            float _MaxDistance;
            float _StepSize;
            float _DensityMultiplier;
            float _NoiseOffset;

            TEXTURE3D(_FogNoise);
            float _DensityThreshold;
            float _NoiseTiling;
            float _NoiseSpeed;

            float remap(float value, float oldMin, float oldMax, float newMin, float newMax)
            {
                float t = saturate((value - oldMin) / (oldMax - oldMin));
                return saturate(lerp(newMin, newMax, t));
            }

            float GetDensity(float3 worldPos)
            {
                float3 samplePos = worldPos * 0.01 * _NoiseTiling;
                samplePos.y += _Time.y * _NoiseSpeed;
                samplePos.x += _Time.y * _NoiseSpeed * .1;
                float noise = _FogNoise.SampleLevel(sampler_TrilinearRepeat, samplePos, 0);
                noise = 1-saturate(noise);
                //noise = remap(noise,0,1,.2,1);
               // float density = dot(noise.x, noise.x);
                float density = saturate(noise.x - _DensityThreshold) * _DensityMultiplier;

                float neutralPlaneHeight = 1.5;
                float npScalar = smoothstep(neutralPlaneHeight, neutralPlaneHeight + 0.5, worldPos.y);
                
                return density * npScalar;
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                  // Interleaved gradient noise
                float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;
                float distLimit = min(viewLength, _MaxDistance);
                
                // Initialized dist travelled differently for each ray to help remove banding
                int frameCount = (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x));
                float distTravelled = InterleavedGradientNoise(pixelCoords, frameCount) * _NoiseOffset;                
                float transmittance = 1;

                while (distTravelled < distLimit)
                {
                    float3 rayPos = entryPoint + rayDir * distTravelled;                    
                    float density = GetDensity(rayPos);
                    if(density > 0)
                    {
                        transmittance *= exp(-density * _StepSize);
                    }
                    
                    distTravelled += _StepSize;
                }

                // half4(worldPos, 1);//
                return lerp(col, _Color, 1.0 - saturate(transmittance));
            }
              
            ENDHLSL
        }
        
        // Banding removal using interleaved gradient noise
        Pass
        {
            Name "RaymarchLieanBandingRemoval"
            
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            
            float _MaxDistance;
            float _StepSize;
            float _DensityMultiplier;
            float _NoiseOffset;

            float GetDensity()
            {
                return _DensityMultiplier;
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                  // Interleaved gradient noise
                float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;

                float distLimit = min(viewLength, _MaxDistance);
                // Initialized dist travelled differently for each ray to help remove banding
                int frameCount = (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x));
                float distTravelled = InterleavedGradientNoise(pixelCoords, frameCount) * _NoiseOffset;
                
                float transmittance = 1;

              

                while (distTravelled < distLimit)
                {
                    float density = GetDensity();
                    if(density > 0)
                    {
                        transmittance *= exp(-density * _StepSize);
                    }
                    distTravelled += _StepSize;
                }

                
                return lerp(col, _Color, 1.0 - saturate(transmittance));
            }
              
            ENDHLSL
        }
        
        
        Pass
        {
            Name "RaymarchLieanBeirsLaw" 
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            
            float _MaxDistance;
            float _StepSize;
            float _DensityMultiplier;

            float GetDensity()
            {
                return _DensityMultiplier;
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float distLimit = min(viewLength, _MaxDistance);
                float distTravelled = 0;
                float transmittance = 1;

                while (distTravelled < distLimit)
                {
                    float density = GetDensity();
                    if(density > 0)
                    {
                        transmittance *= exp(-density * _StepSize);
                    }
                    distTravelled += _StepSize;
                }

                
                return lerp(col, _Color, 1.0 - saturate(transmittance));
            }
              
            ENDHLSL
        }
        
        Pass
        {
            Name "RaymarchLieanAccumulation" 
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            
            float _MaxDistance;
            float _StepSize;
            float _DensityMultiplier;

            float GetDensity()
            {
                return _DensityMultiplier;
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float distLimit = min(viewLength, _MaxDistance);
                float distTravelled = 0;
                float transmittance = 0;

                while (distTravelled < distLimit)
                {
                    float density = GetDensity();
                    if(density > 0)
                    {
                        transmittance += density * _StepSize;
                    }
                    distTravelled += _StepSize;
                }

                
                return lerp(col, _Color, saturate(transmittance));
            }
              
            ENDHLSL
        }
        
        
        Pass
        {
            Name "RaymarchBasic" 
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _MaxDistance;
            float _StepSize;
            float _DensityMultiplier;

            float GetDensity()
            {
                return _DensityMultiplier;
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float distLimit = min(viewLength, _MaxDistance);
                float distTravelled = 0;
                float transmittance = 0;

                while (distTravelled < distLimit)
                {
                    float density = GetDensity();
                    if(density > 0)
                    {
                        transmittance += density * _StepSize;
                    }
                    distTravelled += _StepSize;
                }

                
                return transmittance;
            }
              
            ENDHLSL
        }
        
        
        Pass
        {
            Name "WorldSpaceFrac" 
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            half4 Frag(Varyings IN) : SV_Target
            {
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);
                return half4(frac(worldPos), 1.0);
            }
              
            ENDHLSL
        }
        
        
        
        Pass
        {
            Name "DepthSample" 
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            half4 Frag(Varyings IN) : SV_Target
            {
                float depth = SampleSceneDepth(IN.texcoord);
                //return frac(LinearEyeDepth(depth, _ZBufferParams));
                return depth;
            }
            ENDHLSL
        }



        Pass
        {
            Name "InvertPass" 
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 Frag(Varyings IN) : SV_Target
            {
                return 1.0 - SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
            }
            ENDHLSL
        }



        Pass
        {
            Name "Multiply" 
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                return color * color;
            }
            ENDHLSL
        }
    }
}
