using UnityEditor; // This is needed for editor scripts
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCreator
{
    [MenuItem("Tools/Create Color Tile")]
    public static void CreateColorTile()
    {
        Color tileColor = new Color(0.5f, 0.3f, 0.1f);
        string tileName = "DirtTile";

        // Create a new Tile instance
        Tile newTile = ScriptableObject.CreateInstance<Tile>();

        // Create a tiny 1x1 texture of the chosen color
        Texture2D newTexture = new Texture2D(1, 1);
        newTexture.SetPixel(0, 0, tileColor);
        newTexture.Apply();

        // Create a sprite from this new texture
        newTile.sprite = Sprite.Create(
            newTexture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f), // Pivot
            1
        );

        // Save the Tile Asset to your project folders
        string path = $"Assets/Tilemaps/{tileName}.asset";
        AssetDatabase.CreateAsset(newTile, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Created new tile at: {path}");
    }
}