Shader "SandJamTest/SandStream"
{
    Properties { _Color ("Sand color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            output vert(input v){output o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(output i):SV_Target
            {
                float grain=frac(sin(floor(i.uv.x*180-_Time.y*110)*12.9898)*43758.5453);
                float edge=saturate(1-abs(i.uv.y*2-1));
                return fixed4(_Color.rgb*(.85+grain*.3),edge*(.35+grain*.6));
            }
            ENDCG
        }
    }
}
