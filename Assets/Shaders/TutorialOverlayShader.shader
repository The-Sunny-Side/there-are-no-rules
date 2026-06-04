Shader "Custom/TutorialOverlay"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0, 0, 0, 0.6)
        _HoleCenter ("Hole Center (normalized 0-1)", Vector) = (0.5, 0.5, 0, 0)
        _HoleRadius ("Hole Radius (normalized)", Float) = 0.15
        _HoleSoftness ("Hole Softness", Range(0.001, 0.15)) = 0.03
        _GlowColor ("Glow Color", Color) = (0.4, 1.0, 0.95, 1)
        _GlowWidth ("Glow Width (normalized)", Range(0.0, 0.1)) = 0.03
        _GlowIntensity ("Glow Intensity", Range(0.0, 3.0)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _HoleCenter;
                float  _HoleRadius;
                float  _HoleSoftness;
                float4 _GlowColor;
                float  _GlowWidth;
                float  _GlowIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Screen UV reali da posizione pixel, indipendenti dal RectTransform
                float2 screenUV = IN.positionHCS.xy / _ScreenParams.xy;
                // Flip Y: SV_POSITION ha Y=0 in alto in URP, _HoleCenter ha Y=0 in basso
                screenUV.y = 1.0 - screenUV.y;

                // Correzione aspect ratio per buco circolare
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 delta = screenUV - _HoleCenter.xy;
                delta.x *= aspect;
                float d = length(delta);

                // Buco
                float hole = smoothstep(
                    _HoleRadius - _HoleSoftness,
                    _HoleRadius + _HoleSoftness,
                    d
                );

                // Glow attorno al bordo
                float glowInner   = _HoleRadius + _HoleSoftness;
                float glowOuter   = glowInner + _GlowWidth;
                float glow        = smoothstep(glowInner, glowInner + 0.005, d)
                                  * smoothstep(glowOuter, glowOuter - 0.005, d);
                float glowFalloff = 1.0 - smoothstep(glowInner, glowOuter, d);
                glow *= glowFalloff * _GlowIntensity;

                float3 col = lerp(_GlowColor.rgb * glow, _Color.rgb, saturate(hole - glow * 0.5));
                float  a   = saturate(_Color.a * hole + glow * _GlowColor.a * (1.0 - hole * 0.8));

                return half4(col, a);
            }
            ENDHLSL
        }
    }
}