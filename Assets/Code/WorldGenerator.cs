using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    public int worldWidth = 100;
    public int groundLevel;
    // starting position
    public Vector2Int startPosition = new Vector2Int(0, 0);

    [Header("Noise Settings")]
    public float noiseScale = 0.1f;    // How zoomed in the noise is. Basically controls how much the curve is stretched horizontally.
    public int noiseAmplitude = 10;  // How high the hills can be.
    private float seed;              // seed for the terrain

    [Header("References")]
    public Tilemap groundTilemap; // Drag your Tilemap here
    public Sprite tileSprite;      // Drag your single white pixel sprite here

    // We'll create our tiles in code, so we need a place to store them.
    private Tile dirtTile;
    private Tile grassTile;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        seed = 69;
        groundLevel = startPosition.y;
        CreateTileTypes();
        GenerateWorld();
    }

    void CreateTileTypes()
    {
        // Create an instance of a "Dirt" tile
        dirtTile = ScriptableObject.CreateInstance<Tile>();
        dirtTile.sprite = tileSprite;
        dirtTile.color = new Color(0.5f, 0.3f, 0.1f); // A brownish color

        // Create an instance of a "Grass" tile
        grassTile = ScriptableObject.CreateInstance<Tile>();
        grassTile.sprite = tileSprite;
        grassTile.color = new Color(0.2f, 0.8f, 0.2f); // A greenish color
    }

    void GenerateWorld()
    {
        for (int x = startPosition.x; x < worldWidth - startPosition.x; x++)
        {
            //generate the height of the terrain at this x position using Perlin noise
            float noiseValue = Mathf.PerlinNoise(x * noiseScale + seed, seed); //our y parameter is a constant as we are not making a 3d terrain
            int terrainHeight = groundLevel + (int)(noiseValue * noiseAmplitude);

            groundTilemap.SetTile(new Vector3Int(x, terrainHeight - startPosition.y - 1, 0), grassTile);
            for (int y = startPosition.y; y < terrainHeight - startPosition.y - 1; y++)// dirt if below top layer
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                groundTilemap.SetTile(cellPosition, dirtTile);
            }
        }
    }


    // Update is called once per frame
    void Update()
    {

    }
}
