Shader "Hidden/DirtBrush"
{
    Properties{}
    SubShader
    {
        Tags{ "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; // incoming mask
            float4 _MainTex_TexelSize;
            float2 _BrushUV;     // 0..1
            float _BrushRadius;  // in UV
            float _BrushStrength; // 0..1 (amount to remove dirt)

            struct appdata { float4 vertex: POSITION; float2 uv: TEXCOORD0; };
            struct v2f { float4 pos: SV_POSITION; float2 uv: TEXCOORD0; };
            v2f vert(appdata v){ v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i): SV_Target
            {
                float cur = tex2D(_MainTex, i.uv).r; // 1=dirt
                float d = distance(i.uv, _BrushUV);
                float inBrush = smoothstep(_BrushRadius, _BrushRadius*0.7, _BrushRadius - d);
                // Decrease mask (clean) within brush
                float newMask = saturate(cur - inBrush * _BrushStrength);
                return fixed4(newMask, newMask, newMask, 1);
            }
            ENDCG
        }
    }
}
