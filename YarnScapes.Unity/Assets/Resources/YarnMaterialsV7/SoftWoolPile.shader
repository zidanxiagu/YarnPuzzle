Shader "YarnScapes/Relaxed Knit Pile"
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
        WoolVarying nearVert(WoolInput v) { return WoolVertex(v,.005); }
        WoolVarying farVert(WoolInput v) { return WoolVertex(v,.011); }
        fixed4 Pile(WoolVarying i,float layer) : SV_Target
        {
            clip(i.color.a-.6);
            float4 fiber = tex2D(_FiberTex,i.uv*float2(.76,.88));
            float coverage = saturate(fiber.g*3.2-.05);
            float length = smoothstep(layer-.16,layer+.12,fiber.a);
            // The second sparse coat breaks up the silhouette of the first.
            float alpha = coverage*length*lerp(.72,.52,layer);
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
