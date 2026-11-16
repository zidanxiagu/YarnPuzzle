Shader "YarnScapes/Boundary Wool Fiber"
{
    Properties
    {
        _WeaveGrid("Completed color grid",2D)="black" {}
        _BoardSize("Grid size",Vector)=(60,60,0,0)
    }
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
            sampler2D _WeaveGrid;float4 _BoardSize;
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 cell:TEXCOORD1;};
            struct v2f {float4 position:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float3 cell:TEXCOORD1;};
            v2f vert(appdata v)
            {
                v2f o;
                float age=saturate((_Time.y-v.cell.z)/.18);
                float pop=sin(age*3.14159265)*.075;
                float3 p=v.vertex.xyz;
                p.xy=v.cell.xy+(p.xy-v.cell.xy)*(1+pop);p.z*=1+pop*1.7;
                p.xy+=p.z*float2(-.12,.16);
                o.position=UnityObjectToClipPos(float4(p,1));
                o.color=v.color;o.uv=v.uv;o.cell=float3(v.cell.xy,v.cell.w);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float edge=exp2(-3.8*pow(i.uv.y*2-1,2));
                float taper=smoothstep(0,.15,i.uv.x)*(1-smoothstep(.68,1,i.uv.x));
                float visibility=1;
                if(i.cell.z>.5)
                {
                    float2 direction=i.cell.z<1.5?float2(-1,0):i.cell.z<2.5?float2(1,0):i.cell.z<3.5?float2(0,-1):float2(0,1);
                    float2 ownUv=(i.cell.xy+_BoardSize.xy*.5)/_BoardSize.xy;
                    float2 uv=ownUv+direction/_BoardSize.xy;
                    float valid=step(0,uv.x)*step(0,uv.y)*step(uv.x,1)*step(uv.y,1);
                    float4 own=tex2D(_WeaveGrid,ownUv),neighbor=tex2D(_WeaveGrid,uv)*valid;
                    float same=1-step(.001,abs(own.r-neighbor.r));
                    visibility=lerp(1,lerp(.72,.12,same),step(.5,neighbor.g));
                }
                return fixed4(i.color.rgb,i.color.a*edge*taper*visibility);
            }
            ENDCG
        }
    }
}
