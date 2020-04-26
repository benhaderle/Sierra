using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { Noise, Mesh, FalloffMap }
    public DrawMode drawMode;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    //tuning vairables
    [Range(0, MeshGenerator.NUM_SUPPORTED_CHUNK_SIZES - 1)]
    public int chunkSizeIndex;
    public int mapChunkSize
    {
        get
        {
            return MeshGenerator.SUPPORTED_CHUNK_SIZES[chunkSizeIndex] - 1;    //mapChunkSize + 1 should be an easily divisble number
        }
    } 
    [Range(0,MeshGenerator.NUM_SUPPORTED_LODS - 1)]
    public int levelOfDetailPreview;        //only matters for editor preview

    //should we update the preview every time we change a variable
    public bool autoUpdate;

    float[,] falloffMap;

    //for managing map/mesh generation threads
    Queue<MapThreadInfo<MapData>> mapDataThreadQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Awake()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    //called from a map data thread
    MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.generateNoise(noiseData.normalizeMode, mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.scale, 
            noiseData.octaves, noiseData.peristence, noiseData.lacunarity, center + noiseData.offset);

        if (terrainData.useFalloffMap)
        {
            falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);

            for(int y = 0; y < mapChunkSize + 2; y++){
                for (int x = 0; x < mapChunkSize + 2; x++) {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }

            }
        }
        return new MapData(noiseMap);
    }

    //called when we need to generate a new chunk of mapData
    public void RequestMapData(Action<MapData> callback, Vector2 center)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback, center);
        };

        new Thread(threadStart).Start();
    }

    //called when we need to generate a new lod mesh
    public void RequestMeshData(Action<MeshData> callback, MapData mapData, int lod)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(callback, mapData, lod);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Action<MapData> callback, Vector3 center)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadQueue) {
            mapDataThreadQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    void MeshDataThread(Action<MeshData> callback, MapData mapData, int lod)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod);
        lock (meshDataThreadQueue) {
            meshDataThreadQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    public void DrawMapInEditor()
    {
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.Noise) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.Mesh) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, levelOfDetailPreview));
        }
        else if (drawMode == DrawMode.FalloffMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2)));
        }
    }

    private void Update()
    {
        if(mapDataThreadQueue.Count > 0) {
            for(int i = 0; i < mapDataThreadQueue.Count; i++) {
                MapThreadInfo<MapData> threadInfo = mapDataThreadQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (meshDataThreadQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }

        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
#endif
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}