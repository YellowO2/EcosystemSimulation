using UnityEngine;

public class JumpingCreature : Creature
{
    [Header("Jumper Settings")]
    public float moveForce = 5f;
    public float jumpForce = 5f;

    private float lastDistanceToFood = float.MaxValue;

    protected override void PerformAction(float[] outputs)
    {
        // Output 0: Horizontal movement
        float moveIntent = outputs[0];
        rb.AddForce(new Vector2(moveIntent * moveForce, 0f));

        // Output 1: Jump Intent
        float jumpIntent = outputs[1];
        if (jumpIntent > 0.1f && isGrounded)
        {
            float variableJumpForce = jumpForce * jumpIntent;
            rb.AddForce(new Vector2(0f, variableJumpForce), ForceMode2D.Impulse);
        }
    }
    protected override void UpdateFitnessAndEnergy()
    {
        base.UpdateFitnessAndEnergy();

        //temporary fitness shapping. Comment out when not needed.
        Transform closestFood = FindClosest(foodLayer, foodDetectionRadius);
        if (closestFood != null)
        {
            float currentDistance = Vector2.Distance(transform.position, closestFood.position);

            // If we have moved closer since the last check, give a small fitness reward
            if (currentDistance < lastDistanceToFood)
            {
                fitness += 0.01f;
            }
            lastDistanceToFood = currentDistance;
        }
        else
        {
            // If there's no food in sight, reset the distance
            lastDistanceToFood = float.MaxValue;
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
                fitness += 30f; // Large reward for eating food
            }
        }
    }
}