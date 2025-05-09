Shader "Custom/Water"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _DepthColor("Depth Color", Color) = (0, 0, 0, 1)
        _WorldPos("world pos", Vector) = (0, 0, 0)
        _CubeMap("Reflection Cubemap", Cube) = "" {}
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            //Tags{"LightMode" = "ForwardBase"}

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

                //VR Compatability
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenSpace : TEXCOORD1;
                float height : TEXCOORD2;
                float3 worldPos : TEXCOORD3;

                //VR Compatability
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _CameraDepthTexture;
            samplerCUBE _CubeMap;

            fixed4 _BaseColor;
            fixed4 _DepthColor;
            float3 _WorldPos;
            float _ReflectionStrength;

            v2f vert (appdata v)
            {
                //VR Compatability
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                //Ripples parameters
                float dist = distance(worldPos.xz, _WorldPos.xz);
                float wave = sin(dist * 10 - _Time.y * 4) * 0.1 / (dist + 1);

                float3 displaced = v.vertex.xyz;
                displaced.y += wave;

                float4 modifiedVertex = float4(displaced, 1);

                o.vertex = UnityObjectToClipPos(modifiedVertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.screenSpace = ComputeScreenPos(o.vertex);
                o.height = saturate(v.vertex.y + 1);
                o.worldPos = mul(unity_ObjectToWorld, modifiedVertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldNormal = float3(0, 1, 0);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 reflDir = reflect(-viewDir, worldNormal);
                float3 reflection = texCUBE(_CubeMap, reflDir).rgb;
                
                fixed4 col = _BaseColor;
                float2 screenSpaceUV = i.screenSpace.xy / i.screenSpace.w;
                
                //VR Can't use this function
                //float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenSpaceUV));
                float depth = tex2D(_CameraDepthTexture, screenSpaceUV);
                float3 baseWaterColor = lerp(_DepthColor, _BaseColor, depth);

                float3 finalColor = lerp(baseWaterColor, reflection, _ReflectionStrength);

                return fixed4(finalColor, _BaseColor.a);
            }
            ENDCG
        }
    }
}
