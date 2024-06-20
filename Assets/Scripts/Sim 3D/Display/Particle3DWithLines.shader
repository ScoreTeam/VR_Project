Shader "Instanced/Particle3DWithLines" {
    Properties {
        
    }
    SubShader {
        Tags { "Queue" = "Transparent" }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"

            StructuredBuffer<float3> Positions;
            StructuredBuffer<float3> PrePositions;
            StructuredBuffer<float3> Velocities;
            StructuredBuffer<float3> InitialVelocities;
            Texture2D<float4> ColourMap;
            SamplerState linear_clamp_sampler;
            float velocityMax;

            float scale;
            float3 colour;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 colour : TEXCOORD1;
                float3 normal : NORMAL;
                float3 worldPos : TEXCOORD2;
                uint instanceID : SV_InstanceID;
            };

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID) {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                float3 centreWorld = Positions[instanceID];
                float3 worldVertPos = centreWorld + mul(unity_ObjectToWorld, v.vertex * scale);
                float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

                o.uv = v.texcoord;
                o.normal = v.normal;
                o.pos = UnityObjectToClipPos(objectVertPos);
                o.worldPos = worldVertPos;
                o.instanceID = instanceID;

                float speed = length(Velocities[instanceID]);
                float speedT = saturate(speed / velocityMax);
                float colT = speedT;
                o.colour = ColourMap.SampleLevel(linear_clamp_sampler, float2(colT, 0.5), 0).rgb;

                return o;
            }

            struct geomOut {
                float4 pos : SV_POSITION;
                float4 colour : COLOR;
            };

            [maxvertexcount(2)]
            void geom(triangle v2f input[3], inout LineStream<geomOut> output) {
                uint instanceID = input[0].instanceID;
                float3 pos = Positions[instanceID];
                float3 prePos = PrePositions[instanceID];
                float3 currentVelocity = Velocities[instanceID];
                float3 initialVelocity = InitialVelocities[instanceID];

                float velocityDifference = length(currentVelocity - initialVelocity);

                if (velocityDifference >= 1.0f) {
                    geomOut vertex;

                    vertex.pos = UnityObjectToClipPos(float4(pos, 1.0));
                    vertex.colour = float4(input[0].colour, 0.5);
                    output.Append(vertex);

                    vertex.pos = UnityObjectToClipPos(float4(prePos, 1.0));
                    vertex.colour = float4(input[0].colour, 0.5);
                    output.Append(vertex);
                }
            }

            float4 frag (geomOut i) : SV_Target {
                return i.colour;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
