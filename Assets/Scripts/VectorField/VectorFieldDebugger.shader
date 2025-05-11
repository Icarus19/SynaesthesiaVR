Shader "Debug/VectorFieldDebugger"
{
    Properties
    {}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _VectorFieldTex;
            int _SizeX, _SizeY, _SizeZ;

            float3 SampleVectorField(int x, int y, int z)
            {
                int index = x + y * _SizeX + z * _SizeX * _SizeY;
                int texX = index % 2048;
                int texY = index / 2048;

                float2 uv = (float2(texX, texY) + 0.5) / 2048.0;
                return tex2Dlod(_VectorFieldTex, float4(uv, 0, 0));
            }

            v2f vert (appdata v)
            {
                v2f o;

                uint vertexIndex = v.vertexID / 2;
                uint isEnd = v.vertexID % 2;

                //Assumes a texture of 2048x2048
                int texX = vertexIndex % 2048;
                int texY = vertexIndex / 2048;

                int index = texY * 2048 + texX;

                int x = index % (uint)_SizeX;
                int y = (index / (uint)_SizeX) % (uint)_SizeY;
                int z = index / ((uint)_SizeX * (uint)_SizeY);

                //This assumes the position is always at 0, 0, 0 and then shifts the grid by half size and multiplies it by two
                float3 origin = (float3(x, y, z) - float3(_SizeX, _SizeY, _SizeZ) * 0.5f) * 2.0f;
                float3 dir = SampleVectorField(x, y, z);

                float3 worldPos = origin + (isEnd == 1 ? dir * 1.0f : float3(0, 0, 0));
                o.pos = UnityObjectToClipPos(float4(worldPos, 1.0));

                //Temporary gradient across the field
                //float totalCount = (float)(_SizeX * _SizeY * _SizeZ);
                //float normalizedIndex = (float)index / (totalCount - 1.0);

                //o.color = float4(normalizedIndex, 1.0 - normalizedIndex, 0.5, 1.0);
                //o.color = float4((float)x / _SizeX, (float)y / _SizeY, (float)z / _SizeZ, 1.0);
                //o.color = float4((float)x / _SizeX, (float)y / _SizeY, (float)z / _SizeZ, 1.0);
                o.color = float4(abs(dir), 1.0);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
