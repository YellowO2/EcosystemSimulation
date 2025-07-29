// PlaceableItem.cs (Base Class)
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class PlaceableItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;

    // The "contract": every item must know how to place itself.
    public abstract void Place(Controller controller, Tilemap tilemap, Vector3Int cellPosition);
}