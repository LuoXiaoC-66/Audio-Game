Shader "Custom/GrassTerrain"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor1("Color1", Color) = (1,1,1,1)
        _BaseColor2("Color2", Color) = (1,1,1,1)
        _Cutoff("Cull Off", Range(0, 1)) = 0.1
        _BaseHeight("Base Height", float) = 0.5
        
        [Header(Occlusion)]
        _OccHeight("Height", Range(0,1)) = 0.5
        _OccExp("Exp", Range(1,10)) = 2
        _OccColor("Color", Color) = (0.2,0.2,0.2,1)

        [Header(Light)]
        _BaseAddtive("Base Addtive", Range(0, 1)) = 0.5
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.5

        [Header(Wind)]
        _WindDirAndStrength("Wind World Direction And Strength", vector) = (1,0,0,1)
        _WindNoise("Wind Noise", 2D) = "white" {}
        _WindNoiseStrength("Wind Noise Strength", float) = 0.5
        _StormParams("Storm(Begin,Keep,End,Slient)",Vector) = (1,100,40,100)
        _StormStrength("StormStrength", Range(0, 40)) = 30
    }

    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            ZWrite On
            ZTest On
            Cull Off

            HLSLPROGRAM
            #pragma vertex PassVertex
            #pragma fragment PassFragment

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                uint instanceID     : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
                float4 colorAndOcc  : COLOR;
            };

            TEXTURE2D_X(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseColor1;
            float4 _BaseColor2;
            float _Cutoff;
            float _BaseHeight;

            float _OccHeight;
            float _OccExp;
            float4 _OccColor;

            float _BaseAddtive;
            float _ShadowStrength;

            float2 _GrassSize;
            struct GrassInfo
            {
                float4x4 localToWorld;
            };
            StructuredBuffer<GrassInfo> _GrassInfos;

            float4 _WindDirAndStrength;
            sampler2D _WindNoise;
            float _WindNoiseStrength;
            float4 _StormParams;
            float _StormStrength;
            #define StormFront _StormParams.x
            #define StormMiddle _StormParams.y
            #define StormEnd _StormParams.z
            #define StormSlient _StormParams.w

            float3 _WorldPosOffset;
            float3 _PlayerPos;
            float4 _PlayerParams = float4(1,-1,0,0); //radius strength

            float _CurveValueX;
            float _CurveValueY;

            // 单纯的力太单调了，所以我们加入噪声扰动一下
            float NoiseWind(float3 worldPos, float windStrength)
            {
                float2 noiseUV = (worldPos.xz - _Time.y) / 30;
                float noiseValue = tex2Dlod(_WindNoise, float4(noiseUV,0,0)).r;
                //通过sin函数进行周期摆动,乘以windStrength来控制摆动频率。通常风力越强，摆动频率越高
                noiseValue = sin(noiseValue * windStrength);
                //将扰动再加到风力上,_WindNoiseStrength为扰动幅度，通过材质球配置
                windStrength += noiseValue * _WindNoiseStrength;
                return windStrength;
            }

            // 模拟风浪（风吹麦浪的感觉）
            float StormWind(float3 worldPos, float3 windDir, float windStrength)
            {
                // 风浪
                float storm = _Time.y * (windStrength + _StormStrength);
                // 浪的间隔
                float stormInterval = StormFront + StormMiddle + StormEnd + StormSlient;
                float offsetInInterval = storm % stormInterval;
                float strength = 0;
                if(offsetInInterval < StormFront)
                {
                    //前部,x从0到1
                    strength = offsetInInterval * rcp(StormFront);
                }
                else if(offsetInInterval < StormFront + StormMiddle)
                {
                    //中部
                    strength = 1;
                }
                else if(offsetInInterval < StormFront + StormMiddle + StormEnd)
                {
                    //尾部,x从1到0
                    strength = (StormFront + StormMiddle + StormEnd - offsetInInterval) / StormEnd;
                }

                //基础风力 + 强力风力
                return windStrength + _StormStrength * 0.025 * strength;     
            }

            // 应用风对顶点的偏移作用
            float3 ApplyAllWind(float3 worldPos, float3 worldUpDir, float vertexLocalHeight, float3 windDir, float windStrength)
            {
                windDir = normalize(windDir);
                windStrength = saturate(windStrength);
                
                float radian = windStrength * PI * 0.5f; //根据风力，计算草弯曲角度，从0到90度
                float x,y;  //弯曲后,x为单位球在风方向计量，y为草生长方向计量
                sincos(radian, x, y);
                windDir = windDir - dot(windDir, worldUpDir); //得到wind与grassUpWS的正交向量
                float3 windPos = x * windDir + y * worldUpDir;

                return worldPos + (windPos - worldUpDir) * vertexLocalHeight;
            }

            // 应用交互风效
            float3 ApplyPlayerWind(float3 worldPos, float3 worldUpDir, float vertexLocalHeight)
            {
                if (_PlayerParams.y < 0)
                {
                    return worldPos;
                }
                float3 windDir = normalize(float3(worldPos.x - _PlayerPos.x,0,worldPos.z - _PlayerPos.z));
                float windStrength = _PlayerParams.y * saturate((1 - distance(worldPos, _PlayerPos) / _PlayerParams.x));
                
                float radian = windStrength * PI * 0.5f; //根据风力，计算草弯曲角度，从0到90度
                float x,y;  //弯曲后,x为单位球在风方向计量，y为草生长方向计量
                sincos(radian, x, y);
                windDir = windDir - dot(windDir, worldUpDir); //得到wind与grassUpWS的正交向量
                float3 windPos = x * windDir + y * worldUpDir;

                return worldPos + (windPos - worldUpDir) * vertexLocalHeight;
            }

            // 随机高度
            float RandomHeight(float2 xz)
            {
                float noiseValue = tex2Dlod(_WindNoise, float4(xz,0,0)).r;
                return noiseValue * 0.5;
            }

            // 随机颜色
            float4 RandomColor(float2 xz)
            {
                float noiseValue = tex2Dlod(_WindNoise, float4(xz, 0, 0)).r;
                return lerp(_BaseColor1, _BaseColor2, noiseValue);
            }

            // 光照计算
            float3 LightFunction(Light light, float3 normalWS, float3 albedo)
            {
                float3 color = albedo;
                float3 lightColor = light.color * light.shadowAttenuation;
                float diffuse = (saturate(dot(light.direction, normalWS)) * (1 - _BaseAddtive) + _BaseAddtive);

                return albedo * lightColor * diffuse;
            }

            Varyings PassVertex(Attributes input)
            {
                Varyings output;
                float2 uv = input.uv;
                float4 positionOS = input.positionOS;
                positionOS.y += _BaseHeight;
                positionOS.xy = positionOS.xy * _GrassSize;
                float3 normalOS = input.normalOS;

                uint instanceID = input.instanceID;
                GrassInfo grassInfo = _GrassInfos[instanceID];
                float4 positionWS = mul(grassInfo.localToWorld, positionOS);
                positionWS.xyz += _WorldPosOffset;
                float depth = pow((positionWS - _WorldSpaceCameraPos).z, 2);
                positionWS.x += depth * _CurveValueX;
                positionWS.y += depth * _CurveValueY;
                float3 normalWS = mul(grassInfo.localToWorld, float4(normalOS,0)).xyz;
                float3 grassUpDir = mul(grassInfo.localToWorld, float4(0,1,0,0)).xyz;

                // 随机颜色
                float3 randomColor = RandomColor(mul(grassInfo.localToWorld, abs(input.positionOS)).xz).rgb;
                // 遮蔽
                float occ = saturate(pow(saturate(_OccHeight - uv.y), _OccExp));
                // 随机高度
                positionWS.y += RandomHeight(positionWS.xz) * positionOS.y;
                // 随机风效
                float3 windDir = normalize(_WindDirAndStrength.xyz);
                float windStrength = _WindDirAndStrength.w;
                windStrength = StormWind(positionWS.xyz, windDir, windStrength);
                windStrength = NoiseWind(positionWS.xyz, windStrength);
                positionWS.xyz = ApplyAllWind(positionWS.xyz, grassUpDir, positionOS.y, windDir, windStrength);
                // 来自于玩家的风
                positionWS.xyz = ApplyPlayerWind(positionWS.xyz, grassUpDir, positionOS.y);

                output.uv = uv;
                output.positionWS = positionWS.xyz;
                output.positionCS = mul(UNITY_MATRIX_VP, positionWS);
                output.normalWS = normalWS;
                output.colorAndOcc = float4(randomColor, occ);

                return output;
            }

            float4 PassFragment(Varyings input) : SV_Target
            {
                float4 albedo = SAMPLE_TEXTURE2D_X(_BaseMap, sampler_BaseMap, input.uv);
                if(albedo.a < _Cutoff)
                {
                    discard;
                }
                albedo.rgb = lerp(albedo.rgb * input.colorAndOcc.rgb, _OccColor.rgb, input.colorAndOcc.w);

                float4 color = float4(0, 0, 0, 1);

                Light mainLight;
            #if _MAIN_LIGHT_SHADOWS
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                mainLight = GetMainLight(shadowCoord);
            #else
                mainLight = GetMainLight();
            #endif
                color.rgb += LightFunction(mainLight, input.normalWS, albedo.rgb);

                return color;
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}