using System.Collections.Generic;
using UnityEngine;

// The main container for all save data
[System.Serializable]
public class WorldSaveState
{
    public int generation;
    public WorldGenerator.WorldType worldType;
    public float worldGenSeed;
    public List<TileChange> mapChanges = new List<TileChange>();
    public List<CreatureSaveData> creatures = new List<CreatureSaveData>();
    public List<Vector3> foodPositions = new List<Vector3>();
}

// Holds the state of a single creature
[System.Serializable]
public class CreatureSaveData
{
    public string speciesName;
    public Vector3 position;
    public Vector2 velocity;
    public NeuralNetworkData brainData;
}

[System.Serializable]
public class SpeciesConfiguration
{
    public string speciesName;
    public GameObject prefab;
    public int minPopulation;
    public int restockAmount;
    public int[] networkLayers;
    public string brainArchiveFile;
    
    [Header("Evolution")]
    [Range(0f, 1f)] public float baseMutationRate = 0.05f;
    [Range(0f, 1f)] public float baseMutationStrength = 0.1f;
}


[System.Serializable]
public struct TileChange
{
    public Vector3Int position;
    public string tileName;
}