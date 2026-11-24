Shader "JuanCapi/EnvironmentCutout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off Lighting Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment EnvironmentFrag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"
            fixed4 EnvironmentFrag(v2f input) : SV_Target
            {
                fixed4 c=SampleSpriteTexture(input.texcoord);
                float key=max(0,min(c.r,c.b)-c.g);
                c.a*=1-smoothstep(.06,.43,key);
                // Remove magenta spill from antialiased edges before premultiplication.
                c.r-=key*.7;c.b-=key;
                c*=input.color;c.rgb*=c.a;
                return c;
            }
            ENDCG
        }
    }
}
