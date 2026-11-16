Shader "YarnScapes/Soft Wool Strand"
{
    Properties
    {
        _FiberTex ("Fine cotton filaments", 2D) = "gray" {}
        _LightDirection ("Diffuse light", Vector) = (-0.45,0.55,-0.75,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off ZWrite On
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "SoftWool.cginc"
            WoolVarying vert(WoolInput v) { return WoolVertex(v,0); }
            fixed4 frag(WoolVarying i) : SV_Target { return fixed4(WoolDiffuse(i,0),1); }
            ENDCG
        }
    }
}
