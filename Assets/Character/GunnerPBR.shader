Shader "Character/Gunner PBR (Roughness)"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        [NoScaleOffset] _MetallicMap ("Metallic", 2D) = "black" {}
        [Normal][NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
        [NoScaleOffset] _RoughnessMap ("Roughness", 2D) = "white" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1
        _MetallicStrength ("Metallic Strength", Range(0, 1)) = 1
        _SmoothnessStrength ("Smoothness Strength", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        #include "UnityStandardUtils.cginc"

        sampler2D _MainTex;
        sampler2D _MetallicMap;
        sampler2D _BumpMap;
        sampler2D _RoughnessMap;

        fixed4 _Color;
        half _BumpScale;
        half _MetallicStrength;
        half _SmoothnessStrength;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half metallic = tex2D(_MetallicMap, IN.uv_MainTex).r;
            half roughness = tex2D(_RoughnessMap, IN.uv_MainTex).r;

            o.Albedo = albedo.rgb;
            o.Metallic = saturate(metallic * _MetallicStrength);
            o.Smoothness = saturate((1.0h - roughness) * _SmoothnessStrength);
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);
            o.Occlusion = 1.0h;
            o.Alpha = albedo.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
