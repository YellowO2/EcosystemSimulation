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
    public int populationOverride = 0; // 0 means use species default
    [Range(0f, 5f)] public float globalMutationMultiplier = 1f;
    #endregion


    #region Private State
    private Dictionary<string, List<Creature>> population = new Dictionary<string, List<Creature>>();
    private Dictionary<string, SpeciesConfiguration> speciesConfigMap = new Dictionary<string, SpeciesConfiguration>();
    private Dictionary<string, float> bestFitnessPerSpecies = new Dictionary<string, float>();
    private List<string> activeSpeciesNames = new List<string>();
    private float generationTimer;
    public int currentGeneration { get; private set; }
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
        currentGeneration = 1;

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

            int popSize = populationOverride > 0 ? populationOverride : 1;

            // Spawn the initial population from the seed brain (loaded or random)
            for (int i = 0; i < popSize; i++)
            {
                NeuralNetwork childBrain = new NeuralNetwork(seedBrain);
                if (i > 0) childBrain.Mutate(config.baseMutationRate, config.baseMutationStrength);
                SpawnCreature(config, worldManager.GetSpawnPoint(), childBrain);
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
        currentGeneration++;
        var nextGenerationBrains = new Dictionary<string, List<NeuralNetwork>>();

        foreach (string speciesName in activeSpeciesNames)
        {
            SpeciesConfiguration config = speciesConfigMap[speciesName];
            List<Creature> currentCreatures = population[speciesName];
            currentCreatures.RemoveAll(c => c == null);
            if (currentCreatures.Count == 0) continue;

            //sort the entire population
            currentCreatures.Sort((a, b) => b.fitness.CompareTo(a.fitness));
            List<Creature> survivors = currentCreatures; // Use a clearer name

            // Save champion brain
            if (survivors.Count > 0)
            {
                Creature champion = survivors[0];
                if (!bestFitnessPerSpecies.ContainsKey(speciesName) || champion.fitness > bestFitnessPerSpecies[speciesName])
                {
                    bestFitnessPerSpecies[speciesName] = champion.fitness;
                    DatabaseManager.Instance.SaveBestBrain(speciesName, champion.brain);
                    Debug.Log($"New champion for {speciesName} with fitness {champion.fitness}! Brain saved.");
                }
            }

            var newBrains = new List<NeuralNetwork>();
            int targetPopulation = populationOverride > 0 ? populationOverride : 1;

            // guarantee at least one elite (unchanged brain) and one immigrant (random)
            int eliteCount = Mathf.Max(1, (int)(targetPopulation * 0.1f)); // 10% is a good standard for elites
            for (int i = 0; i < eliteCount && i < survivors.Count; i++)
            {
                newBrains.Add(new NeuralNetwork(survivors[i].brain));
            }

            // Immigrants provide fresh, random genetic material.
            int immigrantCount = Mathf.Max(1, (int)(targetPopulation * 0.05f));
            for (int i = 0; i < immigrantCount; i++)
            {
                newBrains.Add(new NeuralNetwork(config.networkLayers));
            }

            float totalFitness = survivors.Sum(c => c.fitness);

            // If all fitness is zero, every survivor gets an equal chance to be a parent.
            bool useEqualWeight = totalFitness <= 0;

            // Create the rest of the population via Crossover and Mutation
            while (newBrains.Count < targetPopulation)
            {
                NeuralNetwork parentA = SelectParent(survivors, totalFitness, useEqualWeight);
                NeuralNetwork parentB = SelectParent(survivors, totalFitness, useEqualWeight);

                NeuralNetwork childBrain = NeuralNetwork.Crossover(parentA, parentB);
                childBrain.Mutate(config.baseMutationRate * globalMutationMultiplier, config.baseMutationStrength);
                newBrains.Add(childBrain);
            }

            nextGenerationBrains[speciesName] = newBrains;
        }

        // --- Respawn Logic ---
        ClearAllCreatures();

        foreach (string speciesName in activeSpeciesNames)
        {
            if (!nextGenerationBrains.ContainsKey(speciesName)) continue;
            SpeciesConfiguration config = speciesConfigMap[speciesName];
            foreach (var brain in nextGenerationBrains[speciesName])
            {
                SpawnCreature(config, worldManager.GetSpawnPoint(), brain);
            }
        }
    }

    // Helps to select parent where higher fitness have higher chance
    private NeuralNetwork SelectParent(List<Creature> candidates, float totalFitness, bool useEqualWeight)
    {
        if (useEqualWeight)
        {
            // If scores are bad, pick any survivor randomly.
            return candidates[Random.Range(0, candidates.Count)].brain;
        }

        // Roll for parent based on fitness
        float randomValue = Random.Range(0f, totalFitness);
        float currentSum = 0;

        foreach (var candidate in candidates)
        {
            currentSum += candidate.fitness;
            if (currentSum >= randomValue)
            {
                return candidate.brain;
            }
        }
        // in case of floating point errors, return the best.
        return candidates.First().brain;
    }


    public void ClearAllCreatures()
    {
        foreach (var creatureList in population.Values)
        {
            for (int i = creatureList.Count - 1; i >= 0; i--)
            {
                if (creatureList[i] != null) Destroy(creatureList[i].gameObject);
            }
            creatureList.Clear();
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
        currentGeneration = 0;
    }

    public void PackSimulationData(WorldSaveState state)
    {
        state.activeSpeciesNames = new List<string>(this.activeSpeciesNames);
        state.creatures.Clear();
        state.currentGeneration = this.currentGeneration;
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
        this.currentGeneration = state.currentGeneration;

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