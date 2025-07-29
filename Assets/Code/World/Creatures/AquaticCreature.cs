using UnityEngine;
using System.Linq;

public class AquaticCreature : Creature
{
    [Header("Aquatic Settings")]
    public float moveForce = 10f;
    public float underwaterGravityScale = 0f;
    public float underwaterDrag = 3f;
    public LayerMask groundLayer;
    public LayerMask predatorLayer;
    public LayerMask foodLayer;
    public int whiskerLength = 5;
    public float foodDetectionRadius = 10f;
    public float predatorDetectionRadius = 8f;

    private bool isInWater;
    private float originalGravityScale;
    private float originalDrag;
    private float lastDistanceToFood = float.MaxValue;

    protected override void Awake()
    {
        base.Awake();
        originalGravityScale = rb.gravityScale;
        originalDrag = rb.linearDamping;
    }

    protected override float[] GatherInputs()
    {
        float[] wallInputs = SenseWithWhiskers(5, whiskerLength, 90f, groundLayer);
        
        Transform closestFood = FindClosest(foodLayer, foodDetectionRadius);
        Vector2 foodDirection = closestFood ? (closestFood.position - transform.position).normalized : Vector2.zero;

        Transform closestPredator = FindClosest(predatorLayer, predatorDetectionRadius);
        Vector2 predatorDirection = closestPredator ? (closestPredator.position - transform.position).normalized : Vector2.zero;

        return wallInputs.Concat(new float[] {
            foodDirection.x,
            foodDirection.y,
            predatorDirection.x,
            predatorDirection.y,
            rb.linearVelocity.x / 10f,
            rb.linearVelocity.y / 10f
        }).ToArray();
    }

    protected override void PerformAction(float[] outputs)
    {
        if (isInWater)
        {
            Vector2 force = new Vector2(outputs[0], outputs[1]) * moveForce;
            rb.AddForce(force);
        }

        if (rb.linearVelocity.sqrMagnitude > 0.01f) 
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    protected override void UpdateFitnessAndEnergy()
    {
        energy -= (0.5f + rb.linearVelocity.magnitude * 0.1f) * Time.fixedDeltaTime;

        Transform closestFood = FindClosest(foodLayer, foodDetectionRadius);
        if (closestFood != null)
        {
            float currentDistance = Vector2.Distance(transform.position, closestFood.position);
            if (currentDistance < lastDistanceToFood)
            {
                fitness += 0.1f;
            }
            lastDistanceToFood = currentDistance;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            isInWater = true;
            rb.gravityScale = underwaterGravityScale;
            rb.linearDamping = underwaterDrag;
            return;
        }

        if ((foodLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            Plant plant = other.GetComponent<Plant>();
            if (plant != null)
            {
                float energyGained = plant.BeEaten();
                energy += energyGained;
                fitness += 30f;
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
}