
There are two shaders provided in this sample

    Unlit Stretch

        This shader is for visualising the stretch and squish values, through reading and displaying TEXCOORD3.
        It's written in ShaderLab and HLSL so you are able to read the code.

    
    Normal Map When Squished

        This shader applies two extra normals map when squished, as shown with the cardboard box.
        It was created with Shader Graph and has the following properties:

            Diffuse
                Diffuse texture to use
            
            Normal Map
                Normal map used at all times

            Squish Map 1
                A normal map to use when squished

            Squish Map 2
                A secondary normal map to use when squished

            Squish Map 1 Strength
                The strength of squish map 1

            Squish Map 2
                The strength of squish map 2

            Squish Multiplier
                How apparent the squished normal maps will become when squished, the higher the more obvious

            Squish Cutoff
                The minimum amount of squish before the normal maps appear
