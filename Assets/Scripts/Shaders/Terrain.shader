Shader "Custom/Terrain"
{
	Properties
	{
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		const static int MAX_LAYERS = 2;
		const static float EPSILON = 1E-4;

		float minBlend;
		float maxBlend;

		int layerCount;
		float3 baseColors[MAX_LAYERS];
		float baseStartHeights[MAX_LAYERS];
		float baseBlends[MAX_LAYERS];
		float baseColorStrength[MAX_LAYERS];
		float baseTextureScales[MAX_LAYERS];

		sampler2D testTexture;
		float testScale;

		UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input
        {
			float3 worldPos;
			float3 worldNormal;
        };

		float inverselerp(float min, float max, float val) {
			return saturate((val - min) / (max - min));
		}

		float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPos = worldPos / scale;
				
			float3 xProj = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProj = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProj = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
			//o.Albedo = xProj + yProj + zProj;

			return xProj + yProj + zProj;
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			float d = abs(dot(IN.worldNormal, float3(0, 1, 0)));
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			float rockSnowWeight = saturate(inverselerp(minBlend, maxBlend, d));
				
			float3 rock_baseColor = baseColors[0] * baseColorStrength[0];
			float3 rock_textureColor = triplanar(IN.worldPos, baseTextureScales[0], blendAxes, 0) * (1 - baseColorStrength[0]);
			float3 snow_baseColor = baseColors[1] * baseColorStrength[1];
			float3 snow_textureColor = triplanar(IN.worldPos, baseTextureScales[1], blendAxes, 1) * (1 - baseColorStrength[1]);

			o.Albedo = ((rock_baseColor + rock_textureColor) * (1 - rockSnowWeight)) + ((snow_baseColor + snow_textureColor) * rockSnowWeight);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
