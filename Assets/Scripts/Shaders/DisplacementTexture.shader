Shader "Unlit/DisplacementTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", color) = (1,0,0,0)
		_DispPos ("DispPos", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

			fixed4 _DispPos, _Color;

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				float dispFactor = saturate(pow(saturate(1 - (distance(i.uv, _DispPos.xy))), 64));
				fixed4 col = _Color * dispFactor;

				
                return col;
            }
            ENDCG
        }
    }
}
