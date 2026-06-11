Shader "Custom/FlagWave"
{
    Properties
    {
        [Toggle] _UseBackgroundTex ("Usa Texture Sfondo", Float) = 0
        _BackgroundColor ("Sfondo Colore", Color) = (1,0,0,1)
        _BackgroundTex   ("Sfondo Texture", 2D) = "white" {}

        _ContentTex ("Contenuto Texture", 2D) = "white" {}

        _WaveAmplitude ("Wave Amplitude", Float) = 0.08
        _WaveFrequency ("Wave Frequency", Float) = 3.0
        _WaveSpeed     ("Wave Speed",     Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex   vert
            #pragma fragment frag
            #pragma shader_feature_local _USEBACKGROUNDTEX_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BackgroundTex); SAMPLER(sampler_BackgroundTex);
            TEXTURE2D(_ContentTex);    SAMPLER(sampler_ContentTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BackgroundTex_ST;
                float4 _ContentTex_ST;
                half4  _BackgroundColor;
                float  _WaveAmplitude;
                float  _WaveFrequency;
                float  _WaveSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.positionOS.xyz;

                float influence = IN.uv.x;
                float wave = sin(pos.x * _WaveFrequency + _Time.y * _WaveSpeed)
                             * _WaveAmplitude * influence;
                pos.z += wave;

                OUT.positionHCS = TransformObjectToHClip(float4(pos, 1.0));
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN, half facing : VFACE) : SV_Target
            {
                // --- Sfondo (identico su entrambi i lati) ---
                half4 bg;
                #if _USEBACKGROUNDTEX_ON
                    float2 bgUV = TRANSFORM_TEX(IN.uv, _BackgroundTex);
                    bg = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, bgUV);
                #else
                    bg = _BackgroundColor;
                #endif

                // --- Contenuto: flip U sul back face ---
                float2 contentUV = TRANSFORM_TEX(IN.uv, _ContentTex);
                if (facing < 0)
                    contentUV.x = 1.0 - contentUV.x;

                half4 content = SAMPLE_TEXTURE2D(_ContentTex, sampler_ContentTex, contentUV);

                half4 final = lerp(bg, content, content.a);
                final.a = 1.0;
                return final;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}