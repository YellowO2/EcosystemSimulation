using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
    // Enum to define the types of worlds we can generate
    public enum WorldType { Perlin, Stairs }

    [Header("World Settings")]
    public int worldWidth = 100;
    public int groundLevel = 5;

    [Header("Noise Settings")]
    public float noiseScale = 0.1f;
    public int noiseAmplitude = 10;
    private float seed;

    [Header("Platform Settings")]
    public int platformSteps = 5;
    public int platformWidth = 5;
    public int platformHeightStep = 2;

    [Header("References")]
    public Tilemap groundTilemap;
    public Sprite tileSprite;
    public GameObject foodPrefab;

    private Tile dirtTile;
    private Tile grassTile;

    // The main public function to be called by the SimulationManager
    public void GenerateWorld(WorldType type)
    {
        seed = Random.Range(0, 1000); // Use a new seed each time for variety
        CreateTileTypes();
        ClearWorld();

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

    private void GenerateWorld_Perlin()
    {
        for (int x = 0; x < worldWidth; x++)
        {
            float noiseValue = Mathf.PerlinNoise(x * noiseScale + seed, seed);
            int terrainHeight = groundLevel + (int)(noiseValue * noiseAmplitude);

            groundTilemap.SetTile(new Vector3Int(x, terrainHeight, 0), grassTile);
            for (int y = 0; y < terrainHeight; y++)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), dirtTile);
            }

            // Spawn food randomly on the Perlin terrain
            if (Random.value < 0.1f)
            {
                Instantiate(foodPrefab, new Vector3(x, terrainHeight + 1.5f, 0), Quaternion.identity);
            }
        }
    }

    private void GenerateWorld_Stairs()
    {
        // 1. Generate a flat base layer of ground
        for (int x = 0; x < worldWidth; x++)
        {
            groundTilemap.SetTile(new Vector3Int(x, groundLevel, 0), grassTile);
            for (int y = 0; y < groundLevel; y++)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), dirtTile);
            }
        }

        // 2. Generate platforms and get food spawn locations
        List<Vector2> foodSpawnPoints = new List<Vector2>();
        foodSpawnPoints.AddRange(GeneratePlatforms(0, 1));
        foodSpawnPoints.AddRange(GeneratePlatforms(worldWidth + 1, +1));

        // 3. Spawn food on the platforms
        foreach (var point in foodSpawnPoints)
        {
            Instantiate(foodPrefab, point, Quaternion.identity);
        }
    }
    
    // Helper function for the stairs world
    private List<Vector2> GeneratePlatforms(int startX, int direction)
    {
        List<Vector2> spawnPoints = new List<Vector2>();
        int currentY = groundLevel + 1;
        for (int i = 0; i < platformSteps; i++)
        {
            int platformStartX = startX + (i * platformWidth * direction);
            for (int x = 0; x < platformWidth; x++)
            {
                for (int y = 0; y < platformHeightStep; y++)
                {
                    groundTilemap.SetTile(new Vector3Int(platformStartX + (x * direction), currentY + y, 0), dirtTile);
                }
            }
            float foodX = platformStartX + (platformWidth / 2f * direction);
            float foodY = currentY + platformHeightStep + 1f;
            spawnPoints.Add(new Vector2(foodX, foodY));
            currentY += platformHeightStep;
        }
        return spawnPoints;
    }

    private void CreateTileTypes()
    {
        if (dirtTile != null) return;
        dirtTile = ScriptableObject.CreateInstance<Tile>();
        dirtTile.sprite = tileSprite;
        dirtTile.color = new Color(0.5f, 0.3f, 0.1f);
        grassTile = ScriptableObject.CreateInstance<Tile>();
        grassTile.sprite = tileSprite;
        grassTile.color = new Color(0.2f, 0.8f, 0.2f);
    }

    private void ClearWorld()
    {
        groundTilemap.ClearAllTiles();
        GameObject[] foodItems = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject food in foodItems) { Destroy(food); }
    }
}