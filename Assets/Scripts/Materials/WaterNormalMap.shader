Shader "Unlit/WaterNormalMap"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _DrawColor ("Draw Color", Color) = (1, 1, 1, 1)
        _DrawSize ("Draw Size", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 normal : NORMAL;

                //VR Compatability
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;

                //VR Compatability
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _BaseColor;
            fixed4 _DrawColor;
            float _DrawSize;

            //CPU Variables
            float3 _PlayerWorldPosition;

            float2 GenerateNoise(float2 worldPos, float time)
            {
                float dist = distance(worldPos, _PlayerWorldPosition.xz);
                float wave = sin(dist * 20 - time * 5);
                return float2(cos(dist * 20 - time * 5), wave);
            }

            v2f vert (appdata v)
            {
                //VR Compatability
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;

                float2 normalOffset = GenerateNoise(i.worldPos.xz, time);

                float3 fakeNormal = normalize(float3(normalOffset.x, normalOffset.y, 1.0));
                float3 fakeLighting = float3(0.0, 0.0, 1.0);
                float lightingFactor = saturate(dot(fakeNormal, fakeLighting));

                fixed4 col = _BaseColor * lightingFactor;

                //Drawing time
                float dist = distance(i.worldPos.xz, _PlayerWorldPosition.xz);
                float influence = smoothstep(0.005, _DrawSize, dist);
                col = lerp(col, _DrawColor, influence);
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
