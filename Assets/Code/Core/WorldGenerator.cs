using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class WorldGenerator : MonoBehaviour
{
    #region Configuration

    [Header("References")]
    public Tilemap groundTilemap;
    public PolygonCollider2D waterCollider;
    public GameObject foodPrefab;
    public LayerMask groundLayer;
    public WorldObjectDatabase worldObjectDatabase;

    [Header("Tile Assets")]
    public Tile dirtTile;
    public Tile grassTile;
    public Tile waterTile;
    public Transform spawnPoint;

    private WorldPreset currentPreset;
    private float seed;
    private Dictionary<Vector3Int, string> mapChanges = new Dictionary<Vector3Int, string>();
    private Dictionary<string, TileBase> tileAssets;
    
    private List<WorldObject> activeWorldObjects = new List<WorldObject>();

    #endregion


    #region Initialization & Setup
    void Awake()
    {
        InitializeTileAssets();
        if (worldObjectDatabase != null)
        {
            worldObjectDatabase.Initialize();
        }
    }

    private void InitializeTileAssets()
    {
        tileAssets = new Dictionary<string, TileBase>
        {
            { dirtTile.name, dirtTile },
            { grassTile.name, grassTile },
            { waterTile.name, waterTile }
        };
    }

    private void ClearWorld()
    {
        groundTilemap.ClearAllTiles();
        mapChanges.Clear();

        foreach (WorldObject obj in activeWorldObjects)
        {
            if (obj != null) Destroy(obj.gameObject);
        }
        activeWorldObjects.Clear();
    }
    #endregion


    #region Main Generation Logic
    public void GenerateNewWorld(WorldPreset preset)
    {
        this.currentPreset = preset;
        this.seed = Random.Range(0, 1000f);
        ClearWorld();
        GenerateBaseWorld();
    }

    private void GenerateBaseWorld()
    {
        if (currentPreset == null) return;
        switch (currentPreset.worldType)
        {
            case WorldType.Perlin: GenerateWorld_Perlin(); break;
            case WorldType.Flat: GenerateWorld_Flat(); break;
            case WorldType.ClimbingChallenge: GenerateWorld_ClimbingChallenge(); break;
        }
    }

    private void GenerateWorld_ClimbingChallenge()
    {
        int startX = -currentPreset.worldWidth / 2;
        int platformWidth = 10;
        int numberOfSteps = 15;
        Vector2Int stepHeightRange = new Vector2Int(2, 5);
        Vector2Int stepGapRange = new Vector2Int(3, 6);
        int currentX = startX;
        int currentY = currentPreset.groundLevel;

        for (int x = currentX; x < currentX + platformWidth * 2; x++)
        {
            SetTile(new Vector3Int(x, currentY, 0), grassTile);
            for (int y = 0; y < currentY; y++) { SetTile(new Vector3Int(x, y, 0), dirtTile); }
        }
        SpawnObject(foodPrefab, new Vector3(currentX + 2, currentY + 1.5f, 0));

        for (int i = 0; i < numberOfSteps; i++)
        {
            currentX += platformWidth + Random.Range(stepGapRange.x, stepGapRange.y);
            currentY += Random.Range(stepHeightRange.x, stepHeightRange.y);

            for (int x_offset = 0; x_offset < platformWidth; x_offset++)
            {
                int x = currentX + x_offset;
                SetTile(new Vector3Int(x, currentY, 0), grassTile);
                for (int y = 0; y < currentY; y++) { SetTile(new Vector3Int(x, y, 0), dirtTile); }
            }
            SpawnObject(foodPrefab, new Vector3(currentX + platformWidth / 2f, currentY + 1.5f, 0));
        }
    }

    private void GenerateWorld_Perlin()
    {
        Debug.Log("Generating Perlin noise terrain");
        int startX = -currentPreset.worldWidth / 2;
        int endX = currentPreset.worldWidth / 2;
        var terrainPoints = new List<Vector2>();

        for (int y = 0; y < currentPreset.groundLevel + 100; y++)
        {
            SetTile(new Vector3Int(startX - 1, y, 0), dirtTile);
            SetTile(new Vector3Int(endX, y, 0), dirtTile);
        }

        for (int x = startX; x < endX; x++)
        {
            float noiseValue = Mathf.PerlinNoise((x + currentPreset.worldWidth / 2f) * currentPreset.noiseScale + seed, seed);
            int terrainHeight = currentPreset.groundLevel + (int)(noiseValue * currentPreset.noiseAmplitude);
            terrainPoints.Add(new Vector2(x, terrainHeight + 1));
            SetTile(new Vector3Int(x, terrainHeight, 0), grassTile);

            for (int y = 0; y < terrainHeight; y++) { SetTile(new Vector3Int(x, y, 0), dirtTile); }
            for (int y = terrainHeight + 1; y < currentPreset.waterLevel; y++) { SetTile(new Vector3Int(x, y, 0), waterTile); }
        }

        RebuildWaterCollider();
    }

    private void GenerateWorld_Flat()
    {
        int startX = -currentPreset.worldWidth / 2;
        int endX = currentPreset.worldWidth / 2;

        for (int x = startX; x < endX; x++)
        {
            SetTile(new Vector3Int(x, currentPreset.groundLevel, 0), grassTile);
            for (int y = 0; y < currentPreset.groundLevel; y++) { SetTile(new Vector3Int(x, y, 0), dirtTile); }
        }
    }
    private void RebuildWaterCollider()
    {
        if (currentPreset.worldType != WorldType.Perlin || waterCollider == null)
        {
            waterCollider.pathCount = 0; // Clear the collider if not a Perlin world
            return;
        }

        int startX = -currentPreset.worldWidth / 2;
        int endX = currentPreset.worldWidth / 2;
        var terrainPoints = new List<Vector2>();

        // re-calculates the terrain shape using the same seed,
        for (int x = startX; x < endX; x++)
        {
            float noiseValue = Mathf.PerlinNoise((x + currentPreset.worldWidth / 2f) * currentPreset.noiseScale + seed, seed);
            int terrainHeight = currentPreset.groundLevel + (int)(noiseValue * currentPreset.noiseAmplitude);
            terrainPoints.Add(new Vector2(x + 0.5f, terrainHeight + 0.5f));
        }
        
        // This is the same collider generation logic from before.
        var waterShapePoints = new List<Vector2>
        {
            new(startX - 0.5f, currentPreset.waterLevel - 0.5f),
            new(endX - 0.5f, currentPreset.waterLevel - 0.5f)
        };
        waterShapePoints.Add(new Vector2(endX - 0.5f, terrainPoints.Last().y));
        for (int i = terrainPoints.Count - 1; i >= 0; i--)
        {
            waterShapePoints.Add(terrainPoints[i]);
        }
        waterShapePoints.Add(new Vector2(startX - 0.5f, terrainPoints.First().y));
            
        waterCollider.SetPath(0, waterShapePoints);
    }
    #endregion


    #region Save, Load & Utility
    public void SetTile(Vector3Int position, TileBase tile)
    {
        groundTilemap.SetTile(position, tile);
        string tileName = (tile == null) ? "" : tile.name;
        mapChanges[position] = tileName;
    }

    public void PackWorldData(WorldSaveState state)
    {
        state.presetName = this.currentPreset.presetName;
        state.worldGenSeed = this.seed;
        state.mapChanges = mapChanges.Select(kv => new TileChange { position = kv.Key, tileName = kv.Value }).ToList();
        
        state.worldObjects.Clear();
        string foodId = foodPrefab.GetComponent<WorldObject>().objectId;

        foreach (var obj in activeWorldObjects)
        {
            if (obj == null) continue;
            
            else
            {
                state.worldObjects.Add(new WorldObjectData
                {
                    objectId = obj.objectId,
                    position = obj.transform.position,
                    timeSinceCreation = obj.timeSinceCreation
                });
            }
        }
        
        state.spawnPointPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
    }

    public void LoadWorldFromState(WorldSaveState state, WorldPreset preset)
    {
        this.currentPreset = preset;
        this.seed = state.worldGenSeed;

        ClearWorld();

        foreach (var change in state.mapChanges)
        {
            tileAssets.TryGetValue(change.tileName, out TileBase tileToSet);
            SetTile(change.position, tileToSet);
        }

        foreach (var objectData in state.worldObjects)
        {
            GameObject prefab = worldObjectDatabase.GetPrefabById(objectData.objectId);
            if (prefab != null)
            {
                SpawnObject(prefab, objectData.position, objectData.timeSinceCreation);
            }
        }

        if (spawnPoint != null && state.spawnPointPosition != Vector3.zero)
        {
            spawnPoint.position = state.spawnPointPosition;
        }
        RebuildWaterCollider();
    }

    public void ResetFood()
    {
        string foodId = foodPrefab.GetComponent<WorldObject>().objectId;
        List<WorldObject> foodToRemove = activeWorldObjects.Where(obj => obj != null && obj.objectId == foodId).ToList();

        foreach (var foodObject in foodToRemove)
        {
            activeWorldObjects.Remove(foodObject);
            Destroy(foodObject.gameObject);
        }

        Bounds spawnBounds = (currentPreset.waterLevel > currentPreset.groundLevel) ? waterCollider.bounds : groundTilemap.localBounds;
        if (currentPreset.waterLevel <= currentPreset.groundLevel)
        {
            spawnBounds.min = new Vector3(spawnBounds.min.x, currentPreset.groundLevel, spawnBounds.min.z);
        }

        int spawnedCount = 0;
        int attempts = 0;
        while (spawnedCount < currentPreset.foodCount && attempts < currentPreset.foodCount * 10)
        {
            Vector2 randomPoint = new(Random.Range(spawnBounds.min.x, spawnBounds.max.x), Random.Range(spawnBounds.min.y, spawnBounds.max.y));

            if (currentPreset.waterLevel > currentPreset.groundLevel)
            {
                if (waterCollider.OverlapPoint(randomPoint))
                {
                    SpawnObject(foodPrefab, randomPoint);
                    spawnedCount++;
                }
            }
            else
            {
                randomPoint.y = currentPreset.groundLevel + 1;
                SpawnObject(foodPrefab, randomPoint);
                spawnedCount++;
            }
            attempts++;
        }
    }
    
    private void SpawnObject(GameObject prefab, Vector3 position, float timeSinceCreation = 0f)
    {
        GameObject newInstance = Instantiate(prefab, position, Quaternion.identity);
        WorldObject worldObjComp = newInstance.GetComponent<WorldObject>();
        if (worldObjComp != null)
        {
            worldObjComp.timeSinceCreation = timeSinceCreation;
            activeWorldObjects.Add(worldObjComp);
        }
    }

    public Vector2 GetRandomSpawnPoint()
    {
        float randomX = Random.Range(-currentPreset.worldWidth / 2f + 1, currentPreset.worldWidth / 2f - 1);
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(randomX, 100), Vector2.down, 200, groundLayer);
        return hit.collider != null ? hit.point + new Vector2(0, 1f) : new Vector2(0, currentPreset.groundLevel + 1);
    }

    public Vector2 GetSpawnPoint()
    {
        return spawnPoint != null ? (Vector2)spawnPoint.position : GetRandomSpawnPoint();
    }
    #endregion
}