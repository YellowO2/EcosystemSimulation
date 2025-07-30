using System.Collections.Generic;
using UnityEngine;

// The main container for all save data
[System.Serializable]
public class WorldSaveState
{
    public int generation;
    public WorldType worldType;
    public string presetName;  // Name of the preset used for this world (might be removing later)
    public float worldGenSeed;
    public List<TileChange> mapChanges = new List<TileChange>();
    public List<CreatureSaveData> creatures = new List<CreatureSaveData>();
    public List<Vector3> foodPositions = new List<Vector3>();
    public List<string> activeSpeciesNames = new List<string>();

    // NEW list for plants, rocks, etc.
    public List<WorldObjectData> worldObjects = new List<WorldObjectData>();
}

[System.Serializable]
public class WorldObjectData 
{
    public string objectId; // "sunflower", "rock_small"
    public Vector3 position;
    public float timeSinceCreation; // How long this object has existed in the world
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
public class NeuralNetworkData
{
    public int[] layers;
    public float[] weights; // Back to a flattened 1D array to work with JSON
    public float[] biases;
}

[System.Serializable]
public class SpeciesConfiguration
{
    public string speciesName;
    public GameObject prefab;
    public int initialPopulation = 10;
    public int[] networkLayers;

    [Header("Evolution")]
    [Range(0f, 1f)] public float baseMutationRate = 0.1f;
    [Range(0f, 1f)] public float baseMutationStrength = 0.1f;
}


[System.Serializable]
public struct TileChange
{
    public Vector3Int position;
    public string tileName;
}

public enum WorldType { Perlin, Flat ,ClimbingChallenge }


[System.Serializable]
public class WorldPreset
{
    public string presetName;
    public WorldType worldType;
    public int worldWidth = 100;
    public int groundLevel = 5;
    public int waterLevel = 10;
    public int foodCount = 30;
    public float noiseScale = 0.1f;
    public int noiseAmplitude = 10;
    public int platformSteps = 5;
    public int platformWidth = 5;
    public int platformHeightStep = 1;
}