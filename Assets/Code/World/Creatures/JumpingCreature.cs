// JumpingCreature.cs
using System;
using UnityEngine;
using UnityEngine.Rendering;

public class JumpingCreature : Creature
{
    [Header("Jumper Settings")]
    public float moveForce = 5f;
    public float jumpForce = 5f;
    public LayerMask groundLayer;
    public LayerMask foodLayer;
    public float detectionRadius = 10f;

    private bool isGrounded;
    private Vector3 initialPosition;
    private float progressBonus = 0f;

    protected override float[] GatherInputs()
    {
        float[] groundWhiskerInputs = SenseWithWhiskers(5, 5, 36, groundLayer);

        Transform closestFood = FindClosest(foodLayer, detectionRadius);
        Vector2 foodDir = Vector2.zero;
        if (closestFood != null)
        {
            foodDir = (closestFood.position - transform.position).normalized;
        }

        return new float[]
        {
        groundWhiskerInputs[0],
        groundWhiskerInputs[1],
        groundWhiskerInputs[2],
        groundWhiskerInputs[3],
        groundWhiskerInputs[4],
        foodDir.x,
        foodDir.y,
        rb.linearVelocity.x / 10f,
        rb.linearVelocity.y / 10f
        };
    }

    private void Init()
    {
        this.Init();
        initialPosition = transform.position;
    }

    protected override void UpdateFitnessAndEnergy()
    {
        // only update progress if max progress
        progressBonus = Mathf.Max(transform.position.x - initialPosition.x, progressBonus);

        fitness = progressBonus;
    }

    protected override void PerformAction(float[] outputs)
    {
        CheckGrounded();
        // Output 0: Horizontal movement
        float moveInput = outputs[0];
        rb.AddForce(new Vector2(moveInput * moveForce, 0f));

        // Output 1: Jump signal
        if (outputs[1] > 0.5f && isGrounded)
        {
            rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
            Debug.Log("Jumping!");
        }
    }

    public override void Init(NeuralNetwork brain, string speciesName)
    {
        base.Init(brain, speciesName);
        initialPosition = transform.position;
    }
    private void CheckGrounded()
    {
        float raycastDistance = transform.localScale.y / 2 + 0.1f;
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, raycastDistance, groundLayer);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object we overlapped is on the food layer
        if ((foodLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            Plant plant = other.GetComponent<Plant>();
            if (plant != null)
            {
                Debug.Log("Creature ate food!");

                float energyGained = plant.BeEaten();
                energy += energyGained;
                fitness += 100; // Increase fitness based on energy gained
            }
        }
    }
}