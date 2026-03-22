Shader "Hidden/Outline"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Outline"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            // These helpers expose SampleSceneDepth() and SampleSceneNormals()
            // They also handle correct texture declarations for URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float  _Thickness;
            float  _DepthThreshold;
            float  _NormalThreshold;

            // Sobel kernel offsets and weights
            static const float2 kOffsets[9] =
            {
                float2(-1,-1), float2( 0,-1), float2( 1,-1),
                float2(-1, 0), float2( 0, 0), float2( 1, 0),
                float2(-1, 1), float2( 0, 1), float2( 1, 1)
            };
            static const float kWeightsX[9] = { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
            static const float kWeightsY[9] = { -1,-2,-1,  0, 0, 0,  1, 2, 1 };

            float SobelDepth(float2 uv, float2 ts)
            {
                float gx = 0, gy = 0;
                UNITY_UNROLL
                for (int i = 0; i < 9; i++)
                {
                    float d = Linear01Depth(
                        SampleSceneDepth(uv + kOffsets[i] * ts), _ZBufferParams);
                    gx += d * kWeightsX[i];
                    gy += d * kWeightsY[i];
                }
                return sqrt(gx * gx + gy * gy);
            }

            float SobelNormals(float2 uv, float2 ts)
            {
                float3 gx = 0, gy = 0;
                UNITY_UNROLL
                for (int i = 0; i < 9; i++)
                {
                    float3 n = SampleSceneNormals(uv + kOffsets[i] * ts);
                    gx += n * kWeightsX[i];
                    gy += n * kWeightsY[i];
                }
                return sqrt(dot(gx, gx) + dot(gy, gy));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                // _BlitTexture_TexelSize.xy = 1/screenWidth, 1/screenHeight
                float2 ts = _BlitTexture_TexelSize.xy * _Thickness;

                // Scene color comes from the temp copy set as _BlitTexture by Blitter
                half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                float edgeDepth   = SobelDepth  (uv, ts);
                float edgeNormals = SobelNormals(uv, ts);

                float edge = max(step(_DepthThreshold,  edgeDepth),
                                 step(_NormalThreshold, edgeNormals));

                return lerp(sceneColor, _OutlineColor, edge * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}
