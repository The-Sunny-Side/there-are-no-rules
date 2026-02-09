Shader "Custom/RadialBurst"
{
    Properties
    {
        _MainColor ("Colore Principale", Color) = (0.6, 0.3, 0.8, 1)
        _SecondaryColor ("Colore Secondario", Color) = (0.4, 0.2, 0.6, 1)
        _RayCount ("Numero Raggi", Range(8, 64)) = 32
        _RayWidth ("Larghezza Raggi", Range(0.01, 0.5)) = 0.08
        _Center ("Centro (X, Y)", Vector) = (0.5, 0.5, 0, 0)
        _Speed ("Velocità Rotazione", Float) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _MainColor;
            fixed4 _SecondaryColor;
            float _RayCount;
            float _RayWidth;
            float2 _Center;
            float _Speed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calcola vettore dal centro
                float2 dir = i.uv - _Center;
                
                // Calcola angolo con rotazione animata
                float angle = atan2(dir.y, dir.x) + _Time.y * _Speed;
                
                // Normalizza angolo tra 0 e 2*PI
                angle = fmod(angle + 3.14159265, 6.28318530);
                
                // Calcola pattern dei raggi
                float rayPattern = fmod(angle * _RayCount / 6.28318530, 1.0);
                
                // Crea il pattern alternato usando smoothstep per transizioni morbide
                float ray = smoothstep(0.5 - _RayWidth, 0.5, rayPattern) * 
                           (1.0 - smoothstep(0.5, 0.5 + _RayWidth, rayPattern));
                
                // Interpola tra i due colori
                fixed4 col = lerp(_SecondaryColor, _MainColor, ray);
                
                return col;
            }
            ENDCG
        }
    }
}