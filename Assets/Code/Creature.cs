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
    // --- Hunger ---
    public float hungerDrive = 0f; //how hungry the creature is
    private float hungerIncreaseRate = 10f;
    // --- Laziness ---
    public float laziness = 10f;
    // ---- Reproduction ----
    public float reproductionDrive = 50f;
    // ---- Explore ----
    public float explorationDrive = 20f; // How much the creature wants to explore

    // Other traits
    public float flexibility = 1f; // How not stubborn the creature is, i.e, how fast it changes its mind, i.e. time between action choice
    public float lastActionTime = 0f; // When was the last time the creature did something
    public float height = 1f;
    public float width = 1f;
    public float energy = 100f; // How much energy the creature has
    // --- Senses ---
    private float detectionRadius = 5f;
    private Transform target;

    // --- Movement & Physics ---
    [Header("Movement")]
    public float jumpForce = 5f;
    public LayerMask groundLayer;
    private bool isGrounded;

    //UI
    public StatusBar hungerBar;
    public StatusBar reproductionBar;




    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        transform.localScale = new Vector3(width, height, 1f);
    }

    void FixedUpdate()
    {
        CheckGrounded();
        hungerDrive += hungerIncreaseRate * Time.fixedDeltaTime;
        if (hungerDrive > 100) hungerDrive = 100f; // Cap hungerDrive at 100
        hungerBar.UpdateBar(hungerDrive, maxDriveValue);
        reproductionBar.UpdateBar(reproductionDrive, maxDriveValue);
        reproductionDrive += 1f * Time.fixedDeltaTime; // Slowly increase reproduction drive


        if (Time.time - lastActionTime > flexibility)  //check if enough time has passed since the last action
        {
            DecideNextAction();
            lastActionTime = Time.time;
        }

        MoveTowardsTarget();
        if (energy <= 0)
        {
            Die();
        }
    }
    private void CheckGrounded()
    {
        // TODO: Adjust raycast distance based on creature's height
        float raycastDistance = height / 2 + 0.1f; // Add a small buffer
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, raycastDistance, groundLayer);
    }
    public void Jump()
    {
        Debug.Log("try Jumping");
        if (isGrounded)
        {
            Debug.Log("Jumping");
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    void DecideNextAction()
    {
        energy -= 0.1f; // Consuming energy for each decision made
        //sort the drives using an array of tuples
        var drives = new (string name, float value)[]
        {
                ("Hunger", hungerDrive),
                ("Laziness", laziness),
                ("Reproduction", reproductionDrive),
                ("Exploration", explorationDrive)
        };
        Array.Sort(drives, (a, b) => b.value.CompareTo(a.value)); // Sort descending

        bool actionTaken = false;
        foreach (var drive in drives)
        {
            switch (drive.name)
            {
                case "Reproduction":
                    // Try to find a mate. If successful, we're done deciding.
                    if (LookForMate())
                    {
                        actionTaken = true;
                    }
                    break;
                case "Hunger":
                    // If we're not busy with a mate, try to find food.
                    if (LookForFood())
                    {
                        actionTaken = true;
                    }
                    break;
                case "Exploration":
                    // If we're not busy with food or a mate, explore.
                    actionTaken = true;
                    break;
            }

            if (actionTaken)
            {
                break;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == target?.gameObject)
        {
            target = null; //we have reached our target
            if (collision.gameObject.CompareTag("Food"))
            {
                Eat(collision.transform);
            }
            else if (collision.gameObject.CompareTag("Creature"))
            {
                Sex(collision.gameObject.GetComponent<Creature>());
            }
        }
    }


    private void MoveTowardsTarget()
    {
        if (target == null)
        {
            //explore if no target
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            float direction = Mathf.Sign(target.position.x - transform.position.x);

            RaycastHit2D wallHit = Physics2D.Raycast(transform.position, new Vector2(moveDirection, 0), width / 2 + 0.1f, groundLayer);
            if (wallHit.collider != null)
            {
                Debug.Log("Wall detected");
                Jump(); // There's a wall, try to jump over it
            }
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
    }

    bool LookForFood()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        Transform closestFood = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Food"))
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestFood = hit.transform;
                }
            }
        }

        if (closestFood != null)
        {
            target = closestFood;
            return true; // Success! Found food.
        }
        return false;
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
            hungerDrive -= 20f;
            if (hungerDrive < 0) hungerDrive = 0;
            Destroy(food.gameObject);
        }
    }

    private void Sex(Creature mate)
    {
        if (mate != null && reproductionDrive >= 40f && mate.reproductionDrive >= 40f)
        {
            Debug.Log("Mating with " + mate.name);
            this.energy -= 20f; // Consuming energy for mating
                                //mate.energy -= 20f; we shouldnt do this as we want the creature to manage its own energy

            reproductionDrive = 0f; // Decrease reproduction drive after mating
            mate.reproductionDrive = 0f;
            Reproduce(mate); //TODO: We should implement a more complex reproduction system later like pregancy
        }
    }

    private void Reproduce(Creature mate)
    {
        // Instantiate from the PREFAB, not the current object
        //calculate spawn position as ontop of parent
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


