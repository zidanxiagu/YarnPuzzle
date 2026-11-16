#include "UnityCG.cginc"
sampler2D _FiberTex;
float4 _LightDirection;
struct WoolInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    float4 cell : TEXCOORD1;
};
struct WoolVarying
{
    float4 position : SV_POSITION;
    float3 normal : TEXCOORD0;
    float2 uv : TEXCOORD1;
    float4 color : COLOR;
    float3 local : TEXCOORD2;
};
WoolVarying WoolVertex(WoolInput v, float lift)
{
    WoolVarying o;
    float age = saturate((_Time.y-v.cell.z)/0.18);
    float pop = sin(age*3.14159265)*0.075;
    float3 p = v.vertex.xyz + v.normal * lift * step(0.6,v.color.a);
    p.xy = v.cell.xy + (p.xy-v.cell.xy)*(1+pop);
    p.z *= 1+pop*1.7;
    p.xy += p.z*float2(-0.12,0.16);
    o.position = UnityObjectToClipPos(float4(p,1));
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.uv = v.uv;
    o.color = v.color;
    o.local = p;
    return o;
}
float3 WoolDiffuse(WoolVarying i, float pile)
{
    float2 uv=i.uv*float2(.76,.88);
    float4 fiber = tex2D(_FiberTex,uv);
    float3 n = normalize(i.normal);
    float hu=tex2D(_FiberTex,uv+float2(.003,0)).r-tex2D(_FiberTex,uv-float2(.003,0)).r;
    float hv=tex2D(_FiberTex,uv+float2(0,.003)).r-tex2D(_FiberTex,uv-float2(0,.003)).r;
    float3 dp1=ddx(i.local),dp2=ddy(i.local);
    float2 du1=ddx(i.uv),du2=ddy(i.uv);
    float determinant=du1.x*du2.y-du1.y*du2.x;
    float3 along=normalize((dp1*du2.y-dp2*du1.y)*sign(determinant)+float3(.00001,0,0));
    float3 across=normalize(cross(n,along));
    n=normalize(n-(along*hu+across*hv)*1.05*step(.6,i.color.a));
    float wrapped = saturate((dot(n,normalize(_LightDirection.xyz))+.5)/1.5);
    // Broad diffuse light and same-color fiber scattering. No specular lobe.
    float light = .78 + .24*wrapped;
    // Curved, same-color filaments remain visible on the strand itself. This
    // is fine fiber scattering, with no continuous polished highlight.
    float filaments=saturate(fiber.g*2.4);
    // Subpixel filament contrast fades by its screen-space footprint. Fibers
    // follow the tube UV, rather than a world-space noise painted over the knit.
    float phase = i.uv.y*62.83+i.uv.x*27+sin(i.uv.x*13)*.7;
    float filamentAA = 1-smoothstep(.7,2.8,fwidth(phase));
    float fine = sin(phase)*filamentAA*.010;
    float grain = .95 + ((fiber.r-.5)*.64 + (fiber.b-.5)*.30 + filaments*.07+fine)*step(.6,i.color.a);
    float cavity = lerp(.91,1,i.color.a);
    float sheen = pow(1-saturate(abs(n.z)),1.6)*.018;
    return i.color.rgb*(light*grain*cavity+sheen+pile*.035);
}
