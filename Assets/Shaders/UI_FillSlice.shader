Shader "Custom/UI_FillSlice"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillColor   ("Fill Color",  Color) = (0.2, 0.6, 1.0, 1.0)
        _EmptyColor  ("Empty Color", Color) = (0.3, 0.3, 0.3, 0.4)
        _FillAmount  ("Fill Amount", Range(0,1)) = 0.5
        _EdgeWidth   ("Edge Glow Width", Range(0, 0.05)) = 0.015
        _EdgeColor   ("Edge Glow Color", Color) = (0.6, 0.9, 1.0, 1.0)
        // 0 = Bottom→Top  1 = Top→Bottom  2 = Left→Right  3 = Right→Left
        [IntRange] _FillDirection ("Fill Direction", Range(0,3)) = 0

        // Richiesti da Unity UI
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil     ("Stencil ID",         Float) = 0
        _StencilOp   ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask   ("Color Mask", Float) = 15
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
            Ref   [_Stencil]
            Comp  [_StencilComp]
            Pass  [_StencilOp]
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
                float4 vertex      : SV_POSITION;
                fixed4 color       : COLOR;
                float2 texcoord    : TEXCOORD0;
                float4 worldPos    : TEXCOORD1;
                // UV in spazio normalizzato 0..1 rispetto al rect dell'immagine
                float2 localUV     : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D   _MainTex;
            float4      _MainTex_ST;
            fixed4      _FillColor;
            fixed4      _EmptyColor;
            float       _FillAmount;
            float       _EdgeWidth;
            fixed4      _EdgeColor;
            int         _FillDirection;
            float4      _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPos = v.vertex;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.localUV  = v.texcoord;   // 0..1, Y=0 basso Y=1 alto
                o.color    = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Campiona lo sprite (rispetta la forma + trasparenza)
                half4 spriteSample = tex2D(_MainTex, i.texcoord);

                // Scarta i pixel trasparenti dello sprite
                clip(spriteSample.a - 0.01);

                // Applica clip rect UI (scrollview, mask, ecc.)
                half clipAlpha = UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                clip(clipAlpha - 0.001);

                // Seleziona la coordinata in base alla direzione del fill
                // 0 Bottom→Top  1 Top→Bottom  2 Left→Right  3 Right→Left
                float coord;
                if      (_FillDirection == 1) coord = 1.0 - i.localUV.y;
                else if (_FillDirection == 2) coord = i.localUV.x;
                else if (_FillDirection == 3) coord = 1.0 - i.localUV.x;
                else                          coord = i.localUV.y; // default: Bottom→Top

                fixed4 col;

                if (coord < _FillAmount - _EdgeWidth)
                {
                    // Zona piena
                    col = _FillColor;
                }
                else if (coord < _FillAmount)
                {
                    // Bordo luminoso (edge glow)
                    float t = (coord - (_FillAmount - _EdgeWidth)) / _EdgeWidth;
                    col = lerp(_FillColor, _EdgeColor, t);
                }
                else
                {
                    // Zona vuota
                    col = _EmptyColor;
                }

                // Mantieni l'alpha dello sprite (rispetta i bordi)
                col.a *= spriteSample.a * i.color.a * clipAlpha;

                return col;
            }
            ENDCG
        }
    }
}
