Shader "Hidden/Ripple Effect"
{
    Properties
    {
        _MainTex("Base", 2D) = "white" {}
        _GradTex("Gradient", 2D) = "white" {}
        _Reflection("Reflection Color", Color) = (0, 0, 0, 0)
        _Params1("Parameters 1", Vector) = (1, 1, 0.8, 0)
        _Params2("Parameters 2", Vector) = (1, 1, 1, 0)
        _Drop1("Drop 1", Vector) = (0.49, 0.5, 0, 0)
        _Drop2("Drop 2", Vector) = (0.50, 0.5, 0, 0)
        _Drop3("Drop 3", Vector) = (0.51, 0.5, 0, 0)
        _Drop4("Drop 4", Vector) = (0.52, 0.5, 0, 0)
        _Drop5("Drop 5", Vector) = (0.52, 0.5, 0, 0)
        _Drop6("Drop 6", Vector) = (0.5, 0.5, 0, 0)
        _Drop7("Drop 7", Vector) = (0.5, 0.5, 0, 0)
        _Drop8("Drop 8", Vector) = (0.5, 0.5, 0, 0)
        _Drop9("Drop 9", Vector) = (0.5, 0.5, 0, 0)
        _Drop10("Drop 10", Vector) = (0.5, 0.5, 0, 0)
        _WaveParams("Wave Parameters", Vector) = (1, 1, 0, 0)
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float2 _MainTex_TexelSize;

    sampler2D _GradTex;

    half4 _Reflection;
    float4 _Params1;    // [ aspect, 1, scale, 0 ]
    float4 _Params2;    // [ 1, 1/aspect, refraction, reflection ]

    float3 _Drop1;
    float3 _Drop2;
    float3 _Drop3;
    float3 _Drop4;
    float3 _Drop5;
    float3 _Drop6;
    float3 _Drop7;
    float3 _Drop8;
    float3 _Drop9;
    float3 _Drop10;
    float4 _WaveParams; // xy = width, intensity

    float wave(float2 position, float2 origin, float time)
    {
        // Ajustar la posición según el aspect ratio
        float2 adjustedPos = position * float2(_Params2.x, 1);
        float2 adjustedOrigin = origin * float2(_Params2.x, 1);
        
        float d = length(adjustedPos - adjustedOrigin) * _Params1.w; // multiplicar por zoom
        float t = time - (d / _WaveParams.x) * _Params1.z; // ajustar ancho
        return (tex2D(_GradTex, float2(t, 0)).a - 0.5f) * 2 * _WaveParams.y; // multiplicar por intensidad
    }

    float allwave(float2 position)
    {
        return
            wave(position, _Drop1.xy, _Drop1.z) +
            wave(position, _Drop2.xy, _Drop2.z) +
            wave(position, _Drop3.xy, _Drop3.z) +
            wave(position, _Drop4.xy, _Drop4.z) +
            wave(position, _Drop5.xy, _Drop5.z) +
            wave(position, _Drop6.xy, _Drop6.z) +
            wave(position, _Drop7.xy, _Drop7.z) +
            wave(position, _Drop8.xy, _Drop8.z) +
            wave(position, _Drop9.xy, _Drop9.z) +
            wave(position, _Drop10.xy, _Drop10.z);
    }

    half4 frag(v2f_img i) : SV_Target
    {
        const float2 dx = float2(0.01f, 0);
        const float2 dy = float2(0, 0.01f);

        float2 p = i.uv;  // Ya no multiplicamos por _Params1.xy aquí

        float w = allwave(p);
        float2 dw = float2(allwave(p + dx) - w, allwave(p + dy) - w);

        float2 duv = dw * _Params2.xy * 0.2f * _Params2.z;
        half4 c = tex2D(_MainTex, i.uv + duv);
        float fr = pow(length(dw) * 3 * _Params2.w, 3);

        return lerp(c, _Reflection, fr);
    }

    ENDCG

    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            Fog { Mode off }
            CGPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest 
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    } 
}
