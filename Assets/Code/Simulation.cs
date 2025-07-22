using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro; // 1. ADD THIS LINE to use TextMeshPro

public class SimulationManager : MonoBehaviour
{
    [Header("UI Elements")] // A new header for organization
    public TextMeshProUGUI generationText; // 2. ADD THIS LINE to reference the text object

    [Header("Simulation Setup")]
    public GameObject creaturePrefab;
    public WorldGenerator worldGenerator;
    public WorldGenerator.WorldType worldToGenerate;

    [Header("Simulation Parameters")]
    public int populationSize = 50;
    public float simulationTime = 120f;
    [Range(1f, 100f)] public float timeScale = 10f;
    public float mutationRate = 0.1f;
    public float mutationStrength = 0.5f;

    private List<Creature> population = new List<Creature>();
    private int[] networkLayers = new int[] { 6, 5, 2 };
    private int generation = 0;
    private float timer;

    private const string SAVE_FILE_NAME = "/bestBrain.json";
    private string savePath;

    void Start()
    {
        Time.timeScale = this.timeScale;
        savePath = Application.dataPath + SAVE_FILE_NAME;

        List<NeuralNetwork> startingBrains = new List<NeuralNetwork>();

        if (File.Exists(savePath))
        {
            Debug.Log("Loading saved brain from file.");
            NeuralNetwork savedBrain = LoadBrainFromFile();
            for (int i = 0; i < populationSize; i++)
            {
                NeuralNetwork childBrain = new NeuralNetwork(savedBrain);
                childBrain.Mutate(0.1f, 0.1f);
                startingBrains.Add(childBrain);
            }
        }
        else
        {
            Debug.Log("No save file found. Creating fresh population.");
            for (int i = 0; i < populationSize; i++)
            {
                startingBrains.Add(new NeuralNetwork(networkLayers));
            }
        }
        
        worldGenerator.GenerateWorld(worldToGenerate);
        StartNewGeneration(startingBrains);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= simulationTime)
        {
            EvolvePopulation();
        }
    }

    private void StartNewGeneration(List<NeuralNetwork> newBrains)
    {
        generation++;
        timer = 0f;
        Debug.Log("Starting Generation: " + generation);
        
        // 3. ADD THIS LINE to update the text display
        generationText.text = $"Generation: {generation}";

        foreach (var creature in population)
        {
            if (creature != null) Destroy(creature.gameObject);
        }
        population.Clear();

        foreach (var brain in newBrains)
        {
            Creature creature = InstantiateCreature();
            creature.Init(brain);
            population.Add(creature);
        }
    }

    private void EvolvePopulation()
    {
        worldGenerator.ResetFood(); 
        
        foreach (var creature in population)
        {
            if (creature != null && creature.gameObject.activeSelf)
            {
                creature.fitness = creature.energy;
            }
        }
        
        List<Creature> sortedPopulation = population
            .Where(c => c != null && c.gameObject.activeSelf)
            .OrderByDescending(o => o.fitness)
            .ToList();

        if (sortedPopulation.Count == 0)
        {
            Debug.LogWarning("Extinction event! Starting a fresh random generation.");
            List<NeuralNetwork> nextGenerationBrains = new List<NeuralNetwork>();
            for (int i = 0; i < populationSize; i++)
            {
                nextGenerationBrains.Add(new NeuralNetwork(this.networkLayers));
            }
            StartNewGeneration(nextGenerationBrains);
            return;
        }
        
        SaveBestBrain(sortedPopulation[0].brain);
        
        List<NeuralNetwork> newBrains = new List<NeuralNetwork>();
        int eliteCount = Mathf.Max(1, (int)(sortedPopulation.Count * 0.1f));
        for (int i = 0; i < eliteCount; i++)
        {
            newBrains.Add(new NeuralNetwork(sortedPopulation[i].brain));
        }
        
        for (int i = eliteCount; i < populationSize; i++)
        {
            NeuralNetwork parentBrain = newBrains[Random.Range(0, eliteCount)];
            NeuralNetwork childBrain = new NeuralNetwork(parentBrain);
            childBrain.Mutate(mutationRate, mutationStrength);
            newBrains.Add(childBrain);
        }

        StartNewGeneration(newBrains);
    }

    private void SaveBestBrain(NeuralNetwork brain)
    {
        NeuralNetworkData data = brain.GetData();
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    private NeuralNetwork LoadBrainFromFile()
    {
        string json = File.ReadAllText(savePath);
        NeuralNetworkData savedData = JsonUtility.FromJson<NeuralNetworkData>(json);
        NeuralNetwork newBrain = new NeuralNetwork(this.networkLayers);
        newBrain.LoadAndTransferData(savedData);
        return newBrain;
    }

    private Creature InstantiateCreature()
    {
        Vector3 spawnPos = new Vector3(0, worldGenerator.groundLevel + 15f, 0);
        return Instantiate(creaturePrefab, spawnPos, Quaternion.identity).GetComponent<Creature>();
    }

    void OnApplicationQuit()
    {
        Time.timeScale = 1f;
    }
}