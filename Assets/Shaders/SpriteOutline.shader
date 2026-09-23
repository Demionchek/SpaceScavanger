// Обводка спрайта отдельным объектом-силуэтом: рисует только кольцо вокруг
// непрозрачных пикселей. Квад раздвигается в вершинном шейдере, поэтому
// обводка не обрезается краем спрайта и не нужен прозрачный запас в текстуре.
// Параметры спрайта (_SpriteRect, _OutlineOffset) ставит SpriteOutline.cs —
// без него ширина нулевая и обводки не видно.
Shader "Game/2D/Sprite Outline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _SpriteRect ("Sprite UV Rect", Vector) = (0,0,1,1)
        [HideInInspector] _OutlineOffset ("Outline (uv.xy, object.zw)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _SpriteRect;
                float4 _OutlineOffset;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();

                float2 center = (_SpriteRect.xy + _SpriteRect.zw) * 0.5;
                float2 dir = sign(input.uv - center);

                float3 positionOS = input.positionOS;
                positionOS.xy += dir * _OutlineOffset.zw;
                positionOS = UnityFlipSprite(positionOS, unity_SpriteProps.xy);

                o.positionCS = TransformObjectToHClip(positionOS);
                o.uv = input.uv + dir * _OutlineOffset.xy;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            // Вне границ спрайта альфы нет: иначе на атласе или нарезанном листе
            // в обводку затекли бы соседние кадры.
            half SampleAlpha(float2 uv)
            {
                float2 inside = step(_SpriteRect.xy, uv) * step(uv, _SpriteRect.zw);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * inside.x * inside.y;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                float2 r = _OutlineOffset.xy;
                float2 d = r * 0.7071;

                half neighbours = SampleAlpha(input.uv + float2(r.x, 0));
                neighbours = max(neighbours, SampleAlpha(input.uv - float2(r.x, 0)));
                neighbours = max(neighbours, SampleAlpha(input.uv + float2(0, r.y)));
                neighbours = max(neighbours, SampleAlpha(input.uv - float2(0, r.y)));
                neighbours = max(neighbours, SampleAlpha(input.uv + d));
                neighbours = max(neighbours, SampleAlpha(input.uv - d));
                neighbours = max(neighbours, SampleAlpha(input.uv + float2(d.x, -d.y)));
                neighbours = max(neighbours, SampleAlpha(input.uv + float2(-d.x, d.y)));

                half alpha = saturate(neighbours - SampleAlpha(input.uv));
                return half4(input.color.rgb, input.color.a * alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
