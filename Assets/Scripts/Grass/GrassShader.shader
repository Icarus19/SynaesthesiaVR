Shader "Custom/GrassShader"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _TipColor("Tip Color", Color) = (1, 1, 1, 1)
        _TipColorStrength("Tip Color Strength", float) = 1.0
        _TipColorSpreadTexture("Tip Color SpreadTexture", 2D) = "white" {}
        _TipColorScale("Tip Texture Scale", float) = 1.0
        _TipColorOffset("Tip Texture Offset", Vector) = (0, 0, 0, 0)
        _TipColorFactor("Tip Color Factor", float) = 1.0
        _SwayStrength("Sway Strength", float) = 1.0
        _AOColor("Ambient Occlusion Color", Color) = (1, 1, 1, 1)
        _ShadowTexture("Shadow Texture", 2D) = "white" {}
        _ShadowStrength("Shadow Strength", float) = 1.0
        _ShadowScale("Shadow Scale", float) = 1.0
        _ShadowSpeed("Shadow Scroll Speed", Vector) = (1, 1, 1, 1)
        _Scale("Mesh Scale", float) = 1.0
        _HeightmapTex("Heightmap Texture", 2D) = "white" {}
        _HeightmapAmplitude("Heightmap Amplitude", float) = 1.0
        _HeightClumpTex("Height ClumpingTexture", 2D) = "white" {}
        _BladeHeightMinMax("Minimum and Maximum size of blades", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma target 3.0
            //VR Compatability
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "UnityCG.cginc"
            //Advanced Lighting test
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                
                //VR Compatability
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float height : TEXCOORD1;
                float3 worldPos : TEXCOORD2;

                //VR Compatability
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _Color;
            float4 _TipColor;
            float _TipColorStrength;
            sampler2D _TipColorSpreadTexture;
            float _TipColorScale;
            float2 _TipColorOffset;
            float _TipColorFactor;
            float _SwayStrength;
            float4 _AOColor;
            sampler2D _ShadowTexture;
            float _ShadowStrength;
            float _ShadowScale;
            float2 _ShadowSpeed;
            float _Scale;
            float _HeightmapAmplitude;
            sampler2D _HeightmapTex; //Calling it directly from the properties works but not from cpu, nothing else has changed
            sampler2D _HeightClumpTex;
            float2 _BladeHeightMinMax;

            //CPU Variables
            sampler2D _VectorFieldTex;
            int _SizeX, _SizeY, _SizeZ;
            int _InstanceResolution;
            int _GridSize;

            float3 SampleVectorField(int x, int y, int z)
            {
                int index = x + y * _SizeX + z * _SizeX * _SizeY;
                int texX = index % 2048;
                int texY = index / 2048;

                float2 uv = (float2(texX, texY) + 0.5) / 2048.0;
                return tex2Dlod(_VectorFieldTex, float4(uv, 0, 0));
            }

            float SampleHeightmap(float2 uv)
            {
                return tex2Dlod(_HeightmapTex, float4(uv, 0, 0)).r;
            }

            float Hash(int n)
            {
                n = (n << 13) ^ n;
                return (1.0 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0);
            }

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                //VR Compatability
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                //Rotation from random angles
                float angle = Hash(instanceID * 13.37) * 6.2831853;
                float sinA = sin(angle);
                float cosA = cos(angle);
                
                float3 rotatedVertex;
                rotatedVertex.x = v.vertex.x * cosA - v.vertex.z * sinA;
                rotatedVertex.y = v.vertex.y;
                rotatedVertex.z = v.vertex.x * sinA + v.vertex.z * cosA;

                float3 vertexPosition = rotatedVertex * _Scale;

                //This almost drove me insane. Why can I not divide two ints and get a float, or atleast give me a warning or something
                float spacing = (float)_GridSize / (float)_InstanceResolution;
                float row = instanceID % _InstanceResolution;
                float column = instanceID / _InstanceResolution;
                
                float3 offset = float3(row * spacing, 0, column * spacing);
                float randomOffsetX = Hash(instanceID * 3.1) * 2.0 - 1.0;
                float randomOffsetZ = Hash(instanceID * 7.3) * 2.0 - 1.0;
                offset.x += randomOffsetX;
                offset.z += randomOffsetZ;

                //Get height from Heightmap
                float2 heightmapUV = float2(offset.x, offset.z) / _GridSize;
                float heightFromMap = SampleHeightmap(heightmapUV) * _HeightmapAmplitude;
                //Randomize height of blades form map //Clamping was to hard to lets just multiply
                float bladeHeightFromMap = SampleHeightmap(heightmapUV) * _BladeHeightMinMax.x + 1 * _BladeHeightMinMax.y;
                offset.y = heightFromMap;
                offset.x -= _GridSize * 0.5;
                offset.z -= _GridSize * 0.5;

                vertexPosition.y *= bladeHeightFromMap;

                //Displacement from Vectorfield
                //No interpolation needed, it looks good anyway
                //Interpolation might solve the stretching that happens sometimes. But I want this shader to be performant
                float height = saturate(v.vertex.y);
                float3 fieldVector = SampleVectorField(vertexPosition.x, vertexPosition.y, vertexPosition.z);

                //Pivot version
                float3 pivotPos = float3(vertexPosition.x, 0.0, vertexPosition.z);
                float3 bendOffset = float3(
                    fieldVector.x * height,
                    0.0,
                    fieldVector.z * height
                ) * height * _SwayStrength;

                //Offset has to be applied here since the vectorfield expects a normalized input
                vertexPosition += offset;
                vertexPosition.xz += bendOffset.xz;

                o.vertex = UnityObjectToClipPos(float4(vertexPosition, 1.0));
                o.uv = v.uv;
                o.height = v.vertex.y;
                o.worldPos = mul(unity_ObjectToWorld, float4(vertexPosition, 1.0)).xyz;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 ao = lerp(0.0, _AOColor, i.height);
                float4 tipColor = lerp(0.0, _TipColor, i.height * i.height);

                //Vary grass color
                float2 colorUV = (i.worldPos.xz + float2(_GridSize, _GridSize)) / (_GridSize * 2);  // map [-128,128]¨[0,1]
                colorUV = frac(colorUV);    // wrap if tiling (or use saturate(uv) to clamp)
                float noiseVal = tex2D(_TipColorSpreadTexture, colorUV).r * _TipColorStrength;
                float remapped = saturate((noiseVal - 0.5) * _TipColorFactor + 0.5);
                float4 col = lerp(_Color, _TipColor, remapped);

                //Simple Lighting
                float3 normal = normalize(cross(ddx(i.vertex.xyz), ddy(i.vertex.xyz)));
                float diffuseLight = max(dot(normal, normalize(float3(2, 0.2, 1.6))), 0.0f);

                //Fake shadows
                float2 shadowUV = i.worldPos.xz * _ShadowScale + _Time.y * _ShadowSpeed;
                float rawShadow = tex2D(_ShadowTexture, shadowUV).r;
                float shadowStrength = lerp(0.5, rawShadow, _ShadowStrength);

                col *= diffuseLight * shadowStrength * ao;
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
