using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
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
    public int platformHeightStep = 1;

    [Header("References")]
    public Tilemap groundTilemap;
    public Sprite tileSprite;
    public GameObject foodPrefab;

    private Tile dirtTile;
    private Tile grassTile;

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
    }

    private void GenerateWorld_Perlin()
    {
        int startX = -worldWidth / 2;
        int endX = worldWidth / 2;

        for (int y = 0; y < groundLevel + 20; y++)
        {
            groundTilemap.SetTile(new Vector3Int(startX, y, 0), dirtTile);
        }

        for (int x = startX; x < endX; x++)
        {
            float noiseValue = Mathf.PerlinNoise((x + worldWidth / 2f) * noiseScale + seed, seed);
            int terrainHeight = groundLevel + (int)(noiseValue * noiseAmplitude);

            groundTilemap.SetTile(new Vector3Int(x, terrainHeight, 0), grassTile);
            for (int y = 0; y < terrainHeight; y++)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), dirtTile);
            }

            if (Random.value < 0.2f)
            {
                Instantiate(foodPrefab, new Vector3(x, terrainHeight + 1.5f, 0), Quaternion.identity);
            }
        }

        for (int y = 0; y < groundLevel + 20; y++)
        {
            groundTilemap.SetTile(new Vector3Int(endX - 1, y, 0), dirtTile);
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

        List<Vector2> foodSpawnPoints = new List<Vector2>();
        foodSpawnPoints.AddRange(GeneratePlatforms(startX, 1));
        foodSpawnPoints.AddRange(GeneratePlatforms(endX -1 , -1));

        foreach (var point in foodSpawnPoints)
        {
            Instantiate(foodPrefab, point, Quaternion.identity);
        }
    }

    private List<Vector2> GeneratePlatforms(int startX, int direction)
    {
        List<Vector2> spawnPoints = new List<Vector2>();
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
            spawnPoints.Add(new Vector2(foodX, foodY));

            currentX += platformWidth * direction;
            currentY += platformHeightStep;
        }

        for (int h = 0; h < platformHeightStep * platformSteps + 2; h++)
        {
             groundTilemap.SetTile(new Vector3Int(currentX, groundLevel + h, 0), dirtTile);
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