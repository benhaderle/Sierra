using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise 
{
    public static float[,] generateNoise(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++) {
            octaveOffsets[i].x = prng.Next(-100000, 100000) + offset.x;
            octaveOffsets[i].y = prng.Next(-100000, 100000) + offset.y;
        }

        float[,] noiseMap = new float[mapWidth, mapHeight];

        //clamping scale
        if (scale <= 0) scale = .00001f;

        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;

        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                float halfWidth = mapWidth / 2;
                float halfHeight= mapHeight / 2;

                for (int  i = 0; i < octaves; i++) {
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                    float p = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

                    noiseHeight += p * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                noiseMap[x, y] = noiseHeight;
                if (noiseHeight > maxHeight) maxHeight = noiseHeight;
                else if (noiseHeight < minHeight) minHeight = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                noiseMap[x, y] = Mathf.InverseLerp(minHeight, maxHeight, noiseMap[x, y]);
            }
        }

                return noiseMap;
    }
}
