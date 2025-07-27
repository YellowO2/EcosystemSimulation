using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PopulationManager : MonoBehaviour
{
    #region Configuration
    [Header("System References")]
    public WorldGenerator worldManager;
    public SpeciesDatabase speciesDatabase;

    [Header("Simulation Settings")]
    public SimulationMode currentMode;
    public enum SimulationMode { Gym, Ecosystem }
    public float generationTime = 20f;
    [Range(0f, 5f)] public float globalMutationMultiplier = 1f;
    #endregion


    #region Private State
    private Dictionary<string, List<Creature>> population = new Dictionary<string, List<Creature>>();
    private Dictionary<string, SpeciesConfiguration> speciesConfigMap = new Dictionary<string, SpeciesConfiguration>();
    private Dictionary<string, float> bestFitnessPerSpecies = new Dictionary<string, float>();
    private List<string> activeSpeciesNames = new List<string>();
    private float generationTimer;
    #endregion


    #region Unity Lifecycle & Setup
    void OnEnable()
    {
        Creature.OnCreatureBorn += HandleCreatureBorn;
    }

    void OnDisable()
    {
        Creature.OnCreatureBorn -= HandleCreatureBorn;
    }

    void Awake()
    {
        foreach (var config in speciesDatabase.allSpecies)
        {
            speciesConfigMap[config.speciesName] = config;
            population[config.speciesName] = new List<Creature>();
        }
    }

    void Update()
    {
        if (currentMode != SimulationMode.Gym || activeSpeciesNames.Count == 0) return;

        generationTimer += Time.deltaTime;
        if (generationTimer >= generationTime)
        {
            RunNextGymGeneration();
            generationTimer = 0f;
        }
    }
    #endregion


    #region Core Simulation Control
    public void ConfigureAndStartSimulation(List<string> speciesToActivate)
    {
        ClearSimulation();
        this.activeSpeciesNames = speciesToActivate;
        generationTimer = 0f;

        foreach (string speciesName in activeSpeciesNames)
        {
            SpeciesConfiguration config = speciesConfigMap[speciesName];
            
            // Try to load the best brain saved for this world
            NeuralNetworkData loadedBrainData = DatabaseManager.Instance.LoadBestBrainData(speciesName);
            NeuralNetwork seedBrain = new NeuralNetwork(config.networkLayers);

            if (loadedBrainData != null)
            {
                seedBrain.LoadAndTransferData(loadedBrainData);
                Debug.Log($"Loaded champion brain for {speciesName}.");
            }

            // Spawn the initial population from the seed brain (loaded or random)
            for (int i = 0; i < config.initialPopulation; i++)
            {
                NeuralNetwork childBrain = new NeuralNetwork(seedBrain);
                if (i > 0) childBrain.Mutate(config.baseMutationRate, config.baseMutationStrength);
                SpawnCreature(config, worldManager.GetRandomSpawnPointOnGround(), childBrain);
            }
        }
    }

    private Creature SpawnCreature(SpeciesConfiguration config, Vector2 position, NeuralNetwork brain)
    {
        Creature newCreature = Instantiate(config.prefab, position, Quaternion.identity).GetComponent<Creature>();
        newCreature.Init(brain, config.speciesName);
        population[config.speciesName].Add(newCreature);
        return newCreature;
    }

    private void HandleCreatureBorn(Creature parent)
    {
        if (currentMode != SimulationMode.Ecosystem) return;
        if (!speciesConfigMap.ContainsKey(parent.speciesName)) return;

        SpeciesConfiguration config = speciesConfigMap[parent.speciesName];
        Vector2 spawnPos = (Vector2)parent.transform.position + Random.insideUnitCircle * 2f;
        NeuralNetwork childBrain = new NeuralNetwork(parent.brain);
        childBrain.Mutate(config.baseMutationRate * globalMutationMultiplier, config.baseMutationStrength);
        SpawnCreature(config, spawnPos, childBrain);
    }
    #endregion


    #region Gym Mode Logic
    private void RunNextGymGeneration()
    {
        var nextGenerationBrains = new Dictionary<string, List<NeuralNetwork>>();

        foreach (string speciesName in activeSpeciesNames)
        {
            SpeciesConfiguration config = speciesConfigMap[speciesName];
            List<Creature> currentCreatures = population[speciesName];
            currentCreatures.RemoveAll(c => c == null);
            if (currentCreatures.Count == 0) continue;

            List<Creature> positiveFitnessSurvivors = currentCreatures.Where(c => c.fitness > 0).ToList();
            positiveFitnessSurvivors.Sort((a, b) => b.fitness.CompareTo(a.fitness));
            
            // Check for and save a new champion
            if (positiveFitnessSurvivors.Count > 0)
            {
                Creature champion = positiveFitnessSurvivors[0];
                if (!bestFitnessPerSpecies.ContainsKey(speciesName) || champion.fitness > bestFitnessPerSpecies[speciesName])
                {
                    bestFitnessPerSpecies[speciesName] = champion.fitness;
                    DatabaseManager.Instance.SaveBestBrain(speciesName, champion.brain);
                    Debug.Log($"New champion for {speciesName} with fitness {champion.fitness}! Brain saved.");
                }
            }
            
            // --- The rest of the breeding logic remains the same ---
            var newBrains = new List<NeuralNetwork>();
            int targetPopulation = config.initialPopulation;
            if (positiveFitnessSurvivors.Count == 0)
            {
                for (int i = 0; i < targetPopulation; i++) newBrains.Add(new NeuralNetwork(config.networkLayers));
                nextGenerationBrains[speciesName] = newBrains;
                continue;
            }
            int immigrantCount = (int)(targetPopulation * 0.05f);
            for (int i = 0; i < immigrantCount; i++) newBrains.Add(new NeuralNetwork(config.networkLayers));
            int eliteCount = (int)(targetPopulation * 0.05f);
            for (int i = 0; i < eliteCount; i++) newBrains.Add(new NeuralNetwork(positiveFitnessSurvivors[i % positiveFitnessSurvivors.Count].brain));
            var parentPool = positiveFitnessSurvivors.Take(Mathf.Max(1, (int)(positiveFitnessSurvivors.Count * 0.5f))).ToList();
            while (newBrains.Count < targetPopulation)
            {
                NeuralNetwork parentA = parentPool[Random.Range(0, parentPool.Count)].brain;
                NeuralNetwork parentB = parentPool[Random.Range(0, parentPool.Count)].brain;
                NeuralNetwork childBrain = (parentPool.Count >= 2) ? NeuralNetwork.Crossover(parentA, parentB) : new NeuralNetwork(parentA);
                childBrain.Mutate(config.baseMutationRate * globalMutationMultiplier, config.baseMutationStrength);
                newBrains.Add(childBrain);
            }
            nextGenerationBrains[speciesName] = newBrains;
        }

        List<string> speciesToRespawn = new List<string>(activeSpeciesNames);
        ClearSimulation();
        this.activeSpeciesNames = speciesToRespawn;

        foreach (string speciesName in activeSpeciesNames)
        {
            if (!nextGenerationBrains.ContainsKey(speciesName)) continue;
            SpeciesConfiguration config = speciesConfigMap[speciesName];
            foreach (var brain in nextGenerationBrains[speciesName])
            {
                SpawnCreature(config, worldManager.GetRandomSpawnPointOnGround(), brain);
            }
        }
    }
    #endregion


    #region Utility & Data Management
    public void ClearSimulation()
    {
        foreach (var creatureList in population.Values)
        {
            for (int i = creatureList.Count - 1; i >= 0; i--)
            {
                if (creatureList[i] != null) Destroy(creatureList[i].gameObject);
            }
            creatureList.Clear();
        }
        activeSpeciesNames.Clear();
        bestFitnessPerSpecies.Clear();
    }
    
    public void PackSimulationData(WorldSaveState state)
    {
        state.activeSpeciesNames = new List<string>(this.activeSpeciesNames);
        state.creatures.Clear();
        foreach (var creatureList in population.Values)
        {
            foreach (var creature in creatureList)
            {
                if (creature == null) continue;
                state.creatures.Add(new CreatureSaveData
                {
                    speciesName = creature.speciesName,
                    position = creature.transform.position,
                    velocity = creature.GetComponent<Rigidbody2D>().linearVelocity,
                    brainData = creature.brain.GetData()
                });
            }
        }
    }

    public void LoadSimulationFromState(WorldSaveState state)
    {
        ClearSimulation();
        this.activeSpeciesNames = new List<string>(state.activeSpeciesNames);

        foreach (var data in state.creatures)
        {
            if (!speciesConfigMap.ContainsKey(data.speciesName)) continue;
            SpeciesConfiguration config = speciesConfigMap[data.speciesName];
            NeuralNetwork brain = new NeuralNetwork(config.networkLayers);
            brain.LoadAndTransferData(data.brainData);
            
            Creature creature = SpawnCreature(config, data.position, brain);
            if (creature.GetComponent<Rigidbody2D>() != null) creature.GetComponent<Rigidbody2D>().linearVelocity = data.velocity;
        }
    }
    #endregion
}