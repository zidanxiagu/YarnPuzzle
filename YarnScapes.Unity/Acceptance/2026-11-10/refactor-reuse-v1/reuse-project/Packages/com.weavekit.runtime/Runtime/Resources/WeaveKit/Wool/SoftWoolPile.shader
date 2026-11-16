Shader "WeaveKit/Volume Wool Pile"
{
    Properties
    {
        _FiberTex ("Fine cotton filaments", 2D) = "gray" {}
        _LightDirection ("Diffuse light", Vector) = (-0.45,0.55,-0.75,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        CGINCLUDE
        #include "SoftWool.cginc"
        WoolVarying nearVert(WoolInput v) { return WoolVertex(v,.010); }
        WoolVarying farVert(WoolInput v) { return WoolVertex(v,.026); }
        fixed4 Pile(WoolVarying i,float layer) : SV_Target
        {
            clip(i.color.a-.6);
            float4 fiber = tex2D(_FiberTex,i.uv*float2(.76,.88));
            float coverage = saturate(fiber.g*4.2+.025);
            // Spatially varied tufts prevent the outer shell from describing
            // a second continuous polygonal copy of the original tube.
            float2 tuftUv=i.uv*float2(23,8);
            float2 tile=floor(tuftUv);
            float tuft=frac(sin(dot(tile,float2(127.1,311.7)))*43758.5453);
            float aa=saturate(max(fwidth(tuftUv.x),fwidth(tuftUv.y)));
            float tuftCoverage=lerp(smoothstep(.20,.75,tuft),.52,aa);
            coverage*=lerp(1,tuftCoverage,layer);
            // Length data is filtered continuously. A hard height threshold
            // erased the outer layer once its texture reached smaller mips.
            float length = lerp(.55,1,saturate(fiber.a));
            float rim=1-saturate(abs(normalize(i.normal).z));
            float alpha=coverage*length*lerp(.20,.65,pow(rim,.55))*(1-layer*.4)*_FuzzStrength;
            return fixed4(WoolDiffuse(i,1),alpha);
        }
        fixed4 nearFrag(WoolVarying i):SV_Target { return Pile(i,.22); }
        fixed4 farFrag(WoolVarying i):SV_Target { return Pile(i,.53); }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex nearVert
            #pragma fragment nearFrag
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex farVert
            #pragma fragment farFrag
            ENDCG
        }
    }
}
