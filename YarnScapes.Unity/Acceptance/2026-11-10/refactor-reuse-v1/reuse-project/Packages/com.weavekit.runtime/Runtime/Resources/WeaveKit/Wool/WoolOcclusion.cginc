sampler2D _WoolHeight;
float4 _WoolBoardSize;
float _ContactStrength,_FuzzStrength,_VolumeLighting;
float _NeutralGray;
float2 WoolHeight(float2 p)
{
    float2 uv=(p+_WoolBoardSize.xy*.5)/_WoolBoardSize.xy;
    float valid=step(0,uv.x)*step(0,uv.y)*step(uv.x,1)*step(uv.y,1);
    float2 data=tex2D(_WoolHeight,uv).rg;
    return float2(data.r/max(data.g,.001)-.3,saturate(data.g)*valid);
}
float Horizon(float3 p,float2 offset)
{
    float2 h=WoolHeight(p.xy+offset);
    return saturate((h.x+p.z-.006)/(length(offset)*.75+.018))*h.y;
}
float2 WoolVisibility(float3 p)
{
    float ao=Horizon(p,float2(.035,0))+Horizon(p,float2(-.035,0))
            +Horizon(p,float2(0,.035))+Horizon(p,float2(0,-.035));
    ao+=.65*(Horizon(p,float2(.060,.060))+Horizon(p,float2(-.060,.060))
            +Horizon(p,float2(.060,-.060))+Horizon(p,float2(-.060,-.060)));
    ao=saturate(ao/6.6);
    float shadow=0;
    float2 direction=normalize(float2(-.45,.55));
    [unroll] for(int j=0;j<7;j++)
    {
        float d=.018+j*.028;
        float2 h=WoolHeight(p.xy+direction*d);
        float excess=h.x-(-p.z+d*1.055);
        shadow=max(shadow,smoothstep(.005,.020+d*.025,excess)*h.y);
    }
    return float2(1-ao*.46*_ContactStrength,1-shadow*.60*_ContactStrength);
}
