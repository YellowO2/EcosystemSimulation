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
    public float mutationStrength = 0.1f;

    private List<Creature> population = new List<Creature>();
    private int[] networkLayers = new int[] { 9, 6, 2 };
    private int generation = 0;
    private float timer;

    // private const string SAVE_FILE_NAME = "/bestBrain.json";
    private const string SAVE_FILE_NAME = "/topBrains.json";
    private string savePath;
    public int topBrainsToSave = 10;

    [System.Serializable]
    public class BrainSaveData
    {
        public List<NeuralNetworkData> brains = new List<NeuralNetworkData>();
    }

    void Start()
    {
        Time.timeScale = this.timeScale;
        savePath = Application.dataPath + SAVE_FILE_NAME;

        List<NeuralNetwork> startingBrains = new List<NeuralNetwork>();

        if (File.Exists(savePath))
        {
            Debug.Log("Loading saved brains from file.");
            List<NeuralNetwork> savedBrains = LoadTopBrains();
            for (int i = 0; i < populationSize; i++)
            {
                // Pick a random parent from the saved top brains
                NeuralNetwork parentBrain = savedBrains[Random.Range(0, savedBrains.Count)];
                NeuralNetwork childBrain = new NeuralNetwork(parentBrain);
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

        List<Creature> sortedPopulation = population
            .Where(c => c != null && c.gameObject.activeSelf)
            .OrderByDescending(o => o.fitness)
            .ToList();

        if (sortedPopulation.Count < 4)
        {
            Debug.LogWarning("Extinction event: Too few survivors. Starting fresh generation.");
            List<NeuralNetwork> nextGenerationBrains = new List<NeuralNetwork>();
            for (int i = 0; i < populationSize; i++)
            {
                nextGenerationBrains.Add(new NeuralNetwork(this.networkLayers));
            }
            StartNewGeneration(nextGenerationBrains);
            return;
        }

        SaveTopBrains(sortedPopulation);

        List<NeuralNetwork> newBrains = new List<NeuralNetwork>();

        // 1. Elitism for top 10percent
        int eliteCount = Mathf.Max(1, (int)(populationSize * 0.1f));
        for (int i = 0; i < eliteCount && i < sortedPopulation.Count; i++)
        {
            newBrains.Add(new NeuralNetwork(sortedPopulation[i].brain));
        }

        // 2. Random Immigrants
        int randomCount = (int)(populationSize * 0.05f);
        for (int i = 0; i < randomCount; i++)
        {
            newBrains.Add(new NeuralNetwork(this.networkLayers));
        }

        // 3. Crossover & Mutation
        int parentPoolSize = (int)(sortedPopulation.Count * 0.5f);
        parentPoolSize = Mathf.Max(parentPoolSize, 2); // Ensure we have at least 2 parents
        List<NeuralNetwork> parentPool = sortedPopulation.Take(parentPoolSize).Select(c => c.brain).ToList();

        int remainingCount = populationSize - newBrains.Count;
        for (int i = 0; i < remainingCount; i++)
        {
            NeuralNetwork parentA = parentPool[Random.Range(0, parentPool.Count)];
            NeuralNetwork parentB = parentPool[Random.Range(0, parentPool.Count)];

            NeuralNetwork child = NeuralNetwork.Crossover(parentA, parentB);
            child.Mutate(mutationRate, mutationStrength);

            newBrains.Add(child);
        }

        StartNewGeneration(newBrains);
    }

    private void SaveTopBrains(List<Creature> sortedPopulation)
    {
        BrainSaveData saveData = new BrainSaveData();

        int count = Mathf.Min(sortedPopulation.Count, topBrainsToSave);
        for (int i = 0; i < count; i++)
        {
            saveData.brains.Add(sortedPopulation[i].brain.GetData());
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
    }

    private List<NeuralNetwork> LoadTopBrains()
    {
        string json = File.ReadAllText(savePath);
        BrainSaveData loadedData = JsonUtility.FromJson<BrainSaveData>(json);

        List<NeuralNetwork> loadedBrains = new List<NeuralNetwork>();
        foreach (var brainData in loadedData.brains)
        {
            NeuralNetwork brain = new NeuralNetwork(this.networkLayers);
            brain.LoadAndTransferData(brainData);
            loadedBrains.Add(brain);
        }
        return loadedBrains;
    }

    private Creature InstantiateCreature()
    {
        Vector3 spawnPos = new Vector3(0, worldGenerator.groundLevel + 20f, 0);
        return Instantiate(creaturePrefab, spawnPos, Quaternion.identity).GetComponent<Creature>();
    }

    void OnApplicationQuit()
    {
        Time.timeScale = 1f;
    }
}