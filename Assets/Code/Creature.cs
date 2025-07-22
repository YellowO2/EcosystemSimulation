using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Creature : MonoBehaviour
{
    // --- Core Components ---
    public NeuralNetwork brain;
    private Rigidbody2D rb;

    // --- Fitness & Survival ---
    public float energy = 100f;
    public float fitness = 0f;

    // --- Physical Traits ---
    private float moveForce = 5f; // CHANGED: Renamed from moveSpeed to reflect force application
    private float jumpForce = 5f;

    // --- Senses ---
    private float detectionRadius = 15f;
    private LayerMask groundLayer;
    private LayerMask foodLayer;
    private LayerMask creatureLayer;
    private bool isGrounded;
    private bool isInWater; //currrently i am using tags to detect water, not sure if this is the best way

    // --- Physics State ---
    private float originalGravityScale;
    private float originalDrag;

    [Header("Water Physics")]
    public float underwaterGravityScale = 0.2f;
    public float underwaterDrag = 3f;

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

        originalGravityScale = rb.gravityScale;
        originalDrag = rb.linearDamping; // drag is outdated i think, use linearDamping instead
    }

    void FixedUpdate()
    {
        if (brain == null) return;

        float[] inputs = GatherInputs();
        float[] outputs = brain.FeedForward(inputs);

        // --- CHANGED: Use a single Action function ---
        Act(outputs[0], outputs[1]);

        energy -= (0.5f + Mathf.Abs(rb.linearVelocity.magnitude) * 0.1f) * Time.fixedDeltaTime; // More dynamic energy cost
        energyBar.UpdateBar(energy, 100f);
        if (energy <= 0)
        {
            Die();
        }
    }

    private float[] GatherInputs()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.1f, groundLayer);
        Transform closestFood = FindClosest(foodLayer);
        Vector2 foodDir = closestFood ? (closestFood.position - transform.position).normalized : Vector2.zero;

        return new float[6]
        {
            isGrounded ? 1f : 0f,
            rb.linearVelocity.x / 10f,
            rb.linearVelocity.y / 10f,
            foodDir.x,
            foodDir.y,
            isInWater ? 1f : 0f
        };
    }

    // --- NEW: Unified action function ---
    private void Act(float horizontal, float vertical)
    {
        if (isInWater)
        {
            // In water, apply continuous force for swimming
            Vector2 swimForce = new Vector2(horizontal, vertical) * moveForce;
            rb.AddForce(swimForce);
        }
        else if (isGrounded)
        {
            // On land, set horizontal velocity directly for responsiveness
            rb.linearVelocity = new Vector2(horizontal * moveForce, rb.linearVelocity.y);

            // On land, vertical is an impulse jump, only if positive
            if (vertical > 0)
            {
                rb.AddForce(new Vector2(0f, vertical * jumpForce), ForceMode2D.Impulse);
                energy -= 0.5f * vertical; // Jumping costs energy based on force
            }
        }
    }

    // --- REMOVED: Old Move() and Jump() functions are now replaced by Act() ---

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            Debug.Log("Entered water, applying underwater physics.");
            isInWater = true;
            rb.gravityScale = underwaterGravityScale;
            rb.linearDamping = underwaterDrag;
        }
        // --- FIXED: Eat on trigger enter, not exit ---
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
            isInWater = false;
            rb.gravityScale = originalGravityScale;
            rb.linearDamping = originalDrag;
        }
    }

    void Eat(Plant plant)
    {
        float energyGained = plant.BeEaten();
        energy += energyGained;
        if (energy > 100f) energy = 100f;
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