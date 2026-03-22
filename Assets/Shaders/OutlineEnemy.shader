Shader "Custom/OutlineEnemy"
{
    Properties
    {
        _Color   ("Outline Color", Color) = (1, 1, 1, 1)
        _Size    ("Outline Size",  Range(0.001, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "OutlineShell"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull Front
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Size;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                // Scale from object center — uniform expansion, no gaps
                float3 scaled = input.positionOS.xyz * (1.0 + _Size);
                output.positionCS = TransformObjectToHClip(scaled);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(_Color.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
