using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "HandTool", menuName = "Simulation/Items/Hand Tool")]
public class HandItem : PlaceableItem
{
    // When the user clicks with the Hand Tool selected,
    // this method is called. We want it to do nothing.
    // We could add logging like selecting creature or navigation in the future
    public override void Place(Controller controller, WorldGenerator worldGenerator, Vector3Int cellPosition)
    {
        // Intentionally left blank.
    }
}