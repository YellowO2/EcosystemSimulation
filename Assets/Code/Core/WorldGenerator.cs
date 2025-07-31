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

        GameObject[] foodItems = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject food in foodItems) { Destroy(food); }

        WorldObject[] worldObjects = FindObjectsOfType<WorldObject>();
        foreach (var obj in worldObjects)
        {
            Destroy(obj.gameObject);
        }
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
        GameObject[] oldFood = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject food in oldFood) { Destroy(food); }

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
            for (int y = 0; y < currentY; y++)
            {
                SetTile(new Vector3Int(x, y, 0), dirtTile);
            }
        }
        Instantiate(foodPrefab, new Vector3(currentX + 2, currentY + 1.5f, 0), Quaternion.identity);

        for (int i = 0; i < numberOfSteps; i++)
        {
            currentX += platformWidth + Random.Range(stepGapRange.x, stepGapRange.y);
            currentY += Random.Range(stepHeightRange.x, stepHeightRange.y);

            for (int x_offset = 0; x_offset < platformWidth; x_offset++)
            {
                int x = currentX + x_offset;
                SetTile(new Vector3Int(x, currentY, 0), grassTile);
                for (int y = 0; y < currentY; y++)
                {
                    SetTile(new Vector3Int(x, y, 0), dirtTile);
                }
            }
            Vector3 foodPosition = new Vector3(currentX + platformWidth / 2f, currentY + 1.5f, 0);
            Instantiate(foodPrefab, foodPosition, Quaternion.identity);
        }
    }

    private void GenerateWorld_Perlin()
    {
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

            for (int y = 0; y < terrainHeight; y++)
            {
                SetTile(new Vector3Int(x, y, 0), dirtTile);
            }
            for (int y = terrainHeight + 1; y < currentPreset.waterLevel; y++)
            {
                SetTile(new Vector3Int(x, y, 0), waterTile);
            }
        }

        if (waterCollider != null)
        {
            var waterShapePoints = new List<Vector2>
            {
                new Vector2(startX, currentPreset.waterLevel),
                new Vector2(endX, currentPreset.waterLevel)
            };
            for (int i = terrainPoints.Count - 1; i >= 0; i--)
            {
                waterShapePoints.Add(terrainPoints[i]);
            }
            waterCollider.SetPath(0, waterShapePoints);
        }
    }

    private void GenerateWorld_Flat()
    {
        int startX = -currentPreset.worldWidth / 2;
        int endX = currentPreset.worldWidth / 2;

        for (int x = startX; x < endX; x++)
        {
            SetTile(new Vector3Int(x, currentPreset.groundLevel, 0), grassTile);
            for (int y = 0; y < currentPreset.groundLevel; y++)
            {
                SetTile(new Vector3Int(x, y, 0), dirtTile);
            }
        }
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

        state.foodPositions.Clear();
        GameObject[] foodItems = GameObject.FindGameObjectsWithTag("Food");
        foreach (var food in foodItems)
        {
            state.foodPositions.Add(food.transform.position);
        }
        
        state.worldObjects.Clear();
        WorldObject[] objectsToSave = FindObjectsOfType<WorldObject>();
        foreach (var obj in objectsToSave)
        {
            state.worldObjects.Add(new WorldObjectData 
            {
                objectId = obj.objectId,
                position = obj.transform.position,
                timeSinceCreation = obj.timeSinceCreation
            });
        }
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

        foreach (var pos in state.foodPositions)
        {
            Instantiate(foodPrefab, pos, Quaternion.identity);
        }

        foreach (var objectData in state.worldObjects)
        {
            GameObject prefab = worldObjectDatabase.GetPrefabById(objectData.objectId);
            if (prefab != null)
            {
                GameObject newInstance = Instantiate(prefab, objectData.position, Quaternion.identity);
                WorldObject worldObjComp = newInstance.GetComponent<WorldObject>();
                
                if(worldObjComp != null)
                {
                    worldObjComp.timeSinceCreation = objectData.timeSinceCreation;
                }
            }
        }
    }

    public Vector2 GetRandomSpawnPoint()
    {
        float randomX = Random.Range(-currentPreset.worldWidth / 2f + 1, currentPreset.worldWidth / 2f - 1);
        Vector2 rayStart = new Vector2(randomX, 100);

        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 200, groundLayer);
        if (hit.collider != null)
        {
            return hit.point + new Vector2(0, 1f);
        }
        return new Vector2(0, currentPreset.groundLevel + 1);
    }

    public Vector2 GetSpawnPoint()
    {
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        return GetRandomSpawnPoint();
    }

    public void ResetFood()
    {
        GameObject[] oldFood = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject food in oldFood)
        {
            Destroy(food);
        }

        Bounds spawnBounds = waterCollider.bounds;
        if (currentPreset.waterLevel <= currentPreset.groundLevel)
        {
            spawnBounds = groundTilemap.localBounds;
            spawnBounds.min = new Vector3(spawnBounds.min.x, currentPreset.groundLevel, spawnBounds.min.z);
        }

        int spawnedCount = 0;
        int attempts = 0;
        while (spawnedCount < currentPreset.foodCount && attempts < currentPreset.foodCount * 10)
        {
            Vector2 randomPoint = new Vector2(
                Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                Random.Range(spawnBounds.min.y, spawnBounds.max.y)
            );

            if (currentPreset.waterLevel > currentPreset.groundLevel)
            {
                if (waterCollider.OverlapPoint(randomPoint))
                {
                    Instantiate(foodPrefab, randomPoint, Quaternion.identity);
                    spawnedCount++;
                }
            }
            else
            {
                randomPoint.y = currentPreset.groundLevel + 1;
                Instantiate(foodPrefab, randomPoint, Quaternion.identity);
                spawnedCount++;
            }
            attempts++;
        }
    }
    #endregion
}