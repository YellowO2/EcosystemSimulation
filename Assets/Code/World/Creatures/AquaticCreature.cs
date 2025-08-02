using UnityEngine;
using System.Linq;

public class AquaticCreature : Creature
{
    [Header("Aquatic Settings")]
    public float moveForce = 10f;
    private float lastDistanceToFood = float.MaxValue;
    private float lastDistanceToPredator = 0f;

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
        base.UpdateFitnessAndEnergy();

        Transform closestFood = FindClosest(foodLayer, foodDetectionRadius);
        if (closestFood != null)
        {
            float currentDistance = Vector2.Distance(transform.position, closestFood.position);
            if (currentDistance < lastDistanceToFood)
            {
                fitness += 0.01f;
            }
            lastDistanceToFood = currentDistance;
        }
        else
        {
            lastDistanceToFood = float.MaxValue;
        }

        Transform closestPredator = FindClosest(predatorLayer, predatorDetectionRadius);
        if (closestPredator != null)
        {
            float currentDistance = Vector2.Distance(transform.position, closestPredator.position);
            if (currentDistance > lastDistanceToPredator)
            {
                fitness += 0.02f; // Reward for increasing distance from predator
            }
            lastDistanceToPredator = currentDistance;
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);

        if ((foodLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            Plant plant = other.GetComponent<Plant>();
            if (plant != null)
            {
                float energyGained = plant.BeEaten();
                energy += energyGained;
                fitness += 30f;
            }
        }
    }
}