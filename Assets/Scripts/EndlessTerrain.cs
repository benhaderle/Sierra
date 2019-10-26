using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float MAXVIEWDISTANCE = 450;
    public Transform viewer;

    public static Vector2 viewerPosition;
    int chunkSize;
    int chunksVisible;

    Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastFrame = new List<TerrainChunk>();

    private void Start()
    {
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisible = Mathf.RoundToInt(MAXVIEWDISTANCE / chunkSize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        for(int i = 0; i < terrainChunksVisibleLastFrame.Count; i++) {
            terrainChunksVisibleLastFrame[i].SetVisible(false);
        }
        terrainChunksVisibleLastFrame.Clear();

        int currentChunkX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for(int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++) {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                if (terrainChunks.ContainsKey(viewedChunkCoord)) {
                    terrainChunks[viewedChunkCoord].UpdateTerrainChunk();
                    
                }
                else {
                    terrainChunks.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform));
                }

                if (terrainChunks[viewedChunkCoord].IsVisible()) {
                    terrainChunksVisibleLastFrame.Add(terrainChunks[viewedChunkCoord]);
                }
            }
        }
    }

    public class TerrainChunk
    {
        Vector2 position;
        GameObject meshObject;
        Bounds bounds;

        public TerrainChunk(Vector2 coord, int size, Transform parent)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 position3 = new Vector3(position.x, 0, position.y);

            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = position3;
            meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;

            SetVisible(false);
        }

        public void UpdateTerrainChunk()
        {
            float viewerDist = bounds.SqrDistance(viewerPosition);
            bool visible = viewerDist <= MAXVIEWDISTANCE * MAXVIEWDISTANCE;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }
}
