using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Creature : MonoBehaviour
{
    public NeuralNetwork brain;
    private Rigidbody2D rb;
    
    public float energy = 100f;
    public float fitness = 0f;
    private bool isInWater;
    private float lastDistanceToFood = float.MaxValue;
    
    private float moveForce = 10f;
    public float underwaterGravityScale = 0f;
    public float underwaterDrag = 3f;
    private float originalGravityScale;
    private float originalDrag;

    [Header("AI Sensing")]
    public float whiskerLength = 5f;
    public float foodDetectionRadius = 10f;
    public float predatorDetectionRadius = 8f;
    public LayerMask groundLayer;
    public LayerMask predatorLayer;
    public LayerMask foodLayer; // This now defines what the creature eats
    private float[] whiskerDebugDistances = new float[5];

    public void Init(NeuralNetwork brain)
    {
        this.brain = brain;
        this.fitness = 0f;
        this.lastDistanceToFood = float.MaxValue;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

    private float[] GatherInputs()
    {
        // 5 ground whiskers + 2 food dir + 2 predator dir + 2 velocity = 11 inputs
        float[] inputs = new float[11];
        Vector2 forward = transform.right;
        float[] whiskerAngles = { -90f, -45f, 0f, 45f, 90f };

        // 1. Ground Detection (5 inputs)
        for (int i = 0; i < whiskerAngles.Length; i++)
        {
            Vector2 direction = Quaternion.Euler(0, 0, whiskerAngles[i]) * forward;
            RaycastHit2D groundHit = Physics2D.Raycast(transform.position, direction, whiskerLength, groundLayer);
            inputs[i] = (groundHit.collider != null) ? (1f - (groundHit.distance / whiskerLength)) : 0f;
            whiskerDebugDistances[i] = (groundHit.collider != null) ? groundHit.distance : whiskerLength;
        }

        // 2. Food Direction (2 inputs)
        Transform closestFood = FindClosest(foodLayer, foodDetectionRadius);
        Vector2 foodDirection = Vector2.zero;
        if (closestFood != null)
        {
            foodDirection = (closestFood.position - transform.position).normalized;
        }
        inputs[5] = foodDirection.x;
        inputs[6] = foodDirection.y;
        
        // 3. Predator Direction (2 inputs)
        Transform closestPredator = FindClosest(predatorLayer, predatorDetectionRadius);
        Vector2 predatorDirection = Vector2.zero;
        if (closestPredator != null)
        {
            predatorDirection = (closestPredator.position - transform.position).normalized;
        }
        inputs[7] = predatorDirection.x;
        inputs[8] = predatorDirection.y;

        // 4. Self Velocity (2 inputs)
        inputs[9] = rb.linearVelocity.x;
        inputs[10] = rb.linearVelocity.y;

        return inputs;
    }

    private void Act(float horizontalThrust, float verticalThrust)
    {
        if (isInWater)
        {
            Vector2 force = new Vector2(horizontalThrust, verticalThrust) * moveForce;
            rb.AddForce(force);
        }

        if (rb.linearVelocity.sqrMagnitude > 0.01f) 
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Creature collided with: " + other.name);
        if (other.CompareTag("Water"))
        {
            isInWater = true;
            rb.gravityScale = underwaterGravityScale;
            rb.linearDamping = underwaterDrag;
            return;
        }

        // Check if the collided object's layer is in our 'foodLayer' mask
        if ((foodLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            // Try to eat it as a Plant
            Plant plant = other.GetComponent<Plant>();
            if (plant != null)
            {
                Eat(plant);
                return;
            }
            
            Debug.Log("Found a food item: " + other.name);
            // Try to eat it as a Creature
            Creature creature = other.GetComponentInParent<Creature>();
            if (creature != null)
            {
                Debug.Log("Creature detected.");
                creature.Die();
                this.energy += 50f;
                this.fitness += 50f;
                return;
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

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector2 forward = transform.right;
        float[] whiskerAngles = { -90f, -45f, 0f, 45f, 90f };
        for (int i = 0; i < whiskerAngles.Length; i++)
        {
            Vector2 direction = Quaternion.Euler(0, 0, whiskerAngles[i]) * forward;
            float dist = whiskerDebugDistances[i];
            Gizmos.color = dist < whiskerLength ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * dist);
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, foodDetectionRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, predatorDetectionRadius);
    }

    private Transform FindClosest(LayerMask layer, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, layer);
        Transform closest = null;
        float minDistance = Mathf.Infinity;
        foreach (var hit in hits)
        {
            //debug log for if pray is creature
            if (hit.CompareTag("Creature")) Debug.Log("Found a creature in the layer: " + hit.name);
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

    private void Eat(Plant plant)
    {
        float energyGained = plant.BeEaten();
        energy += energyGained;
        if (energy > 200f) energy = 200f;
        fitness += 30f;
    }

    public void Die()
    {
        gameObject.SetActive(false);
    }
}