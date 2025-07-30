// PlaceableItem.cs (Base Class)
using UnityEngine;
using UnityEngine.Tilemaps;

//TODO: I feel there is a repeat between this and the WorldObjectDatabase, consider merging
public abstract class PlaceableItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;

    // The "contract": every item must know how to place itself.
    public abstract void Place(Controller controller, WorldGenerator worldGenerator, Vector3Int cellPosition);
}