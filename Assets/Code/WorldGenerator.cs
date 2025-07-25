using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;


public class WorldGenerator : MonoBehaviour
{
    public enum WorldType { Perlin, Stairs }

    [Header("World Settings")]
    public int worldWidth = 100;
    public int groundLevel = 5;
    public int waterLevel = 10;
    public int foodCount = 30;
    public WorldType worldToGenerate;

    [Header("Noise Settings")]
    public float noiseScale = 0.1f;
    public int noiseAmplitude = 10;
    private float seed;

    [Header("Platform Settings")]
    public int platformSteps = 5;
    public int platformWidth = 5;
    public int platformHeightStep = 1;

    [Header("References")]
    public Tilemap groundTilemap;
    public PolygonCollider2D waterCollider;
    public GameObject foodPrefab;

    [Header("Tile Assets")]
    public Tile dirtTile;
    public Tile grassTile;
    public Tile waterTile;
    private Dictionary<Vector3Int, string> mapChanges = new Dictionary<Vector3Int, string>();
    private Dictionary<string, TileBase> tileAssets;

    void Awake()
    {
        InitializeTileAssets();
    }

    public void GenerateBaseWorld(WorldType type)
    {
        switch (type)
        {
            case WorldType.Perlin:
                GenerateWorld_Perlin();
                break;
            case WorldType.Stairs:
                GenerateWorld_Stairs();
                break;
        }
    }

    public void SetTile(Vector3Int position, TileBase tile)
    {
        groundTilemap.SetTile(position, tile);
        string tileName = (tile == null) ? "" : tile.name;
        mapChanges[position] = tileName;
    }

    public void PackWorldData(WorldSaveState state)
    {
        state.worldType = this.worldToGenerate;
        state.worldGenSeed = this.seed;

        // Convert dictionary of map changes to a list for serialization
        state.mapChanges = mapChanges.Select(kv => new TileChange { position = kv.Key, tileName = kv.Value }).ToList();

        // Find all food objects and save their positions
        state.foodPositions.Clear();
        GameObject[] foodItems = GameObject.FindGameObjectsWithTag("Food");
        foreach (var food in foodItems)
        {
            state.foodPositions.Add(food.transform.position);
        }
    }

    public void LoadWorldFromState(WorldSaveState state)
    {
        worldToGenerate = state.worldType;
        seed = state.worldGenSeed;

        ClearWorld();
        GenerateBaseWorld(worldToGenerate);

        // Apply custom tile changes
        foreach (var change in state.mapChanges)
        {
            tileAssets.TryGetValue(change.tileName, out TileBase tileToSet);
            // We use SetTile without recording the change again
            groundTilemap.SetTile(change.position, tileToSet);
            mapChanges[change.position] = change.tileName; // Re-populate the dictionary
        }

        // Spawn food at saved positions
        foreach (var pos in state.foodPositions)
        {
            Instantiate(foodPrefab, pos, Quaternion.identity);
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

    public void GenerateNewWorld(WorldType type)
    {
        this.worldToGenerate = type;
        this.seed = Random.Range(0, 1000f); // Generate a new, random seed

        ClearWorld();
        GenerateBaseWorld(type);
        ResetFood();
    }

    private void GenerateWorld_Perlin()
    {
        int startX = -worldWidth / 2;
        int endX = worldWidth / 2;
        var terrainPoints = new List<Vector2>();

        for (int y = 0; y < groundLevel + 100; y++)
        {
            groundTilemap.SetTile(new Vector3Int(startX - 1, y, 0), dirtTile);
            groundTilemap.SetTile(new Vector3Int(endX, y, 0), dirtTile);
        }

        for (int x = startX; x < endX; x++)
        {
            float noiseValue = Mathf.PerlinNoise((x + worldWidth / 2f) * noiseScale + seed, seed);
            int terrainHeight = groundLevel + (int)(noiseValue * noiseAmplitude);
            terrainPoints.Add(new Vector2(x, terrainHeight + 1));
            groundTilemap.SetTile(new Vector3Int(x, terrainHeight, 0), grassTile);
            for (int y = 0; y < terrainHeight; y++)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), dirtTile);
            }
            for (int y = terrainHeight + 1; y < waterLevel; y++)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), waterTile);
            }
        }

        if (waterCollider != null)
        {
            var waterShapePoints = new List<Vector2>
            {
                new Vector2(startX, waterLevel),
                new Vector2(endX, waterLevel)
            };
            for (int i = terrainPoints.Count - 1; i >= 0; i--)
            {
                waterShapePoints.Add(terrainPoints[i]);
            }
            waterCollider.SetPath(0, waterShapePoints);
        }
    }

    private void GenerateWorld_Stairs()
    {
        int startX = -worldWidth / 2;
        int endX = worldWidth / 2;
        for (int x = startX; x < endX; x++)
        {
            groundTilemap.SetTile(new Vector3Int(x, groundLevel, 0), grassTile);
            for (int y = 0; y < groundLevel; y++)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), dirtTile);
            }
        }
        GeneratePlatforms(startX, 1);
        GeneratePlatforms(endX - 1, -1);
    }

    private void GeneratePlatforms(int startX, int direction)
    {
        int currentX = startX;
        int currentY = groundLevel + 1;
        for (int i = 0; i < platformSteps; i++)
        {
            for (int w = 0; w < platformWidth; w++)
            {
                for (int h = 0; h < platformHeightStep; h++)
                {
                    int tileX = currentX + (w * direction) + (direction == -1 ? 1 : 0);
                    groundTilemap.SetTile(new Vector3Int(tileX, currentY + h, 0), dirtTile);
                }
            }
            currentX += platformWidth * direction;
            currentY += platformHeightStep;
        }
        for (int h = 0; h < platformHeightStep * platformSteps + 2; h++)
        {
            groundTilemap.SetTile(new Vector3Int(currentX, groundLevel + h, 0), dirtTile);
        }
    }

    //MANAGING FUNCTIONS ------

    public Vector2 GetRandomSpawnPointOnGround()
    {
        float randomX = Random.Range(-worldWidth / 2f + 1, worldWidth / 2f - 1);
        Vector2 rayStart = new Vector2(randomX, 100); // Start raycast from high up

        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 200);

        if (hit.collider != null)
        {
            // Spawn just above the point of impact
            return hit.point + new Vector2(0, 1f);
        }

        // Fallback in case raycast fails (e.g., no ground below)
        return new Vector2(0, groundLevel + 1);
    }

    public void ResetFood()
    {
        GameObject[] oldFood = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject food in oldFood)
        {
            Destroy(food);
        }
        Bounds waterBounds = waterCollider.bounds;
        int spawnedCount = 0;
        int attempts = 0;
        while (spawnedCount < foodCount && attempts < foodCount * 10)
        {
            Vector2 randomPoint = new Vector2(
                Random.Range(waterBounds.min.x, waterBounds.max.x),
                Random.Range(waterBounds.min.y, waterBounds.max.y)
            );
            if (waterCollider.OverlapPoint(randomPoint))
            {
                Instantiate(foodPrefab, randomPoint, Quaternion.identity);
                spawnedCount++;
            }
            attempts++;
        }
    }

    private void ClearWorld()
    {
        groundTilemap.ClearAllTiles();
        mapChanges.Clear();
        GameObject[] foodItems = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject food in foodItems) { Destroy(food); }
    }
}