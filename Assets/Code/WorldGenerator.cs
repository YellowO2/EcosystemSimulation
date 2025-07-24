using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[System.Serializable]
public struct TileChange
{
    public Vector3Int position;
    public string tileName;
}

[System.Serializable]
public class MapSaveData
{
    public List<TileChange> changes = new List<TileChange>();
}

public class WorldGenerator : MonoBehaviour
{
    public enum WorldType { Perlin, Stairs }

    [Header("World Settings")]
    public int worldWidth = 100;
    public int groundLevel = 5;
    public int waterLevel = 10;
    public int foodCount = 30;

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
    private string savePath;

    public void GenerateWorld(WorldType type)
    {
        savePath = Application.dataPath + "/mapChanges.json";
        InitializeTileAssets();
        seed = Random.Range(0, 1000);
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

        LoadMapChanges();
        ResetFood();
    }

    public void SetTile(Vector3Int position, TileBase tile)
    {
        groundTilemap.SetTile(position, tile);
        string tileName = (tile == null) ? "" : tile.name;
        mapChanges[position] = tileName;
    }

    private void SaveMapChanges()
    {
        if (mapChanges.Count == 0) return;

        MapSaveData saveData = new MapSaveData();
        saveData.changes = mapChanges.Select(c => new TileChange { position = c.Key, tileName = c.Value }).ToList();
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Saved {mapChanges.Count} map changes.");
    }

    private void LoadMapChanges()
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        MapSaveData loadedData = JsonUtility.FromJson<MapSaveData>(json);

        foreach (var change in loadedData.changes)
        {
            tileAssets.TryGetValue(change.tileName, out TileBase tileToSet);
            SetTile(change.position, tileToSet);
        }
        Debug.Log($"Loaded and applied {loadedData.changes.Count} map changes.");
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
    
    void OnApplicationQuit()
    {
        SaveMapChanges();
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