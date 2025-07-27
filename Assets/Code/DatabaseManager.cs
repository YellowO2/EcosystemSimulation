using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    [Header("References")]
    public PopulationManager populationManager;
    public WorldGenerator worldGenerator;
    public WorldDatabase worldDatabase;

    private string saveDirectoryPath;
    private string currentWorldName;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        saveDirectoryPath = Path.Combine(Application.dataPath, "Saves");
    }

    // --- NEW BRAIN SAVE/LOAD METHODS (USING THE CORRECT DATA TYPE) ---

    public void SaveBestBrain(string speciesName, NeuralNetwork brain)
    {
        if (string.IsNullOrEmpty(currentWorldName)) return;

        string worldFolderPath = Path.Combine(saveDirectoryPath, currentWorldName);
        string brainFilePath = Path.Combine(worldFolderPath, $"{speciesName}_best_brain.json");

        NeuralNetworkData saveData = brain.GetData();

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(brainFilePath, json);
    }

    public NeuralNetworkData LoadBestBrainData(string speciesName)
    {
        if (string.IsNullOrEmpty(currentWorldName)) return null;

        string brainFilePath = Path.Combine(saveDirectoryPath, currentWorldName, $"{speciesName}_best_brain.json");

        if (!File.Exists(brainFilePath))
        {
            return null;
        }

        string json = File.ReadAllText(brainFilePath);
        // Deserialize directly into your existing NeuralNetworkData class.
        NeuralNetworkData loadedData = JsonUtility.FromJson<NeuralNetworkData>(json);
        return loadedData;
    }


    public List<string> GetSavedWorldNames()
    {
        var worldNames = new List<string>();
        if (Directory.Exists(saveDirectoryPath))
        {
            var worldDirectories = Directory.GetDirectories(saveDirectoryPath);
            foreach (var dir in worldDirectories)
            {
                worldNames.Add(Path.GetFileName(dir));
            }
        }
        return worldNames;
    }

    public void SaveWorld(string worldName)
    {
        if (string.IsNullOrEmpty(worldName))
        {
            Debug.LogError("Save failed: World name cannot be empty.");
            return;
        }
        currentWorldName = worldName;

        // Get file path
        string worldFolderPath = Path.Combine(saveDirectoryPath, worldName);
        string worldFilePath = Path.Combine(worldFolderPath, "worldState.json");
        Directory.CreateDirectory(worldFolderPath); // Creates the folder if it doesn't exist

        // Prepare data to save
        WorldSaveState state = new WorldSaveState();
        worldGenerator.PackWorldData(state);
        populationManager.PackSimulationData(state);

        // Write to file
        string json = JsonUtility.ToJson(state, true);
        File.WriteAllText(worldFilePath, json);

        Debug.Log($"World '{worldName}' saved successfully.");
    }

    public void LoadWorld(string worldName)
    {
        string worldFilePath = Path.Combine(saveDirectoryPath, worldName, "worldState.json");

        if (!File.Exists(worldFilePath))
        {
            Debug.LogError($"Load failed: Save file for world '{worldName}' not found.");
            return;
        }
        currentWorldName = worldName;


        // Read file
        string json = File.ReadAllText(worldFilePath);
        WorldSaveState state = JsonUtility.FromJson<WorldSaveState>(json);

        WorldPreset presetToLoad = worldDatabase.FindPreset(state.presetName); // Load world preset by name
        if (presetToLoad == null)
        {
            Debug.LogError($"Load failed: Could not find World Preset '{state.presetName}' in the database.");
            return;
        }

        // Clear the current scene
        populationManager.ClearSimulation(); // (p.s. worldGenerator does clearing internally so dont need)

        // Ask other managers to load their state from the container
        worldGenerator.LoadWorldFromState(state, presetToLoad); 
        populationManager.LoadSimulationFromState(state);

        Debug.Log($"World '{worldName}' loaded successfully.");
    }

    // A helper function to easily re-save the currently open world
    public void SaveCurrentWorld()
    {
        if (string.IsNullOrEmpty(currentWorldName))
        {
            Debug.LogError("Save failed: No world is currently loaded. Use SaveWorld(\"newWorldName\") first.");
            return;
        }
        SaveWorld(currentWorldName);
    }

    public void DeleteWorld(string worldName)
    {
        string worldFolderPath = Path.Combine(saveDirectoryPath, worldName);
        if (Directory.Exists(worldFolderPath))
        {
            Directory.Delete(worldFolderPath, true); // The 'true' recursively deletes all files inside
            Debug.Log($"World '{worldName}' deleted.");
        }
        else
        {
            Debug.LogWarning($"Could not delete world '{worldName}': Directory not found.");
        }
    }


}