using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Creature : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D body;
    public GameObject creaturePrefab;

    private float moveDirection = 1f;
    private float moveSpeed = 2f;

    // === Drives (The "Brain Chemicals") ===
    public float maxDriveValue = 100f;
    // --- Laziness ---
    public float laziness = 10f;
    // // ---- Reproduction ----
    // public float reproductionDrive = 50f;
    // Other traits
    public float flexibility = 1f; // How not stubborn the creature is, i.e, how fast it changes its mind, i.e. time between action choice
    public float lastActionTime = 0f; // When was the last time the creature did something
    public float height = 1f;
    public float width = 1f;
    public float energy = 100f; // How much energy the creature has
    public float survivalTime = 0f; // A score for how well it's doing
    // --- Senses ---
    private float detectionRadius = 5f;
    private Transform target;
    public NeuralNetwork brain;
    private Transform closestFood;
    private int[] networkLayers = new int[] { 5, 4, 2 };

    // --- Movement & Physics ---
    [Header("Movement")]
    public float jumpForce = 5f;
    public LayerMask groundLayer;
    public LayerMask foodLayer;
    private bool isGrounded;
    

    //UI
    public StatusBar energyBar;
    public StatusBar reproductionBar;


    public void Init(NeuralNetwork brain)
    {
        this.brain = brain;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        transform.localScale = new Vector3(width, height, 1f);
    }

    void FixedUpdate()
    {
        UpdateDrives();
        if (brain == null) return;

        LookForFood();
        float[] inputs = GetInputs();
        float[] outputs = brain.FeedForward(inputs);
        
        Move(outputs[0]);
        if (outputs[1] > 0.5f)
        {
            Jump();
        }

        energy -= Time.fixedDeltaTime; // Constant energy drain
        survivalTime += Time.fixedDeltaTime; // survivalTime is survival time

        if (energy <= 0)
        {
            Die();
        }
    }

    private float[] GetInputs()
    {
        CheckGrounded();
        
        Vector2 foodDir = Vector2.zero;
        if (closestFood != null)
        {
            foodDir = (closestFood.position - transform.position).normalized;
        }

        return new float[]
        {
            foodDir.x,
            foodDir.y,
            rb.linearVelocity.x / 10f, // Normalize velocity
            rb.linearVelocity.y / 10f, // Normalize velocity
            isGrounded ? 1f : 0f
        };
    }
    
private void UpdateDrives()
    {
        // Update energy bar
        if (energyBar != null)
        {
            energyBar.UpdateBar(energy, 100);
        }
        // reproductionBar.UpdateBar(reproductionDrive, maxDriveValue);
            energy -= 0.5f * Time.fixedDeltaTime; // Small energy decay over time
    }


    private void CheckGrounded()
    {
        // TODO: Adjust raycast distance based on creature's height
        float raycastDistance = height / 2 + 0.1f; // Add a small buffer
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, raycastDistance, groundLayer);
    }
private void Jump()
    {
        if (isGrounded)
        {
            rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
            energy -= 0.5f; // Jumping costs energy
        }
    }

     void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Food"))
        {
            Eat(collision.transform);
        }
    }
    


    private void Move(float horizontal)
    {
        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
        energy -= Mathf.Abs(horizontal) * 0.01f; // Moving costs energy
    }

    private void LookForFood()
    {
        // Simple check to avoid checking every frame if we already have a target
        if (closestFood != null && Vector2.Distance(transform.position, closestFood.position) < detectionRadius) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, foodLayer);
        float minDistance = Mathf.Infinity;
        Transform foundFood = null;

        foreach (var hit in hits)
        {
            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                foundFood = hit.transform;
            }
        }
        closestFood = foundFood;
    }

    bool LookForMate()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Creature") && hit.gameObject != gameObject)
            {
                Creature potentialMate = hit.GetComponent<Creature>();
                Debug.Log("Potential mate detected: " + hit.name);
                // TODO: Later Check if the other creature is willing to mate
                if (potentialMate != null)
                {
                    target = potentialMate.transform;
                    return true;
                }
            }
        }
        return false;
    }

    void Eat(Transform food)
    {
        if (food != null)
        {
            energy += 20f; // Gain energy from food
            Destroy(food.gameObject);
        }
    }

    private void Reproduce(Creature mate) // for later use
    {
        Vector2 spawnPosition = new Vector2(transform.position.x, transform.position.y + height / 2 + 0.5f);
        GameObject offspringObj = Instantiate(creaturePrefab, spawnPosition, Quaternion.identity);
        Creature child = offspringObj.GetComponent<Creature>();

        // NOW you can add inheritance
        child.moveSpeed = (this.moveSpeed + mate.moveSpeed) / 2f;
        child.jumpForce = (this.jumpForce + mate.jumpForce) / 2f;
    }

    void Die()
    {
        // You can add animations or effects here later
        Destroy(gameObject);
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}


