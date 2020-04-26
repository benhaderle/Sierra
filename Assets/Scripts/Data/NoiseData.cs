using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdateableData
{
    public int octaves;
    public int seed;
    public float scale;
    [Range(0, 1)]
    public float peristence;
    public float lacunarity;
    public Vector2 offset;

    public Noise.NormalizeMode normalizeMode;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
    }
#endif
}
