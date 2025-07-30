// For mapping the saved world objects to their prefabs
//TODO: Actually my system currently is abit messed up. I should place stuff using the database too, so i dont need to store the prefabs.
using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "WorldObjectDatabase", menuName = "Simulation/World Object Database")]
public class WorldObjectDatabase : ScriptableObject
{
    public List<GameObject> objectPrefabs;

    private Dictionary<string, GameObject> database;

    public void Initialize()
    {
        database = new Dictionary<string, GameObject>();
        foreach (var prefab in objectPrefabs)
        {
            var obj = prefab.GetComponent<WorldObject>();
            // Make sure the prefab has the component and the ID isn't already in use
            if (obj != null && !database.ContainsKey(obj.objectId))
            {
                database.Add(obj.objectId, prefab);
            }
        }
    }

    public GameObject GetPrefabById(string id)
    {
        if (database == null) Initialize(); // Safety check

        database.TryGetValue(id, out GameObject prefab);
        return prefab;
    }
}