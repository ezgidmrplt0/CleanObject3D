Shader "Custom/DirtMaskedSurface"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DirtTex ("Dirt (RGB)", 2D) = "white" {}
        _DirtColor ("Dirt Color", Color) = (0.2,0.2,0.2,1)
        _DirtIntensity ("Dirt Intensity", Range(0,1)) = 1
        _DirtMask ("Dirt Mask (R)", 2D) = "white" {}
        _UseDirtTex ("Use Dirt Texture", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        sampler2D _DirtTex;
        fixed4 _DirtColor;
        half _DirtIntensity;
        sampler2D _DirtMask;
    half _UseDirtTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_DirtTex;
            float2 uv_DirtMask;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 baseCol = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed3 dirtTexRGB = tex2D(_DirtTex, IN.uv_DirtTex).rgb;
            fixed mask = tex2D(_DirtMask, IN.uv_DirtMask).r; // 1=dirt, 0=clean

            // Choose source for dirt: texture (if provided) else solid color
            fixed3 dirtRGB = lerp(_DirtColor.rgb, dirtTexRGB, saturate(_UseDirtTex));

            // Multiply darken: base * lerp(1, dirtRGB, mask*intensity)
            fixed blend = saturate(mask * _DirtIntensity);
            fixed3 finalRGB = baseCol.rgb * lerp(1.0, dirtRGB, blend);

            o.Albedo = finalRGB;
            o.Metallic = 0;
            o.Smoothness = 0.4;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
