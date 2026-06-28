Shader "Custom/BoostTrail"
{
    Properties
    {
        // ── Arrow ────────────────────────────────────────────────────────────
        _ArrowTex       ("Arrow Texture",           2D)            = "white" {}
        _ArrowColor     ("Arrow Color",             Color)         = (1, 0.6, 0, 1)
        _ArrowSize      ("Arrow Size (world units)", Float)        = 1.0
        _ScrollSpeed    ("Scroll Speed",            Float)         = 2.0

        // 0=Forward, 1=Back, 2=Right, 3=Left
        _Direction      ("Direction (0=Fwd 1=Back 2=Right 3=Left)", Float) = 0.0

        // ── Pulse ────────────────────────────────────────────────────────────
        _PulseSpeed     ("Pulse Speed",             Float)         = 3.0
        _PulseAmount    ("Pulse Amount",            Range(0,0.45)) = 0.25
        _PulsePhaseStep ("Pulse Phase Step",        Float)         = 1.8

        // ── Flash Wave ───────────────────────────────────────────────────────
        _FlashColor     ("Flash Color",             Color)         = (1, 1, 0.7, 1)
        _FlashSpeed     ("Flash Wave Speed",        Float)         = 4.0
        _FlashWidth     ("Flash Wave Width",        Range(0.01,1)) = 0.2
        _FlashIntensity ("Flash Intensity",         Range(0,2))    = 1.0

        // ── Background ───────────────────────────────────────────────────────
        _BgColor        ("Background Color",        Color)         = (0.05, 0.05, 0.1, 1)
        _BgOpacity      ("Background Opacity",      Range(0,1))    = 0.6

        // ── Full-width trail ─────────────────────────────────────────────────
        [Toggle] _ShowFullTrail  ("Show Full Trail",    Float)     = 1
        _TrailColor     ("Trail Color",             Color)         = (1, 0.8, 0.2, 0.7)
        _TrailSpeed     ("Trail Speed",             Float)         = 6.0
        _TrailWidth     ("Trail Width",             Range(0,0.5))  = 0.07

        // ── Side trails ──────────────────────────────────────────────────────
        [Toggle] _ShowSideTrails ("Show Side Trails",  Float)      = 1
        [Toggle] _SideTrailPulse ("Side Trail Pulse",  Float)      = 1
        _SideTrailColor ("Side Trail Color",        Color)         = (1, 0.4, 0, 0.9)
        _SideTrailWidth ("Side Trail Width",        Range(0,0.4))  = 0.05
        _SideTrailPulseSpeed ("Side Pulse Speed",   Float)         = 2.0
        _SideTrailPulseAmt   ("Side Pulse Amount",  Range(0,1))    = 0.5

        // ── Repeat X ────────────────────────────────────────────────────────
        [Toggle] _RepeatX ("Repeat X", Float)                      = 0

        // Settati da BoostTrail.cs - non modificare manualmente
        _QuadWidth  ("Quad Width  (script)", Float)                = 1.0
        _QuadLength ("Quad Length (script)", Float)                = 4.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Offset -1, -1
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "BoostTrailPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_ArrowTex);
            SAMPLER(sampler_ArrowTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ArrowTex_ST;
                half4  _ArrowColor;
                float  _ArrowSize;
                float  _ScrollSpeed;
                float  _Direction;

                float  _PulseSpeed;
                float  _PulseAmount;
                float  _PulsePhaseStep;

                half4  _FlashColor;
                float  _FlashSpeed;
                float  _FlashWidth;
                float  _FlashIntensity;

                half4  _BgColor;
                float  _BgOpacity;

                float  _ShowFullTrail;
                half4  _TrailColor;
                float  _TrailSpeed;
                float  _TrailWidth;

                float  _ShowSideTrails;
                float  _SideTrailPulse;
                half4  _SideTrailColor;
                float  _SideTrailWidth;
                float  _SideTrailPulseSpeed;
                float  _SideTrailPulseAmt;

                float  _RepeatX;
                float  _QuadWidth;
                float  _QuadLength;
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
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv; // [0..1] sul quad

                // ── Tiling adattivo ──────────────────────────────────────────
                float safeSize = max(_ArrowSize, 0.001);
                float tilesY   = max(_QuadLength / safeSize, 1.0);
                float tilesX   = _RepeatX > 0.5 ? max(_QuadWidth / safeSize, 1.0) : 1.0;

                // ── Assi in base alla direzione ──────────────────────────────
                // scrollAxisUV : UV [0..1] lungo cui scorrono le frecce
                // crossAxisUV  : UV [0..1] perpendicolare (side trails)
                int dir = (int)round(_Direction);

                float scrollAxisUV = uv.y;
                float crossAxisUV  = uv.x;
                float tilesScroll  = tilesY;
                float tilesCross   = tilesX;

                if (dir == 2 || dir == 3)
                {
                    scrollAxisUV = uv.x;
                    crossAxisUV  = uv.y;
                    tilesScroll  = tilesX;
                    tilesCross   = tilesY;
                }

                // ── Scroll ───────────────────────────────────────────────────
                // Segno: Forward(0)/Right(2) = -1 (UV decrescono = tile si muovono
                //        verso UV=0 = direzione "avanti" percepita).
                // Back(1)/Left(3) = +1.
                float scrollSign = (dir == 1 || dir == 3) ? 1.0 : -1.0;
                float scrollOffset = scrollSign * _Time.y * _ScrollSpeed;

                // ── tileCoord: spazio tiled con scroll applicato ─────────────
                // x = asse cross (non scrolla), y = asse scroll (scrolla)
                float2 tileCoord = float2(
                    crossAxisUV  * tilesCross,
                    scrollAxisUV * tilesScroll + scrollOffset
                );

                // ── Indice tile (senza scroll, per fase pulse stabile) ───────
                // Usiamo le UV NON scrollate per l'indice, così la fase non cambia
                // mentre le frecce scorrono (evita flickering del pulse).
                float2 tileCoordStatic = float2(
                    crossAxisUV  * tilesCross,
                    scrollAxisUV * tilesScroll
                );
                float tileIdxCross  = floor(tileCoordStatic.x);
                float tileIdxScroll = floor(tileCoordStatic.y);
                float tileIndex     = tileIdxScroll * tilesCross + tileIdxCross;

                float phase  = tileIndex * _PulsePhaseStep;
                float pulse  = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed + phase);
                float shrink = _PulseAmount * pulse;

                // ── UV locali nel tile scrollato [0..1] ──────────────────────
                float2 localUV = frac(tileCoord); // [0..1] dentro il tile corrente

                // Pulse: contrai verso il centro SENZA wrappare.
                // Se la UV contratta esce da [0..1] → pixel trasparente (bordo pulito).
                float2 sampleUV = (localUV - 0.5) * (1.0 - shrink * 2.0) + 0.5;

                // inBounds: 1 se dentro [0..1] dopo contrazione, 0 altrimenti.
                // Questo crea lo spazio tra una freccia e l'altra senza box visibili.
                float inBounds = step(0.0, sampleUV.x) * step(sampleUV.x, 1.0)
                               * step(0.0, sampleUV.y) * step(sampleUV.y, 1.0);

                // Clamp per evitare campionamento fuori texture (non wrappare)
                sampleUV = clamp(sampleUV, 0.0, 1.0);

                // Flip Y della texture: la freccia punta verso UV.y=1 (alto).
                // Con Forward(0) vogliamo che la punta sia nella direzione di moto,
                // quindi flippiamo Y per Forward e Right; Back e Left rimangono.
                if (dir == 0 || dir == 2)
                    sampleUV.y = 1.0 - sampleUV.y;

                // Per Right/Left la freccia deve anche essere ruotata di 90 gradi.
                // Ruotiamo le UV locali di 90 deg per dir 2 e 3.
                if (dir == 2 || dir == 3)
                {
                    float2 centered = sampleUV - 0.5;
                    sampleUV = float2(-centered.y, centered.x) + 0.5;
                }

                half4 arrowSample = SAMPLE_TEXTURE2D(_ArrowTex, sampler_ArrowTex, sampleUV);

                // ── Flash Wave ───────────────────────────────────────────────
                // Onda che percorre tutta la lunghezza della scia (usa coord statica,
                // non scrollata, così l'onda ha velocità indipendente dalle frecce).
                float axisCoord = scrollAxisUV * tilesScroll; // 0..tilesScroll
                float waveFront = frac(_Time.y * _FlashSpeed / max(tilesScroll, 0.001)) * tilesScroll;
                float diff      = waveFront - axisCoord;
                      diff      = diff - tilesScroll * floor(diff / tilesScroll + 0.5);
                float wave      = saturate(1.0 - abs(diff) / (_FlashWidth * tilesScroll));

                // ── Freccia finale ───────────────────────────────────────────
                // Usiamo SOLO l'alpha della texture come maschera di forma.
                // Il colore viene da arrowColor (freccia nera su alpha → solo alpha conta).
                half4 arrowCol;
                arrowCol.rgb = lerp(_ArrowColor.rgb, _FlashColor.rgb, wave * _FlashIntensity);
                arrowCol.a   = arrowSample.a * inBounds;

                // ── Sfondo dell'intera scia (unico piano, no tile) ───────────
                half4 bgCol = half4(_BgColor.rgb, _BgColor.a * _BgOpacity);

                // ── Full-width trail ─────────────────────────────────────────
                // Una fascia luminosa che scorre lungo l'asse di scorrimento delle frecce.
                // Scorre più veloce delle frecce e copre tutta la larghezza (cross axis).
                float trailAlpha = 0.0;
                if (_ShowFullTrail > 0.5)
                {
                    // Posizione normalizzata lungo l'asse di scroll [0..1]
                    float tUV  = frac(scrollAxisUV - _Time.y * _TrailSpeed / max(tilesScroll, 0.001));
                    float tUV2 = frac(tUV + 0.5);
                    float mask  = smoothstep(_TrailWidth, 0.0, abs(tUV  - 0.5));
                           mask += smoothstep(_TrailWidth, 0.0, abs(tUV2 - 0.5));
                    trailAlpha  = saturate(mask);
                }

                // ── Side trails ──────────────────────────────────────────────
                float sideAlpha = 0.0;
                if (_ShowSideTrails > 0.5)
                {
                    float sideW = _SideTrailWidth;
                    if (_SideTrailPulse > 0.5)
                    {
                        float sidePulse = 0.5 + 0.5 * sin(_Time.y * _SideTrailPulseSpeed);
                        sideW *= lerp(1.0 - _SideTrailPulseAmt, 1.0, sidePulse);
                    }
                    sideW = max(sideW, 0.001);

                    float edgeL   = smoothstep(sideW, 0.0, crossAxisUV);
                    float edgeR   = smoothstep(sideW, 0.0, 1.0 - crossAxisUV);
                    sideAlpha     = saturate(edgeL + edgeR);
                }

                // ── Compositing ──────────────────────────────────────────────
                // Layer order (basso → alto):
                //   1. Sfondo
                //   2. Full-width trail
                //   3. Frecce (alpha dalla texture)
                //   4. Side trails (ai bordi, sopra tutto)

                half4 col = bgCol;

                // 2 - full trail
                if (_ShowFullTrail > 0.5)
                {
                    half4 tc = half4(_TrailColor.rgb, _TrailColor.a * trailAlpha);
                    col.rgb  = lerp(col.rgb, tc.rgb, tc.a);
                    col.a    = saturate(col.a + tc.a * (1.0 - col.a));
                }

                // 3 - frecce
                col.rgb = lerp(col.rgb, arrowCol.rgb, arrowCol.a);
                col.a   = saturate(col.a + arrowCol.a * (1.0 - col.a));

                // 4 - side trails
                if (_ShowSideTrails > 0.5)
                {
                    half4 sc = half4(_SideTrailColor.rgb, _SideTrailColor.a * sideAlpha);
                    col.rgb  = lerp(col.rgb, sc.rgb, sc.a);
                    col.a    = saturate(col.a + sc.a * (1.0 - col.a));
                }

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
