Shader "Custom/MarkerCircle"
{
    Properties
    {
        _ColorInner ("Color Inner (core)", Color) = (0.4, 1.0, 0.95, 1)
        _ColorOuter ("Color Outer (rim)", Color) = (0.6, 0.2, 1.0, 1)
        _ColorArc ("Color Arcs", Color) = (1.0, 1.0, 1.0, 1)
        _RingWidth ("Ring Width", Range(0.01, 0.3)) = 0.06
        _PulseSpeed ("Pulse Speed", Range(0.1, 5.0)) = 1.2
        _RotateSpeed ("Rotate Speed", Range(0.0, 5.0)) = 0.5
        _ArcCount ("Arc Count", Range(2, 12)) = 6
        _ArcFill ("Arc Fill", Range(0.1, 0.9)) = 0.55
        _RippleSpeed ("Ripple Speed", Range(0.1, 4.0)) = 1.0
        _GlowIntensity ("Glow Intensity", Range(1.0, 5.0)) = 2.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha One
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorInner;
                float4 _ColorOuter;
                float4 _ColorArc;
                float _RingWidth;
                float _PulseSpeed;
                float _RotateSpeed;
                float _ArcCount;
                float _ArcFill;
                float _RippleSpeed;
                float _GlowIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // Smooth repeat per archi
            float arcMask(float angle, float count, float fill)
            {
                float sector = frac(angle / (2.0 * 3.14159265) * count);
                return smoothstep(0.0, 0.04, sector) * smoothstep(fill + 0.04, fill, sector);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv * 2.0 - 1.0;
                float dist = length(uv);
                float angle = atan2(uv.y, uv.x);

                float t = _Time.y;

                // --- Pulse ---
                float pulse = 1.0 + 0.06 * sin(t * _PulseSpeed * 6.28318);
                float outerR  = 0.72 * pulse;
                float innerR  = outerR - _RingWidth;

                // --- Ring principale con gradiente ciano→viola ---
                float ring = smoothstep(innerR - 0.015, innerR, dist)
                           * smoothstep(outerR + 0.015, outerR, dist);

                // gradiente radiale: ciano al centro del ring, viola verso il bordo
                float ringT = saturate((dist - innerR) / _RingWidth);
                float3 ringColor = lerp(_ColorInner.rgb, _ColorOuter.rgb, ringT);

                // --- Glow morbido attorno al ring ---
                float glowOuter = smoothstep(outerR + 0.25, outerR, dist) * (1.0 - ring);
                float glowInner = smoothstep(innerR - 0.18, innerR, dist) * (1.0 - ring);
                float glow = (glowOuter + glowInner) * 0.35;
                float3 glowColor = lerp(_ColorInner.rgb, _ColorOuter.rgb, saturate(dist / outerR));

                // --- Secondo ring esterno sottile (sfasato nel pulse) ---
                float pulse2 = 1.0 + 0.06 * sin(t * _PulseSpeed * 6.28318 + 3.14159);
                float outerR2 = 0.88 * pulse2;
                float ring2 = smoothstep(outerR2 - 0.012, outerR2, dist)
                            * smoothstep(outerR2 + 0.012, outerR2 - 0.024, dist);
                ring2 *= 0.5;

                // --- Archi ruotanti (layer 1: veloci) ---
                float rotAngle1 = angle + t * _RotateSpeed;
                float arcs1 = arcMask(rotAngle1, _ArcCount, _ArcFill);
                // solo nella fascia del ring
                float arcZone1 = smoothstep(innerR - 0.03, innerR + 0.01, dist)
                               * smoothstep(outerR + 0.03, outerR - 0.01, dist);
                float arcContrib1 = arcs1 * arcZone1;

                // --- Archi ruotanti (layer 2: lenti, opposti, meno) ---
                float rotAngle2 = angle - t * _RotateSpeed * 0.4;
                float arcs2 = arcMask(rotAngle2, _ArcCount * 0.5, _ArcFill * 0.6);
                float arcZone2 = smoothstep(outerR2 - 0.03, outerR2, dist)
                               * smoothstep(outerR2 + 0.015, outerR2, dist);
                float arcContrib2 = arcs2 * arcZone2 * 0.6;

                // --- Ripple: onda che si espande dal centro ---
                float rippleT = frac(t * _RippleSpeed);          // 0→1 in loop
                float rippleR = rippleT * (outerR + 0.15);
                float ripple  = smoothstep(rippleR - 0.04, rippleR, dist)
                              * smoothstep(rippleR + 0.04, rippleR, dist);
                ripple *= (1.0 - rippleT);                        // svanisce espandendosi
                ripple *= smoothstep(outerR + 0.18, outerR - 0.05, dist); // clip oltre il ring

                // --- Compositing ---
                float3 col = float3(0, 0, 0);
                float  a   = 0.0;

                // ring principale
                col += ringColor * ring * _GlowIntensity;
                a   += ring * 0.95;

                // glow alone
                col += glowColor * glow * _GlowIntensity * 0.5;
                a   += glow * 0.4;

                // ring2 esterno
                col += _ColorOuter.rgb * ring2 * _GlowIntensity;
                a   += ring2 * 0.6;

                // archi layer1 (bianchi/ciano sul ring)
                col += lerp(_ColorArc.rgb, _ColorInner.rgb, 0.4) * arcContrib1 * _GlowIntensity;
                a   += arcContrib1 * 0.9;

                // archi layer2 (viola sul ring esterno)
                col += lerp(_ColorArc.rgb, _ColorOuter.rgb, 0.6) * arcContrib2 * _GlowIntensity;
                a   += arcContrib2 * 0.7;

                // ripple (ciano)
                col += _ColorInner.rgb * ripple * _GlowIntensity * 0.8;
                a   += ripple * 0.5;

                // discard pixel trasparenti
                clip(a - 0.005);

                return half4(col, saturate(a));
            }
            ENDHLSL
        }
    }
}