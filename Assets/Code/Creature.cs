using UnityEngine;

public class Creature : MonoBehaviour
{
    // --- Core Components ---
    public NeuralNetwork brain;
    private Rigidbody2D rb;

    // --- Fitness & Survival ---
    public float energy = 100f;
    public float fitness = 0f; // Calculated by SimulationManager at the end

    // --- Physical Traits ---
    private float moveSpeed = 5f;
    private float jumpForce = 12f;

    // --- Senses ---
    private float detectionRadius = 15f;
    private LayerMask groundLayer;
    private LayerMask foodLayer;
    private LayerMask creatureLayer;
    private bool isGrounded;

    // --- UI ---
    public StatusBar energyBar;


    public void Init(NeuralNetwork brain)
    {
        this.brain = brain;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        groundLayer = LayerMask.GetMask("Ground");
        foodLayer = LayerMask.GetMask("Food");
        creatureLayer = LayerMask.GetMask("Creature");
    }

    void FixedUpdate()
    {
        if (brain == null) return;

        // === 1. GET INPUTS ===
        float[] inputs = GatherInputs();

        // === 2. THINK ===
        float[] outputs = brain.FeedForward(inputs);

        // === 3. ACT ===
        Move(outputs[0]);
        if (outputs[1] > 0.5f) Jump();

        // --- Survival ---
        energy -= 0.8f * Time.fixedDeltaTime; // Base metabolic cost
        energyBar.UpdateBar(energy, 100f);
        if (energy <= 0)
        {
            Die();
        }
    }

    private float[] GatherInputs()
    {
        // Ground Sensor
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.1f, groundLayer);

        // Find closest objects
        Transform closestFood = FindClosest(foodLayer);

        // Normalize directions (or use zero if nothing is found)
        Vector2 foodDir = closestFood ? (closestFood.position - transform.position).normalized : Vector2.zero;
    

        // The 7 inputs for the Neural Network
        return new float[5]
        {
            isGrounded ? 1f : 0f,
            rb.linearVelocity.x / 10f, // Normalize
            rb.linearVelocity.y / 10f, // Normalize
            foodDir.x,
            foodDir.y,
        };
    }

    private void Move(float horizontal)
    {
        if (isGrounded)
        {
             rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
        }
       
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
            energy -= 1f; // Jumping costs energy
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Food"))
        {
            Eat(collision.gameObject);
        }
    }

    void Eat(GameObject food)
    {
        energy += 25f;
        if (energy > 100f) energy = 100f;
        Destroy(food);
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
        // Don't Destroy immediately. Just deactivate.
        // The SimulationManager will clean up all game objects at the end of the generation.
        gameObject.SetActive(false);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}