using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Creature : MonoBehaviour
{
    // Brain
    public NeuralNetwork brain;

    // Components
    private Rigidbody2D rb;

    // Vitals & State
    public float energy = 100f;
    public float fitness = 0f;
    private bool isInWater;
    private float lastDistanceToFood = float.MaxValue;
    private bool isFacingRight = true;

    // Physics & Movement
    private float moveForce = 10f;
    public float underwaterGravityScale = 0f;
    public float underwaterDrag = 3f;
    private float originalGravityScale;
    private float originalDrag;

    // --- MODIFIED: Sensing ---
    [Header("AI Sensing")]
    public float whiskerLength = 5f;
    public LayerMask groundLayer; // Assign "Ground" layer ONLY
    public float foodDetectionRadius = 10f;
    private float[] whiskerDebugDistances = new float[5]; 

    // Layers
    private LayerMask foodLayer;

    public void Init(NeuralNetwork brain)
    {
        this.brain = brain;
        this.fitness = 0f;
        this.lastDistanceToFood = float.MaxValue;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        foodLayer = LayerMask.GetMask("Food");
        isFacingRight = true;
        originalGravityScale = rb.gravityScale;
        originalDrag = rb.linearDamping;
    }

    void FixedUpdate()
    {
        if (brain == null) return;
        
        float[] inputs = GatherInputs();
        float[] outputs = brain.FeedForward(inputs);
        Act(outputs[0], outputs[1]);

        UpdateFitness();
        energy -= (0.5f + rb.linearVelocity.magnitude * 0.1f) * Time.fixedDeltaTime;
        if (energy <= 0) Die();
    }

    // --- REWRITTEN: Hybrid input gathering ---
    private float[] GatherInputs()
    {
        // 5 ground whiskers + 2 food direction + 2 velocity = 9 inputs
        float[] inputs = new float[9];
        Vector2 forward = isFacingRight ? Vector2.right : Vector2.left;

        // --- 1. Ground Detection (5 inputs) ---
        float[] whiskerAngles = { -90f, -45f, 0f, 45f, 90f };
        for (int i = 0; i < whiskerAngles.Length; i++)
        {
            Vector2 direction = Quaternion.Euler(0, 0, whiskerAngles[i]) * forward;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, whiskerLength, groundLayer);

            inputs[i] = (hit.collider != null) ? (1f - (hit.distance / whiskerLength)) : 0f;
            whiskerDebugDistances[i] = (hit.collider != null) ? hit.distance : whiskerLength;
        }

        // --- 2. Food Direction (2 inputs) ---
        Transform closestFood = FindClosest(foodLayer, foodDetectionRadius);
        Vector2 foodDirection = Vector2.zero;
        if (closestFood != null)
        {
            foodDirection = (closestFood.position - transform.position).normalized;
        }
        inputs[5] = foodDirection.x;
        inputs[6] = foodDirection.y;

        // --- 3. Self Velocity (2 inputs) ---
        inputs[7] = rb.linearVelocity.x;
        inputs[8] = rb.linearVelocity.y;

        return inputs;
    }

    private void Act(float horizontalThrust, float verticalThrust)
    {
        if (horizontalThrust > 0.1f && !isFacingRight) Flip();
        else if (horizontalThrust < -0.1f && isFacingRight) Flip();
        
        if (isInWater)
        {
            Vector2 force = new Vector2(horizontalThrust, verticalThrust) * moveForce;
            rb.AddForce(force);
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    // Helper to see the ground-detecting whiskers
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector2 forward = isFacingRight ? Vector2.right : Vector2.left;
        float[] whiskerAngles = { -90f, -45f, 0f, 45f, 90f };

        for (int i = 0; i < whiskerAngles.Length; i++)
        {
            Vector2 direction = Quaternion.Euler(0, 0, whiskerAngles[i]) * forward;
            float dist = whiskerDebugDistances[i];
            Gizmos.color = dist < whiskerLength ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * dist);
        }
    }

    // --- Modified FindClosest to accept a radius ---
    private Transform FindClosest(LayerMask layer, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, layer);
        Transform closest = null;
        float minDistance = Mathf.Infinity;
        foreach (var hit in hits)
        {
            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = hit.transform;
            }
        }
        return closest;
    }

    private void UpdateFitness()
    {
        Transform closestFood = FindClosest(foodLayer, foodDetectionRadius);
        if (closestFood != null)
        {
            float currentDistance = Vector2.Distance(transform.position, closestFood.position);
            if (currentDistance < lastDistanceToFood) fitness += 0.1f;
            lastDistanceToFood = currentDistance;
        }
    }

    void Eat(Plant plant)
    {
        float energyGained = plant.BeEaten();
        energy += energyGained;
        if (energy > 200f) energy = 200f;
        fitness += 30f;
    }

    private void Die()
    {
        gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            isInWater = true;
            rb.gravityScale = underwaterGravityScale;
            rb.linearDamping = underwaterDrag;
        }
        else if (other.CompareTag("Food"))
        {
            Plant plant = other.GetComponent<Plant>();
            if (plant != null) Eat(plant);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            isInWater = false;
            rb.gravityScale = originalGravityScale;
            rb.linearDamping = originalDrag;
        }
    }
}