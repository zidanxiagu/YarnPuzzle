Shader "YarnScapes/Soft Wool Fiber"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 cell:TEXCOORD1; };
            struct v2f { float4 position:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                float age=saturate((_Time.y-v.cell.z)/0.18);
                float pop=sin(age*3.14159265)*0.075;
                float3 p=v.vertex.xyz;
                p.xy=v.cell.xy+(p.xy-v.cell.xy)*(1+pop);
                p.z*=1+pop*1.7;
                p.xy+=p.z*float2(-0.12,0.16);
                o.position=UnityObjectToClipPos(float4(p,1));
                o.color=v.color; o.uv=v.uv;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float edge=exp2(-3.8*pow(i.uv.y*2-1,2));
                float taper=smoothstep(0,.15,i.uv.x)*(1-smoothstep(.68,1,i.uv.x));
                return fixed4(i.color.rgb, i.color.a*edge*taper);
            }
            ENDCG
        }
    }
}
