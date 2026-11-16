Shader "WeaveKit/Actual Wool Height"
{
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        Pass
        {
            Cull Off ZWrite On
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input {float4 vertex:POSITION;float4 cell:TEXCOORD1;};
            struct Varying {float4 position:SV_POSITION;float height:TEXCOORD0;};
            Varying vert(Input v)
            {
                float age=saturate((_Time.y-v.cell.z)/.18);
                float pop=sin(age*3.14159265)*.075;
                float3 p=v.vertex.xyz;
                p.xy=v.cell.xy+(p.xy-v.cell.xy)*(1+pop);p.z*=1+pop*1.7;
                p.xy+=p.z*float2(-.12,.16);
                Varying o;o.position=UnityObjectToClipPos(float4(p,1));o.height=.3-p.z;return o;
            }
            float4 frag(Varying i):SV_Target{return float4(i.height,1,0,1);}
            ENDCG
        }
    }
}
