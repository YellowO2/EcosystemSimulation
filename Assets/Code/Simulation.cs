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

    private List<Creature> population = new List<Creature>();
    private int[] networkLayers = new int[] { 5, 4, 2 }; // Inputs, hidden, outputs
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
        
        worldGenerator.GenerateWorld(worldToGenerate);
        
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
        List<Creature> sortedPopulation = population.OrderByDescending(o => o.energy).ToList();
        SaveBestBrain(sortedPopulation[0].brain);

        List<NeuralNetwork> nextGenerationBrains = new List<NeuralNetwork>();

        int eliteCount = (int)(populationSize * 0.2f);
        for (int i = 0; i < eliteCount; i++)
        {
            nextGenerationBrains.Add(new NeuralNetwork(sortedPopulation[i].brain));
        }

        for (int i = eliteCount; i < populationSize; i++)
        {
            NeuralNetwork parent = sortedPopulation[Random.Range(0, eliteCount)].brain;
            NeuralNetwork childBrain = new NeuralNetwork(parent);
            childBrain.Mutate(0.2f, 0.2f);
            nextGenerationBrains.Add(childBrain);
        }

        StartNewGeneration(nextGenerationBrains);
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
        NeuralNetworkData data = JsonUtility.FromJson<NeuralNetworkData>(json);
        NeuralNetwork brain = new NeuralNetwork(data.layers);
        brain.LoadData(data);
        return brain;
    }

    private Creature InstantiateCreature()
    {
        Vector3 spawnPos = new Vector3(worldGenerator.worldWidth / 2f, worldGenerator.groundLevel + 5f, 0);
        return Instantiate(creaturePrefab, spawnPos, Quaternion.identity).GetComponent<Creature>();
    }
    
    void OnApplicationQuit()
    {
        Time.timeScale = 1f;
    }
}