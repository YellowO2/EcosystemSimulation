// GameObjectItem.cs
using UnityEngine;
using UnityEngine.Tilemaps;


[CreateAssetMenu(fileName = "New GameObject Item", menuName = "Simulation/Items/GameObject Item")]
public class GameObjectItem : PlaceableItem
{
    public GameObject prefabToPlace; // The specific Prefab to instantiate

    public override void Place(Controller controller, WorldGenerator worldGenerator, Vector3Int cellPosition)
    {
        // Add logic to check if the spot is valid
        TileBase tileBelow = worldGenerator.groundTilemap.GetTile(new Vector3Int(cellPosition.x, cellPosition.y -1, cellPosition.z));
        if (tileBelow == null)
        {
            Debug.Log("Cannot place here, needs ground below!");
            return;
        }

        Vector3 worldPosition = worldGenerator.groundTilemap.GetCellCenterWorld(cellPosition);
        Instantiate(prefabToPlace, worldPosition, Quaternion.identity);
    }
}