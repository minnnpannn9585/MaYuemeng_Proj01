Shader "Forest/FeedbackParticle"
{
    Properties { _Shape ("Shape", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off Lighting Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            float _Shape;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float r = length(p);
                float aa = max(fwidth(r), 0.008);
                float alpha;
                float light = 0;
                if (_Shape < 0.5)
                {
                    float edge = r + 0.055 * sin(p.x * 13 + p.y * 6) * sin(p.y * 11);
                    alpha = pow(saturate(1 - edge), 1.65);
                }
                else if (_Shape < 1.5)
                {
                    float d = pow(abs(p.x), 0.5) + pow(abs(p.y), 0.5);
                    alpha = 1 - smoothstep(0.92, 1.04 + aa, d);
                    alpha = max(alpha, 0.23 * exp(-r * r * 12));
                    light = 0.45 * exp(-r * r * 18);
                }
                else if (_Shape < 2.5)
                {
                    float ring = abs(r - 0.73);
                    alpha = (1 - smoothstep(0.015, 0.035 + aa, ring)) * 0.8;
                    alpha += 0.12 * exp(-ring * ring * 240);
                }
                else if (_Shape < 3.5) alpha = exp(-r * r * 5) * (1 - smoothstep(0.7, 1, r));
                else if (_Shape < 4.5)
                {
                    float d = abs(p.y) + p.x * p.x * 0.72;
                    alpha = (1 - smoothstep(0.36, 0.4 + aa, d)) * (1 - smoothstep(0.8, 0.95, abs(p.x)));
                    light = (1 - smoothstep(0.015, 0.04, abs(p.y + 0.025 * sin(p.x * 4)))) * 0.25;
                }
                else if (_Shape < 5.5) alpha = pow(saturate(1 - abs(p.x)), 0.65) * (1 - smoothstep(0.1, 0.7, abs(p.y)));
                else alpha = 1 - smoothstep(0.22, 0.5 + aa, r);
                return fixed4(lerp(i.color.rgb, 1, light), saturate(alpha) * i.color.a);
            }
            ENDHLSL
        }
    }
}
