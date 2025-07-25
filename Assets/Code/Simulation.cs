using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;

public class SimulationManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI generationText;

    [Header("World Setup")]
    public GameObject creaturePrefab;
    public GameObject predatorPrefab;
    public WorldGenerator worldGenerator;
    public WorldGenerator.WorldType worldToGenerate;

    [Header("Evolution Parameters")]
    public float simulationTime = 120f;
    [Range(1f, 100f)] public float timeScale = 10f;
    public int topBrainsToSave = 5;

    [Header("Prey Population")]
    public int preyPopulationSize = 50;
    public float preyMutationRate = 0.1f;
    public float preyMutationStrength = 0.1f;
    private List<Creature> preyPopulation = new List<Creature>();
    private int[] preyNetworkLayers = new int[] { 11, 8, 2 };
    private const string PREY_SAVE_FILE = "/preyBrains.json";
    
    [Header("Predator Population")]
    public int predatorPopulationSize = 5;
    public float predatorMutationRate = 0.15f;
    public float predatorMutationStrength = 0.15f;
    private List<Creature> predatorPopulation = new List<Creature>();
    private int[] predatorNetworkLayers = new int[] { 11, 8, 2 };
    private const string PREDATOR_SAVE_FILE = "/predatorBrains.json";

    private int generation = 0;
    private float timer;

    [System.Serializable]
    public class BrainSaveData 
    { 
        public int generation;
        public List<NeuralNetworkData> brains = new List<NeuralNetworkData>(); 
    }

    void Start()
    {
        Time.timeScale = this.timeScale;

        List<NeuralNetwork> startingPreyBrains = LoadTopBrains(Application.dataPath + PREY_SAVE_FILE, preyNetworkLayers, preyPopulationSize);
        List<NeuralNetwork> startingPredatorBrains = LoadTopBrains(Application.dataPath + PREDATOR_SAVE_FILE, predatorNetworkLayers, predatorPopulationSize);

        worldGenerator.GenerateWorld(worldToGenerate);
        StartNewGeneration(startingPreyBrains, startingPredatorBrains);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= simulationTime)
        {
            EvolvePopulations();
        }
    }

    private void StartNewGeneration(List<NeuralNetwork> preyBrains, List<NeuralNetwork> predatorBrains)
    {
        generation++;
        timer = 0f;
        generationText.text = $"Generation: {generation}";
        Debug.Log("Starting Generation: " + generation);

        foreach (var c in preyPopulation) { if (c != null) Destroy(c.gameObject); }
        foreach (var p in predatorPopulation) { if (p != null) Destroy(p.gameObject); }
        preyPopulation.Clear();
        predatorPopulation.Clear();

        foreach (var brain in preyBrains)
        {
            Creature prey = Instantiate(creaturePrefab, new Vector3(0, worldGenerator.groundLevel + 20f, 0), Quaternion.identity).GetComponent<Creature>();
            prey.Init(brain);
            preyPopulation.Add(prey);
        }

        foreach (var brain in predatorBrains)
        {
            Creature predator = Instantiate(predatorPrefab, new Vector3(0, worldGenerator.waterLevel/2, 0), Quaternion.identity).GetComponent<Creature>();
            predator.Init(brain);
            predatorPopulation.Add(predator);
        }
    }

    private void EvolvePopulations()
    {
        worldGenerator.ResetFood();

        List<NeuralNetwork> nextGenPreyBrains = EvolveSpecies(preyPopulation, preyPopulationSize, preyMutationRate, preyMutationStrength, preyNetworkLayers, PREY_SAVE_FILE);
        List<NeuralNetwork> nextGenPredatorBrains = EvolveSpecies(predatorPopulation, predatorPopulationSize, predatorMutationRate, predatorMutationStrength, predatorNetworkLayers, PREDATOR_SAVE_FILE);

        StartNewGeneration(nextGenPreyBrains, nextGenPredatorBrains);
    }
    
    private List<NeuralNetwork> EvolveSpecies(List<Creature> currentPopulation, int popSize, float mutRate, float mutStr, int[] netLayers, string saveFileName)
    {
        List<Creature> sortedPop = currentPopulation.Where(c => c != null && c.gameObject.activeSelf && c.fitness > 0).OrderByDescending(o => o.fitness).ToList();

        SaveTopBrains(sortedPop, Application.dataPath + saveFileName);

        if (sortedPop.Count < 4) 
        {
            Debug.LogWarning($"{saveFileName} EXTINCTION. Starting fresh.");
            List<NeuralNetwork> freshBrains = new List<NeuralNetwork>();
            for (int i = 0; i < popSize; i++) { freshBrains.Add(new NeuralNetwork(netLayers)); }
            return freshBrains;
        }

        List<NeuralNetwork> newBrains = new List<NeuralNetwork>();
        int eliteCount = Mathf.Max(1, (int)(popSize * 0.1f));
        for (int i = 0; i < eliteCount && i < sortedPop.Count; i++)
        {
            newBrains.Add(new NeuralNetwork(sortedPop[i].brain));
        }

        int randomCount = (int)(popSize * 0.05f);
        for (int i = 0; i < randomCount; i++)
        {
            newBrains.Add(new NeuralNetwork(netLayers));
        }
        
        List<NeuralNetwork> parentPool = sortedPop.Take((int)(sortedPop.Count * 0.5f)).Select(c => c.brain).ToList();
        int remainingCount = popSize - newBrains.Count;
        for (int i = 0; i < remainingCount; i++)
        {
            NeuralNetwork parentA = parentPool[Random.Range(0, parentPool.Count)];
            NeuralNetwork parentB = parentPool[Random.Range(0, parentPool.Count)];
            NeuralNetwork child = NeuralNetwork.Crossover(parentA, parentB);
            child.Mutate(mutRate, mutStr);
            newBrains.Add(child);
        }
        return newBrains;
    }

    private void SaveTopBrains(List<Creature> sortedPopulation, string path)
    {
        BrainSaveData saveData = new BrainSaveData();
        saveData.generation = this.generation;
        int count = Mathf.Min(sortedPopulation.Count, topBrainsToSave);
        for (int i = 0; i < count; i++)
        {
            saveData.brains.Add(sortedPopulation[i].brain.GetData());
        }
        File.WriteAllText(path, JsonUtility.ToJson(saveData, true));
    }

    private List<NeuralNetwork> LoadTopBrains(string path, int[] netLayers, int popSize)
    {
        List<NeuralNetwork> loadedBrains = new List<NeuralNetwork>();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            BrainSaveData loadedData = JsonUtility.FromJson<BrainSaveData>(json);
            
            this.generation = loadedData.generation;

            foreach (var brainData in loadedData.brains)
            {
                NeuralNetwork brain = new NeuralNetwork(netLayers);
                brain.LoadAndTransferData(brainData);
                loadedBrains.Add(brain);
            }
            Debug.Log($"Loaded generation {this.generation} with {loadedBrains.Count} brains from {path}");
        }

        List<NeuralNetwork> startingBrains = new List<NeuralNetwork>();
        if (loadedBrains.Count > 0)
        {
            for (int i = 0; i < popSize; i++)
            {
                NeuralNetwork parentBrain = loadedBrains[Random.Range(0, loadedBrains.Count)];
                NeuralNetwork childBrain = new NeuralNetwork(parentBrain);
                childBrain.Mutate(0.1f, 0.1f);
                startingBrains.Add(childBrain);
            }
        }
        else
        {
            Debug.Log($"No save file found at {path}. Creating fresh population.");
            for (int i = 0; i < popSize; i++)
            {
                startingBrains.Add(new NeuralNetwork(netLayers));
            }
        }
        return startingBrains;
    }

    void OnApplicationQuit()
    {
        Time.timeScale = 1f;
    }
}