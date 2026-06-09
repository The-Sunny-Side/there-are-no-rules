Shader "Custom/TutorialOverlay"
{
    Properties
    {
        // Richiesta da Unity UI — non usata ma evita il warning _MainTex
        _MainTex       ("Texture", 2D)              = "white" {}
        _Color         ("Overlay Color", Color)      = (0, 0, 0, 0.6)
        _HoleCenter    ("Hole Center (0-1)", Vector)  = (0.5, 0.5, 0, 0)
        _HoleRadius    ("Hole Radius (norm)", Float)  = 0.15
        _HoleSoftness  ("Hole Softness", Range(0.001, 0.15)) = 0.03
        _GlowColor     ("Glow Color", Color)         = (0.4, 1.0, 0.95, 1)
        _GlowWidth     ("Glow Width (norm)", Range(0.0, 0.1)) = 0.03
        _GlowIntensity ("Glow Intensity", Range(0.0, 3.0))    = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
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
                // UV passate dal vertex shader:
                // (0,0) = bottom-left, (1,1) = top-right
                // coerenti con WorldToScreenPoint / Screen.height usati nel C#
                // Non dipendono dall'origine dei pixel coordinates (OpenGL ES vs Vulkan)
                float2 screenUV    : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
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
                // Le UV di Unity UI sul quad vanno da (0,0) a (1,1) con Y=0 in basso
                // Stesso sistema di riferimento di Screen.height/WorldToScreenPoint
                // Nessun flip necessario: funziona uguale su OpenGL ES e Vulkan
                OUT.screenUV = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.screenUV;

                // Aspect ratio: corregge il delta X per ottenere un buco circolare
                // _ScreenParams.x/y = risoluzione fisica del device, corretta su Android
                float aspect = _ScreenParams.x / _ScreenParams.y;

                float2 delta = uv - _HoleCenter.xy;
                delta.x *= aspect;
                float d = length(delta);

                // Overlay con buco: hole=0 dentro il cerchio, hole=1 fuori
                float hole = smoothstep(
                    _HoleRadius - _HoleSoftness,
                    _HoleRadius + _HoleSoftness,
                    d
                );

                // Glow attorno al bordo
                float glowInner   = _HoleRadius + _HoleSoftness;
                float glowOuter   = glowInner + _GlowWidth;
                float glowMask    = smoothstep(glowInner, glowInner + 0.005, d)
                                  * smoothstep(glowOuter, glowOuter - 0.005, d);
                float glowFalloff = 1.0 - smoothstep(glowInner, glowOuter, d);
                float glow        = glowMask * glowFalloff * _GlowIntensity;

                float3 col = lerp(_GlowColor.rgb * glow, _Color.rgb, saturate(hole - glow * 0.5));
                float  a   = saturate(_Color.a * hole + glow * _GlowColor.a * (1.0 - hole * 0.8));

                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
