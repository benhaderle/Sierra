Shader "Skybox/Custom Skybox"
{
	Properties
	{
		//gradient
		_TopColor("Top Color", color) = (.5,.5,1,0)
		_MidColor("Middle Color", color) = (.5,1,.5,0)
		_BotColor("Bottom Color", color) = (1,.5,.5,0)
		_TopExp("Top Exponent", range(.01, 2)) = 1
		_BotExp("Bottom Exponent", range(.01, 2)) = 1
		//sun
		_SunPos("Sun Position", Vector) = (0,0,0)
		_SunColor("Sun Color", color) = (.5,.5,0, 0)
		_SunSize("Sun Size", float) = 1
		_SunFalloff("Sun Falloff", range(1, 50)) = 1	
		//halo
		_HaloColor("Halo Color", color) = (.5,.5,0,0)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off 
		Tags { "RenderType" = "Background" "Queue" = "Background" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				o.uv.y;
                return o;
            }

			fixed4 _TopColor;
			fixed4 _MidColor;
			fixed4 _BotColor;
			float _TopExp;
			float _BotExp;

			fixed3 _SunPos;
			fixed4 _SunColor;
			float _SunSize;
			float _SunFalloff;

			fixed4 _HaloColor;

			fixed4 frag(v2f i) : SV_Target
			{
				half4 color; 

				//tri color gradient
				half y = i.uv.y + .1f;
				half mix1 = pow(saturate(y * 2), _BotExp);
				half mix2 = pow(saturate((y - .5f) * 2), _TopExp);
				color = lerp(lerp(_BotColor, _MidColor, mix1), _TopColor, mix2);

				//sun
				half sunDist = length(_SunPos - i.uv);
				half spot = 1 - smoothstep(0, _SunSize, sunDist);
				half sunMix = 1 - pow(.125, spot * _SunFalloff);
				color = lerp(color, _SunColor, sunMix);

				//halo
				half center = i.uv.x - _SunPos.x;
				half ratPow = -1.8 * (log(_SunPos.y) - .1);
				half num = pow(2.25, ratPow);
				half denom = pow(3, ratPow);
				half haloMix = .3 * saturate((2 / sunDist) * (((-(center * center) + _SunPos.y * num) / (center * center + _SunPos.y * denom)) - y));
				color = lerp(color, _HaloColor, haloMix);

				//horizon
				half horizonMix = saturate((.1 - abs(y + .008 * sin(i.uv.x * 20))) / .1);
				color = lerp(color, _SunColor, horizonMix);

				return color;
            }
            ENDCG
        }
    }
}
