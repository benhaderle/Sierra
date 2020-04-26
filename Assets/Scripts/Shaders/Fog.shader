Shader "Fog"
{
    Properties
    {
		_MainTex("Base (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			#define NUMKEYS 5

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

			sampler2D _CameraDepthTexture;
			sampler2D _MainTex;
			
			float4 _FogColors[NUMKEYS];
			float _FogTimes[NUMKEYS];

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv));
				
				int lowerBound = 0;
				for (int it = 1; it < NUMKEYS - 1; it++) {
					if (depth > _FogTimes[it])	lowerBound = it;
				} 
				half range = _FogTimes[lowerBound + 1] - _FogTimes[lowerBound];
				float fogColorMix = (depth - _FogTimes[lowerBound]) / range;
				float4 fogColor = lerp(_FogColors[lowerBound], _FogColors[lowerBound + 1], saturate(fogColorMix));

				float4 col = tex2D(_MainTex, i.uv.xy);
				if(depth < 0.9999) col = col * (1 - fogColor.a) + fogColor * fogColor.a * .75;

				return col;
            }
            ENDCG
        }
    }
}
