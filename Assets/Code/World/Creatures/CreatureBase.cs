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
    protected bool isInWater; // Standardized flag for environment state

    [Header("Reproduction")]
    public float energyToReproduce = 150f;
    public float reproductionEnergyCost = 60f;

    [Header("Senses")]
    public LayerMask foodLayer;
    public LayerMask predatorLayer;
    public float foodDetectionRadius = 10f;
    public float predatorDetectionRadius = 6f;
    protected int whiskerCount = 5;
    protected float whiskerLength = 5f;
    protected bool isGrounded;
    public LayerMask groundLayer;

    [Header("Debug")]
    private (int count, float length, float[] distances)? whiskerDebugData;
    private float? detectionRadiusDebug;

    private int frameSkip = 4;
    private int frameCounter;
    protected Rigidbody2D rb;
    private float originalGravityScale;
    private float originalLinearDrag;
    private float[] lastBrainInputs;
    private float[] lastBrainOutputs;
    public float[] GetLastInputs() { return lastBrainInputs; }
    public float[] GetLastOutputs() { return lastBrainOutputs; }



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
        originalGravityScale = rb.gravityScale;
        originalLinearDrag = rb.linearDamping;
    }

    protected virtual void FixedUpdate()
    {
        if (brain == null) return;
        CheckGrounded();
        frameCounter++;
        if (frameCounter >= frameSkip)
        {
            frameCounter = 0;
            lastBrainInputs = GatherInputs();
            lastBrainOutputs = brain.FeedForward(lastBrainInputs);
            PerformAction(lastBrainOutputs);
        }
        else if (lastBrainOutputs != null)
        {
            PerformAction(lastBrainOutputs);
        }

        // HandleSpriteFlipping();
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

    protected virtual float[] GatherInputs()
    {
        // Vision: Wall/Obstacle detection
        float[] whiskerInputs = SenseWithWhiskers(whiskerCount, whiskerLength, groundLayer);

        // Senses: Food and Predator location
        Transform closestFood = FindClosest(foodLayer, foodDetectionRadius);
        Vector2 foodDirection = closestFood ? (closestFood.position - transform.position).normalized : Vector2.zero;

        Transform closestPredator = FindClosest(predatorLayer, predatorDetectionRadius);
        Vector2 predatorDirection = closestPredator ? (closestPredator.position - transform.position).normalized : Vector2.zero;

        // Proprioception: Internal state awareness
        float[] internalStateInputs = {
            isGrounded ? 1f : 0f,
            isInWater ? 1f : 0f,
            rb.linearVelocity.x / 10f,
            rb.linearVelocity.y / 10f
        };

        // Combine all sensory data into a single, standardized array
        float[] allInputs = new float[whiskerInputs.Length + 2 + 2 + 4];
        whiskerInputs.CopyTo(allInputs, 0);
        allInputs[whiskerInputs.Length] = foodDirection.x;
        allInputs[whiskerInputs.Length + 1] = foodDirection.y;
        allInputs[whiskerInputs.Length + 2] = predatorDirection.x;
        allInputs[whiskerInputs.Length + 3] = predatorDirection.y;
        internalStateInputs.CopyTo(allInputs, whiskerInputs.Length + 4);

        return allInputs;
    }
    protected abstract void PerformAction(float[] outputs);

    // Now virtual with a base implementation for universal energy cost
    protected virtual void UpdateFitnessAndEnergy()
    {
        // Nothing for now
    }

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

    // private void HandleSpriteFlipping()
    // {
    //     float horizontalVelocity = rb.linearVelocity.x;
    //     if (horizontalVelocity > 0.1f)
    //     {
    //         transform.localScale = new Vector3(1, 1, 1);
    //     }
    //     else if (horizontalVelocity < -0.1f)
    //     {
    //         transform.localScale = new Vector3(-1, 1, 1);
    //     }
    // }

    protected float[] SenseWithWhiskers(int whiskerCount, float whiskerLength, LayerMask layerMask)
    {
        float[] whiskerInputs = new float[whiskerCount];
        float[] whiskerDistances = new float[whiskerCount];

        float totalAngleSpread = 180f;
        float startAngle = -totalAngleSpread / 2f;
        float angleStep = totalAngleSpread / (whiskerCount - 1);
        Vector2 facingDirection = (transform.localScale.x > 0) ? Vector2.right : Vector2.left;

        for (int i = 0; i < whiskerCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.forward);
            Vector2 direction = transform.TransformDirection(rotation * facingDirection);

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
        Vector2 facingDirection = (transform.localScale.x > 0) ? Vector2.right : Vector2.left;

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.forward);
            Vector2 direction = transform.TransformDirection(rotation * facingDirection);
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

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            Debug.Log($" entered water.");
            isInWater = true;
            rb.gravityScale = 0;
            rb.linearDamping = 1;
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            isInWater = false;
            rb.gravityScale = originalGravityScale;
            rb.linearDamping = originalLinearDrag;
        }
    }

    protected virtual void CheckGrounded()
    {
        Vector2 castSize = new Vector2(transform.lossyScale.x * 0.9f, 0.1f);
        float castDistance = transform.lossyScale.y * 0.5f;
        Vector2 castOrigin = transform.position;

        RaycastHit2D hit = Physics2D.BoxCast(castOrigin, castSize, 0f, Vector2.down, castDistance, groundLayer);

        if (hit.collider != null)
        {
            // check if the thing we hit is not our own collider.
            if (hit.collider.gameObject != this.gameObject)
            {
                isGrounded = true;
                return;
            }
        }
        isGrounded = false;
    }
    
}