Shader "Unlit/Bra"
{
    Properties {}
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            StructuredBuffer<float> _Buff;
            uint _N;

            v2f vert (appdata v)
            {
                v2f o;


                float3 wp = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;

                uint x = (uint)(wp.x * _N / 10);
                float y = _Buff[x] / exp(1);
                wp.y += y;

                o.vertex = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                o.uv = v.uv;
                

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                uint x = (uint)(i.uv.x * _N);
                return _Buff[x] / exp(1);
            }
            ENDCG
        }
    }
}
