Shader "Custom/DustParticleEffect"
{
    Properties
    {
        _MainTex ("Dust Texture", 2D) = "white" {}
        _ParticleCount ("Particle Count", Range(5, 50)) = 20
        _MinSize ("Min Particle Size", Float) = 0.5
        _MaxSize ("Max Particle Size", Float) = 2.0
        _Speed ("Rise Speed", Float) = 1.0
        _SpawnRate ("Spawn Rate", Float) = 1.0
        _Brightness ("Brightness", Float) = 1.0
        _Alpha ("Alpha", Range(0,1)) = 1.0
        _WindStrength ("Wind Strength", Float) = 0.5
        _BuildingHeight ("Building Height", Float) = 10.0
        _BuildingWidth ("Building Width", Float) = 5.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _ParticleCount;
            float _MinSize, _MaxSize;
            float _Speed, _SpawnRate;
            float _Brightness, _Alpha;
            float _WindStrength;
            float _BuildingHeight, _BuildingWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float alpha : TEXCOORD2;
            };
            
            // 랜덤 함수
            float random(float2 st) 
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            float2 random2(float2 st) 
            {
                st = float2(dot(st, float2(127.1, 311.7)),
                           dot(st, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // 오브젝트의 월드 위치 가져오기
                float3 objectWorldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                
                // LineRenderer의 UV를 파티클 ID로 사용
                float particleID = floor(v.uv.x * _ParticleCount);
                float2 seed = float2(particleID, floor(_Time.y * _SpawnRate));
                
                // 각 파티클의 라이프타임 계산
                float lifeTime = frac(_Time.y * _Speed + random(seed + 1.0));
                
                // 파티클 크기 (랜덤)
                float size = lerp(_MinSize, _MaxSize, random(seed + 2.0));
                
                // 로컬 오프셋 계산 (오브젝트 중심 기준)
                float localX = (random(seed + 3.0) - 0.5) * _BuildingWidth;
                float localY = lifeTime * _BuildingHeight;
                float localZ = (random(seed + 4.0) - 0.5) * 2.0;
                
                // 바람 효과 (로컬)
                float2 wind = random2(seed + 5.0) * _WindStrength;
                float windX = wind.x * lifeTime;
                float windZ = wind.y * lifeTime;
                
                // 로컬 파티클 위치
                float3 localParticlePos = float3(
                    localX + windX,
                    localY,
                    localZ + windZ
                );
                
                // 최종 월드 위치 = 오브젝트 월드 위치 + 로컬 오프셋
                float3 finalWorldPos = objectWorldPos + localParticlePos;
                
                // 빌보드 효과를 위한 카메라 방향 계산
                float3 viewDir = normalize(finalWorldPos - _WorldSpaceCameraPos);
                float3 upDir = float3(0, 1, 0);
                float3 rightDir = normalize(cross(upDir, viewDir));
                upDir = cross(viewDir, rightDir);
                
                // 쿼드의 각 정점 오프셋 (빌보드)
                float2 offset = (v.uv - 0.5) * 2.0; // -1 to 1 범위
                float3 vertexOffset = (rightDir * offset.x + upDir * offset.y) * size;
                
                // 최종 정점 위치
                float3 finalVertexPos = finalWorldPos + vertexOffset;
                o.vertex = mul(UNITY_MATRIX_VP, float4(finalVertexPos, 1.0));
                
                // UV는 그대로 사용
                o.uv = v.uv;
                
                // 알파 계산 (페이드 인/아웃)
                float fadeIn = smoothstep(0.0, 0.2, lifeTime);
                float fadeOut = smoothstep(1.0, 0.8, lifeTime);
                o.alpha = fadeIn * fadeOut;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 원형 마스크 (부드러운 파티클 모양)
                float2 centeredUV = (i.uv - 0.5) * 2.0;
                float mask = 1.0 - smoothstep(0.7, 1.0, length(centeredUV));
                
                // 텍스처 샘플링
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb *= _Brightness;
                col.a *= _Alpha * i.alpha * mask;
                
                return col;
            }
            ENDCG
        }
    }
}