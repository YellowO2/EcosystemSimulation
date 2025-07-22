using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
    public enum WorldType { Perlin, Stairs }

    [Header("World Settings")]
    public int worldWidth = 100;
    public int groundLevel = 5; //when generating with perlin noise, as out amplitude is 5, the average level will be 10
    public int waterLevel = 10; //since average ground level is 10, water will be that as well

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
    public Tilemap waterTilemap;
    public Sprite tileSprite;
    public GameObject foodPrefab;

    private Tile dirtTile;
    private Tile grassTile;
    private Tile waterTile;
    private List<Vector3> foodSpawnPoints = new List<Vector3>();


    public void GenerateWorld(WorldType type)
    {
        seed = Random.Range(0, 1000);
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
        ResetFood();
    }



    private void GenerateWorld_Perlin()
{
    int startX = -worldWidth / 2;
    int endX = worldWidth / 2;

    // Boundary walls (no changes here)
    for (int y = 0; y < groundLevel + 50; y++)
    {
        groundTilemap.SetTile(new Vector3Int(startX, y, 0), dirtTile);
        groundTilemap.SetTile(new Vector3Int(endX - 1, y, 0), dirtTile);
    }

    for (int x = startX; x < endX; x++)
    {
        // --- Terrain Generation ---
        float noiseValue = Mathf.PerlinNoise((x + worldWidth / 2f) * noiseScale + seed, seed);
        int terrainHeight = groundLevel + (int)(noiseValue * noiseAmplitude);

        groundTilemap.SetTile(new Vector3Int(x, terrainHeight, 0), grassTile);
        for (int y = 0; y < terrainHeight; y++)
        {
            groundTilemap.SetTile(new Vector3Int(x, y, 0), dirtTile);
        }

        // 1. Spawn some food on the sea floor
        if (Random.value < 0.15f)
        {
            foodSpawnPoints.Add(new Vector3(x, terrainHeight + 1.5f, 0));
        }

        // 2. Spawn some food floating in the water
        if (Random.value < 0.05f)
        {
            // Pick a random height between the sea floor and the water surface
            float randomY = Random.Range(terrainHeight + 3f, waterLevel - 1f);
            foodSpawnPoints.Add(new Vector3(x, randomY, 0));
        }
        // 3. Fill the water area with tiles
        for (int y = 0; y < waterLevel; y++)
        {
            if (groundTilemap.GetTile(new Vector3Int(x, y, 0)) == null)
            {
                waterTilemap.SetTile(new Vector3Int(x, y, 0), waterTile);
            }
        }
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

            float foodX = currentX + (platformWidth / 2f * direction);
            float foodY = currentY + platformHeightStep + 1f;
            foodSpawnPoints.Add(new Vector3(foodX, foodY, 0)); // Add to the main list

            currentX += platformWidth * direction;
            currentY += platformHeightStep;
        }

        for (int h = 0; h < platformHeightStep * platformSteps + 2; h++)
        {
            groundTilemap.SetTile(new Vector3Int(currentX, groundLevel + h, 0), dirtTile);
        }
    }


    public void ResetFood()
    {
        // 1. Destroy all old plants
        GameObject[] oldFood = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject food in oldFood)
        {
            Destroy(food);
        }

        // 2. Spawn new plants from our saved list of locations
        foreach (var pos in foodSpawnPoints)
        {
            Instantiate(foodPrefab, pos, Quaternion.identity);
        }
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

        waterTile = ScriptableObject.CreateInstance<Tile>();
        waterTile.sprite = tileSprite;
        waterTile.color = new Color(0.1f, 0.3f, 0.8f, 0.7f); // Added transparency
    }

    private void ClearWorld()
    {
        groundTilemap.ClearAllTiles();
        waterTilemap.ClearAllTiles();
        GameObject[] foodItems = GameObject.FindGameObjectsWithTag("Food");

        foreach (GameObject food in foodItems) { Destroy(food); }
    }
}