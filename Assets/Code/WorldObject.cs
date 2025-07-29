using UnityEngine;

public class WorldObject : MonoBehaviour
{
    public string objectId;
    public float timeSinceCreation;

    void Update()
    {
        // Automatically track how long this object has existed
        timeSinceCreation += Time.deltaTime;
    }
}