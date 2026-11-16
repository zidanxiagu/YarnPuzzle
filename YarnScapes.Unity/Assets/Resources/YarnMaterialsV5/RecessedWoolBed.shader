Shader "YarnScapes/Fine Recessed Wool Bed"
{
    Properties
    {
        _FiberTex ("Wool grain",2D)="gray" {}
        _WeaveGrid ("Completed color grid",2D)="black" {}
        _BedPalette ("Wool color lookup",2D)="white" {}
        _BoardSize ("Grid size",Vector)=(60,60,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-20" }
        Pass
        {
            Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _FiberTex,_WeaveGrid,_BedPalette;
            float4 _BoardSize;
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 cell:TEXCOORD1;};
            struct v2f {float4 position:SV_POSITION;float4 color:COLOR;float2 local:TEXCOORD0;float2 center:TEXCOORD1;};
            v2f vert(appdata v)
            {
                v2f o;
                float age=saturate((_Time.y-v.cell.z)/.18);
                float pop=sin(age*3.14159265)*.035;
                float3 p=v.vertex.xyz;
                p.xy=v.cell.xy+(p.xy-v.cell.xy)*(1+pop);
                p.xy+=p.z*float2(-.12,.16);
                o.position=UnityObjectToClipPos(float4(p,1));
                o.local=v.uv; o.center=v.cell.xy; o.color=v.color;
                return o;
            }
            float4 Cell(float2 c)
            {
                float2 uv=(c+_BoardSize.xy*.5)/_BoardSize.xy;
                float inside=step(0,uv.x)*step(0,uv.y)*step(uv.x,1)*step(uv.y,1);
                return tex2D(_WeaveGrid,uv)*inside;
            }
            // Both sides of a color boundary evaluate this same world-space
            // curve, so alpha coverage meets without a white grid seam.
            float SharedEdge(float boundaryIndex,float along)
            {
                return sin(along*12.56637+sin(boundaryIndex*1.71)*.38)*.027
                     + sin(along*27.1+boundaryIndex*2.13)*.008;
            }
            float Margin(float distance,float offset,float4 neighbor,float ownId)
            {
                float same=1-step(.001,abs(neighbor.r-ownId));
                // Interior support overlaps. An exposed support edge retreats
                // behind the loops. Unlike the old quad it is not a silhouette.
                float completed=step(.5,neighbor.g);
                float inset=lerp(.043,lerp(-.030,-.034,same),completed);
                offset*=1-same*completed;
                return distance+offset-inset;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float own=Cell(i.center).r;
                float2 q=i.local;
                float2 w=i.center+q-.5;
                float dl=Margin(q.x,-SharedEdge(i.center.x-.5,w.y),Cell(i.center-float2(1,0)),own);
                float dr=Margin(1-q.x,SharedEdge(i.center.x+.5,w.y),Cell(i.center+float2(1,0)),own);
                float db=Margin(q.y,-SharedEdge(i.center.y-.5,w.x),Cell(i.center-float2(0,1)),own);
                float dt=Margin(1-q.y,SharedEdge(i.center.y+.5,w.x),Cell(i.center+float2(0,1)),own);
                float d=min(min(dl,dr),min(db,dt));
                float4 grain=tex2D(_FiberTex,w*.83);
                float edge=smoothstep(-.014,.019,d+(grain.g-.2)*.009);
                float shade=.84+(grain.r-.5)*.08;
                // Every overlapping bed evaluates a common color field. The
                // appearance must not depend on fill order or saved-load chunks.
                float2 c=floor(w+_BoardSize.xy*.5)+.5-_BoardSize.xy*.5;
                float2 selected=c;
                float2 local=w-c;
                if(local.x<-.5+SharedEdge(c.x-.5,w.y) && Cell(c-float2(1,0)).g>.5)selected.x-=1;
                else if(local.x>.5+SharedEdge(c.x+.5,w.y) && Cell(c+float2(1,0)).g>.5)selected.x+=1;
                if(local.y<-.5+SharedEdge(c.y-.5,w.x) && Cell(c-float2(0,1)).g>.5)selected.y-=1;
                else if(local.y>.5+SharedEdge(c.y+.5,w.x) && Cell(c+float2(0,1)).g>.5)selected.y+=1;
                float4 chosen=Cell(selected);
                float3 color=lerp(i.color.rgb,tex2D(_BedPalette,float2((chosen.r*255+.5)/256,.5)).rgb,step(.5,chosen.g));
                return fixed4(color*shade,edge);
            }
            ENDCG
        }
    }
}
