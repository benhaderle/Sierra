using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float chunkUpdateMoveThreshold = 25;
    const float sqrhunkUpdateMoveThreshold = chunkUpdateMoveThreshold * chunkUpdateMoveThreshold;
    const float chunkColliderThreshold = 5;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;
    public static float maxViewDist;
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    public Vector2 oldViewerPosition;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisible;
    int previousLODIndex;

    Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastFrame = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        chunkSize = mapGenerator.mapChunkSize -1;
        chunksVisible = Mathf.RoundToInt(maxViewDist / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

        //update colliders more frequently
        if(viewerPosition != oldViewerPosition)
        {
            foreach(TerrainChunk t in terrainChunksVisibleLastFrame){
                t.UpdateCollisionMesh();
            }
        }

        //only want to update when we move past the threshold
        if ((oldViewerPosition - viewerPosition).SqrMagnitude() > sqrhunkUpdateMoveThreshold) {
            UpdateVisibleChunks();
            oldViewerPosition = viewerPosition;
        }

    }

    void UpdateVisibleChunks()
    {
        //get rid of previously visible chunks
        for(int i = 0; i < terrainChunksVisibleLastFrame.Count; i++) {
            terrainChunksVisibleLastFrame[i].SetVisible(false);
        }
        terrainChunksVisibleLastFrame.Clear();

        //getting our center chunk pos
        int currentChunkX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        //looping thru our visible chunk pos's
        for(int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++) {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                //if this chunk is cached
                if (terrainChunks.ContainsKey(viewedChunkCoord)) {
                    terrainChunks[viewedChunkCoord].UpdateTerrainChunk();
                    
                }
                else {  //else add it to the cache
                    terrainChunks.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, colliderLODIndex, mapMaterial, transform));
                }          
            }
        }
    }

    public class TerrainChunk
    {
        Vector2 position;
        GameObject meshObject;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int colliderLODIndex;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;
        bool hasColliderMesh;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, int colliderLODIndex, Material material, Transform parent)
        {
            hasColliderMesh = false;
            this.colliderLODIndex = colliderLODIndex;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 position3 = new Vector3(position.x, 0, position.y);

            //setting up lod array
            this.detailLevels = detailLevels;
            lodMeshes = new LODMesh[detailLevels.Length];
            for(int i = 0; i < lodMeshes.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            //instantiating gameObject
            meshObject = new GameObject("Terrain Chunk");
            meshObject.transform.position = position3 * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();

            SetVisible(false);

            //make a request to the mapGen for the mesh at this pos
            mapGenerator.RequestMapData(OnMapDataReceived, this.position);
        }

        //called when mapGen generates data for this chunk
        void OnMapDataReceived(MapData mapData)
        {
            //validate reception
            this.mapData = mapData;
            mapDataReceived = true;

            //generate texture
            //Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            //meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived) {
                //getting distance from viewer for visibility check + lod
                float viewerDist = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDist <= maxViewDist;
                SetVisible(visible);

                if (visible) {
                    //getting lod
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++) {
                        if (viewerDist > detailLevels[i].visibleDistanceThreshold) {
                            lodIndex = i + 1;
                        }
                        else break;
                    }

                    //if we're at a different lod than last update, we need to change meshes
                    if (lodIndex != previousLODIndex) {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh) {              //if lod mesh is cached
                            meshFilter.mesh = lodMesh.mesh;
                            previousLODIndex = lodIndex;
                        }
                        else if (!lodMesh.requestedMesh) {  //if not, we need to generate it
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    //add this chunk to the visible set
                    terrainChunksVisibleLastFrame.Add(this);    
                }
            }
        }

        public void UpdateCollisionMesh()
        {
            if (!hasColliderMesh)
            {
                float sqrDist = bounds.SqrDistance(viewerPosition);

                //requesting lod mesh once we're visible
                if (sqrDist < detailLevels[colliderLODIndex].sqrVisibleDistanceThreshold)
                {
                    if (!lodMeshes[colliderLODIndex].requestedMesh)
                    {
                        lodMeshes[colliderLODIndex].RequestMesh(mapData);
                    }

                }

                //setting mesh once we have it
                if (bounds.SqrDistance(viewerPosition) < chunkColliderThreshold * chunkColliderThreshold)
                {
                    if (lodMeshes[colliderLODIndex].hasMesh)
                    {
                        meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                        hasColliderMesh = true;
                    }
                }
            }
        }


        //just turns mesh on/off
        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool requestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        //called by mapgenerator
        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        }

        //called when we need this lod mesh generated for the first time
        public void RequestMesh(MapData mapData)
        {
            requestedMesh = true;
            mapGenerator.RequestMeshData(OnMeshDataReceived, mapData, lod);
        }
    }

    //setting lod level + threshold for that level
    [System.Serializable]
    public struct LODInfo
    {
        [Range(0, MeshGenerator.NUM_SUPPORTED_LODS - 1)]
        public int lod;
        public float visibleDistanceThreshold;

        public float sqrVisibleDistanceThreshold{
            get {
                return visibleDistanceThreshold * visibleDistanceThreshold;
            }
        }
    }
}
