Shader "Custom/UI_GlowEllipse"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _GlowRadius  ("Glow Radius",   Range(0, 1))    = 0.35
        _GlowSoft    ("Glow Softness", Range(0.01, 1)) = 0.3
        _CoreRadius  ("Core Radius",   Range(0, 1))    = 0.18
        _CoreSoft    ("Core Softness", Range(0.01, 1)) = 0.08

        // Richiesti Unity UI
        _StencilComp      ("Stencil Comparison", Float) = 8
        _Stencil          ("Stencil ID",         Float) = 0
        _StencilOp        ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask        ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float2 uv       : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float     _GlowRadius;
            float     _GlowSoft;
            float     _CoreRadius;
            float     _CoreSoft;
            float4    _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPos = v.vertex;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.uv       = v.texcoord;
                o.color    = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = i.color;   // colore dell'Image, già impostato da te

                float2 centered = i.uv - 0.5;
                float  dist     = length(centered);

                // Core: l'ellisse solida
                float core = smoothstep(_CoreRadius, _CoreRadius - _CoreSoft, dist);

                // Glow ring: alone morbido attorno al core
                float glow     = 1.0 - smoothstep(_CoreRadius, _GlowRadius + _GlowSoft, dist);
                float glowRing = glow - smoothstep(_CoreRadius - _CoreSoft, _CoreRadius, dist) * glow;

                // Alpha finale: piena nel core, sfuma nell'alone
                col.a = saturate(core + glowRing * 0.7) * i.color.a;

                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                clip(col.a - 0.001);

                return col;
            }
            ENDCG
        }
    }
}
