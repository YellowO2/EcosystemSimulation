using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;

public class PopulationManager : MonoBehaviour
{
    [Header("System References")]
    public WorldGenerator worldManager;

    [Header("UI Elements")]
    public TextMeshProUGUI populationText;

    [Header("Global Evolution Settings")]
    [Range(0f, 5f)] public float globalMutationMultiplier = 1f;

    [Header("System Settings")]
    public float populationCheckInterval = 10f;

    [Header("Species Management")]
    public List<SpeciesConfiguration> speciesToManage;

    private Dictionary<string, List<Creature>> population = new Dictionary<string, List<Creature>>();
    private Dictionary<string, SpeciesConfiguration> speciesConfigMap = new Dictionary<string, SpeciesConfiguration>();
    private float populationCheckTimer;
    
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
        foreach (var config in speciesToManage)
        {
            speciesConfigMap[config.speciesName] = config;
            population[config.speciesName] = new List<Creature>();
        }
    }

    public void StartFreshSimulation()
    {
        ClearSimulation();
        foreach (var config in speciesToManage)
        {
            SpawnCreatures(config, config.restockAmount);
        }
    }

    void Update()
    {
        populationCheckTimer += Time.deltaTime;
        if (populationCheckTimer >= populationCheckInterval)
        {
            CheckAndRestockPopulation();
            populationCheckTimer = 0f;
        }
        UpdateUI();
    }

    private void CheckAndRestockPopulation()
    {
        foreach (var config in speciesToManage)
        {
            population[config.speciesName].RemoveAll(item => item == null);
            int currentCount = population[config.speciesName].Count;
            if (currentCount < config.minPopulation)
            {
                SpawnCreatures(config, config.restockAmount);
            }
        }
    }

    private void SpawnCreatures(SpeciesConfiguration config, int count)
    {
        string brainPath = Path.Combine(Application.dataPath, config.brainArchiveFile);
        List<NeuralNetwork> brains = LoadStarterBrains(brainPath, config, count);

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPosition = worldManager.GetRandomSpawnPointOnGround();
            Creature newCreature = Instantiate(config.prefab, spawnPosition, Quaternion.identity).GetComponent<Creature>();
            newCreature.Init(brains[i], config.speciesName);
            population[config.speciesName].Add(newCreature);
        }
    }

    private List<NeuralNetwork> LoadStarterBrains(string path, SpeciesConfiguration config, int count)
    {
        List<NeuralNetwork> starterBrains = new List<NeuralNetwork>();
        for (int i = 0; i < count; i++)
        {
            starterBrains.Add(new NeuralNetwork(config.networkLayers));
        }
        return starterBrains;
    }

    private void UpdateUI()
    {
        if (populationText == null) return;
        string text = "";
        foreach (var config in speciesToManage)
        {
            text += $"{config.speciesName}: {population[config.speciesName].Count} | ";
        }
        populationText.text = text;
    }
    
    private void HandleCreatureBorn(Creature parent)
    {
        if (!speciesConfigMap.ContainsKey(parent.speciesName)) return;

        SpeciesConfiguration config = speciesConfigMap[parent.speciesName];
        Vector2 spawnPos = (Vector2)parent.transform.position + Random.insideUnitCircle * 2f;

        Creature child = Instantiate(config.prefab, spawnPos, Quaternion.identity).GetComponent<Creature>();

        NeuralNetwork childBrain = new NeuralNetwork(parent.brain);
        float effectiveMutationRate = config.baseMutationRate * globalMutationMultiplier;
        childBrain.Mutate(effectiveMutationRate, config.baseMutationStrength);
        
        child.Init(childBrain, parent.speciesName);
        population[parent.speciesName].Add(child);
    }
    
    public void PackSimulationData(WorldSaveState state)
    {
        state.creatures.Clear();
        foreach (var creatureList in population.Values)
        {
            foreach (var creature in creatureList)
            {
                if (creature == null) continue;
                CreatureSaveData data = new CreatureSaveData
                {
                    speciesName = creature.speciesName,
                    position = creature.transform.position,
                    velocity = creature.GetComponent<Rigidbody2D>().linearVelocity,
                    brainData = creature.brain.GetData()
                };
                state.creatures.Add(data);
            }
        }
    }

    public void LoadSimulationFromState(WorldSaveState state)
    {
        ClearSimulation();
        foreach (var data in state.creatures)
        {
            if (!speciesConfigMap.ContainsKey(data.speciesName)) continue;

            SpeciesConfiguration config = speciesConfigMap[data.speciesName];
            Creature creature = Instantiate(config.prefab, data.position, Quaternion.identity).GetComponent<Creature>();

            NeuralNetwork brain = new NeuralNetwork(config.networkLayers);
            brain.LoadAndTransferData(data.brainData);

            creature.Init(brain, config.speciesName);
            creature.GetComponent<Rigidbody2D>().linearVelocity = data.velocity;

            population[config.speciesName].Add(creature);
        }
    }

    public void ClearSimulation()
    {
        foreach (var creatureList in population.Values)
        {
            foreach (var creature in creatureList)
            {
                if (creature != null) Destroy(creature.gameObject);
            }
            creatureList.Clear();
        }
    }
}