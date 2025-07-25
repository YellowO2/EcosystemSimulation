using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    [Header("References")]
    public PopulationManager populationManager;
    public WorldGenerator worldGenerator;

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

        // Clear the current scene
        populationManager.ClearSimulation();
        // (worldGenerator is cleared by its own LoadWorldFromState method)

        // Ask other managers to load their state from the container
        worldGenerator.LoadWorldFromState(state);
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