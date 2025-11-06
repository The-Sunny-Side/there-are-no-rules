Shader "Lattice Samples/Unlit Stretch"
{
    Properties
    { }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        struct Attributes
        {
            float4 positionOS   : POSITION;
            float2 stretch      : TEXCOORD3;
        };

        struct Varyings
        {
            float4 positionHCS  : SV_POSITION;
            float2 stretch      : TEXCOORD3;
        };            

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.stretch = IN.stretch;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            // Total amount of stretch (.x is along tangent, .y is along binormal)
            float totalStretch = IN.stretch.x * IN.stretch.y;

            // Get either amount of stretch or squash
            float value = totalStretch < 1 ? 1 - totalStretch : totalStretch - 1;
            value *= 2;

            // Colour it red if stretch, blue if squash
            float3 colour = totalStretch < 1 
                ? lerp(0.75, float3(0,0.4,1), value) 
                : lerp(0.75, float3(1,0.1,0), value);

            return float4(colour, 1);
        }

        ENDHLSL

        Pass
        {
            Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
			Tags { "LightMode"="DepthNormalsOnly" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}