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

            //CPU Variables
            StructuredBuffer<float4> _PositionBuffer;
            StructuredBuffer<float2> _RotationBuffer;
            StructuredBuffer<float3> _VectorField;
            int _SizeX;
            int _SizeY;
            int _SizeZ;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                //VR Compatability
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 pos = _PositionBuffer[instanceID];
                float3 offset = pos.xyz;
                float scale = pos.w;
                float2 sincos = _RotationBuffer[instanceID];

                float3 vertexPosition = v.vertex.xyz * scale;

                //Rotation
                float rotatedX = vertexPosition.x * sincos.y - vertexPosition.z * sincos.x;
                float rotatedZ = vertexPosition.x * sincos.x + vertexPosition.z * sincos.y;

                vertexPosition.x = rotatedX;
                vertexPosition.z = rotatedZ;

                vertexPosition += offset;

                //Displacement from Vectorfield
                //No interpolation needed, it looks good anyway
                //Interpolation might solve the stretching that happens sometimes. But I want this shader to be performant
                float height = saturate(v.vertex.y);
                float3 halfFieldSize = float3(_SizeX, _SizeY, _SizeZ) * 2.0 * 0.5;
                float3 shiftedPos = vertexPosition + halfFieldSize;
                float3 normalizedPos = clamp(shiftedPos / float3(_SizeX, _SizeY, _SizeZ) / 2.0, 0.0, 1.0);

                //Map to grid
                int x = (int)(normalizedPos.x * _SizeX - 1);
                int y = (int)(normalizedPos.y * _SizeY - 1);
                int z = (int)(normalizedPos.z * _SizeZ - 1);

                int index = x + y * _SizeX + z * _SizeX * _SizeY;

                float3 fieldVector = _VectorField[index];

                //Pivot version
                float3 pivotPos = float3(vertexPosition.x, 0.0, vertexPosition.z);
                float3 bendOffset = float3(
                    fieldVector.x * height,
                    0.0,
                    fieldVector.z * height
                ) * height * _SwayStrength;

                vertexPosition.xz += bendOffset.xz;

                o.vertex = UnityObjectToClipPos(float4(vertexPosition, 1.0));
                o.uv = v.uv;
                o.height = height;
                o.worldPos = mul(unity_ObjectToWorld, float4(vertexPosition, 1.0)).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 baseColor = _Color;
                float4 tipColor = _TipColor;

                float4 ao = lerp(_AOColor, 1.0f, i.uv.y);

                //Vary grass color
                float2 worldUV = (i.worldPos.xz + _TipColorOffset) * _TipColorScale;
                float tipTexture = tex2D(_TipColorSpreadTexture, worldUV);
                float tipStrength = i.height * _TipColorStrength * lerp(1.0, tipTexture, _TipColorFactor);
                float4 col = lerp(baseColor, tipColor, tipStrength) * ao;

                //Simple Lighting
                float3 normal = normalize(cross(ddx(i.vertex.xyz), ddy(i.vertex.xyz)));
                float diffuseLight = max(dot(normal, normalize(float3(2, 0.2, 1.6))), 0.0f);

                //Fake shadows
                float2 shadowUV = i.worldPos.xz * _ShadowScale + _Time.y * _ShadowSpeed;
                float rawShadow = tex2D(_ShadowTexture, shadowUV).r;
                float shadowStrength = lerp(0.5, rawShadow, _ShadowStrength);

                col *= diffuseLight * shadowStrength;
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
