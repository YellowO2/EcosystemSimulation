using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Creature : MonoBehaviour
{
    // ... (Core Components, Energy, Traits, etc. are the same) ...
    public NeuralNetwork brain;
    private Rigidbody2D rb;
    public float energy = 100f;
    public float fitness = 0f;
    private float moveForce = 8f;
    // private float jumpForce = 5f;
    public float rotationForce = 1f;
    private float detectionRadius = 10f;
    private LayerMask groundLayer;
    private LayerMask foodLayer;
    private bool isGrounded;
    private bool isInWater;
    private float originalGravityScale;
    private float originalDrag;
    public float underwaterGravityScale = 0f;
    public float underwaterDrag = 3f;
    public StatusBar energyBar;
    private Vector2 Forward => -transform.right;


    public void Init(NeuralNetwork brain)
    {
        this.brain = brain;
        this.fitness = 0f; // Always reset fitness
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
       
        groundLayer = LayerMask.GetMask("Ground");
        foodLayer = LayerMask.GetMask("Food");
        originalGravityScale = rb.gravityScale;
        originalDrag = rb.linearDamping; // Corrected from linearDamping
    }

    void FixedUpdate()
    {
        if (brain == null) return;

        float[] inputs = GatherInputs();
        float[] outputs = brain.FeedForward(inputs);
        Act(outputs[0], outputs[1]);

        // --- UPDATED: Fitness Calculation Logic ---
        UpdateFitness();

        energy -= (0.5f + rb.linearVelocity.magnitude * 0.1f) * Time.fixedDeltaTime;
        energyBar.UpdateBar(energy, 100f);
        if (energy <= 0)
        {
            Die();
        }
    }

    // --- NEW: Centralized Fitness Logic ---
    private void UpdateFitness()
    {
        // Small reward for surviving
        fitness += Time.fixedDeltaTime * 0.01f;
    }

    private float[] GatherInputs()
    {

        Transform closestFood = FindClosest(foodLayer);
        bool seesFood = closestFood != null;
        float foodAngle = 0f;
        float foodDist = 1f; // Default to 1 (max distance)

        if (seesFood)
        {
            Vector2 toFood = closestFood.position - transform.position;

            //relative angle
            foodAngle = Vector2.SignedAngle(this.Forward, toFood.normalized) / 180f; // Normalized -1 to 1

            foodDist = toFood.magnitude / detectionRadius; // Normalized 0-1
        }

        // Let's use 4 simple inputs for now.
        // 1. How fast am I spinning?
        // 2. Do I see food?
        // 3. What angle is the food at?
        // 4. How far is the food?
        return new float[4]
        {
            rb.angularVelocity / 360f, // Normalized rotation speed
            seesFood ? 1f : 0f,
            foodAngle,
            foodDist
        };
    }

    private void Act(float turn, float thrust)
    {
        if (isInWater)
        {
            // OUTPUT 1: Turn (-1 to 1)
            // We use -turn because AddTorque is counter-clockwise. This makes a positive 'turn' value turn right.
            rb.AddTorque(-turn * rotationForce);

            // OUTPUT 2: Thrust (let's use 0 to 1 for simplicity, and represents forward direction only)
            float thrustClamped = Mathf.Clamp01(thrust);
            rb.AddForce(moveForce * thrustClamped * this.Forward);
        }
        else
        {
            // Land movement later
        }
    }

    // --- UPDATED: Eat() now provides the big "jackpot" fitness reward ---
    void Eat(Plant plant)
    {
        float energyGained = plant.BeEaten();
        energy += energyGained;
        if (energy > 200f) energy = 200f;

        fitness += 30f; // Big reward for eating!
    }


    // ... (Triggers, FindClosest, Die, Gizmos functions are fine) ...
    void OnTriggerEnter2D(Collider2D other)
    {


        if (other.CompareTag("Water"))
        {
            Debug.Log("Entered water  is it still running? This should only happen once.");
            if (isInWater) return;
            isInWater = true;
            rb.gravityScale = underwaterGravityScale;
            rb.linearDamping = underwaterDrag;
        }
        else if (other.CompareTag("Food"))
        {
            Plant plant = other.GetComponent<Plant>();
            if (plant != null)
            {
                Eat(plant);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {

        if (other.CompareTag("Water"))
        {
            if (!isInWater) return; // Prevents multiple exits
            isInWater = false;

            rb.gravityScale = originalGravityScale;
            rb.linearDamping = originalDrag;
        }
    }
    private Transform FindClosest(LayerMask layer, bool findOtherCreatures = false)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, layer);
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (findOtherCreatures && hit.gameObject == this.gameObject) continue;

            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = hit.transform;
            }
        }
        return closest;
    }

    private void Die()
    {
        gameObject.SetActive(false);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}