// using UnityEngine;
// using System;

// [RequireComponent(typeof(Rigidbody2D))]
// public class Creature : MonoBehaviour
// {
//     public static event Action<Creature> OnCreatureBorn;

//     [Header("Identity")]
//     public string speciesName { get; private set; }
//     public NeuralNetwork brain;

//     [Header("State")]
//     public float energy = 100f;
//     public float fitness = 0f;

//     [Header("Reproduction")]
//     public float energyToReproduce = 150f;
//     public float reproductionEnergyCost = 60f;

//     [Header("Physics")]
//     private Rigidbody2D rb;
//     private bool isInWater;
//     private float moveForce = 10f;
//     public float underwaterGravityScale = 0f;
//     public float underwaterDrag = 3f;
//     private float originalGravityScale;
//     private float originalDrag;

//     [Header("AI Sensing")]
//     public float whiskerLength = 5f;
//     public float foodDetectionRadius = 10f;
//     public float predatorDetectionRadius = 8f;
//     public LayerMask groundLayer;
//     public LayerMask predatorLayer;
//     public LayerMask foodLayer;
//     private float lastDistanceToFood = float.MaxValue;
//     private float[] whiskerDebugDistances = new float[5];

//     public void Init(NeuralNetwork brain, string speciesName)
//     {
//         this.speciesName = speciesName;
//         this.brain = brain;
//         this.energy = 100f;
//         this.fitness = 0f;
//         this.lastDistanceToFood = float.MaxValue;
//     }

//     void Awake()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         originalGravityScale = rb.gravityScale;
//         originalDrag = rb.linearDamping;
//     }

//     void FixedUpdate()
//     {
//         if (brain == null) return;
        
//         float[] inputs = GatherInputs();
//         float[] outputs = brain.FeedForward(inputs);
//         Act(outputs[0], outputs[1]);

//         UpdateFitness();
        
//         energy -= (0.5f + rb.linearVelocity.magnitude * 0.1f) * Time.fixedDeltaTime;

//         if (energy >= energyToReproduce)
//         {
//             Reproduce();
//         }
//         if (energy <= 0)
//         {
//             Die();
//         }
//     }

//     private void Reproduce()
//     {
//         energy -= reproductionEnergyCost;
//         OnCreatureBorn?.Invoke(this); 
//     }

//     private float[] GatherInputs()
//     {
//         float[] inputs = new float[11];
//         Vector2 forward = transform.right;
//         float[] whiskerAngles = { -90f, -45f, 0f, 45f, 90f };

//         for (int i = 0; i < whiskerAngles.Length; i++)
//         {
//             Vector2 direction = Quaternion.Euler(0, 0, whiskerAngles[i]) * forward;
//             RaycastHit2D groundHit = Physics2D.Raycast(transform.position, direction, whiskerLength, groundLayer);
//             inputs[i] = (groundHit.collider != null) ? (1f - (groundHit.distance / whiskerLength)) : 0f;
//             whiskerDebugDistances[i] = (groundHit.collider != null) ? groundHit.distance : whiskerLength;
//         }

//         Transform closestFood = FindClosest(foodLayer, foodDetectionRadius);
//         Vector2 foodDirection = Vector2.zero;
//         if (closestFood != null)
//         {
//             foodDirection = (closestFood.position - transform.position).normalized;
//         }
//         inputs[5] = foodDirection.x;
//         inputs[6] = foodDirection.y;
        
//         Transform closestPredator = FindClosest(predatorLayer, predatorDetectionRadius);
//         Vector2 predatorDirection = Vector2.zero;
//         if (closestPredator != null)
//         {
//             predatorDirection = (closestPredator.position - transform.position).normalized;
//         }
//         inputs[7] = predatorDirection.x;
//         inputs[8] = predatorDirection.y;

//         inputs[9] = rb.linearVelocity.x;
//         inputs[10] = rb.linearVelocity.y;

//         return inputs;
//     }

//     private void Act(float horizontalThrust, float verticalThrust)
//     {
//         if (isInWater)
//         {
//             Vector2 force = new Vector2(horizontalThrust, verticalThrust) * moveForce;
//             rb.AddForce(force);
//         }

//         if (rb.linearVelocity.sqrMagnitude > 0.01f) 
//         {
//             float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
//             transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
//         }
//     }

//     void OnTriggerEnter2D(Collider2D other)
//     {
//         if (other.CompareTag("Water"))
//         {
//             isInWater = true;
//             rb.gravityScale = underwaterGravityScale;
//             rb.linearDamping = underwaterDrag;
//             return;
//         }

//         if ((foodLayer.value & (1 << other.gameObject.layer)) != 0)
//         {
//             Plant plant = other.GetComponent<Plant>();
//             if (plant != null)
//             {
//                 Eat(plant);
//                 return;
//             }
            
//             Creature creature = other.GetComponentInParent<Creature>();
//             if (creature != null && creature != this)
//             {
//                 creature.Die();
//                 this.energy += 50f;
//                 this.fitness += 50f;
//                 return;
//             }
//         }
//     }
    
//     void OnTriggerExit2D(Collider2D other)
//     {
//         if (other.CompareTag("Water"))
//         {
//             isInWater = false;
//             rb.gravityScale = originalGravityScale;
//             rb.linearDamping = originalDrag;
//         }
//     }

//     void OnDrawGizmosSelected()
//     {
//         if (!Application.isPlaying) return;

//         Vector2 forward = transform.right;
//         float[] whiskerAngles = { -90f, -45f, 0f, 45f, 90f };
//         for (int i = 0; i < whiskerAngles.Length; i++)
//         {
//             Vector2 direction = Quaternion.Euler(0, 0, whiskerAngles[i]) * forward;
//             float dist = whiskerDebugDistances[i];
//             Gizmos.color = dist < whiskerLength ? Color.red : Color.green;
//             Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * dist);
//         }
        
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(transform.position, foodDetectionRadius);
//         Gizmos.color = Color.magenta;
//         Gizmos.DrawWireSphere(transform.position, predatorDetectionRadius);
//     }

//     private Transform FindClosest(LayerMask layer, float radius)
//     {
//         Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, layer);
//         Transform closest = null;
//         float minDistance = Mathf.Infinity;
//         foreach (var hit in hits)
//         {
//             float distance = Vector2.Distance(transform.position, hit.transform.position);
//             if (distance < minDistance)
//             {
//                 minDistance = distance;
//                 closest = hit.transform;
//             }
//         }
//         return closest;
//     }

//     private void UpdateFitness()
//     {
//         Transform closestFood = FindClosest(foodLayer, foodDetectionRadius);
//         if (closestFood != null)
//         {
//             float currentDistance = Vector2.Distance(transform.position, closestFood.position);
//             if (currentDistance < lastDistanceToFood)
//             {
//                 fitness += 0.1f;
//             }
//             lastDistanceToFood = currentDistance;
//         }
//     }

//     private void Eat(Plant plant)
//     {
//         float energyGained = plant.BeEaten();
//         energy += energyGained;
//         if (energy > 200f) energy = 200f;
//         fitness += 30f;
//     }

//     public void Die()
//     {
//         Destroy(gameObject);
//     }
// }