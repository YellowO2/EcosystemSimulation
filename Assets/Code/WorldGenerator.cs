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
    // public Tilemap waterTilemap; // tilemap is causing errors 
    public PolygonCollider2D waterCollider;
    public Sprite tileSprite;
    public GameObject foodPrefab;

    private Tile dirtTile;
    private Tile grassTile;
    private Tile waterTile;
    private List<Vector3> foodSpawnPoints = new List<Vector3>();
    public int foodCount = 30;


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
        var terrainPoints = new List<Vector2>();

        for (int y = 0; y < groundLevel + 100; y++)
        {
            groundTilemap.SetTile(new Vector3Int(startX-1, y, 0), dirtTile);
            groundTilemap.SetTile(new Vector3Int(endX, y, 0), dirtTile);
        }

        for (int x = startX; x < endX; x++)
        {
            float noiseValue = Mathf.PerlinNoise((x + worldWidth / 2f) * noiseScale + seed, seed);
            int terrainHeight = groundLevel + (int)(noiseValue * noiseAmplitude);

            // A tile at (x,y) has its top edge at y+1. This is the "seafloor" level.
            terrainPoints.Add(new Vector2(x, terrainHeight + 1));

            groundTilemap.SetTile(new Vector3Int(x, terrainHeight, 0), grassTile);
            for (int y = 0; y < terrainHeight; y++)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), dirtTile);
            }

            for (int y = terrainHeight + 1; y < waterLevel; y++)
            {
                // Draw visual water tiles directly onto the ground tilemap
                groundTilemap.SetTile(new Vector3Int(x, y, 0), waterTile);
            }

            if (Random.value < 0.20f)
            {
                float randomY = Random.Range(terrainHeight + 3f, waterLevel - 1f);
                foodSpawnPoints.Add(new Vector3(x, randomY, 0));
            }
        }

        if (waterCollider != null)
        {
            var waterShapePoints = new List<Vector2>
            {
                // 1. Define the water surface
                new Vector2(startX, waterLevel),
                new Vector2(endX, waterLevel)
            };

            // 2. Add the seafloor points in reverse order to create the bottom edge
            for (int i = terrainPoints.Count - 1; i >= 0; i--)
            {
                waterShapePoints.Add(terrainPoints[i]);
            }

            // 3. Apply the complete shape to the polygon collider
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
        // 1. Destroy all old food
        GameObject[] oldFood = GameObject.FindGameObjectsWithTag("Food");
        foreach (GameObject food in oldFood)
        {
            Destroy(food);
        }

        // 2. Spawn new food in random valid locations
        Bounds waterBounds = waterCollider.bounds;
        int spawnedCount = 0;
        int attempts = 0; // Failsafe to prevent infinite loops

        while (spawnedCount < foodCount && attempts < foodCount * 10)
        {
            // Pick a random point within the collider's bounding box
            Vector2 randomPoint = new Vector2(
                Random.Range(waterBounds.min.x, waterBounds.max.x),
                Random.Range(waterBounds.min.y, waterBounds.max.y)
            );

            // Check if the point is actually inside the water polygon
            if (waterCollider.OverlapPoint(randomPoint))
            {
                Instantiate(foodPrefab, randomPoint, Quaternion.identity);
                spawnedCount++;
            }
            attempts++;
        }
    }

    private void CreateTileTypes()
    {
        if (dirtTile != null) return;
        // --- Dirt Tile ---
        dirtTile = ScriptableObject.CreateInstance<Tile>();
        dirtTile.sprite = tileSprite;
        dirtTile.color = new Color(0.5f, 0.3f, 0.1f);
        // dirtTile.colliderType = Tile.ColliderType.Grid;

        // --- Grass Tile ---
        grassTile = ScriptableObject.CreateInstance<Tile>();
        grassTile.sprite = tileSprite;
        grassTile.color = new Color(0.2f, 0.8f, 0.2f);
        // grassTile.colliderType = Tile.ColliderType.Grid;

        // --- Water Tile ---
        waterTile = ScriptableObject.CreateInstance<Tile>();
        waterTile.sprite = tileSprite;
        waterTile.color = new Color(0.1f, 0.3f, 0.8f, 0.7f);
        waterTile.colliderType = Tile.ColliderType.None; // we will use a PolygonCollider2D instead
    }

    private void ClearWorld()
    {
        groundTilemap.ClearAllTiles();
        foodSpawnPoints.Clear();
        // waterTilemap.ClearAllTiles();
        GameObject[] foodItems = GameObject.FindGameObjectsWithTag("Food");

        foreach (GameObject food in foodItems) { Destroy(food); }
    }
}