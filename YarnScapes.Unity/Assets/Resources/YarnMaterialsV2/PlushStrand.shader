Shader "YarnScapes/Plush Strand"
{
    Properties
    {
        _FiberTex ("Cotton roughness", 2D) = "gray" {}
        _LightDirection ("Key light", Vector) = (-0.45,0.55,-0.75,0)
        _Fuzz ("Fiber pass", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _FiberTex;
            float4 _LightDirection;
            float _Fuzz;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 cell : TEXCOORD1;
            };
            struct v2f
            {
                float4 position : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 color : COLOR;
                float3 local : TEXCOORD2;
            };
            v2f vert(appdata v)
            {
                v2f o;
                float age = saturate((_Time.y-v.cell.z)/0.18);
                float pop = sin(age*3.14159265)*0.075;
                float3 p = v.vertex.xyz;
                p.xy = v.cell.xy + (p.xy-v.cell.xy)*(1+pop);
                p.z *= 1+pop*1.7;
                // A small oblique projection exposes the sides of the raised yarn.
                p.xy += p.z*float2(-0.12,0.16);
                o.position = UnityObjectToClipPos(float4(p,1));
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                o.color = v.color;
                o.local = p;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float2 fiberUv = i.uv*float2(0.7,1.0);
                float fiber = tex2D(_FiberTex, fiberUv).r;
                float fiberAcross = tex2D(_FiberTex, fiberUv+float2(0,0.002)).r
                                  - tex2D(_FiberTex, fiberUv-float2(0,0.002)).r;
                float3 dp1=ddx(i.local), dp2=ddy(i.local);
                float2 duv1=ddx(i.uv), duv2=ddy(i.uv);
                float determinant=duv1.x*duv2.y-duv1.y*duv2.x;
                float3 along=normalize((dp1*duv2.y-dp2*duv1.y)*sign(determinant)+float3(0.00001,0,0));
                float3 across=normalize(cross(n,along));
                // Fine aligned fibers break up the smooth tube highlight.
                n=normalize(n+across*fiberAcross*1.2);
                float3 l = normalize(_LightDirection.xyz);
                float ndl = dot(n,l);
                float wrap = saturate((ndl+0.28)/1.28);
                float twist = sin((i.uv.x*4.8+i.uv.y*2.5)*6.2831853);
                float roughness = 0.98 + (fiber-0.5)*0.50 + twist*0.014;
                float cavity = lerp(0.86,1.0,i.color.a);
                float shade = (0.66+0.39*wrap)*roughness*cavity;
                // Broad fiber sheen, rather than a glossy plastic highlight.
                float rim = pow(1-saturate(abs(n.z)),2.0)*0.10;
                float soft = pow(saturate(dot(n,normalize(l+float3(0,0,-1)))),6)*0.008;
                float3 rgb = i.color.rgb*shade + (rim+soft)*lerp(i.color.rgb,0.68,0.22);
                return fixed4(rgb,1);
            }
            ENDCG
        }
    }
}
