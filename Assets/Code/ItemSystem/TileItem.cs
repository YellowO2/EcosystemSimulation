// TileItem.cs
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Tile Item", menuName = "Simulation/Items/Tile Item")]
public class TileItem : PlaceableItem
{
    public TileBase tileToPlace; // The specific Tile asset to place

    public override void Place(Controller controller, WorldGenerator worldGenerator, Vector3Int cellPosition)
    {
        worldGenerator.SetTile(cellPosition, tileToPlace);
    }
}