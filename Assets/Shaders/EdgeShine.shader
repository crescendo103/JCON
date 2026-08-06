// usingweaponUI(edge) 같은 UI 프레임 스프라이트 위로 대각선 빛이 주기적으로 스치는 효과.
// HealthBarFlash.shader와 같은 구조(URP HLSL, Image.material에 직접 꽂는 방식)이지만,
// 그 쪽은 C# 코루틴이 _FlashAmount를 매 프레임 갱신해야 동작하는 반면 이건 셰이더가
// _Time만으로 스스로 반복 재생돼서 별도 스크립트 없이 머티리얼만 붙이면 바로 반짝인다.
Shader "Custom/EdgeShine"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineWidth ("Shine Width (0~1 UV)", Range(0.02, 1)) = 0.15
        _CycleDuration ("Cycle Duration (sec, 스침 간격)", Float) = 2.5
        _SweepDuration ("Sweep Duration (sec, 한 번 스치는 시간)", Float) = 0.6
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ShineColor;
            float _ShineWidth;
            float _CycleDuration;
            float _SweepDuration;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 baseColor = texColor * IN.color * _Color;

                // _CycleDuration마다 한 번, 그 안에서 _SweepDuration 동안만 빛줄기가 좌하단에서
                // 우상단으로(diag = uv.x+uv.y 기준) 지나가고 나머지 시간은 쉰다.
                float cycleT = frac(_Time.y / max(_CycleDuration, 0.0001));
                float sweepT = cycleT * _CycleDuration / max(_SweepDuration, 0.0001);
                float pos = lerp(-_ShineWidth, 1.0 + _ShineWidth, saturate(sweepT));

                float diag = (IN.uv.x + IN.uv.y) * 0.5;
                float band = 1.0 - saturate(abs(diag - pos) / _ShineWidth);
                band *= step(sweepT, 1.0); // 스침이 끝난 뒤(대기 구간)엔 빛을 완전히 끈다
                band *= band; // 가장자리를 더 뾰족하게

                // texColor.a로 마스킹해서 프레임 스프라이트 모양 밖으로는 빛이 새지 않는다.
                float3 finalRGB = baseColor.rgb + _ShineColor.rgb * band * texColor.a;
                return float4(finalRGB, baseColor.a);
            }
            ENDHLSL
        }
    }
}
