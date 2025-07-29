// Creature.cs
using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Creature : MonoBehaviour
{
    public static event Action<Creature> OnCreatureBorn;

    [Header("Identity")]
    public string speciesName { get; private set; }
    public NeuralNetwork brain;

    [Header("State")]
    public float energy = 100f;
    public float fitness = 0f;

    [Header("Reproduction")]
    public float energyToReproduce = 150f;
    public float reproductionEnergyCost = 60f;

    [Header("Debug")]
    private (int count, float length, float angle, float[] distances)? whiskerDebugData;
    private float? detectionRadiusDebug;


    protected Rigidbody2D rb;

    public virtual void Init(NeuralNetwork brain, string speciesName)
    {
        this.speciesName = speciesName;
        this.brain = brain;
        this.energy = 100f;
        this.fitness = 0f;
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void FixedUpdate()
    {
        if (brain == null) return;

        float[] inputs = GatherInputs();
        float[] outputs = brain.FeedForward(inputs);
        PerformAction(outputs);

        UpdateFitnessAndEnergy();

        if (energy >= energyToReproduce)
        {
            Reproduce();
        }
        if (energy <= 0)
        {
            Die();
        }
    }

    // --- Abstract Methods: Must be implemented by child classes ---
    protected abstract float[] GatherInputs();
    protected abstract void PerformAction(float[] outputs);
    protected abstract void UpdateFitnessAndEnergy();


    private void Reproduce()
    {
        energy -= reproductionEnergyCost;
        OnCreatureBorn?.Invoke(this);
    }

    public void Die()
    {
        Destroy(gameObject);
    }

    protected Transform FindClosest(LayerMask layer, float radius)
    {
        // Store the radius for the gizmo to use
        detectionRadiusDebug = radius;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, layer);
        Transform closest = null;
        float minDistance = Mathf.Infinity;
        foreach (var hit in hits)
        {
            if (hit.transform == this.transform) continue;
            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = hit.transform;
            }
        }
        return closest;
    }

    protected float[] SenseWithWhiskers(int whiskerCount, int whiskerLength, float maxAngle, LayerMask layerMask)
    {
        float[] whiskerInputs = new float[whiskerCount];
        float angleStep = (whiskerCount > 1) ? (maxAngle * 2) / (whiskerCount - 1) : 0;

        for (int i = 0; i < whiskerCount; i++)
        {
            float currentAngle = -maxAngle + (i * angleStep);
            Vector2 direction = Quaternion.Euler(0, 0, currentAngle) * transform.right;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, whiskerLength, layerMask);

            whiskerInputs[i] = (hit.collider != null) ? 1f - (hit.distance / whiskerLength) : 0f;
        }
        return whiskerInputs;
    }

    protected virtual void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (whiskerDebugData.HasValue)
        {
            var (count, length, maxAngle, distances) = whiskerDebugData.Value;
            float angleStep = (count > 1) ? (maxAngle * 2) / (count - 1) : 0;
            for (int i = 0; i < count; i++)
            {
                float currentAngle = -maxAngle + (i * angleStep);
                Vector2 direction = Quaternion.Euler(0, 0, currentAngle) * transform.right;
                float dist = distances[i];
                Gizmos.color = dist < length ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * dist);
            }
        }

        if (detectionRadiusDebug.HasValue)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadiusDebug.Value);
        }
    }
}