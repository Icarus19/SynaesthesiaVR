Shader "Custom/WaveSimulation"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _ReflectionTexture ("ReflectionTexture", CUBE) = "white" {}
        _ReflectionStrength ("Reflection Strength", float) = 1.0
        _DistortionStrength ("Distortion Strength", float) = 1.0
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _DataTexture ("Data Texture", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", float) = 0.2
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
            //VR Compatability
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "UnityCG.cginc"

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
                float3 viewDir :TEXCOORD1;
                //VR Compatability
                UNITY_VERTEX_OUTPUT_STEREO
            };

            //Properties
            sampler2D _MainTex;
            fixed4 _BaseColor;
            float _ReflectionStrength;
            float _DistortionStrength;
            float _NoiseStrength;

            //CPU Variables
            sampler2D _DataTexture;
            samplerCUBE _ReflectionTexture;

            //Random noise function
            float rand(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                //VR Compatability
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = 1 - v.uv; //Don't know why but the UV is flipped from the texture
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float3 viewDir = _WorldSpaceCameraPos - worldPos.xyz;
                o.viewDir = viewDir;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 data = tex2D(_DataTexture, i.uv);
                
                float3 normal = normalize(float3(data.z, 1.0, data.w));
                float3 viewDir = normalize(i.viewDir);
                //float3 reflDir = reflect(-viewDir, normal);

                float3 distortion = float3(data.zw, 0) * _DistortionStrength;
                float3 distoredNormal = normalize(normal + distortion);
                float3 reflDir = reflect(-viewDir, distoredNormal);

                //Would look better with a wave texture or something but ehh
                float noise = rand(i.uv) * _NoiseStrength;
                float3 noisyReflDir = reflDir + float3(noise, noise, 0);

                fixed4 reflCol = texCUBE(_ReflectionTexture, noisyReflDir);

                fixed4 col = tex2D(_MainTex, i.uv + 0.2 * data.zw);
                col *= _BaseColor;

                float3 lightDir = normalize(float3(-3, 10, 3));
                float NdotL = max(0.0, dot(normal, lightDir));
                float specular = pow(NdotL, 10.0);
                //col.rgb += reflCol.rgb * specular;

                //col.rgb = lerp(col.rgb, reflCol.rgb, _ReflectionStrength);
                float fresnel = pow(1.0 - max(0.0, dot(normal, viewDir)), 3.0);
                col.rgb = lerp(col.rgb, reflCol.rgb, fresnel * _ReflectionStrength);

                return col;
            }
            ENDCG
        }
    }
}
