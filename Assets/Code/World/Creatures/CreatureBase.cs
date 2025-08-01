// Creature.cs
using UnityEngine;
using System;
using System.ComponentModel.Design;

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
    private (int count, float length, float[] distances)? whiskerDebugData;
    private float? detectionRadiusDebug;

    private int frameSkip = 4;
    private int frameCounter;
    private float[] lastBrainOutputs;



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

        frameCounter++;
        if (frameCounter >= frameSkip)
        {
            frameCounter = 0; // Reset counter

            // --- Run Expensive Logic ---
            float[] inputs = GatherInputs();
            float[] outputs = brain.FeedForward(inputs);
            lastBrainOutputs = outputs; // Store the new decision
            PerformAction(outputs);
        }
        else if (lastBrainOutputs != null)
        {
            // --- Reuse Old Decision ---
            // On skipped frames, just re-apply the last action.
            PerformAction(lastBrainOutputs);
        }

        HandleSpriteFlipping();
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



    private void HandleSpriteFlipping()
    {
        // Get the horizontal velocity.
        float horizontalVelocity = rb.linearVelocity.x;

        // small threshold (0.1f) to prevent the creature from rapidly
        // flipping back and forth if it's moving very slowly.
        if (horizontalVelocity > 0.1f)
        {
            // Moving right
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontalVelocity < -0.1f)
        {
            // Moving left
            transform.localScale = new Vector3(-1, 1, 1);
        }
        // If velocity is between -0.1 and 0.1, the sprite doesn't flip.
    }

    protected float[] SenseWithWhiskers(int whiskerCount, float whiskerLength, LayerMask layerMask)
    {
        float[] whiskerInputs = new float[whiskerCount];
        float[] whiskerDistances = new float[whiskerCount];

        float totalAngleSpread = 180f;
        float startAngle = -totalAngleSpread / 2f;
        float angleStep = totalAngleSpread / (whiskerCount - 1);
        Vector2 facingDirection = (transform.localScale.x > 0) ? (Vector2)transform.right : -(Vector2)transform.right;

        for (int i = 0; i < whiskerCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Vector2 direction = Quaternion.Euler(0, 0, currentAngle) * facingDirection;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, whiskerLength, layerMask);

            if (hit.collider != null)
            {
                whiskerInputs[i] = 1f - (hit.distance / whiskerLength);
                whiskerDistances[i] = hit.distance;
            }
            else
            {
                whiskerInputs[i] = 0f;
                whiskerDistances[i] = whiskerLength;
            }
        }

        whiskerDebugData = (whiskerCount, whiskerLength, whiskerDistances);

        return whiskerInputs;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !whiskerDebugData.HasValue) return;

        var (count, length, distances) = whiskerDebugData.Value;

        if (distances == null || distances.Length != count) return;

        float totalAngleSpread = 180f;
        float startAngle = -totalAngleSpread / 2f;
        float angleStep = (count > 1) ? totalAngleSpread / (count - 1) : 0;
        Vector2 facingDirection = (transform.localScale.x > 0) ? (Vector2)transform.right : -(Vector2)transform.right;
        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Vector2 direction = Quaternion.Euler(0, 0, currentAngle) * facingDirection;
            float dist = distances[i];

            Gizmos.color = dist < length ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * dist);
        }

        // if (detectionRadiusDebug.HasValue)
        // {
        //     Gizmos.color = Color.yellow;
        //     Gizmos.DrawWireSphere(transform.position, detectionRadiusDebug.Value);
        // }
    }
}