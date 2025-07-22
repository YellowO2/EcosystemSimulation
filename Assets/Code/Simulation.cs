using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
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
    private int[] networkLayers = new int[] { 6, 5, 2 }; // Inputs, hidden, outputs
    private int generation = 0;
    private float timer;

    // [Header("Food Settings")]
    // public GameObject planktonPrefab;
    // public int initialPlanktonCount = 50;


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
        //generate world only once at the start
        worldGenerator.GenerateWorld(worldToGenerate);

        // for (int i = 0; i < initialPlanktonCount; i++)
        // {
        //     float x = Random.Range(-worldGenerator.worldWidth / 2f, worldGenerator.worldWidth / 2f);
        //     float y = Random.Range(worldGenerator.waterLevel, worldGenerator.waterLevel + 10f);
        //     Instantiate(planktonPrefab, new Vector2(x, y), Quaternion.identity);
        // }

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
        worldGenerator.ResetFood(); // Reset food for the next generation
        // --- 1. Calculate Fitness for each Creature ---
        foreach (var creature in population)
        {
            if (creature != null && creature.gameObject.activeSelf)
            {
                // add more calculations here if needed when more properties are added
                creature.fitness = creature.energy;
            }
        }

        // --- 2. Sort by the new Fitness Score ---
        List<Creature> sortedPopulation = population
            .Where(c => c != null && c.gameObject.activeSelf) // Only consider living creatures
            .OrderByDescending(o => o.fitness)
            .ToList();

        if (sortedPopulation.Count == 0)
        {
            Debug.LogWarning("Extinction event! Starting a fresh random generation.");
            // Create a new list of completely random brains.
            List<NeuralNetwork> nextGenerationBrains = new List<NeuralNetwork>();
            for (int i = 0; i < populationSize; i++)
            {
                nextGenerationBrains.Add(new NeuralNetwork(this.networkLayers));
            }
            StartNewGeneration(nextGenerationBrains);
            return; // Stop here.
        }

        // --- 3. Save the Best Brain ---
        SaveBestBrain(sortedPopulation[0].brain);

        // --- 4. Breed the Next Generation ---
        List<NeuralNetwork> newBrains = new List<NeuralNetwork>();

        // The top 10% of creatures (the "elites") get to pass on their brains directly.
        int eliteCount = Mathf.Max(1, (int)(sortedPopulation.Count * 0.1f));
        for (int i = 0; i < eliteCount; i++)
        {
            newBrains.Add(new NeuralNetwork(sortedPopulation[i].brain));
        }

        // Fill the rest of the population by mutating the elites.
        for (int i = eliteCount; i < populationSize; i++)
        {
            // Select a random parent from the elite group.
            NeuralNetwork parentBrain = newBrains[Random.Range(0, eliteCount)];
            NeuralNetwork childBrain = new NeuralNetwork(parentBrain);
            childBrain.Mutate(mutationRate, mutationStrength); // Apply mutation
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

        // 1. Create a NEW brain instance using the NEW layer structure.
        //    This brain is empty but has the correct [7, 6, 3] size.
        NeuralNetwork newBrain = new NeuralNetwork(this.networkLayers);

        // 2. Use our special function to transfer the old, learned weights
        //    from the saved data into the new, larger brain structure.
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