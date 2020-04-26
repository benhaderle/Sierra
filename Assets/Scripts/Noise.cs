using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class Noise 
{
    public enum NormalizeMode { Local, Global };
    //local normalizeMode normalizes max height by chunk instead of doing it globally

    public static float[,] generateNoise(NormalizeMode normalizeMode, int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++) {
            octaveOffsets[i].x = prng.Next(-100000, 100000) + offset.x;
            octaveOffsets[i].y = prng.Next(-100000, 100000) - offset.y;
            
            maxPossibleHeight += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        float[,] noiseMap = new float[mapWidth, mapHeight];

        //clamping scale
        if (scale <= 0) scale = .00001f;

        float maxLocalHeight = float.MinValue;
        float minHeight = float.MaxValue;

        //generating noise samples
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                amplitude = 1;
                frequency = 1;
                float localNoiseHeight = 0;

                float halfWidth = mapWidth / 2;
                float halfHeight= mapHeight / 2;

                //generating octave samples
                for (int  i = 0; i < octaves; i++) {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float p = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

                    //increasing variables
                    localNoiseHeight += p * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                noiseMap[x, y] = localNoiseHeight;

                //updating min max
                if (localNoiseHeight > maxLocalHeight) maxLocalHeight = localNoiseHeight;
                else if (localNoiseHeight < minHeight) minHeight = localNoiseHeight;
            }
        }

        //normalizing our generated samples
        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                if (normalizeMode == NormalizeMode.Local) {
                    noiseMap[x, y] = Mathf.InverseLerp(minHeight, maxLocalHeight, noiseMap[x, y]);
                }
                else {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight * 1.06f);    //use the last float as a tuning variable
                    //making sure sample is not negative
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }
        /*
         * 
         * FOR WRITING NOISE IMAGES
         * 
         *
        Texture2D tex = new Texture2D(mapWidth, mapHeight, TextureFormat.RGB24, false);
        Color[] colors = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int i = x + y * mapWidth;
                colors[i] = Color.white * noiseMap[x, y];
            }
        }
        tex.SetPixels(colors);
        tex.Apply();

        // Encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();
        Object.Destroy(tex);
        File.WriteAllBytes(Application.dataPath + "/../noise.png", bytes);
        /**/

        return noiseMap;
    }
}
